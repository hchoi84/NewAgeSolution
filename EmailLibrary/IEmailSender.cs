using EmailSenderLibrary.Utilities;
using MimeKit;

namespace EmailSenderLibrary
{
  public interface IEmailSender
  {
    void SetConnectionInfo(EmailSenderServerEnum emailServer, string host, int port, string senderEmail, string senderPassword, string senderName, string websiteName);

    void GenerateTokenConfirmationContent(EmailSenderTypeEnum emailType, string toName, string toEmail, string tokenLink);

    public void SendEmail();
  }
}
