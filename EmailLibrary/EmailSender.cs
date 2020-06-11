using EmailSenderLibrary.Utilities;
using MimeKit;
using MailKit.Net.Smtp;
using System.Security.Authentication;

namespace EmailSenderLibrary
{
  public class EmailSender
  {
    private readonly EmailServerEnum _emailServer;
    private readonly string _host;
    private readonly int _port;
    private readonly string _senderEmail;
    private readonly string _senderPassword;
    private readonly string _websiteName;

    public EmailSender(EmailServerEnum emailServer, string host, int port, string senderEmail, string senderPassword, string websiteName)
    {
      _emailServer = emailServer;
      _host = host;
      _port = port;
      _senderEmail = senderEmail;
      _senderPassword = senderPassword;
      _websiteName = websiteName;
    }

    public void SendConfirmationToken(EmailTypeEnum emailType, string fromName, string toName, string toEmail, string tokenLink)
    {
      MailboxAddress from = new MailboxAddress(fromName, _senderEmail);
      MailboxAddress to = new MailboxAddress(toName, toEmail);

      MimeMessage message = new MimeMessage();
      message.From.Add(from);
      message.To.Add(to);
      
      GenerateEmail(emailType, toName, tokenLink, message);

      SendEmail(message);
    }

    private void GenerateEmail(EmailTypeEnum emailType, string toName, string tokenLink, MimeMessage message)
    {
      string subject;
      BodyBuilder bodyBuilder = new BodyBuilder();

      if (emailType == EmailTypeEnum.EmailConfirmation)
      {
        subject = "New Registration Confirmation";
        bodyBuilder.HtmlBody =
          $"<h1>Hello {toName} </h1> \n\n" +
          $"<p>You've recently registered on { _websiteName }</p> \n\n" +
          "<p>Please click below to confirm your email address</p> \n\n" +
          $"<a href='{tokenLink}'><button style='color:#fff; background-color:#007bff; border-color:#007bff;'>Confirm</button></a> \n\n" +
          "<p>If the link doesn't work, you can copy and paste the below URL</p> \n\n" +
          $"<p> {tokenLink} </p> \n\n\n" +
          "<p>Thank you!</p>";
      }
      else
      {
        subject = "Password Reset Request";
        bodyBuilder.HtmlBody =
          $"<h1>Hello {toName} </h1> \n\n" +
          $"<p>You've recently requested for password reset</p> \n\n" +
          "<p>Please click below to reset your password</p> \n\n" +
          $"<a href='{tokenLink}'><button style='color:#fff; background-color:#007bff; border-color:#007bff;'>Confirm</button></a> \n\n" +
          "<p>If the link doesn't work, you can copy and paste the below URL</p> \n\n" +
          $"<p> {tokenLink} </p> \n\n\n" +
          "<p>Thank you!</p>";
      }

      message.Subject = subject;
      message.Body = bodyBuilder.ToMessageBody();
    }

    private void SendEmail(MimeMessage message)
    {
      using (SmtpClient client = new SmtpClient())
      {
        if (_emailServer == EmailServerEnum.Rackspace)
        {
          client.CheckCertificateRevocation = false;
          client.SslProtocols = SslProtocols.Tls;
        }
        client.Connect(_host, _port, MailKit.Security.SecureSocketOptions.SslOnConnect);
        client.Authenticate(_senderEmail, _senderPassword);
        client.Send(message);
        client.Disconnect(true);
      }
    }
  }
}
