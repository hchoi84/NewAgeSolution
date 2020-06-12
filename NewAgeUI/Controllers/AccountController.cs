using System.Threading.Tasks;
using EmailSenderLibrary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NewAgeUI.Models;
using NewAgeUI.Securities;
using NewAgeUI.ViewModels;
using EmailSenderLibrary.Utilities;

namespace NewAgeUI.Controllers
{
  [AllowAnonymous]
  public class AccountController : Controller
  {
    private readonly UserManager<Employee> _userManager;
    private readonly IEmployee _employee;
    private readonly ILogger<AccountController> _logger;

    public AccountController(UserManager<Employee> userManager, IEmployee employee, ILogger<AccountController> logger)
    {
      _userManager = userManager;
      _employee = employee;
      _logger = logger;
    }

    [HttpGet("/Register")]
    public IActionResult Register() => View();

    [HttpPost("/Register")]
    public async Task<IActionResult> RegisterAsync(RegisterViewModel registerViewModel)
    {
      if (!ModelState.IsValid) return View();

      (Employee employee, string message) = await _employee.RegisterUserAsync(registerViewModel);

      if (employee == null)
      {
        ModelState.AddModelError(string.Empty, message);

        return View();
      }

      string websiteName = "NewAge";

      var token = await _userManager.GenerateEmailConfirmationTokenAsync(employee);
      var tokenLink = Url.Action("ConfirmEmail", "Account", new { userId = employee.Id, token }, Request.Scheme);

      EmailSender emailSender = new EmailSender(RackspaceSecret.EmailServer, RackspaceSecret.Host, RackspaceSecret.Port, RackspaceSecret.SenderEmail, RackspaceSecret.SenderPassword, websiteName);

      emailSender.SendConfirmationToken(EmailSenderTypeEnum.EmailConfirmation, $"{ websiteName } Admin", employee.FullName, employee.Email, tokenLink);

      GenerateMessage("Registration Success", "Please check your email for confirmation link");

      return RedirectToAction("Login");
    }

    [AcceptVerbs("GET", "POST")]
    public async Task<IActionResult> ValidateEmailAddress(string emailAddress)
    {
      string validDomain = "golfio.com";
      string userEnteredDomain = emailAddress.Split('@')[1].ToLower();

      if (userEnteredDomain != validDomain) 
        return Json($"Only { validDomain } email addresses are allowed");

      var user = await _userManager.FindByEmailAsync(emailAddress);

      if (user != null) 
        return Json("Email address already in use");

     return Json(true);
    }

    [HttpGet("/EmailConfirmation")]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
      if (userId == null || token == null)
      {
        GenerateMessage("Error", "The email confirmation token link is invalid");

        return RedirectToAction("Login");
      }

      Employee employee = await _userManager.FindByIdAsync(userId);

      if (employee == null)
      {
        GenerateMessage("Error", "No user found");

        return RedirectToAction("Login");
      }

      IdentityResult result = await _userManager.ConfirmEmailAsync(employee, token);

      if (!result.Succeeded)
      {
        GenerateMessage("Error", "Something went wrong while confirming the email");

        return RedirectToAction("Login");
      }

      GenerateMessage("Email Confirmed", "You may now login");

      return RedirectToAction("Login");
    }

    [HttpGet("/Login")]
    public IActionResult Login()
    {
      return View();
    }

    //[HttpPost("/Login")]
    //public IActionResult Login()
    //{
        // Store full name in sessions with key "FullName"
    //}

    public void GenerateMessage(string title, string message)
    {
      TempData["MessageTitle"] = title;
      TempData["Message"] = message;
    }
  }
}
