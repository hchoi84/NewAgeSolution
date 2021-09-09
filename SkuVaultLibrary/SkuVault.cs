using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using SkuVaultLibrary.Securities;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SkuVaultLibrary
{
  public class SkuVault : ISkuVault
  {
    #region strings
    private readonly string _appjson = "application/json";
    private readonly string _tenantToken = "TenantToken";
    private readonly string _userToken = "UserToken";
    //private readonly string _locationCode = "LocationCode";
    //private readonly string _store = "STORE";
    //private readonly string _quantity = "Quantity";
    //private readonly string _items = "Items";
    #endregion

    public SkuVault()
    {
      GetTokensAsync().Wait();
    }

    private async Task GetTokensAsync()
    {
      if (string.IsNullOrWhiteSpace(Secrets.TenantToken) ||
        string.IsNullOrWhiteSpace(Secrets.UserToken))
      {
        string reqUri = "https://app.skuvault.com/api/gettokens";

        string body = JsonConvert.SerializeObject(
          new { Secrets.Email, Secrets.Password });

        StringContent content = new StringContent(body, Encoding.UTF8, _appjson);

        JObject tokens = await PostDataAsync(reqUri, content);

        Secrets.TenantToken = tokens[_tenantToken].ToString();
        Secrets.UserToken = tokens[_userToken].ToString();
      }
    }

    private async Task<JObject> PostDataAsync(string reqUri, StringContent content)
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

    public async Task UpdateDropShip(Dictionary<string, int> skuAndNewQty)
    {
      List<object> items = new List<object>();
      int count = 0;

      foreach (var item in skuAndNewQty)
      {
        items.Add(new
        {
          Sku = item.Key,
          LocationCode = "DROPSHIP",
          Quantity = item.Value,
          WarehouseId = 4081
        });

        count++;

        if (items.Count == 100 || count == skuAndNewQty.Count)
        {
          string reqUri = "https://app.skuvault.com/api/inventory/setItemQuantities";

          string body = JsonConvert.SerializeObject(new
          {
            Items = items,
            Secrets.TenantToken,
            Secrets.UserToken
          });

          StringContent content = new StringContent(body, Encoding.UTF8, _appjson);

          await PostDataAsync(reqUri, content);

          items.Clear();
        }
      }
    }
  }
}
