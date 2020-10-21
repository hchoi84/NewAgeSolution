using ChannelAdvisorLibrary.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
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

    private readonly List<string> _labelNames = new List<string>
    {
      "Closeout", "Discount", "MAPNoShow", "MAPShow", "NPIP"
    };

    private void EstablishConnection()
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

    public async Task<IEnumerable<JObject>> GetProductsAsync(string filter, string expand, string select)
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

        if (jObject["error"] != null)
        {
          throw new Exception(jObject["error"]["message"].ToString());
        }

        reqUri = (string)jObject[_odataNextLink];

        foreach (JObject item in jObject["value"]) jObjects.Add(item);
      }

      return jObjects;
    }

    // NoSalesReport
    public async Task<IEnumerable<NoSalesReportModel>> GetNoSalesReport(DateTime lastSoldDate)
    {
      string filter = $"LastSaleDateUtc lt {lastSoldDate:yyyy-MM-dd}";
      string expand = "";
      string select = "ParentProductID";
      IEnumerable<string> distinctParentIds = await GetDistinctParentIdsAsync(filter, expand, select);

      IEnumerable<JObject> jObjects = await GetChildrenPerParentIdAsync(distinctParentIds.ToList());

      IEnumerable<NoSalesReportModel> model = ConvertToNoSalesReportModel(jObjects.ToList());

      model = AddParentInfo(model)
        .OrderBy(m => m.Sku)
        .ToList();

      return model;
    }

    private async Task<IEnumerable<string>> GetDistinctParentIdsAsync(string filter, string expand, string select)
    {
      IEnumerable<JObject> jObjects = await GetProductsAsync(filter, expand, select);

      IEnumerable<string> distinctParentIds = jObjects
        .Select(j => j[select].ToString())
        .Distinct()
        .Where(s => !string.IsNullOrWhiteSpace(s));

      return distinctParentIds;
    }

    private async Task<IEnumerable<JObject>> GetChildrenPerParentIdAsync(List<string> distinctParentIds)
    {
      List<JObject> jObjects = new List<JObject>();

      //Since ChannelAdvisorAPI only allows up to 10 filters, request product information for every 10 parent ids
      while (distinctParentIds.Count > 0)
      {
        bool isMoreThan10 = distinctParentIds.Count > 10;
        int x = isMoreThan10 ? 10 : distinctParentIds.Count;

        IEnumerable<string> first10 = distinctParentIds
          .GetRange(0, x)
          .Select(parentId => $"ParentProductId eq { parentId }");

        distinctParentIds.RemoveRange(0, x);

        string filter = string.Join(" or ", first10);
        string expand = "Attributes,Labels,DCQuantities";
        string select = "ProfileId,Sku,UPC,ParentSku,CreateDateUtc,LastSaleDateUtc,TotalAvailableQuantity";

        jObjects.AddRange(await GetProductsAsync(filter, expand, select));
      }

      return jObjects;
    }

    private IEnumerable<NoSalesReportModel> ConvertToNoSalesReportModel(List<JObject> jObjects)
    {
      List<NoSalesReportModel> model = new List<NoSalesReportModel>();

      jObjects = jObjects.OrderBy(j => j[_sku].ToString()).ToList();

      while (jObjects.Count > 0)
      {
        JObject pointer = jObjects[0];
        bool hasNext = pointer.Next != null;

        string sku = pointer[_sku].ToString();
        string upc = pointer["UPC"].ToString();
        string parentSku = pointer["ParentSku"].ToString();
        DateTime createDateUtc = pointer["CreateDateUtc"].ToObject<DateTime>();
        int totalAvailQty = pointer["TotalAvailableQuantity"].ToObject<int>();
        string itemName = pointer[_attributes]
            .FirstOrDefault(i => i[_name].ToObject<string>() == _itemName)[_Value]
            .ToObject<string>();
        string allName = pointer[_attributes]
            .FirstOrDefault(i => i[_name].ToObject<string>() == _allName)[_Value]
            .ToObject<string>()
            .Replace(itemName, string.Empty);
        JToken label = pointer[_labels]
            .FirstOrDefault(i => _labelNames.Contains(i[_name].ToObject<string>()));
        string labelValue = label != null ? label[_name].ToString() : "No Label";
        DateTime? lastSaleDateUtc;
        int fbaQty;
        
        JToken fba = pointer[_dcQuantities]
          .FirstOrDefault(i => i[_distributionCenterID].ToObject<int>() == -4);

        if (hasNext && pointer[_sku].ToString() == pointer.Next[_sku].ToString())
        {
          DateTime? pointerLSDUtc = pointer[_lastSaleDateUtc].ToObject<DateTime?>();
          DateTime? nextLSDUtc = pointer.Next[_lastSaleDateUtc].ToObject<DateTime?>();
          lastSaleDateUtc = pointerLSDUtc > nextLSDUtc ? pointerLSDUtc : nextLSDUtc;

          JToken nextFba = pointer.Next[_dcQuantities]
          .FirstOrDefault(i => i[_distributionCenterID].ToObject<int>() == -4);

          int pointerFbaQty = fba != null ? fba[_availableQuantity].ToObject<int>() : 0;
          int nextFbaQty = nextFba != null ? nextFba[_availableQuantity].ToObject<int>() : 0;
          fbaQty = pointerFbaQty + nextFbaQty;

          jObjects.RemoveRange(0, 2);
        }
        else
        {
          lastSaleDateUtc = pointer["LastSaleDateUtc"].ToObject<DateTime?>();
          fbaQty = fba != null ? fba[_availableQuantity].ToObject<int>() : 0;

          jObjects.RemoveRange(0, 1);
        }

        model.Add(new NoSalesReportModel
        {
          Sku = sku,
          UPC = upc,
          ParentSKU = parentSku,
          CreateDateUtc = createDateUtc,
          TotalAvailableQuantity = totalAvailQty,
          LastSaleDateUtc = lastSaleDateUtc,
          FBA = fbaQty,
          ItemName = itemName,
          AllName = allName,
          ProductLabel = labelValue
        });
      }

      return model;
    }

    private IEnumerable<NoSalesReportModel> AddParentInfo(IEnumerable<NoSalesReportModel> model)
    {
      List<NoSalesReportModel> m = model.ToList();
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

        m.Add(prod);
      }

      return m;
    }

    public List<string> GetAcctNames() => new List<string> { GetMainAcctName(), GetOtherAcctName() };
    public string GetMainAcctName() => Secrets.MainName;
    public string GetOtherAcctName() => Secrets.OtherName;
    public int GetMainProfileId() => Secrets.MainProfileId;
  }
}
