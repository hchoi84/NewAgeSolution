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

    #region BufferSetter
    //public async Task<Dictionary<string, int>> GetProductsToUpdate(
    //  Dictionary<string, int> activeBufferProducts)
    //{
    //  Dictionary<string, int> productsToUpdate = new Dictionary<string, int>();
    //  int pageNumber = 0;
    //  int pageSize = 10000;
    //  bool hasMoreProducts;

    //  do
    //  {
    //    hasMoreProducts = false;

    //    List<JToken> jTokens = await GetInventoryByLocationAsync(pageNumber, pageSize);
    //    Dictionary<string, int> storeProducts = RetrieveStoreProducts(jTokens);
    //    ProcessUniqueSkuAndQty(storeProducts, activeBufferProducts, productsToUpdate);

    //    if (jTokens.Count >= pageSize)
    //    {
    //      hasMoreProducts = true;
    //      pageNumber++;
    //    }

    //    Thread.Sleep(12000);

    //  } while (hasMoreProducts);

    //  activeBufferProducts
    //    .ToList()
    //    .ForEach(sku => productsToUpdate.Add(sku.Key, 0));

    //  return productsToUpdate;
    //}

    //private async Task<List<JToken>> GetInventoryByLocationAsync(int pageNumber, int pageSize)
    //{
    //  string _tenantToken = this._tenantToken;
    //  string _userToken = this._userToken;

    //  string reqUri = "https://app.skuvault.com/api/inventory/getInventoryByLocation";

    //  string body = JsonConvert.SerializeObject(
    //      new
    //      {
    //        IsReturnByCodes = false,
    //        PageNumber = pageNumber,
    //        PageSize = pageSize,
    //        //ProductSKUs = new[] { "YOA2071_02", "ANN0583_004" },
    //        Secrets.TenantToken,
    //        Secrets.UserToken,
    //      });

    //  StringContent content = new StringContent(body, Encoding.UTF8, _appjson);

    //  JObject result = await PostDataAsync(reqUri, content);

    //  List<JToken> jTokens = result[_items].Select(i => i).ToList();

    //  return jTokens;
    //}

    //private Dictionary<string, int> RetrieveStoreProducts(List<JToken> jTokens)
    //{
    //  Dictionary<string, int> skuAndStoreQty = new Dictionary<string, int>();

    //  foreach (JProperty product in jTokens)
    //  {
    //    if (product.Value.Count() == 0 || product.Name.Length <= 7) continue;

    //    var storeLoc = product.Value.FirstOrDefault(l => l[_locationCode].ToString() == _store);
    //    if (storeLoc == null) continue;

    //    int qty = storeLoc[_quantity].Value<int>();
    //    var name = product.Name;
    //    skuAndStoreQty.Add(name, qty);
    //  }

    //  return skuAndStoreQty;
    //}

    //private void ProcessUniqueSkuAndQty(
    //  Dictionary<string, int> storeProducts,
    //  Dictionary<string, int> activeBufferProducts,
    //  Dictionary<string, int> productsToUpdate)
    //{
    //  foreach (KeyValuePair<string, int> item in storeProducts)
    //  {
    //    if (activeBufferProducts.Contains(item))
    //    {
    //      activeBufferProducts.Remove(item.Key);
    //      continue;
    //    }

    //    if (activeBufferProducts.ContainsKey(item.Key))
    //    {
    //      activeBufferProducts.Remove(item.Key);
    //    }

    //    productsToUpdate.Add(item.Key, item.Value);
    //  }
    //}
    #endregion

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
