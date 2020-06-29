using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using SkuVaultLibrary;
using NewAgeUI.Securities;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using FileReaderLibrary.Models;
using NewAgeUI.ViewModels;
using System.IO;
using Microsoft.VisualBasic.FileIO;
using System.Linq;
using System;
using Org.BouncyCastle.Math.EC.Rfc7748;

namespace NewAgeUI.Controllers
{
  [AllowAnonymous]
  [Route("[Controller]")]
  public class SkuVaultController : Controller
  {
    private readonly string _tenantToken = "TenantToken";
    private readonly string _userToken = "UserToken";

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
      //SkuVault skuVault = new SkuVault();

      //JObject tokens = await GetTokensAsync(skuVault);

      //string reqUri = "https://app.skuvault.com/api/inventory/getInventoryByLocation";

      //string body = JsonConvert.SerializeObject(
      //  new
      //  {
      //    IsReturnByCodes = false,
      //    PageNumber = 0,
      //    PageSize = 1000,
      //    ProductSKUs = new[] { "ANN0583_004" },
      //    TenantToken = tokens[_tenantToken],
      //    UserToken = tokens[_userToken]
      //  });

      //StringContent content = new StringContent(body, Encoding.UTF8, "application/json");

      //JObject result = await skuVault.GetDataAsync(reqUri, content);

      //return Json(result);

      return RedirectToAction(nameof(SetBufferByStoreQty));
    }


    #region SetBufferByStoreQty
    [HttpGet("SetBufferByStoreQty")]
    public IActionResult SetBufferByStoreQty() => View();

    [HttpPost("SetBufferByStoreQty")]
    public async Task<IActionResult> SetBufferByStoreQty(FileImportViewModel model)
    {
      //List<ProductQuantityBufferModel> buffers = new List<ProductQuantityBufferModel>();
      //await ReadFile(model, buffers);

      string complete = "";
      var jObject = await GetInventoryByLocationAsync();
      foreach (JProperty item in jObject["Items"])
      {
        string sku = item.Name;
        string loc = item.Value[0]["LocationCode"].ToString();
        string quantity = item.Value[0]["Quantity"].ToString();

        complete = $"{ sku }: { loc } { quantity }";
        break;
      }

      return Json(complete);
    }

    private async Task ReadFile(FileImportViewModel model, List<ProductQuantityBufferModel> buffers)
    {
      List<string> headerNames = new List<string>
      {
        "Code", "SKU", "Channel Name", "Do not send quantity for this SKU", "Include incoming quantity mode", "Buffer Quantity Mode", "Buffer quantity", "Maximum quantity to push mode", "Maximum quantity to push", "Push constant quantity mode", "Push constant quantity", "Check marketplace quantity before update", "Delay interval", "Maximum consecutive delays"
      };
      List<int> headerIndexes = new List<int>();

      List<string> lines = new List<string>();
      Stream file = model.CSVFile.OpenReadStream();

      using (var reader = new StreamReader(file))
      {
        while (reader.Peek() >= 0) lines.Add(await reader.ReadLineAsync());
      }

      TextFieldParser parser = new TextFieldParser(new StringReader(lines[0]));
      parser.SetDelimiters(",");
      List<string> headers = parser.ReadFields().ToList();

      foreach (string header in headerNames)
      {
        int index = headers.IndexOf(header);
        headerIndexes.Add(index);
      }
      int i = 0;
      foreach (string line in lines.Skip(1))
      {
        parser = new TextFieldParser(new StringReader(line));
        parser.SetDelimiters(",");
        string[] rawFields = parser.ReadFields();

        ProductQuantityBufferModel buffer = new ProductQuantityBufferModel
        {
          Code = rawFields[headerIndexes[i++]],
          SKU = rawFields[headerIndexes[i++]],
          ChannelName = rawFields[headerIndexes[i++]],
          DoNotSendQuantityForThisSKU = rawFields[headerIndexes[i++]],
          IncludeIncomingQuantityMode = rawFields[headerIndexes[i++]],
          BufferQuantityMode = rawFields[headerIndexes[i++]],
          BufferQuantity = rawFields[headerIndexes[i++]],
          MaximumQuantityToPushMode = rawFields[headerIndexes[i++]],
          MaximumQuantityToPush = rawFields[headerIndexes[i++]],
          PushConstantQuantityMode = rawFields[headerIndexes[i++]],
          PushConstantQuantity = rawFields[headerIndexes[i++]],
          CheckMarketplaceQuantityBeforeUpdate = rawFields[headerIndexes[i++]],
          DelayInterval = rawFields[headerIndexes[i++]],
          MaximumConsecutiveDelays = rawFields[headerIndexes[i]],
        };

        i = 0;
        buffers.Add(buffer);
      }
    }

    private async Task<JObject> GetInventoryByLocationAsync()
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
          ProductSKUs = new[] { "YOA2071_02", "ANN0583_004" },
          TenantToken = tokens[_tenantToken],
          UserToken = tokens[_userToken]
        });

      StringContent content = new StringContent(body, Encoding.UTF8, "application/json");

      JObject jObject = await skuVault.GetDataAsync(reqUri, content);

      return jObject;
    }
    #endregion

    private async Task<JObject> GetTokensAsync(SkuVault skuVault)
    {
      string reqUri = "https://app.skuvault.com/api/gettokens";

      string body = JsonConvert.SerializeObject(
        new { SkuVaultSecret.Email, SkuVaultSecret.Password });

      StringContent content = new StringContent(body, Encoding.UTF8, "application/json");

      JObject tokens = await skuVault.GetDataAsync(reqUri, content);

      return tokens;
    }
  }
}
