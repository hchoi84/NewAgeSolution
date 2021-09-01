using EmailSenderLibrary.Utilities;
using MimeKit;
using System.IO;

namespace EmailSenderLibrary
{
  public interface IEmailSender
  {
    MimeMessage GenerateContent(string toName, string toEmail, string subject, string body, string attachmentName = null, byte[] attachment = null);
    void SendEmail(MimeMessage message);
    string GetDoamin();
  }
}
