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
using EmailSenderLibrary;
using FileReaderLibrary;
using FileReaderLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewAgeUI.BackgroundServices;
using NewAgeUI.Models;
using NewAgeUI.ViewModels;
using Newtonsoft.Json.Linq;
using SkuVaultLibrary;

namespace NewAgeUI.Controllers
{
  //[AllowAnonymous]
  public class HomeController : Controller
  {
    private readonly ILogger<HomeController> _logger;
    private readonly IChannelAdvisor _channelAdvisor;
    private readonly ISkuVault _skuVault;
    private readonly IFileReader _fileReader;
    private readonly UserManager<Employee> _userManager;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private readonly IEmailSender _emailSender;

    public HomeController(ILogger<HomeController> logger, IChannelAdvisor channelAdvisor, ISkuVault skuVault, IFileReader fileReader, UserManager<Employee> userManager, IBackgroundTaskQueue backgroundTaskQueue, IEmailSender emailSender)
    {
      _logger = logger;
      _channelAdvisor = channelAdvisor;
      _skuVault = skuVault;
      _fileReader = fileReader;
      _userManager = userManager;
      _backgroundTaskQueue = backgroundTaskQueue;
      _emailSender = emailSender;
    }

    [HttpGet("")]
    public IActionResult Index() => View();

    // NoSalesReport
    [HttpGet("NoSalesReport")]
    public IActionResult NoSalesReport() => View();

