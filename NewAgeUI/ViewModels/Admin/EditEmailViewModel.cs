using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace NewAgeUI.ViewModels
{
  public class EditEmailViewModel
  {
    public string EmployeeId { get; set; }

    public string EmailAddress { get; set; }

    [Required]
    [Display(Name = "Email Address")]
    [DataType(DataType.EmailAddress)]
    [Remote("ValidateEmailAddress", "Account")]
    public string NewEmailAddress { get; set; }
  }
}
