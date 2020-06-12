using Microsoft.AspNetCore.Identity;
using NewAgeUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NewAgeUI.Models
{
  public interface IEmployee
  {
    Task<(Employee, string)> RegisterUserAsync(RegisterViewModel registerViewModel);
  }
}
