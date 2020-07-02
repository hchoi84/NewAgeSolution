using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using ChannelAdvisorLibrary;
using ChannelAdvisorLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;
using NewAgeUI.Models;
using NewAgeUI.Securities;
using NewAgeUI.Utilities;
using NewAgeUI.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkuVaultLibrary;

namespace NewAgeUI.Controllers
{
  [AllowAnonymous]
  public class HomeController : Controller
  {
    private readonly ILogger<HomeController> _logger;
    private readonly IChannelAdvisor _channelAdvisor;
    private readonly ISkuVault _skuVault;

    public HomeController(ILogger<HomeController> logger, IChannelAdvisor channelAdvisor, ISkuVault skuVault)
    {
      _logger = logger;
      _channelAdvisor = channelAdvisor;
      _skuVault = skuVault;
    }

    [HttpGet("")]
    public IActionResult Index() => View();

    #region NoSalesReport
    [HttpGet("NoSalesReport")]
    public IActionResult NoSalesReport() => View();

    [HttpGet("ProductsByLastSoldDate")]
    public IActionResult ProductsByLastSoldDate()
    {
      List<ProductsByLastSoldDateViewModel> model = HttpContext.Session.GetObject< List<ProductsByLastSoldDateViewModel>>("model");

      HttpContext.Session.Remove("model");

      return View(model);
    }

    [HttpPost("ProductsByLastSoldDate")]
    public async Task<IActionResult> ProductsByLastSoldDate(DateTime lastSoldDate)
    {
      List<string> distinctParentIds = await GetDistinctParentIdsAsync(lastSoldDate);

      List<JObject> jObjects = await GetChildrenPerParentIdAsync(distinctParentIds);

      List<ProductsByLastSoldDateViewModel> model = new List<ProductsByLastSoldDateViewModel>();

      List<JObject> filteredByProfileId = jObjects.Where(j => j["ProfileID"].ToObject<int>() == ChannelAdvisorSecret.mainProfileId).ToList();
      ConvertToViewModel(model, filteredByProfileId);

      filteredByProfileId = jObjects.Where(j => j["ProfileID"].ToObject<int>() == ChannelAdvisorSecret.otherProfileId).ToList();
      ConvertToViewModel(model, filteredByProfileId);

      //Create parent information
      List<IGrouping<string, ProductsByLastSoldDateViewModel>> groupedByParentSku = model.GroupBy(m => m.ParentSKU).ToList();

      foreach (var group in groupedByParentSku)
      {
        ProductsByLastSoldDateViewModel prod = new ProductsByLastSoldDateViewModel();

        prod.Sku = group.First().ParentSKU;
        prod.CreateDateUtc = group.First().CreateDateUtc;
        prod.TotalAvailableQuantity = group.Sum(g => g.TotalAvailableQuantity);
        prod.FBA = group.Sum(g => g.FBA);
        prod.AllName = group.First().ItemName;
        prod.ProductLabel = group.First().ProductLabel;

        model.Add(prod);
      }

      model = model.OrderBy(m => m.Sku).ToList();

      HttpContext.Session.SetObject("model", model);

      return RedirectToAction(nameof(ProductsByLastSoldDate));
    }

    private async Task<List<string>> GetDistinctParentIdsAsync(DateTime lastSoldDate)
    {
      //Create necessary filters
      string filter = $"LastSaleDateUtc lt { lastSoldDate.ToString("yyyy-MM-dd") }";
      string expand = "";
      string select = "ParentProductID";

      //Get products via ChannelAdvisorAPI
      List<JObject> jObjects = await _channelAdvisor.GetProductsAsync(filter, expand, select);

      //Get distinct parent ids
      List<string> distinctParentIds = jObjects
        .Where(j => !string.IsNullOrWhiteSpace(j[select].ToObject<string>()))
        .Select(j => j[select].ToObject<string>())
        .Distinct()
        .ToList();

      return distinctParentIds;
    }

