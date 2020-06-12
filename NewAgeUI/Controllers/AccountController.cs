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
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Linq;

namespace NewAgeUI.Controllers
{
  [AllowAnonymous]
  public class AccountController : Controller
  {
    private readonly UserManager<Employee> _userManager;
    private readonly SignInManager<Employee> _signInManager;
    private readonly IEmployee _employee;
    private readonly ILogger<AccountController> _logger;

    public AccountController(UserManager<Employee> userManager, SignInManager<Employee> signInManager, IEmployee employee, ILogger<AccountController> logger)
    {
      _userManager = userManager;
      _signInManager = signInManager;
      _employee = employee;
      _logger = logger;
    }

    #region Register
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

      // TODO: Remove on production
      _logger.LogInformation(tokenLink);

      //EmailSender emailSender = new EmailSender(RackspaceSecret.EmailServer, RackspaceSecret.Host, RackspaceSecret.Port, RackspaceSecret.SenderEmail, RackspaceSecret.SenderPassword, websiteName);

      //emailSender.SendConfirmationToken(EmailSenderTypeEnum.EmailConfirmation, $"{ websiteName } Admin", employee.FullName, employee.Email, tokenLink);

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

      Employee employee = await _userManager.FindByEmailAsync(emailAddress);

      if (employee != null)
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
    #endregion

    [HttpGet("/Login")]
    public IActionResult Login() => View();

    [HttpPost("/Login")]
    public async Task<IActionResult> Login(LoginViewModel loginViewModel, string returnUrl)
    {
      if (!ModelState.IsValid) return View();

      SignInResult signInResult = await _signInManager.PasswordSignInAsync(loginViewModel.EmailAddress, loginViewModel.Password, loginViewModel.RememberMe, false);

      if (signInResult.Succeeded)
      {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
          return Redirect(returnUrl);
        else 
          return RedirectToAction("Index", "Home");
      }

      ModelState.AddModelError(string.Empty, "Invalid login attempt");
      return View();
    }

    public async Task<IActionResult> Logout(string returnUrl = null)
    {
      await _signInManager.SignOutAsync();

      if (returnUrl != null)
        return LocalRedirect(returnUrl);
      else
        return RedirectToAction("Index", "Home");
    }

    public void GenerateMessage(string title, string message)
    {
      TempData["MessageTitle"] = title;
      TempData["Message"] = message;
    }
  }
}
