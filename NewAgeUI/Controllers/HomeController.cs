using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ChannelAdvisorLibrary;
using ChannelAdvisorLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

    [HttpPost("ProductsByLastSoldDate")]
    public async Task<IActionResult> ProductsByLastSoldDate(DateTime lastSoldDate)
    {
      //TODO: validate lastSoldDate. Ensure month and date is 2 digits and year is 4 digits. Must be in the past. 

      List<Task> tasks = new List<Task>();
      List<ProductModel> products = await GetSiblingsAsync(lastSoldDate);

      List<ProductsByLastSoldDateViewModel> model = new List<ProductsByLastSoldDateViewModel>();

      tasks.Add(Task.Run(() => AddParentInfo(products, model)));
      tasks.Add(Task.Run(() => AddChildInfo(products, model)));

      Task.WaitAll(tasks.ToArray());

      model = model.OrderBy(m => m.SKU).ToList();

      return RedirectToAction(nameof(ProductsByLastSoldDate), new { model });
    }

    [HttpGet("ProductsByLastSoldDate")]
    public IActionResult ProductsByLastSoldDate(List<ProductsByLastSoldDateViewModel> model) => View(model);

    private async Task<List<ProductModel>> GetSiblingsAsync(DateTime lastSoldDate)
    {
      List<ProductModel> products = new List<ProductModel>();

      string filter = $"LastSaleDateUtc lt { lastSoldDate.ToString("yyyy-MM-dd") }";

      List<string> parentProductIds = new List<string>();

      (await _channelAdvisor.GetProductsAsync(filter, "", "ParentProductID"))
        .ForEach(p => parentProductIds.Add(p.ParentProductID.ToString()));

      parentProductIds = parentProductIds.Distinct().ToList();
      parentProductIds.RemoveAll(d => d == "");

      while (parentProductIds.Count > 0)
      {
        bool isMoreThan10 = parentProductIds.Count > 10;
        int x = isMoreThan10 ? 10 : parentProductIds.Count;

        List<string> first10 = new List<string>();

        parentProductIds.GetRange(0, x).ForEach(parentId => first10.Add($"ParentProductId eq { parentId }"));
        parentProductIds.RemoveRange(0, x);

        filter = string.Join(" or ", first10);

        products.AddRange(await _channelAdvisor.GetProductsAsync(filter, "Attributes,Labels,DCQuantities", ""));
      }

      return products;
    }

    private void AddParentInfo(List<ProductModel> products, List<ProductsByLastSoldDateViewModel> model)
    {
      IEnumerable<IGrouping<string, ProductModel>> groupedByParentSku = products.GroupBy(p => p.ParentSku);

      foreach (var g in groupedByParentSku)
      {
        IEnumerable<ProductModel> productsWithMainFBA = g.Where(p => p.ProfileID == ChannelAdvisorSecret.mainProfileId && p.DCQuantities.Exists(dc => dc.DistributionCenterID == -4));

        IEnumerable<ProductModel> productsWithOtherFBA = g.Where(p => p.ProfileID == ChannelAdvisorSecret.otherProfileId && p.DCQuantities.Exists(dc => dc.DistributionCenterID == -4));

        ProductsByLastSoldDateViewModel p = new ProductsByLastSoldDateViewModel()
        {
          SKU = g.Key,
          UPC = "",
          Description = g.First().Attributes.FirstOrDefault(a => a.Name == "Item Name").Value,
          Created = g.First().CreateDateUtc.ToString("yyyy-MM-dd"),
          GLSD = "",
          GBLSD = "",
          WHQTY = g.Sum(p => p.TotalAvailableQuantity),
          GFBA = productsWithMainFBA == null ? 0 : productsWithMainFBA.Sum(p => p.DCQuantities.First(dc => dc.DistributionCenterID == -4).AvailableQuantity),
          GBFBA = productsWithOtherFBA == null ? 0 : productsWithOtherFBA.Sum(p => p.DCQuantities.First(dc => dc.DistributionCenterID == -4).AvailableQuantity),
        };

        model.Add(p);
      }
    }

    private void AddChildInfo(List<ProductModel> products, List<ProductsByLastSoldDateViewModel> model)
    {
      IEnumerable<IGrouping<int, ProductModel>> groupedByProfileId = products.GroupBy(p => p.ProfileID);

      IGrouping<int, ProductModel> group = groupedByProfileId.FirstOrDefault(g => g.Key == ChannelAdvisorSecret.mainProfileId);

      foreach (ProductModel product in group)
      {
        AddToModel(product, model);
      }

      group = groupedByProfileId.FirstOrDefault(g => g.Key == ChannelAdvisorSecret.otherProfileId);

      foreach (ProductModel product in group)
      {
        DcQuantityModel fbaQty = product.DCQuantities.FirstOrDefault(dc => dc.DistributionCenterID == -4);

        ProductsByLastSoldDateViewModel record = model.FirstOrDefault(m => m.SKU == product.Sku);

        if (record != null)
        {
          record.GBLSD = product.LastSaleDateUtc.HasValue ? ((DateTime)product.LastSaleDateUtc).ToString("yyyy-MM-dd") : "NEVER";
          record.GBFBA = fbaQty != null ? fbaQty.AvailableQuantity : 0;
        }
        else
        {
          AddToModel(product, model);
        }
      }
    }

    private void AddToModel(ProductModel product, List<ProductsByLastSoldDateViewModel> model)
    {
      DcQuantityModel fbaQty = product.DCQuantities.FirstOrDefault(dc => dc.DistributionCenterID == -4);
      string itemName = product.Attributes.FirstOrDefault(a => a.Name == "Item Name").Value;

      ProductsByLastSoldDateViewModel p = new ProductsByLastSoldDateViewModel()
      {
        SKU = product.Sku,
        UPC = product.UPC,
        ParentSKU = product.ParentSku,
        Description = product.Attributes.FirstOrDefault(a => a.Name == "All Name").Value.Replace(itemName, "").Trim(),
        Created = product.CreateDateUtc.ToString("yyyy-MM-dd"),
        GLSD = product.LastSaleDateUtc.HasValue ? ((DateTime)product.LastSaleDateUtc).ToString("yyyy-MM-dd") : "NEVER",
        GBLSD = "NEVER",
        WHQTY = product.TotalAvailableQuantity,
        GFBA = fbaQty != null ? fbaQty.AvailableQuantity : 0,
        GBFBA = 0,
      };

      model.Add(p);
    }
    #endregion

    #region SetBufferByStoreQty
    [HttpGet("SetBufferByStoreQty")]
    public IActionResult SetBufferByStoreQty() => View();

    [HttpPost("SetBufferByStoreQty")]
    public async Task<IActionResult> SetBufferByStoreQty(FileImportViewModel model)
    {
      List<string> productSkusWithBuffer = await GetSkusFromFile(model);

      Dictionary<string, int> productsWithStoreQty = await GetProductsFromAPI();

      //Iterate productSkusWithBuffer
      //If product is in dictionary, continue
      //Else add with 0 quantity to remove buffer
      foreach (var product in productSkusWithBuffer)
      {
        if (!productsWithStoreQty.ContainsKey(product))
        {
          productsWithStoreQty.Add(product, 0);
        }
      }

      //Convert dictionary to List<string>
      StringBuilder sb = new StringBuilder();
      
      sb.AppendLine("SKU,Code,Channel Name,Do not send quantity for this SKU,Include incoming quantity mode,Buffer Quantity Mode,Buffer quantity,Maximum quantity to push mode,Maximum quantity to push,Push constant quantity mode,Push constant quantity,Check marketplace quantity,Delay interval,Maximum consecutive delays");

      foreach (var product in productsWithStoreQty)
      {
        string bufferQuantityMode = product.Value == 0 ? "Off" : "Subtract";

        sb.AppendLine($"{ product.Key },,CA Golfio,Off,Off,Off,{ bufferQuantityMode },{ product.Value },Off,20000,Off,0,Off,30,1");
      }

      //Generate CSV file that's ready to be imported to SkuVault
      //Display and download to user's computer
      HttpContext.Session.SetObject("storeBuffer", sb);

      return RedirectToAction(nameof(SetBufferByStoreQty));
    }

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

    private async Task<Dictionary<string, int>> GetProductsFromAPI()
    {
      //Export products from ChannelAdvisor
      string filter = $"ProfileId eq { ChannelAdvisorSecret.mainProfileId } and IsParent eq false and DCQuantities/Any (c: c/DistributionCenterId eq 0 and c/AvailableQuantity gt 0 and c/AvailableQuantity lt 10000)";

      //Only retrieve SKU and Warehouse Locations
      string select = $"Sku, WarehouseLocation";

      _channelAdvisor.SetConnection(new CaConnectionModel
      {
        TokenUrl = ChannelAdvisorSecret.tokenUrl,
        ApplicationId = ChannelAdvisorSecret.applicationId,
        SharedSecret = ChannelAdvisorSecret.sharedSecret,
        RefreshToken = ChannelAdvisorSecret.refreshToken,
        TokenExpireBuffer = ChannelAdvisorSecret.tokenExpireBuffer
      });

      var products = await _channelAdvisor.GetProductsAsync(filter, "", select);

      //Create a dictionary<SKU, Qty>
      Dictionary<string, int> productsWithStoreQty = new Dictionary<string, int>();

      //Retrieve Store quantity from Warehouse Locations
      foreach (var product in products)
      {
        if (product.WarehouseLocation == null || !product.WarehouseLocation.Contains("STORE")) continue;

        int storeQty = Int32.Parse(product.WarehouseLocation.Split(',').FirstOrDefault(loc => loc.Contains("STORE")).Replace("STORE(", "").Replace(")", ""));

        productsWithStoreQty.Add(product.Sku, storeQty);
      }

      return productsWithStoreQty;
    }

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
