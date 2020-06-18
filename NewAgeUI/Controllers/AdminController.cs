using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewAgeUI.Models;
using NewAgeUI.Utilities;
using NewAgeUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace NewAgeUI.Controllers
{
  [Authorize(Policy = "Admin")]
  [Route("Admin")]
  public class AdminController : Controller
  {
    private readonly UserManager<Employee> _userManager;
    private readonly ILogger<AdminController> _logger;
    private readonly IRackspace _rackspace;

    public AdminController(UserManager<Employee> userManager, ILogger<AdminController> logger, IRackspace rackspace)
    {
      _userManager = userManager;
      _logger = logger;
      _rackspace = rackspace;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
      List<Employee> employees = await _userManager.Users.ToListAsync();
      List<AdminViewModel> adminViewModels = new List<AdminViewModel>();

      foreach (Employee employee in employees)
      {
        List<string> claimType = new List<string>();

        (await _userManager.GetClaimsAsync(employee)).ToList().ForEach(c => claimType.Add(c.Type));

        AdminViewModel adminViewModel = new AdminViewModel
        {
          EmployeeId = employee.Id,
          FullName = employee.FullName,
          EmailAddress = employee.Email,
          IsEmailVerified = employee.EmailConfirmed,
          AccessPermission = string.Join(", ", claimType),
        };

        adminViewModels.Add(adminViewModel);
      }

      return View(adminViewModels);
    }

    [HttpGet("{employeeId}")]
    public async Task<IActionResult> ViewEmployee(string employeeId)
    {
      Employee employee = await _userManager.FindByIdAsync(employeeId);

      List<string> claimType = new List<string>();

      (await _userManager.GetClaimsAsync(employee)).ToList().ForEach(c => claimType.Add(c.Type));

      ViewEmployeeViewModel model = new ViewEmployeeViewModel
      {
        EmployeeId = employee.Id,
        FirstName = employee.FirstName,
        LastName = employee.LastName,
        EmailAddress = employee.Email,
        StartDate = employee.StartDate,
        AccessPermission = string.Join(", ", claimType),
        OfficeLocation = employee.OfficeLocation
      };

      return View(model);
    }

    [HttpGet("{employeeId}/EditName")]
    public async Task<IActionResult> EditName(string employeeId)
    {
      Employee employee = await _userManager.FindByIdAsync(employeeId);

      EditNameViewModel model = new EditNameViewModel
      {
        EmployeeId = employee.Id,
        FirstName = employee.FirstName,
        LastName = employee.LastName
      };

      return View(model);
    }

    [HttpPost("{employeeId}/EditName")]
    public async Task<IActionResult> EditName(EditNameViewModel model)
    {
      if (!ModelState.IsValid) return View();

      Employee employee = await _userManager.FindByIdAsync(model.EmployeeId);

      employee.FirstName = model.FirstName;
      employee.LastName = model.LastName;

      IdentityResult result = await _userManager.UpdateAsync(employee);

      if (!result.Succeeded)
      {
        ModelState.AddModelError(string.Empty, "Something went wrong updating");
        return View();
      }

      return RedirectToAction(nameof(ViewEmployee), new { employeeId = model.EmployeeId });
    }

    [HttpGet("{employeeId}/EditEmail")]
    public async Task<IActionResult> EditEmail(string employeeId)
    {
      Employee employee = await _userManager.FindByIdAsync(employeeId);

      EditEmailViewModel model = new EditEmailViewModel
      {
        EmployeeId = employee.Id,
        EmailAddress = employee.Email
      };

      return View(model);
    }

    [HttpPost("{employeeId}/EditEmail")]
    public async Task<IActionResult> EditEmail(EditEmailViewModel model)
    {
      if (!ModelState.IsValid) return View();

      Employee employee = await _userManager.FindByIdAsync(model.EmployeeId);

      string token = await _userManager.GenerateChangeEmailTokenAsync(employee, model.NewEmailAddress);

      string tokenLink = Url.Action("ConfirmEmailChange", "Account", new { userId = model.EmployeeId, newEmail = model.NewEmailAddress, token }, Request.Scheme);

      string subject = "Email Change Request Confirmation";

      string body = $"<h1>Hello { employee.FullName } </h1> \n\n" +
          $"<p>You've recently requested for an email change from { employee.Email } to { model.NewEmailAddress }</p> \n\n" +
          "<p>Please click below to confirm</p> \n\n" +
          $"<a href='{ tokenLink }'><button style='color:#fff; background-color:#007bff; border-color:#007bff;'>Confirm</button></a> \n\n" +
          "<p>If the link doesn't work, you can copy and paste the below URL</p> \n\n" +
          $"<p> { tokenLink } </p> \n\n\n" +
          "<p>Thank you!</p>";

      try
      {
        _rackspace.SendEmail(employee, subject, body);
      }
      catch (Exception e)
      {
        _logger.LogError(e.Message);
        ModelState.AddModelError(string.Empty, "Something went wrong. Please contact the Admin");

        return View();
      }

      return RedirectToAction(nameof(ViewEmployee), new { employeeId = model.EmployeeId });
    }

    [HttpGet("{employeeId}/EditAP")]
    public async Task<IActionResult> EditAP(string employeeId)
    {
      Employee employee = await _userManager.FindByIdAsync(employeeId);

      EditAPViewModel model = new EditAPViewModel() { EmployeeId = employeeId };

      List<Claim> userClaims = (await _userManager.GetClaimsAsync(employee)).ToList();

      string[] availableClaimTypes = Enum.GetNames(typeof(ClaimTypeEnum));

      foreach (var availableClaimType in availableClaimTypes)
      {
        if (userClaims.Exists(c => c.Type.ToString() == availableClaimType))
        {
          model.ClaimTypes.Add(availableClaimType);
          model.ClaimValues.Add(true);
          continue;
        }

        model.ClaimTypes.Add(availableClaimType);
        model.ClaimValues.Add(false);
      }

      return View(model);
    }

    [HttpPost("{employeeId}/EditAP")]
    public async Task<IActionResult> EditAP(EditAPViewModel model)
    {
      Employee employee = await _userManager.FindByIdAsync(model.EmployeeId);
      List<Claim> userClaims = (await _userManager.GetClaimsAsync(employee)).ToList();

      bool isGivenAdminAccess = model.ClaimTypes.Contains(ClaimTypeEnum.Admin.ToString());

      if (isGivenAdminAccess)
      {
        bool isInUserClaims = userClaims.Exists(c => c.Type == ClaimTypeEnum.Admin.ToString());

        if (!isInUserClaims)
        {
          await _userManager.RemoveClaimsAsync(employee, userClaims);
          await _userManager.AddClaimAsync(employee, new Claim(ClaimTypeEnum.Admin.ToString(), "true"));
        }
      }
      else
      {
        for (var i = 0; i < model.ClaimTypes.Count; i++)
        {
          bool isInUserClaims = userClaims.Exists(c => c.Type == model.ClaimTypes[i]);

          if (model.ClaimValues[i] == true)
          {
            if (isInUserClaims) continue;

            await _userManager.AddClaimAsync(employee, new Claim(model.ClaimTypes[i], "true"));
          }
          else
          {
            if (!isInUserClaims) continue;

            Claim claim = userClaims.FirstOrDefault(c => c.Type == model.ClaimTypes[i]);
            await _userManager.RemoveClaimAsync(employee, claim);
          }
        }
      }

      return RedirectToAction(nameof(ViewEmployee), new { employeeId = model.EmployeeId });
    }
  }
}
