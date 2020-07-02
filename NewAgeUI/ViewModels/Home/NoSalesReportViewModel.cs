using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;

namespace NewAgeUI.ViewModels
{
  public class NoSalesReportViewModel
  {
    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Last Sold Date")]
    [Remote("ValidateDate", "Home")]
    public DateTime LastSoldDate { get; set; }
  }
}