    [HttpPost("NoSalesReport")]
    public async Task<IActionResult> NoSalesReport(DateTime lastSoldDate)
    {
      IEnumerable<NoSalesReportModel> model = await _channelAdvisor.GetNoSalesReport(lastSoldDate);

      List<string> lines = new List<string>();

      foreach (var p in model)
      {
        lines.Add($"{ p.Sku },{ p.UPC },{p.CreateDateUtc:yyyy-MM-dd},\"{ p.AllName }\",{p.LastSaleDateUtc:yyyy-MM-dd},{ p.ProductLabel },{ p.WHQuantity },{ p.FBAQuantity },{ p.StoreQty }");
      }

      string header = "SKU,UPC,Created,All Name,Last Sold Date,Label,WH Qty,FBA Qty,STR Qty";
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

    // BufferSetter
    [HttpGet("BufferSetter")]
    public IActionResult BufferSetter() => View();

    [HttpPost("BufferSetter")]
    public async Task<IActionResult> BufferSetter(FileImportViewModel model)
    {
      if (!ModelState.IsValid)
      {
        return View();
      }

      string fileExtension = Path.GetExtension(model.CSVFile.FileName);
      if (fileExtension != ".csv")
      {
        ModelState.AddModelError("", "File must be a CSV type");
        return View();
      }

      Dictionary<string, int> fromFile = await _fileReader.RetrieveSkuAndQty(model.CSVFile);
      await _backgroundTaskQueue.Enqueue(GenerateBufferImportFile, fromFile, model.Email);

      TempData["Message"] = "In progress. Completed file will be emailed to you. This task can take up to 30 minutes";
      return RedirectToAction("Index");
    }

    private async ValueTask GenerateBufferImportFile(Dictionary<string, int> file, string email)
    {
      _logger.LogInformation("Beginning CA fetch");
      List<JObject> fromCA = await _channelAdvisor.GetForBufferAsync();
      _logger.LogInformation("Ended CA fetch");
      _logger.LogInformation("Beginning Import File Generator");
      StringBuilder sb = _fileReader.GenerateBufferImportSB(file, fromCA);
      _logger.LogInformation("Ended Import File Generator");

      byte[] fileContent = new UTF8Encoding().GetBytes(sb.ToString());

      _logger.LogInformation($"Sending email to {email}");
      string subject = "Your buffer import file is ready";
      _emailSender.SendEmail(
        _emailSender.GenerateContent("Importer", email, "Buffer Import File", subject, "StoreBufferImport.csv", fileContent));
    }

    // DropShipUpdater
    [HttpGet("DropShipUpdater")]
    public IActionResult DropShipUpdater() => View();

    [HttpPost("DropShipUpdater")]
    public async Task<IActionResult> DropShipUpdaterBatch()
    {
      IEnumerable<JObject> jObjects;
      try
      {
        jObjects = await GetProductsToUpdateAsync();
      }
      catch (Exception e)
      {
        return Json(e.Message);
      }

      List<string> lines = new List<string>();
      Dictionary<string, int> skuAndNewQty = new Dictionary<string, int>();
      GenerateLinesAndNewQty(jObjects, lines, skuAndNewQty);

      await _skuVault.UpdateDropShip(skuAndNewQty);

      string header = "SKU,InvFlag,Label,All Name,Qty";
      StringBuilder sb = _fileReader.GenerateSB(true, header, lines);

      byte[] fileContents = new UTF8Encoding().GetBytes(sb.ToString());
      string contentType = "text/csv";
      string fileName = $"DropShipUpdate-{ DateTime.Now.ToShortDateString() }.csv";
      FileContentResult file = File(fileContents, contentType, fileName);

      return file;
    }

    private async Task<IEnumerable<JObject>> GetProductsToUpdateAsync()
    {
      string filterBase = $"ProfileId eq { _channelAdvisor.GetMainProfileId() } and Attributes/Any (c:c/Name eq 'invflag' and c/Value eq";
      string taq = "TotalAvailableQuantity";
      string[] filters =
      {
        $"{ filterBase } 'Green') and { taq } le 0",
        $"{ filterBase } 'Green') and { taq } ge 15000 and { taq } lt 19999",
        $"{ filterBase } 'Green') and { taq } gt 19999",
        $"{ filterBase } 'Red') and { taq } ge 15000",
      };
      string expand = "Attributes,Labels";
      string select = $"Sku,{ taq }";

      List<JObject> jObjects = new List<JObject>();
      for (var i = 0; i < filters.Length; i++)
      {
        try
        {
          jObjects.AddRange(await _channelAdvisor.GetProductsAsync(filters[i], expand, select));
        }
        catch (Exception e)
        {
          throw new Exception(e.Message, e);
        }
      }

      return jObjects;
    }

    private void GenerateLinesAndNewQty(IEnumerable<JObject> jObjects, List<string> lines, Dictionary<string, int> skuAndNewQty)
    {
      string[] _labelNames = { "Closeout", "Discount", "MAPNoShow", "MAPShow", "NPIP" };

      foreach (var jObject in jObjects)
      {
        JToken label = jObject["Labels"].FirstOrDefault(i => _labelNames.Contains(i["Name"].ToString()));
        string labelValue = label != null ? label["Name"].ToString() : string.Empty;
        if (string.IsNullOrEmpty(labelValue)) continue;

        string sku = jObject["Sku"].ToString();
        string invFlag = jObject["Attributes"]
          .FirstOrDefault(i => i["Name"].ToString() == "invflag")["Value"]
          .ToString();
        string allName = jObject["Attributes"]
          .FirstOrDefault(i => i["Name"].ToString() == "All Name")["Value"]
          .ToString();
        int qty = jObject["TotalAvailableQuantity"].ToObject<int>();

        lines.Add($"{ sku },{ invFlag },{ labelValue },\"{ allName }\",{ qty }");

        int newQty = -1;
        if (invFlag == "Green")
        {
          if (qty <= 0 || (qty > 15000 && qty < 19999)) newQty = 19999;
          else if (qty > 19999) newQty = 0;
        }
        else if (invFlag == "Red" && qty > 15000) newQty = 0;

        if (newQty != -1) skuAndNewQty.Add(sku, newQty);
      }
    }

    // ZDTSummarizer
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

      List<ZDTSummaryModel> summary = await _fileReader.SummarizeAsync(model.CSVFile);

      string header = "Date,Count,Avg Wait Sec,Avg Talk Sec";
      List<string> lines = summary.Select(i => $"{ i.CallDate },{ i.Count },{ i.AvgWaitSec },{ i.AvgTalkSec }").ToList();
      StringBuilder sb = _fileReader.GenerateSB(true, header, lines);

      byte[] fileContents = new UTF8Encoding().GetBytes(sb.ToString());
      string contentTypes = "text/csv";
      string fileName = $"ZendeskTalkSummyar-{ DateTime.Now.ToShortDateString() }.csv";
      FileContentResult file = File(fileContents, contentTypes, fileName);

      return file;
    }

    // Other
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
