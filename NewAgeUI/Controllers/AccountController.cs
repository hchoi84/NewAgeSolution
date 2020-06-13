using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NewAgeUI.Models;
using NewAgeUI.ViewModels;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace NewAgeUI.Controllers
{
  [AllowAnonymous]
  public class AccountController : Controller
  {
    private readonly UserManager<Employee> _userManager;
    private readonly SignInManager<Employee> _signInManager;
    private readonly IEmployee _employee;
    private readonly ILogger<AccountController> _logger;
    private readonly string _websiteName = "NewAge";

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

      var token = await _userManager.GenerateEmailConfirmationTokenAsync(employee);
      var tokenLink = Url.Action("ConfirmEmail", "Account", new { userId = employee.Id, token }, Request.Scheme);

      // TODO: Remove on production
      _logger.LogInformation(tokenLink);

      //EmailSender emailSender = new EmailSender(RackspaceSecret.EmailServer, RackspaceSecret.Host, RackspaceSecret.Port, RackspaceSecret.SenderEmail, RackspaceSecret.SenderPassword, _websiteName);

      //emailSender.SendConfirmationToken(EmailSenderTypeEnum.EmailConfirmation, $"{ websiteName } Admin", employee.FullName, employee.Email, tokenLink);

      GenerateToastMessage("Registration Success", "Please check your email for confirmation link");

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
        GenerateToastMessage("Error", "The email confirmation token link is invalid");

        return RedirectToAction("Login");
      }

      Employee employee = await _userManager.FindByIdAsync(userId);

      if (employee == null)
      {
        GenerateToastMessage("Error", "No user found");

        return RedirectToAction("Login");
      }

      IdentityResult result = await _userManager.ConfirmEmailAsync(employee, token);

      if (!result.Succeeded)
      {
        GenerateToastMessage("Error", "Something went wrong while confirming the email");

        return RedirectToAction("Login");
      }

      GenerateToastMessage("Email Confirmed", "You may now login");

      return RedirectToAction("Login");
    }
    #endregion

    #region Login
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
    #endregion

    #region ForgotPassword
    [HttpGet("/ForgotPassword")]
    public IActionResult ForgotPassword() => View();

    [HttpPost("/ForgotPassword")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel forgotPasswordViewModel)
    {
      if (!ModelState.IsValid) return View();

      Employee employee = await _userManager.FindByEmailAsync(forgotPasswordViewModel.EmailAddress);

      if (employee == null)
      {
        ModelState.AddModelError(string.Empty, "Invalid email address. Please ensure the email is correct and already registered");
        
        return View();
      }

      if (!employee.EmailConfirmed)
      {
        ModelState.AddModelError(string.Empty, "You have not confirmed your email yet");

        return View();
      }

      string token = await _userManager.GeneratePasswordResetTokenAsync(employee);

      var passwordResetLink = Url.Action("ResetPassword", "Account", new { emailAddress = forgotPasswordViewModel.EmailAddress, token }, Request.Scheme);

      // TODO: Remove on production
      _logger.LogInformation(passwordResetLink);
      //EmailSender emailSender = new EmailSender(EmailSenderServerEnum.Rackspace, RackspaceSecret.Host, RackspaceSecret.Port, RackspaceSecret.SenderEmail, RackspaceSecret.SenderPassword, _websiteName);

      //emailSender.SendConfirmationToken(EmailSenderTypeEnum.PasswordReset, $"{ _websiteName } Admin", employee.FullName, forgotPasswordViewModel.EmailAddress, passwordResetLink);

      GenerateToastMessage("Password Reset Email Sent", "Please check your email for password reset link");

      return RedirectToAction(nameof(Login));
    }

    [HttpGet("/ResetPassword")]
    public async Task<IActionResult> ResetPassword(string emailAddress, string token)
    {
      if (token == null || emailAddress == null)
      {
        GenerateToastMessage("Invalid Link", "The link to reset your password is invalid.");
        return RedirectToAction(nameof(Login));
      }

      Employee employee = await _userManager.FindByEmailAsync(emailAddress);
      
      bool isValidToken = await _userManager.VerifyUserTokenAsync(employee, TokenOptions.DefaultProvider, "ResetPassword", token);

      if (!isValidToken)
      {
        GenerateToastMessage("Invalid Token", "The token you provided is invalid");
        return RedirectToAction(nameof(Login));
      }
      
      return View();
    }

    [HttpPost("/ResetPassword")]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel resetPasswordViewModel)
    {
      if (!ModelState.IsValid) return View();

      Employee employee = await _userManager.FindByEmailAsync(resetPasswordViewModel.EmailAddress);

      IdentityResult identityResult = await _userManager.ResetPasswordAsync(employee, resetPasswordViewModel.Token, resetPasswordViewModel.Password);

      if (!identityResult.Succeeded) 
        GenerateToastMessage("Error", "Something went wrong and the password wasn't able to get reset. Please contact the Admin");
      else
        GenerateToastMessage("Success!", "Password has been reset sucessfully");

      return RedirectToAction(nameof(Login));
    }
    #endregion

    public async Task<IActionResult> Logout(string returnUrl = null)
    {
      await _signInManager.SignOutAsync();

      if (returnUrl != null)
        return LocalRedirect(returnUrl);
      else
        return RedirectToAction("Index", "Home");
    }

    public void GenerateToastMessage(string title, string message)
    {
      TempData["MessageTitle"] = title;
      TempData["Message"] = message;
    }
  }
}
