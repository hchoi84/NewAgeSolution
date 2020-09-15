using EmailSenderLibrary.Utilities;
using MimeKit;

namespace EmailSenderLibrary
{
  public interface IEmailSender
  {
    MimeMessage GenerateContent(string toName, string toEmail, string subject, string body);
    void SendEmail(MimeMessage message);
    string GetDoamin();
  }
}
