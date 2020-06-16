using System;
using System.Threading.Tasks;
using EmailSenderLibrary;
using EmailSenderLibrary.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NewAgeUI.Models;
using NewAgeUI.Securities;
using NewAgeUI.ViewModels;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace NewAgeUI.Controllers
{
  [Route("[Controller]")]
  public class AccountController : Controller
  {
    private readonly UserManager<Employee> _userManager;
    private readonly SignInManager<Employee> _signInManager;
    private readonly IEmployee _employee;
    private readonly ILogger<AccountController> _logger;
    private readonly IEmailSender _emailSender;
    private readonly string _websiteName = "NewAge";

    public AccountController(UserManager<Employee> userManager, SignInManager<Employee> signInManager, IEmployee employee, ILogger<AccountController> logger, IEmailSender emailSender)
    {
      _userManager = userManager;
      _signInManager = signInManager;
      _employee = employee;
      _logger = logger;
      _emailSender = emailSender;
    }

    #region Register
    [AllowAnonymous]
    [HttpGet("Register")]
    public IActionResult Register() => View();

    [AllowAnonymous]
    [HttpPost("Register")]
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
      //_logger.LogInformation(tokenLink);

      try
      {
        SendTokenConfirmationEmail(EmailSenderTypeEnum.EmailConfirmation, employee, tokenLink);
      }
      catch (Exception e)
      {
        _logger.LogError(e.Message);
        ModelState.AddModelError(string.Empty, "Something went wrong. Please contact the Admin");
        
        return View();
      }

      GenerateToastMessage("Registration Success", "Please check your email for confirmation link");

      return RedirectToAction("Login");
    }

    [AllowAnonymous]
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

    [AllowAnonymous]
    [HttpGet("EmailConfirmation")]
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
    [AllowAnonymous]
    [HttpGet("Login")]
    public IActionResult Login() => View();

    [AllowAnonymous]
    [HttpPost("Login")]
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
    [AllowAnonymous]
    [HttpGet("ForgotPassword")]
    public IActionResult ForgotPassword() => View();

    [AllowAnonymous]
    [HttpPost("ForgotPassword")]
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

      try
      {
        SendTokenConfirmationEmail(EmailSenderTypeEnum.PasswordReset, employee, passwordResetLink);
      }
      catch (Exception e)
      {
        _logger.LogError(e.Message);
        ModelState.AddModelError(string.Empty, "Something went wrong. Please contact the Admin");

        return View();
      }

      GenerateToastMessage("Password Reset Email Sent", "Please check your email for password reset link");

      return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet("ResetPassword")]
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

    [AllowAnonymous]
    [HttpPost("ResetPassword")]
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

    #region Profile
    [HttpGet("Profile")]
    public async Task<IActionResult> Profile()
    {
      Employee employee = await _userManager.GetUserAsync(User);

      ProfileViewModel profileViewModel = new ProfileViewModel
      {
        FirstName = employee.FirstName,
        LastName = employee.LastName,
        EmailAddress = employee.Email,
        IsEmailVerified = employee.EmailConfirmed,
        OfficeLocation = employee.OfficeLocation,
        StartDate = employee.StartDate.ToString("yyyy-MM-dd"),
      };

      return View(profileViewModel);
    }

    [HttpGet("ChangePassword")]
    public IActionResult ChangePassword() => View();

    [HttpPost("ChangePassword")]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel changePasswordViewModel)
    {
      if (!ModelState.IsValid) return View();

      Employee employee = await _userManager.GetUserAsync(User);

      bool isCorrectCurrentPassword = await _userManager.CheckPasswordAsync(employee, changePasswordViewModel.CurrentPassword);

      if (!isCorrectCurrentPassword)
      {
        ModelState.AddModelError(string.Empty, "Incorrect current password");
        
        return View();
      }

      IdentityResult result = await _userManager.ChangePasswordAsync(employee, changePasswordViewModel.CurrentPassword, changePasswordViewModel.NewPassword);

      if (!result.Succeeded)
      {
        ModelState.AddModelError(string.Empty, "Something went wrong.");

        return View();
      }

      TempData["Success"] = "Password has been updated successfully";

      return RedirectToAction(nameof(Profile));
    }
    #endregion

    [Authorize]
    public async Task<IActionResult> Logout(string returnUrl = null)
    {
      await _signInManager.SignOutAsync();

      if (returnUrl != null)
        return LocalRedirect(returnUrl);
      else
        return RedirectToAction("Login", "Account");
    }

    private void GenerateToastMessage(string title, string message)
    {
      TempData["MessageTitle"] = title;
      TempData["Message"] = message;
    }

    private void SendTokenConfirmationEmail(EmailSenderTypeEnum emailSenderType, Employee employee, string tokenLink)
    {
      _emailSender.SetConnectionInfo(RackspaceSecret.EmailServer, RackspaceSecret.Host, RackspaceSecret.Port, RackspaceSecret.SenderEmail, RackspaceSecret.SenderPassword, $"{ _websiteName } Admin", _websiteName);

      _emailSender.GenerateTokenConfirmationContent(emailSenderType, employee.FullName, employee.Email, tokenLink);

      _emailSender.SendEmail();
    }
  }
}
