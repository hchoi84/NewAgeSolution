using Microsoft.AspNetCore.Identity;
using NewAgeUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace NewAgeUI.Models
{
  public class SqlEmployee : IEmployee
  {
    private readonly UserManager<Employee> _userManager;

    public SqlEmployee(UserManager<Employee> userManager)
    {
      _userManager = userManager;
    }

    public object ClaimType { get; private set; }

    public async Task<(Employee, string)> RegisterUserAsync(RegisterViewModel registerViewModel)
    {
      Claim newClaim;

      Employee employee = new Employee
      {
        FirstName = registerViewModel.FirstName,
        LastName = registerViewModel.LastName,
        Email = registerViewModel.EmailAddress,
        UserName = registerViewModel.EmailAddress,
        OfficeLocation = Enum.GetName(typeof(OfficeLocationEnum), Int32.Parse(registerViewModel.OfficeLocation))
      };

      bool isFirstUser = !_userManager.Users.Any();

      IdentityResult identityResult = await _userManager.CreateAsync(employee, registerViewModel.Password);

      if (!identityResult.Succeeded) return (null, "Failed to create User");

      if (isFirstUser)
        newClaim = new Claim(ClaimTypeEnum.Admin.ToString(), "true");
      else
        newClaim = new Claim(ClaimTypeEnum.User.ToString(), "true");

      identityResult = await _userManager.AddClaimAsync(employee, newClaim);

      if (!identityResult.Succeeded) return (null, "Failed to add Claim");

      return (employee, "Success");
    }
  }
}
