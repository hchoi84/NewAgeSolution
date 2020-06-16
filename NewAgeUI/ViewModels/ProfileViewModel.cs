using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;

namespace NewAgeUI.ViewModels
{
  public class ProfileViewModel
  {
    [Display(Name = "First Name")]
    public string FirstName { get; set; }

    [Display(Name = "Last Name")]
    public string LastName { get; set; }

    [Display(Name = "Office Location")]
    public string OfficeLocation { get; set; }

    [Display(Name = "Email Address")]
    public string EmailAddress { get; set; }

    public bool IsEmailVerified { get; set; }

    [Display(Name = "Start Date")]
    public string StartDate { get; set; }

    public string FullName 
    {
      get { return $"{ FirstName } { LastName }"; }
    }
  }
}
