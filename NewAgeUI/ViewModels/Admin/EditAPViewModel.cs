using System.Collections.Generic;
using System.Security.Claims;

namespace NewAgeUI.ViewModels
{
  public class EditAPViewModel
  {
    public string EmployeeId { get; set; }
    public List<string> ClaimTypes { get; set; } = new List<string>();
    public List<bool> ClaimValues { get; set; } = new List<bool>();
  }
}
