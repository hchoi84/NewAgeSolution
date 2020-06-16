using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NewAgeUI.Models
{
  public class Employee : IdentityUser
  {
    [Required]
    [Display(Name = "First Name")]
    [StringLength(30, MinimumLength = 2, ErrorMessage = "Value must be between {2} to {1} characters")]
    [MaxLength(30)]
    public string FirstName { get; set; }

    [Required]
    [Display(Name = "Last Name")]
    [StringLength(30, MinimumLength = 2, ErrorMessage = "Value must be between {2} to {1} characters")]
    [MaxLength(30)]
    public string LastName { get; set; }

    [Required]
    [Display(Name = "Office Location")]
    [MaxLength(5)]
    public string OfficeLocation { get; set; }

    public DateTime StartDate { get; set; } = DateTime.Now;
    public DateTime EndDate { get; set; } = DateTime.Now;

    public string FullName 
    { 
      get { return $"{ FirstName } { LastName }"; }
    }
  }
}
