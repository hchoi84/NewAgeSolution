using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewAgeUI.Models;
using NewAgeUI.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NewAgeUI.Controllers
{
  [Authorize(Policy = "Admin")]
  [Route("Admin")]
  public class AdminController : Controller
  {
    private readonly UserManager<Employee> _userManager;

    public AdminController(UserManager<Employee> userManager)
    {
      _userManager = userManager;
    }

    [Authorize(Policy = "Admin")]
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

    [Authorize(Policy = "Admin")]
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

    [Authorize(Policy = "Admin")]
    [HttpGet("{employeeId}/EditName")]
    public async Task<IActionResult> EditName(string employeeId)
    {
      Employee employee = await _userManager.FindByIdAsync(employeeId);

      EditEmployeeNameViewModel model = new EditEmployeeNameViewModel
      {
        EmployeeId = employee.Id,
        FirstName = employee.FirstName,
        LastName = employee.LastName
      };

      return View(model);
    }

    [Authorize(Policy = "Admin")]
    [HttpGet("{employeeId}/EditEmail")]
    public async Task<IActionResult> EditEmail(string employeeId)
    {

      return View();
    }

    [Authorize(Policy = "Admin")]
    [HttpGet("{employeeId}/EditAP")]
    public async Task<IActionResult> EditAP(string employeeId)
    {

      return View();
    }
  }
}
