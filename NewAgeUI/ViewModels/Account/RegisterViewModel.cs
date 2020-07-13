using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace NewAgeUI.ViewModels
{
  public class RegisterViewModel
  {
    [Required]
    [Display(Name = "First Name")]
    [StringLength(30, MinimumLength = 2, ErrorMessage = "{0} must be between {2} to {1} characters")]
    [MaxLength(30)]
    public string FirstName { get; set; }

    [Required]
    [Display(Name = "Last Name")]
    [StringLength(30, MinimumLength = 2, ErrorMessage = "{0} must be between {2} to {1} characters")]
    [MaxLength(30)]
    public string LastName { get; set; }

    [Required]
    [Display(Name = "Office Location")]
    [MaxLength(5)]
    public string OfficeLocation { get; set; }

    [Required]
    [Display(Name = "Email Address")]
    [DataType(DataType.EmailAddress)]
    [Remote("ValidateEmailAddress", "Account")]
    public string EmailAddress { get; set; }

    [Required]
    [Display(Name = "Password")]
    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "{0} must be at least {1} characters long and contain lower, upper, digit, and special character")]
    public string Password { get; set; }

    [Required]
    [Display(Name = "Confirm Password")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Does not match with password")]
    public string ConfirmPassword { get; set; }
  }
}
