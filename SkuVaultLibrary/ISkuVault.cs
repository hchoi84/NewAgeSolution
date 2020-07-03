using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace SkuVaultLibrary
{
  public interface ISkuVault
  {
    Task<JObject> GetDataAsync(string reqUri, StringContent content);
    Task<List<JToken>> GetInventoryByLocationAsync(int pageNumber, int pageSize);
    Dictionary<string, int> GetStoreQty(List<JToken> jTokens);
  }
}
