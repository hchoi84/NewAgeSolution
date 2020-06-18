using EmailSenderLibrary.Utilities;
using MimeKit;

namespace EmailSenderLibrary
{
  public interface IEmailSender
  {
    public void SetConnectionInfo(EmailSenderServerEnum emailServer, string host, int port, string senderEmail, string senderPassword, string websiteName);

    public void GenerateContent(string toName, string toEmail, string subject, string body);

    public void SendEmail();
  }
}
