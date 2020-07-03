using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using SkuVaultLibrary.Securities;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace SkuVaultLibrary
{
  public class SkuVault : ISkuVault
  {
    #region strings
    private readonly string _appjson = "application/json";
    private readonly string _tenantToken = "TenantToken";
    private readonly string _userToken = "UserToken";
    private readonly string _locationCode = "LocationCode";
    private readonly string _store = "STORE";
    private readonly string _quantity = "Quantity";
    private readonly string _items = "Items";
    #endregion

    private async Task<JObject> GetTokensAsync()
    {
      string reqUri = "https://app.skuvault.com/api/gettokens";

      string body = JsonConvert.SerializeObject(
        new { Secrets.Email, Secrets.Password });

      StringContent content = new StringContent(body, Encoding.UTF8, _appjson);

      JObject tokens = await GetDataAsync(reqUri, content);

      return tokens;
    }

    public async Task<JObject> GetDataAsync(string reqUri, StringContent content)
    {
      string result;

      using (HttpClient client = new HttpClient())
      {
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(_appjson));
        HttpResponseMessage response = await client.PostAsync(reqUri, content);
        HttpContent httpContent = response.Content;
        result = await httpContent.ReadAsStringAsync();
      }

      JObject jObject = JObject.Parse(result);

      return jObject;
    }

    public async Task<List<JToken>> GetInventoryByLocationAsync(int pageNumber, int pageSize)
    {
      string _tenantToken = this._tenantToken;
      string _userToken = this._userToken;
      JObject tokens = await GetTokensAsync();

      string reqUri = "https://app.skuvault.com/api/inventory/getInventoryByLocation";

      string body = JsonConvert.SerializeObject(
          new
          {
            IsReturnByCodes = false,
            PageNumber = pageNumber,
            PageSize = pageSize,
            //ProductSKUs = new[] { "YOA2071_02", "ANN0583_004" },
            TenantToken = tokens[_tenantToken],
            UserToken = tokens[_userToken]
          });

      StringContent content = new StringContent(body, Encoding.UTF8, _appjson);

      JObject result = await GetDataAsync(reqUri, content);

      List<JToken> jTokens = result[_items].Select(i => i).ToList();

      return jTokens;
    }

    public Dictionary<string, int> GetStoreQty(List<JToken> jTokens)
    {
      Dictionary<string, int> skuAndStoreQty = new Dictionary<string, int>();

      foreach (JProperty product in jTokens)
      {
        if (product.Value.Count() == 0 || product.Name.Length <= 7) continue;

        var storeLoc = product.Value.FirstOrDefault(l => l[_locationCode].ToString() == _store);

        if (storeLoc == null) continue;

        int qty = storeLoc[_quantity].Value<int>();

        var name = product.Name;

        skuAndStoreQty.Add(name, qty);
      }

      return skuAndStoreQty;
    }
  }
}
