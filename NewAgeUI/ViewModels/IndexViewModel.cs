using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;

namespace NewAgeUI.ViewModels
{
  public class IndexViewModel
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

    public DateTime StartDate { get; set; }

    public string FullName 
    {
      get { return $"{ FirstName } { LastName }"; }
    }
  }
}
