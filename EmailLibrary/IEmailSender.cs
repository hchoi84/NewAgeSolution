using EmailSenderLibrary.Utilities;
using MimeKit;

namespace EmailSenderLibrary
{
  public interface IEmailSender
  {
    public MimeMessage GenerateContent(string toName, string toEmail, string subject, string body);
    public void SendEmail(MimeMessage message);
    public string GetDoamin();
  }
}
