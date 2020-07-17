using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ChannelAdvisorLibrary;
using ChannelAdvisorLibrary.Models;
using FileReaderLibrary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;
using NewAgeUI.Models;
using NewAgeUI.Utilities;
using NewAgeUI.ViewModels;
using Newtonsoft.Json.Linq;
using SkuVaultLibrary;

namespace NewAgeUI.Controllers
{
  public class HomeController : Controller
  {
    private readonly ILogger<HomeController> _logger;
    private readonly IChannelAdvisor _channelAdvisor;
    private readonly ISkuVault _skuVault;
    private readonly IFileReader _fileReader;

    public HomeController(ILogger<HomeController> logger, IChannelAdvisor channelAdvisor, ISkuVault skuVault, IFileReader fileReader)
    {
      _logger = logger;
      _channelAdvisor = channelAdvisor;
      _skuVault = skuVault;
      _fileReader = fileReader;
    }

    [HttpGet("")]
    public IActionResult Index() => View();

    #region NoSalesReport
    [HttpGet("NoSalesReport")]
    public IActionResult NoSalesReport() => View();

    [HttpPost("NoSalesReport")]
    public async Task<IActionResult> NoSalesReport(DateTime lastSoldDate)
    {
      string filter = $"LastSaleDateUtc lt {lastSoldDate:yyyy-MM-dd}";
      string expand = "";
      string select = "ParentProductID";
      List<string> distinctParentIds = await _channelAdvisor.GetDistinctParentIdsAsync(filter, expand, select);

      List<JObject> jObjects = await _channelAdvisor.GetChildrenPerParentIdAsync(distinctParentIds);

      List<NoSalesReportModel> model = _channelAdvisor.ConvertToNoSalesReportModel(jObjects);

      model = _channelAdvisor.AddParentInfo(model).OrderBy(m => m.Sku).ToList();

      List<string> lines = new List<string>();

      foreach (var product in model)
      {
        string line = $"{ product.Sku }, { product.UPC }, {product.CreateDateUtc:yyyy-MM-dd}, { product.AllName }, {product.LastSaleDateUtc:yyyy-MM-dd}, { product.ProductLabel }, { product.TotalAvailableQuantity } / { product.FBA }";

        lines.Add(line);
      }

      StringBuilder sb = _fileReader.ConvertToNoSalesReportStringBuilder(lines);

      FileContentResult file = File(new UTF8Encoding().GetBytes(sb.ToString()), "text/csv", "NoSalesReport.csv");

      return file;
    }

    [AcceptVerbs("Get", "Post")]
    public IActionResult ValidateDate(DateTime lastSoldDate)
    {
      if (lastSoldDate >= DateTime.Today) return Json("Date must be in the past");

      return Json(true);
    }
    #endregion

    #region SetBufferByStoreQty
    [HttpGet("SetBufferByStoreQty")]
    public IActionResult SetBufferByStoreQty() => View();

    [HttpPost("SetBufferByStoreQty")]
    public async Task<IActionResult> SetBufferByStoreQty(FileImportViewModel model)
    {
      string fileExtension = Path.GetExtension(model.CSVFile.FileName);
      if (fileExtension != ".csv") 
      {
        ModelState.AddModelError("", "File must be a CSV type");
        return View(); 
      }

      Dictionary<string, int> skuAndQtyFromFile = await _fileReader.RetrieveSkuAndQty(model.CSVFile);

      int pageNumber = 0;
      int pageSize = 10000;
      bool hasMoreProducts;
      Dictionary<string, int> skuAndQtyForImport = new Dictionary<string, int>();

      do
      {
        hasMoreProducts = false;

        List<JToken> jTokens = await _skuVault.GetInventoryByLocationAsync(pageNumber, pageSize);

        Dictionary<string, int> skuAndQtyFromSV = _skuVault.RetrieveSkuAndQty(jTokens);

        _skuVault.ProcessUniqueSkuAndQty(skuAndQtyFromSV, skuAndQtyFromFile, skuAndQtyForImport);

        if (jTokens.Count >= pageSize)
        {
          hasMoreProducts = true;
          pageNumber++;
        }

        Thread.Sleep(12000);

      } while (hasMoreProducts);

      skuAndQtyFromFile.ToList().ForEach(sku => skuAndQtyForImport.Add(sku.Key, 0));

      StringBuilder sb = _fileReader.ConvertToStoreBufferStringBuilder(skuAndQtyForImport, _channelAdvisor.GetMainName(), true);
      sb.Append(_fileReader.ConvertToStoreBufferStringBuilder(skuAndQtyForImport, _channelAdvisor.GetOtherName(), false));

      FileContentResult file = File(new UTF8Encoding().GetBytes(sb.ToString()), "text/csv", "StoreBuffer.csv");

      return file;
    }
    #endregion

    #region UpdateDropShip
    [HttpGet("UpdateDropShip")]
    public IActionResult UpdateDropShipTask() => View();

    [HttpPost("UpdateDropShip")]
    public async Task<IActionResult> UpdateDropShip()
    {
      Dictionary<string, string> filters = new Dictionary<string, string>
      {
        { $"ProfileId eq { _channelAdvisor.GetMainProfileId() } and Attributes/Any (c:c/Name eq 'invflag' and c/Value eq 'Green') and TotalAvailableQuantity le 0", "Green" },
        { $"ProfileId eq { _channelAdvisor.GetMainProfileId() } and Attributes/Any (c:c/Name eq 'invflag' and c/Value eq 'Green') and TotalAvailableQuantity ge 15000 and TotalAvailableQuantity lt 19999", "Green" },
        { $"ProfileId eq { _channelAdvisor.GetMainProfileId() } and Attributes/Any (c:c/Name eq 'invflag' and c/Value eq 'Red') and TotalAvailableQuantity ge 15000 and TotalAvailableQuantity le 19999", "Red" }
      };

      string expand = "Attributes,Labels";
      string select = "Sku,TotalAvailableQuantity";

      List<JObject> jObjects = new List<JObject>();
      StringBuilder sb = new StringBuilder();

      foreach (var filter in filters)
      {
        try
        {
          jObjects = await _channelAdvisor.GetProductsAsync(filter.Key, expand, select);
        }
        catch (Exception e)
        {
          return Json(e.Message);
        }

        List<UpdateDropShipReportModel> products = _channelAdvisor.ConvertToUpdateDropShipReportModel(jObjects);

        List<string> lines = new List<string>();

        foreach (var product in products)
        {
          string line = $"{product.Sku},{product.InvFlag},{product.Label},{product.AllName},{product.Qty}";

          lines.Add(line);
        }

        sb.Append(_fileReader.ConvertToUpdateDropShipQtyReport(lines));

        int qtyToUpdateTo = filter.Value == "Green" ? 19999 : 0;

        List<string> skus = products.Select(p => p.Sku).ToList();

        await _skuVault.UpdateDropShip(skus, qtyToUpdateTo);
      }

      FileContentResult file = File(new UTF8Encoding().GetBytes(sb.ToString()), "text/csv", "StoreBuffer.csv");

      return file;
    }
    #endregion

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
      return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
  }
}
