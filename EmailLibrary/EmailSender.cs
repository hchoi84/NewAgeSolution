using EmailSenderLibrary.Models;
using EmailSenderLibrary.Utilities;
using MimeKit;
using MailKit.Net.Smtp;
using System.Security.Authentication;

namespace EmailSenderLibrary
{
  public static class EmailSender
  {
    public static void SendConfirmationToken(EmailSenderModel emailSenderModel, EmailType emailType, string fromName, string toName, string toEmail, string tokenLink)
    {
      MailboxAddress from = new MailboxAddress(fromName, emailSenderModel.SenderEmail);
      MailboxAddress to = new MailboxAddress(toName, toEmail);
      BodyBuilder bodyBuilder = new BodyBuilder();

      string emailSubject = GenerateEmail(toName, tokenLink, emailType, bodyBuilder);

      MimeMessage message = new MimeMessage();
      message.From.Add(from);
      message.To.Add(to);
      message.Subject = emailSubject;
      message.Body = bodyBuilder.ToMessageBody();

      SendEmail(message, emailSenderModel);
    }

    private static string GenerateEmail(string toName, string tokenLink, EmailType emailType, BodyBuilder bodyBuilder)
    {
      string websiteName = "NewAge";
      string emailSubject;

      if (emailType == EmailType.EmailConfirmation)
      {
        emailSubject = "New Registration Confirmation";
        bodyBuilder.HtmlBody =
          $"<h1>Hello {toName} </h1> \n\n" +
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
          $"<h1>Hello {toName} </h1> \n\n" +
          $"<p>You've recently requested for password reset</p> \n\n" +
          "<p>Please click below to reset your password</p> \n\n" +
          $"<a href='{tokenLink}'><button style='color:#fff; background-color:#007bff; border-color:#007bff;'>Confirm</button></a> \n\n" +
          "<p>If the link doesn't work, you can copy and paste the below URL</p> \n\n" +
          $"<p> {tokenLink} </p> \n\n\n" +
          "<p>Thank you!</p>";
      }

      return emailSubject;
    }

    private static void SendEmail(MimeMessage message, EmailSenderModel emailSecret)
    {
      using (SmtpClient client = new SmtpClient())
      {
        if (emailSecret.EmailServer == EmailServer.Rackspace)
        {
          client.CheckCertificateRevocation = false;
          client.SslProtocols = SslProtocols.Tls;
        }
        client.Connect(emailSecret.Host, emailSecret.Port, MailKit.Security.SecureSocketOptions.SslOnConnect);
        client.Authenticate(emailSecret.SenderEmail, emailSecret.SenderPassword);
        client.Send(message);
        client.Disconnect(true);
      }
    }
  }
}
