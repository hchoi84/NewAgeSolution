using NewAgeUI.Models;

namespace NewAgeUI.Utilities
{
  public interface IRackspace
  {
    void SendEmail(Employee employee, string subject, string body);
  }
}
