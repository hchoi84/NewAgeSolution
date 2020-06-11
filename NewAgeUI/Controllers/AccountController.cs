using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NewAgeUI.Models;
using NewAgeUI.ViewModels;

namespace NewAgeUI.Controllers
{
  [AllowAnonymous]
  public class AccountController : Controller
  {
    private readonly UserManager<Employee> _userManager;

    public AccountController(UserManager<Employee> userManager)
    {
      _userManager = userManager;
    }

    [HttpGet("/Register")]
    public IActionResult Register()
    {
      return View();
    }

    [HttpPost("/Register")]
    public IActionResult Register(RegisterViewModel registerViewModel)
    {
      if (ModelState.IsValid)
      {
        return RedirectToAction("Index", "Home");
      }

      return View();
    }

    [AcceptVerbs("GET", "POST")]
    public async Task<IActionResult> IsEmailInUse(string emailAddress)
    {
      var user = await _userManager.FindByEmailAsync(emailAddress);

      if (user == null)
      {
        return Json(true);
      }
      else
      {
        return Json($"Email is already in use");
      }
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
  }
}
