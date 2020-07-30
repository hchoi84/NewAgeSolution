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
      List<NoSalesReportModel> model = await _channelAdvisor.GetNoSalesReport(lastSoldDate);

      List<string> lines = new List<string>();
      model.ForEach(p => lines.Add($"{ p.Sku },{ p.UPC },{p.CreateDateUtc:yyyy-MM-dd},{ p.AllName },{p.LastSaleDateUtc:yyyy-MM-dd},{ p.ProductLabel },{ p.TotalAvailableQuantity } / { p.FBA }"));

      string header = "SKU,UPC,Created,All Name,Last Sold Date,Label,WH/FBA Qty";
      StringBuilder sb = _fileReader.GenerateSB(true, header, lines);

      byte[] fileContent = new UTF8Encoding().GetBytes(sb.ToString());
      string contentType = "text/csv";
      string fileName = $"NoSalesReport-{ DateTime.Now.ToShortDateString() }.csv";
      FileContentResult file = File(fileContent, contentType, fileName);

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

      Dictionary<string, int> activeBufferProducts = await _fileReader.RetrieveSkuAndQty(model.CSVFile);
      Dictionary<string, int> productsToUpdate = await _skuVault.GetProductsToUpdate(activeBufferProducts);

      StringBuilder sb = new StringBuilder();
      foreach (var accountName in _channelAdvisor.GetAcctNames())
      {
        sb.Append(_fileReader.ConvertToStoreBufferSB(
          sb.Length == 0,
          productsToUpdate,
          accountName));
      }

      byte[] fileContent = new UTF8Encoding().GetBytes(sb.ToString());
      string contentType = "text/csv";
      string fileName = $"StoreBuffer-{ DateTime.Now.ToShortDateString() }.csv";
      FileContentResult file = File(fileContent, contentType, fileName);

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
      StringBuilder sb = _fileReader.GenerateSB(true, header, lines);

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

      List<string> lines = summary.Select(i => $"{ i.CallDate },{ i.Count },{ i.AvgWaitSec },{ i.AvgTalkSec }").ToList();

      string header = "Date,Count,Avg Wait Sec,Avg Talk Sec";
      StringBuilder sb = _fileReader.GenerateSB(true, header, lines);

      FileContentResult file = File(new UTF8Encoding().GetBytes(sb.ToString()), "text/csv", "ZendeskTalkSummary.csv");

      return file;
    }
    #endregion

    [HttpGet("UserList")]
    public async Task<IActionResult> UserList()
    {
      List<Employee> employees = (await _userManager.Users.ToListAsync()).OrderBy(e => e.FullName).ToList();
      List<UserListViewModel> model = new List<UserListViewModel>();

      foreach (Employee employee in employees)
      {
        List<string> claimType = (await _userManager.GetClaimsAsync(employee)).Select(c => c.Type).ToList();

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