    private async Task<List<JObject>> GetChildrenPerParentIdAsync(List<string> distinctParentIds)
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

        jObjects.AddRange(await _channelAdvisor.GetProductsAsync(filter, expand, select));
      }

      return jObjects;
    }

    private void ConvertToViewModel(List<ProductsByLastSoldDateViewModel> model, List<JObject> filteredByProfileId)
    {
      int modelCount = model.Count;

      foreach (var item in filteredByProfileId)
      {
        var fbaQty = item["DCQuantities"].FirstOrDefault(i => i["DistributionCenterID"].ToObject<int>() == -4);

        ProductsByLastSoldDateViewModel p = new ProductsByLastSoldDateViewModel();

        if (modelCount != 0)
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

        ProductsByLastSoldDateViewModel prod = item.ToObject<ProductsByLastSoldDateViewModel>();

        string allName = item["Attributes"]
          .FirstOrDefault(i => i["Name"].ToObject<string>() == "All Name")["Value"].ToObject<string>();

        string itemName = item["Attributes"]
          .FirstOrDefault(i => i["Name"].ToObject<string>() == "Item Name")["Value"].ToObject<string>();

        List<string> labelNames = new List<string>()
        { "Closeout", "Discount", "MAPNoShow", "MAPShow", "NPIP" };

        prod.FBA = fbaQty != null ? fbaQty["AvailableQuantity"].ToObject<int>() : 0;
        prod.ItemName = itemName;
        prod.AllName = allName.Replace(itemName, "");
        prod.ProductLabel = item["Labels"].FirstOrDefault(i => labelNames.Contains(i["Name"].ToObject<string>()))["Name"].ToObject<string>();

        model.Add(prod);
      }
    }
    #endregion

    #region SetBufferByStoreQty
    [HttpGet("SetBufferByStoreQty")]
    public IActionResult SetBufferByStoreQty() => View();

    //[HttpPost("SetBufferByStoreQty")]
    //public async Task<IActionResult> SetBufferByStoreQty(FileImportViewModel model)
    //{
    //  List<string> productSkusWithBuffer = await GetSkusFromFile(model);

    //  Dictionary<string, int> productsWithStoreQty = await GetProductsFromAPI();

    //  //Iterate productSkusWithBuffer
    //  //If product is in dictionary, continue
    //  //Else add with 0 quantity to remove buffer
    //  foreach (var product in productSkusWithBuffer)
    //  {
    //    if (!productsWithStoreQty.ContainsKey(product))
    //    {
    //      productsWithStoreQty.Add(product, 0);
    //    }
    //  }

    //  //Convert dictionary to List<string>
    //  StringBuilder sb = new StringBuilder();

    //  sb.AppendLine("SKU,Code,Channel Name,Do not send quantity for this SKU,Include incoming quantity mode,Buffer Quantity Mode,Buffer quantity,Maximum quantity to push mode,Maximum quantity to push,Push constant quantity mode,Push constant quantity,Check marketplace quantity,Delay interval,Maximum consecutive delays");

    //  foreach (var product in productsWithStoreQty)
    //  {
    //    string bufferQuantityMode = product.Value == 0 ? "Off" : "Subtract";

    //    sb.AppendLine($"{ product.Key },,CA Golfio,Off,Off,Off,{ bufferQuantityMode },{ product.Value },Off,20000,Off,0,Off,30,1");
    //  }

    //  //Generate CSV file that's ready to be imported to SkuVault
    //  //Display and download to user's computer
    //  HttpContext.Session.SetObject("storeBuffer", sb);

    //  return RedirectToAction(nameof(SetBufferByStoreQty));
    //}

    [HttpPost("DownloadReport")]
    public IActionResult DownloadReport()
    {
      if (HttpContext.Session.GetObject<StringBuilder>("storeBuffer") == null)
      {
        return RedirectToAction(nameof(SetBufferByStoreQty));
      }

      var file = File(new UTF8Encoding().GetBytes(HttpContext.Session.GetObject<StringBuilder>("storeBuffer").ToString()), "text/csv", "buffer.csv");

      HttpContext.Session.Remove("storeBuffer");

      return file;
    }

    private async Task<List<string>> GetSkusFromFile(FileImportViewModel model)
    {
      //TODO: validate the ChannelName before proceeding. User could have exported the wrong info
      string sku = "SKU";

      Dictionary<string, int> headerIndex = new Dictionary<string, int>
      { { sku, 0 } };

      List<string> lines = new List<string>();
      Stream file = model.CSVFile.OpenReadStream();
      List<string> result = new List<string>();

      using (var reader = new StreamReader(file))
      {
        while (reader.Peek() >= 0) lines.Add(await reader.ReadLineAsync());
      }

      TextFieldParser parser = new TextFieldParser(new StringReader(lines[0]));
      parser.SetDelimiters(",");
      List<string> headers = parser.ReadFields().ToList();

      headerIndex[sku] = headers.IndexOf(sku);

      foreach (string line in lines.Skip(1))
      {
        parser = new TextFieldParser(new StringReader(line));
        parser.SetDelimiters(",");
        string[] rawFields = parser.ReadFields();

        result.Add(rawFields[headerIndex[sku]]);
      }

      return result;
    }

    //private async Task<Dictionary<string, int>> GetProductsFromAPI()
    //{
    //  //Export products from ChannelAdvisor
    //  string filter = $"ProfileId eq { ChannelAdvisorSecret.mainProfileId } and IsParent eq false and DCQuantities/Any (c: c/DistributionCenterId eq 0 and c/AvailableQuantity gt 0 and c/AvailableQuantity lt 10000)";

    //  //Only retrieve SKU and Warehouse Locations
    //  string select = $"Sku, WarehouseLocation";

    //  _channelAdvisor.SetConnection(new CaConnectionModel
    //  {
    //    TokenUrl = ChannelAdvisorSecret.tokenUrl,
    //    ApplicationId = ChannelAdvisorSecret.applicationId,
    //    SharedSecret = ChannelAdvisorSecret.sharedSecret,
    //    RefreshToken = ChannelAdvisorSecret.refreshToken,
    //    TokenExpireBuffer = ChannelAdvisorSecret.tokenExpireBuffer
    //  });

    //  var products = await _channelAdvisor.GetProductsAsync(filter, "", select);

    //  //Create a dictionary<SKU, Qty>
    //  Dictionary<string, int> productsWithStoreQty = new Dictionary<string, int>();

    //  //Retrieve Store quantity from Warehouse Locations
    //  foreach (var product in products)
    //  {
    //    if (product.WarehouseLocation == null || !product.WarehouseLocation.Contains("STORE")) continue;

    //    int storeQty = Int32.Parse(product.WarehouseLocation.Split(',').FirstOrDefault(loc => loc.Contains("STORE")).Replace("STORE(", "").Replace(")", ""));

    //    productsWithStoreQty.Add(product.Sku, storeQty);
    //  }

    //  return productsWithStoreQty;
    //}

    private async Task<JObject> GetInventoryByLocationAsync(int pageNumber, int pageSize)
    {
      JObject tokens = await GetTokensAsync();

      string reqUri = "https://app.skuvault.com/api/inventory/getInventoryByLocation";

      string _tenantToken = "TenantToken";
      string _userToken = "UserToken";

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

      StringContent content = new StringContent(body, Encoding.UTF8, "application/json");

      JObject jObject = await _skuVault.GetDataAsync(reqUri, content);

      return jObject;
    }

    private async Task<JObject> GetTokensAsync()
    {
      string reqUri = "https://app.skuvault.com/api/gettokens";

      string body = JsonConvert.SerializeObject(
        new { SkuVaultSecret.Email, SkuVaultSecret.Password });

      StringContent content = new StringContent(body, Encoding.UTF8, "application/json");

      JObject tokens = await _skuVault.GetDataAsync(reqUri, content);

      return tokens;
    }
    #endregion

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
      return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
  }
}
