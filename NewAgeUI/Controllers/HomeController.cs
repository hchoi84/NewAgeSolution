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
using FileReaderLibrary.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewAgeUI.Models;
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
    private readonly UserManager<Employee> _userManager;

    public HomeController(ILogger<HomeController> logger, IChannelAdvisor channelAdvisor, ISkuVault skuVault, IFileReader fileReader, UserManager<Employee> userManager)
    {
      _logger = logger;
      _channelAdvisor = channelAdvisor;
      _skuVault = skuVault;
      _fileReader = fileReader;
      _userManager = userManager;
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
        string line = $"{ product.Sku },{ product.UPC },{product.CreateDateUtc:yyyy-MM-dd},{ product.AllName },{product.LastSaleDateUtc:yyyy-MM-dd},{ product.ProductLabel },{ product.TotalAvailableQuantity } / { product.FBA }";

        lines.Add(line);
      }

      string header = "SKU,UPC,Created,All Name,Last Sold Date,Label,WH/FBA Qty";
      StringBuilder sb = _fileReader.GenerateStringBuilder(true, header, lines);

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

    #region BufferSetter
    [HttpGet("BufferSetter")]
    public IActionResult BufferSetter() => View();

    [HttpPost("BufferSetter")]
    public async Task<IActionResult> BufferSetter(FileImportViewModel model)
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

    #region DropShipUpdater
    [HttpGet("DropShipUpdater")]
    public IActionResult DropShipUpdater() => View();

    [HttpPost("DropShipUpdater")]
    public async Task<IActionResult> DropShipUpdaterBatch()
    {
      int mainProfileId = _channelAdvisor.GetMainProfileId();
      string filterBase = $"ProfileId eq { mainProfileId } and Attributes/Any (c:c/Name eq 'invflag' and c/Value eq";
      string taq = "TotalAvailableQuantity";

      List<string> filters = new List<string>
      {
        $"{ filterBase } 'Green') and { taq } le 0",
        $"{ filterBase } 'Green') and { taq } ge 15000 and { taq } lt 19999",
        $"{ filterBase } 'Red') and { taq } ge 15000",
      };
      string expand = "Attributes,Labels";
      string select = $"Sku,{ taq }";

      List<UpdateDropShipReportModel> products = new List<UpdateDropShipReportModel>();

      foreach (var filter in filters)
      {
        List<JObject> jObjects = new List<JObject>();

        try
        {
          jObjects = await _channelAdvisor.GetProductsAsync(filter, expand, select);
        }
        catch (Exception e)
        {
          return Json(e.Message);
        }

        products.AddRange(_channelAdvisor.ConvertToUpdateDropShipReportModel(jObjects));
      }

      List<string> lines = new List<string>();
      Dictionary<string, int> skuAndNewQty = new Dictionary<string, int>();

      foreach (var product in products)
      {
        lines.Add($"{product.Sku},{product.InvFlag},{product.Label},\"{product.AllName}\",{product.Qty}");

        skuAndNewQty.Add(product.Sku, product.InvFlag == "Green" ? 19999 : 0);
      }

      string header = "SKU,InvFlag,Label,All Name,Qty";
      StringBuilder sb = _fileReader.GenerateStringBuilder(true, header, lines);

      await _skuVault.UpdateDropShip(skuAndNewQty);

      FileContentResult file = File(new UTF8Encoding().GetBytes(sb.ToString()), "text/csv", "DropShipUpdate.csv");

      return file;
    }
    #endregion

    #region ZDTSummarizer
    [HttpGet("ZDTSummarizer")]
    public IActionResult ZDTSummarizer() => View();

    [HttpPost("ZDTSummarizer")]
    public async Task<IActionResult> ZDTSummarizer(FileImportViewModel model)
    {
      string fileExtension = Path.GetExtension(model.CSVFile.FileName);

      if (fileExtension != ".csv")
      {
        ModelState.AddModelError("", "File must be a CSV type");

        return View();
      }

      List<ZDTModel> callHistory = await _fileReader.ReadZendeskTalkExportFile(model.CSVFile);

      List<ZDTSummaryModel> summary = _fileReader.SummarizeCallHistory(callHistory);

      List<string> lines = summary.Select(i => $"{ i.Date },{ i.Category },{ i.Count },{ i.AvgWaitMin },{ i.AvgTalkMin }").ToList();

      string header = "Date,Category,Count,Avg Wait Min,Avg Talk Min";
      StringBuilder sb = _fileReader.GenerateStringBuilder(true, header, lines);

      FileContentResult file = File(new UTF8Encoding().GetBytes(sb.ToString()), "text/csv", "ZendeskTalkSummary.csv");

      return file;
    }
    #endregion

    [HttpGet("UserList")]
    public async Task<IActionResult> UserList()
    {
      List<Employee> employees = await _userManager.Users.ToListAsync();
      List<UserListViewModel> model = new List<UserListViewModel>();

      foreach (Employee employee in employees)
      {
        List<string> claimType = new List<string>();

        (await _userManager.GetClaimsAsync(employee)).ToList().ForEach(c => claimType.Add(c.Type));

        model.Add(new UserListViewModel
        {
          FullName = employee.FullName,
          EmailAddress = employee.Email,
          AccessPermission = string.Join(", ", claimType),
        });
      }

      return View(model);
    }

    [HttpGet("VersionHistory")]
    public IActionResult VersionHistory() => View();

    [HttpGet("AccessDenied")]
    public IActionResult AccessDenied() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
      return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
  }
}
