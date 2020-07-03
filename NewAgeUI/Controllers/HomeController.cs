using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Microsoft.Extensions.Logging;
using NewAgeUI.Models;
using NewAgeUI.Utilities;
using NewAgeUI.ViewModels;
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
    private readonly IFileReader _fileReader;

    public HomeController(ILogger<HomeController> logger, IChannelAdvisor channelAdvisor, ISkuVault skuVault, IFileReader fileReader)
    {
      _logger = logger;
      _channelAdvisor = channelAdvisor;
      _skuVault = skuVault;
      _fileReader = fileReader;
    }

    [HttpGet("")]
    //TODO: Add links to tasks
    public IActionResult Index() => View();

    #region NoSalesReport
    [HttpGet("NoSalesReport")]
    //TODO: Add instruction and how high level of what the system does as a collapsable
    public IActionResult NoSalesReport() => View();

    [HttpPost("NoSalesReport")]
    public async Task<IActionResult> NoSalesReport(DateTime lastSoldDate)
    {
      string filter = $"LastSaleDateUtc lt { lastSoldDate.ToString("yyyy-MM-dd") }";
      string expand = "";
      string select = "ParentProductID";
      List<string> distinctParentIds = await _channelAdvisor.GetDistinctParentIdsAsync(filter, expand, select);

      List<JObject> jObjects = await _channelAdvisor.GetChildrenPerParentIdAsync(distinctParentIds);

      List<NoSalesReportModel> model = _channelAdvisor.ConvertToNoSalesReportModel(jObjects);

      model = _channelAdvisor.AddParentInfo(model).OrderBy(m => m.Sku).ToList();

      HttpContext.Session.SetObject("model", model);

      return RedirectToAction(nameof(NoSalesReportResult));
    }

    [HttpGet("NoSalesReportResult")]
    //TODO: Implement download button
    //TODO: Notify user once they log out, the data will no longer be available
    public IActionResult NoSalesReportResult()
    {
      List<NoSalesReportModel> model = HttpContext.Session.GetObject<List<NoSalesReportModel>>("model");

      HttpContext.Session.Remove("model");

      return View(model);
    }

    [AcceptVerbs("Get", "Post")]
    public IActionResult ValidateDate(DateTime lastSoldDate)
    {
      if (lastSoldDate >= DateTime.Today)
      {
        return Json("Date must be in the past");
      }

      return Json(true);
    }
    #endregion

    #region SetBufferByStoreQty
    [HttpGet("SetBufferByStoreQty")]
    public IActionResult SetBufferByStoreQty() => View();

    [HttpPost("SetBufferByStoreQty")]
    public async Task<IActionResult> SetBufferByStoreQty(FileImportViewModel model)
    {
      List<string> skus = await _fileReader.GetSkusFromFileAsync(model.CSVFile);

      int pageNumber = 0;
      int pageSize = 10000;
      bool hasMoreProducts;
      Dictionary<string, int> skuAndStoreQty = new Dictionary<string, int>();

      do
      {
        hasMoreProducts = false;

        List<JToken> jTokens = await _skuVault.GetInventoryByLocationAsync(pageNumber, pageSize);

        Dictionary<string, int> productsWithStoreQty = _skuVault.GetStoreQty(jTokens);
        
        foreach (KeyValuePair<string, int> item in productsWithStoreQty)
        {
          skuAndStoreQty.Add(item.Key, item.Value);
        }

        if (jTokens.Count >= pageSize)
        {
          hasMoreProducts = true;
          pageNumber++;
        }

        Thread.Sleep(12000);

      } while (hasMoreProducts);

      skus.Where(sku => !skuAndStoreQty.ContainsKey(sku)).ToList().ForEach(sku => skuAndStoreQty.Add(sku, 0));

      StringBuilder sb = _fileReader.ConvertToStoreBufferStringBuilder(skuAndStoreQty);

      HttpContext.Session.SetObject("storeBuffer", sb);

      return RedirectToAction(nameof(SetBufferByStoreQty));
    }

    [HttpPost("DownloadReport")]
    public IActionResult DownloadReport()
    {
      StringBuilder data = HttpContext.Session.GetObject<StringBuilder>("storeBuffer");

      if (data == null) return RedirectToAction(nameof(SetBufferByStoreQty));

      FileContentResult file = File(new UTF8Encoding().GetBytes(data.ToString()), "text/csv", "buffer.csv");

      HttpContext.Session.Remove("storeBuffer");

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
