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

    [HttpGet("/Login")]
    public IActionResult Login()
    {
      return View();
    }
  }
}
