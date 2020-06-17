using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NewAgeUI.ViewModels
{
  public class ViewEmployeeViewModel
  {
    public string EmployeeId { get; set; }

    [Display(Name = "First Name")]
    public string FirstName { get; set; }

    [Display(Name = "Last Name")]
    [MaxLength(30)]
    public string LastName { get; set; }

    [Display(Name = "Email Address")]
    public string EmailAddress { get; set; }

    [Display(Name = "Start Date")]
    public DateTime StartDate { get; set; }

    [Display(Name = "Access Permission")]
    public string AccessPermission { get; set; }

    [Display(Name = "Office Location")]
    public string OfficeLocation { get; set; }
  }
}
