using ChannelAdvisorLibrary.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ChannelAdvisorLibrary
{
  public class ChannelAdvisor : IChannelAdvisor
  {
    #region strings
    private readonly string _accessToken = "access_token";
    private readonly string _expiresIn = "expires_in";
    private readonly string _appForm = "application/x-www-form-urlencoded";
    private readonly string _appJson = "application/json";
    private readonly string _odataNextLink = "@odata.nextLink";
    private readonly string _value = "value";
    private readonly string _profileId = "ProfileID";
    private readonly string _dcQuantities = "DCQuantities";
    private readonly string _distributionCenterID = "DistributionCenterID";
    private readonly string _sku = "Sku";
    private readonly string _lastSaleDateUtc = "LastSaleDateUtc";
    private readonly string _availableQuantity = "AvailableQuantity";
    private readonly string _attributes = "Attributes";
    private readonly string _name = "Name";
    private readonly string _allName = "All Name";
    private readonly string _Value = "Value";
    private readonly string _itemName = "Item Name";
    private readonly string _labels = "Labels";
    #endregion

    private readonly List<string> _labelNames = new List<string>() { "Closeout", "Discount", "MAPNoShow", "MAPShow", "NPIP" };

    public void EstablishConnection()
    {
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
            { HttpRequestHeader.ContentType.ToString(), _appForm },
          },
          Content = new StringContent($"grant_type=refresh_token&refresh_token={ Secrets.RefreshToken }", Encoding.UTF8, _appJson),
        };

        HttpClient client = new HttpClient();
        HttpResponseMessage response = client.SendAsync(request).Result;
        HttpContent content = response.Content;
        string json = content.ReadAsStringAsync().Result;
        JObject result = JObject.Parse(json);
        Secrets.AccessToken = result[_accessToken].ToString();
        Secrets.TokenExpireDateTime = DateTime.Now.AddSeconds(Convert.ToDouble(result[_expiresIn]) - Secrets.TokenExpireBuffer);
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

        reqUri = (string)jObject[_odataNextLink];

        foreach (JObject item in jObject[_value]) jObjects.Add(item);
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
        List<JObject> filteredByProfileId = jObjects.Where(j => j[_profileId].ToObject<int>() == profileId).ToList();

        foreach (var item in filteredByProfileId)
        {
          var fbaQty = item[_dcQuantities].FirstOrDefault(i => i[_distributionCenterID].ToObject<int>() == -4);
          NoSalesReportModel p = new NoSalesReportModel();

          if (profileId == Secrets.OtherProfileId)
          {
            p = model.FirstOrDefault(m => m.Sku == item[_sku].ToObject<string>());

            if (p != null)
            {
              DateTime? lsd = item[_lastSaleDateUtc].ToObject<DateTime?>();
              p.LastSaleDateUtc = p.LastSaleDateUtc > lsd ? lsd : p.LastSaleDateUtc;
              p.FBA += fbaQty != null ? fbaQty[_availableQuantity].ToObject<int>() : 0;
              continue;
            }
          }

          p = item.ToObject<NoSalesReportModel>();

          string allName = item[_attributes].FirstOrDefault(i => i[_name].ToObject<string>() == _allName)[_Value].ToObject<string>();

          string itemName = item[_attributes].FirstOrDefault(i => i[_name].ToObject<string>() == _itemName)[_Value].ToObject<string>();

          p.FBA = fbaQty != null ? fbaQty[_availableQuantity].ToObject<int>() : 0;
          p.ItemName = itemName;
          p.AllName = allName.Replace(itemName, string.Empty);
          p.ProductLabel = item[_labels].FirstOrDefault(i => _labelNames.Contains(i[_name].ToObject<string>()))[_name].ToObject<string>();

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
