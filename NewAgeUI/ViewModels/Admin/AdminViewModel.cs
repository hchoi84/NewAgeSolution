using System;

namespace NewAgeUI.ViewModels
{
  public class AdminViewModel
  {
    public string EmployeeId { get; set; }
    public string FullName { get; set; }
    public string EmailAddress { get; set; }
    public bool IsEmailVerified { get; set; }
    public string AccessPermission { get; set; }
  }
}
