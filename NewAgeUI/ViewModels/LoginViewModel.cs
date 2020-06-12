using System.ComponentModel.DataAnnotations;

namespace NewAgeUI.ViewModels
{
  public class LoginViewModel
  {
    [Required]
    [Display(Name = "Email Address")]
    public string EmailAddress { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [Display(Name = "Remember Me")]
    public bool RememberMe { get; set; }
  }
}
