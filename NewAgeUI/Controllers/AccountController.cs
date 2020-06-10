using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NewAgeUI.Controllers
{
  [AllowAnonymous]
  public class AccountController : Controller
  {
    [HttpGet("/Register")]
    public IActionResult Register()
    {
      return View();
    }

    //[HttpPost("/Register")]
    //public IActionResult Register()
    //{
    //  return RedirectToAction("Index", "Home");
    //}

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
