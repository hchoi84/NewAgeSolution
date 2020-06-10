using EmailSenderLibrary.Securities;
using EmailSenderLibrary.Utilities;
using MimeKit;
using MailKit.Net.Smtp;
using System.Security.Authentication;

namespace EmailLibrary
{
  public class EmailSender
  {
    public void EmailConfirmationToken(string fullName, string email, string tokenLink, EmailType emailType)
    {
      string senderName = "NewAge Admin";

      MailboxAddress from = new MailboxAddress(senderName, EmailSecret.emailAddress);
      MailboxAddress to = new MailboxAddress(fullName, email);
      BodyBuilder bodyBuilder = new BodyBuilder();

      string emailSubject = GenerateEmail(fullName, tokenLink, emailType, bodyBuilder);
      
      SendEmail(emailSubject, from, to, bodyBuilder);
    }

    private string GenerateEmail(string fullName, string tokenLink, EmailType emailType, BodyBuilder bodyBuilder)
    {
      string websiteName = "NewAge";
      string emailSubject;
      if (emailType == EmailType.EmailConfirmation)
      {
        emailSubject = "New Registration Confirmation";
        bodyBuilder.HtmlBody =
          $"<h1>Hello {fullName} </h1> \n\n" +
          $"<p>You've recently registered for { websiteName }</p> \n\n" +
          "<p>Please click below to confirm your email address</p> \n\n" +
          $"<a href='{tokenLink}'><button style='color:#fff; background-color:#007bff; border-color:#007bff;'>Confirm</button></a> \n\n" +
          "<p>If the link doesn't work, you can copy and paste the below URL</p> \n\n" +
          $"<p> {tokenLink} </p> \n\n\n" +
          "<p>Thank you!</p>";
      }
      else
      {
        emailSubject = "Password Reset Request";
        bodyBuilder.HtmlBody =
          $"<h1>Hello {fullName} </h1> \n\n" +
          $"<p>You've recently requested for password reset</p> \n\n" +
          "<p>Please click below to reset your password</p> \n\n" +
          $"<a href='{tokenLink}'><button style='color:#fff; background-color:#007bff; border-color:#007bff;'>Confirm</button></a> \n\n" +
          "<p>If the link doesn't work, you can copy and paste the below URL</p> \n\n" +
          $"<p> {tokenLink} </p> \n\n\n" +
          "<p>Thank you!</p>";
      }

      return emailSubject;
    }

    private void SendEmail(string emailSubject, MailboxAddress from, MailboxAddress to, BodyBuilder bodyBuilder)
    {
      MimeMessage message = new MimeMessage();
      message.From.Add(from);
      message.To.Add(to);
      message.Subject = emailSubject;
      message.Body = bodyBuilder.ToMessageBody();

      using (SmtpClient client = new SmtpClient())
      {
        client.SslProtocols = SslProtocols.Tls;
        client.Connect(EmailSecret.host, EmailSecret.port, MailKit.Security.SecureSocketOptions.SslOnConnect);
        client.Authenticate(EmailSecret.emailAddress, EmailSecret.apiPassword);
        client.Send(message);
        client.Disconnect(true);
      }
    }
  }
}
