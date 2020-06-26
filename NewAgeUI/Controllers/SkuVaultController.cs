using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using SkuVaultLibrary;
using NewAgeUI.Securities;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;

namespace NewAgeUI.Controllers
{
  [AllowAnonymous]
  public class SkuVaultController : Controller
  {
    private readonly string _tenantToken = "TenantToken";
    private readonly string _userToken = "UserToken";

    public async Task<IActionResult> Index()
    {
      SkuVault skuVault = new SkuVault();

      JObject tokens = await GetTokensAsync(skuVault);

      string reqUri = "https://app.skuvault.com/api/inventory/getInventoryByLocation";

      string body = JsonConvert.SerializeObject(
        new
        {
          IsReturnByCodes = false,
          PageNumber = 0,
          PageSize = 1000,
          ProductSKUs = new[] { "ANN0583_004" },
          TenantToken = tokens[_tenantToken],
          UserToken = tokens[_userToken]
        });
      
      StringContent content = new StringContent(body, Encoding.UTF8, "application/json");

      JObject result = await skuVault.GetDataAsync(reqUri, content);

      return Json(result);
    }

    private async Task<JObject> GetTokensAsync(SkuVault skuVault)
    {
      string body;
      string reqUri;
      StringContent content;

      reqUri = "https://app.skuvault.com/api/gettokens";

      body = JsonConvert.SerializeObject(
        new { SkuVaultSecret.Email, SkuVaultSecret.Password });

      content = new StringContent(body, Encoding.UTF8, "application/json");

      JObject tokens = await skuVault.GetDataAsync(reqUri, content);

      return tokens;
    }
  }
}
