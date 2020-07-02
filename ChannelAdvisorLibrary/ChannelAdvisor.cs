using ChannelAdvisorLibrary.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ChannelAdvisorLibrary
{
  public class ChannelAdvisor : IChannelAdvisor
  {
    public void EstablishConnection()
    {
      string accessToken = "access_token";
      string expiresIn = "expires_in";

      if (Secrets.TokenExpireDateTime < DateTime.Now || Secrets.TokenExpireDateTime == null)
      {
        string auth = string.Concat(Secrets.ApplicationId, ":", Secrets.SharedSecret);
        byte[] authBytes = Encoding.ASCII.GetBytes(auth);
        string encodedAuth = Convert.ToBase64String(authBytes);
        string authorization = string.Concat("Basic ", encodedAuth);

        HttpRequestMessage request = new HttpRequestMessage
        {
          RequestUri = new Uri(Secrets.TokenUrl),
          Method = HttpMethod.Post,
          Headers = {
            { HttpRequestHeader.Authorization.ToString(), authorization },
            { HttpRequestHeader.ContentType.ToString(), "application/x-www-form-urlencoded" },
          },
          Content = new StringContent($"grant_type=refresh_token&refresh_token={ Secrets.RefreshToken }", Encoding.UTF8, "application/json"),
        };

        HttpClient client = new HttpClient();
        HttpResponseMessage response = client.SendAsync(request).Result;
        HttpContent content = response.Content;
        string json = content.ReadAsStringAsync().Result;
        JObject result = JObject.Parse(json);
        Secrets.AccessToken = result[accessToken].ToString();
        Secrets.TokenExpireDateTime = DateTime.Now.AddSeconds(Convert.ToDouble(result[expiresIn]) - Secrets.TokenExpireBuffer);
      }
    }

    public async Task<List<JObject>> GetProductsAsync(string filter, string expand, string select)
    {
      EstablishConnection();

      string reqUri = $"https://api.channeladvisor.com/v1/Products?access_token={ Secrets.AccessToken }";

      if (!string.IsNullOrWhiteSpace(filter)) reqUri += $"&$filter={ filter }";
      if (!string.IsNullOrWhiteSpace(expand)) reqUri += $"&$expand={ expand }";
      if (!string.IsNullOrWhiteSpace(select)) reqUri += $"&$select={ select }";

      List<JObject> jObjects = new List<JObject>();

      while (reqUri != null)
      {
        string result;

        using (HttpClient client = new HttpClient())
        {
          HttpResponseMessage response = await client.GetAsync(reqUri);
          HttpContent content = response.Content;
          result = await content.ReadAsStringAsync();
        }
        JObject jObject = JObject.Parse(result);

        reqUri = (string)jObject["@odata.nextLink"];

        foreach (JObject item in jObject["value"]) jObjects.Add(item);
      }

      return jObjects;
    }

    public async Task<List<string>> GetDistinctParentIdsAsync(string filter, string expand, string select)
    {
      //Get products via ChannelAdvisorAPI
      List<JObject> jObjects = await GetProductsAsync(filter, expand, select);

      //Get distinct parent ids
      List<string> distinctParentIds = jObjects
        .Where(j => !string.IsNullOrWhiteSpace(j[select].ToObject<string>()))
        .Select(j => j[select].ToObject<string>())
        .Distinct()
        .ToList();

      return distinctParentIds;
    }

    public async Task<List<JObject>> GetChildrenPerParentIdAsync(List<string> distinctParentIds)
    {
      List<JObject> jObjects = new List<JObject>();

      //Since ChannelAdvisorAPI only allows up to 10 filters, we'll request product information for every 10 parent ids
      while (distinctParentIds.Count > 0)
      {
        bool isMoreThan10 = distinctParentIds.Count > 10;
        int x = isMoreThan10 ? 10 : distinctParentIds.Count;

        List<string> first10 = distinctParentIds.GetRange(0, x).Select(parentId => $"ParentProductId eq { parentId }").ToList();

        distinctParentIds.RemoveRange(0, x);

        string filter = string.Join(" or ", first10);
        string expand = "Attributes,Labels,DCQuantities";
        string select = "ProfileId,Sku,UPC,ParentSku,CreateDateUtc,LastSaleDateUtc,TotalAvailableQuantity";

        jObjects.AddRange(await GetProductsAsync(filter, expand, select));
      }

      return jObjects;
    }

    public List<NoSalesReportModel> ConvertToNoSalesReportModel(List<JObject> jObjects)
    {
      List<int> profileIds = new List<int> { Secrets.MainProfileId, Secrets.OtherProfileId };
      List<NoSalesReportModel> model = new List<NoSalesReportModel>();

      foreach (int profileId in profileIds)
      {
        List<JObject> filteredByProfileId = jObjects.Where(j => j["ProfileID"].ToObject<int>() == profileId).ToList();

        foreach (var item in filteredByProfileId)
        {
          var fbaQty = item["DCQuantities"].FirstOrDefault(i => i["DistributionCenterID"].ToObject<int>() == -4);
          NoSalesReportModel p = new NoSalesReportModel();

          if (profileId == Secrets.OtherProfileId)
          {
            p = model.FirstOrDefault(m => m.Sku == item["Sku"].ToObject<string>());

            if (p != null)
            {
              DateTime? lsd = item["LastSaleDateUtc"].ToObject<DateTime?>();
              p.LastSaleDateUtc = p.LastSaleDateUtc > lsd ? lsd : p.LastSaleDateUtc;
              p.FBA += fbaQty != null ? fbaQty["AvailableQuantity"].ToObject<int>() : 0;
              continue;
            }
          }

          p = item.ToObject<NoSalesReportModel>();

          string allName = item["Attributes"].FirstOrDefault(i => i["Name"].ToObject<string>() == "All Name")["Value"].ToObject<string>();

          string itemName = item["Attributes"].FirstOrDefault(i => i["Name"].ToObject<string>() == "Item Name")["Value"].ToObject<string>();

          List<string> labelNames = new List<string>() { "Closeout", "Discount", "MAPNoShow", "MAPShow", "NPIP" };

          p.FBA = fbaQty != null ? fbaQty["AvailableQuantity"].ToObject<int>() : 0;
          p.ItemName = itemName;
          p.AllName = allName.Replace(itemName, "");
          p.ProductLabel = item["Labels"].FirstOrDefault(i => labelNames.Contains(i["Name"].ToObject<string>()))["Name"].ToObject<string>();

          model.Add(p);
        }
      }

      return model;
    }

    public List<NoSalesReportModel> AddParentInfo(List<NoSalesReportModel> model)
    {
      //Create parent information
      List<IGrouping<string, NoSalesReportModel>> groupedByParentSku = model.GroupBy(m => m.ParentSKU).ToList();

      foreach (var group in groupedByParentSku)
      {
        NoSalesReportModel prod = new NoSalesReportModel
        {
          Sku = group.First().ParentSKU,
          CreateDateUtc = group.First().CreateDateUtc,
          TotalAvailableQuantity = group.Sum(g => g.TotalAvailableQuantity),
          FBA = group.Sum(g => g.FBA),
          AllName = group.First().ItemName,
          ProductLabel = group.First().ProductLabel
        };

        model.Add(prod);
      }

      return model;
    }

    #region Record Keeping
    //private void ConvertToModel(JObject jObject, List<ProductModel> products)
    //{
    //  foreach (var p in (JArray)jObject["value"])
    //  {
    //    ProductModel productModel = p.ToObject<ProductModel>();

    //    if (p["Attributes"] != null)
    //    {
    //      foreach (var attribute in (JArray)p["Attributes"])
    //      {
    //        AttributeModel attributeModel = attribute.ToObject<AttributeModel>();
    //        productModel.Attributes.Add(attributeModel);
    //      }
    //    }

    //    if (p["Labels"] != null)
    //    {
    //      foreach (var label in (JArray)p["Labels"])
    //      {
    //        LabelModel labelModel = label.ToObject<LabelModel>();
    //        productModel.Labels.Add(labelModel);
    //      }
    //    }

    //    if (p["DCQuantities"] != null)
    //    {
    //      foreach (var dcQty in (JArray)p["DCQuantities"])
    //      {
    //        DcQuantityModel dcQuantityModel = dcQty.ToObject<DcQuantityModel>();
    //        productModel.DCQuantities.Add(dcQuantityModel);
    //      } 
    //    }

    //    products.Add(productModel);
    //  }
    //}

    //public List<T> ConvertToListOf<T>(JArray jArray)
    //{
    //  List<T> items = new List<T>();

    //  foreach (var item in jArray)
    //  {
    //    items.Add(item.ToObject<T>());
    //  }

    //  return items;
    //}
    #endregion
  }
}
