using System.ComponentModel.DataAnnotations;

namespace NewAgeUI.ViewModels
{
  public class ResetPasswordViewModel
  {
    public string EmailAddress { get; set; }
    public string Token { get; set; }

    [Required]
    [Display(Name = "Password")]
    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "{0} must be at least {1} characters long and contain lower, upper, digit, and non-alphaneumeric")]
    public string Password { get; set; }

    [Required]
    [Display(Name = "Confirm Password")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Does not match with Password")]
    public string ConfirmPassword { get; set; }
  }
}
