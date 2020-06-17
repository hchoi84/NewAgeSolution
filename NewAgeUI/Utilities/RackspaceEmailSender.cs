using EmailSenderLibrary;
using NewAgeUI.Models;
using NewAgeUI.Securities;

namespace NewAgeUI.Utilities
{
  public class RackspaceEmailSender : IRackspace
  {
    private IEmailSender _emailSender;
    private readonly string _websiteName = "NewAge";

    public RackspaceEmailSender(IEmailSender emailSender)
    {
      _emailSender = emailSender;
    }

    public void SendEmail(Employee employee, string subject, string body)
    {
      _emailSender.SetConnectionInfo(RackspaceSecret.EmailServer, RackspaceSecret.Host, RackspaceSecret.Port, RackspaceSecret.SenderEmail, RackspaceSecret.SenderPassword, _websiteName);

      _emailSender.GenerateContent(employee.FullName, employee.Email, subject, body);

      _emailSender.SendEmail();
    }
  }
}
