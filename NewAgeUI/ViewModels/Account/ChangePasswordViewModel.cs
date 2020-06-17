using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NewAgeUI.ViewModels
{
  public class ChangePasswordViewModel
  {
    [Required]
    [Display(Name = "Current Password")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; }

    [Required]
    [Display(Name = "New Password")]
    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "{0} must be at least {1} characters long and contain lower, upper, digit, and non-alphaneumeric")]
    public string NewPassword { get; set; }

    [Required]
    [Display(Name = "Confirm New Password")]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Does not match with password")]
    public string ConfirmNewPassword { get; set; }
  }
}
