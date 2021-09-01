using EmailSenderLibrary.Utilities;
using MimeKit;
using MailKit.Net.Smtp;
using System.Security.Authentication;
using System;
using EmailSenderLibrary.Securities;

namespace EmailSenderLibrary
{
  public class EmailSender : IEmailSender
  {
    public MimeMessage GenerateContent(string toName, string toEmail, string subject, string body, string fileName = "", byte[] file = null)
    {
      MimeMessage message = new MimeMessage();

      MailboxAddress from = new MailboxAddress($"{ Secrets.WebsiteName } Admin", Secrets.SenderEmail);
      MailboxAddress to = new MailboxAddress(toName, toEmail);

      BodyBuilder bodyBuilder = new BodyBuilder()
      {
        HtmlBody = body
      };

      if (!string.IsNullOrWhiteSpace(fileName) && file != null)
      {
        bodyBuilder.Attachments.Add(fileName, file, new ContentType("text", "csv"));
      }

      message.To.Add(to);
      message.From.Add(from);
      message.Subject = subject;
      message.Body = bodyBuilder.ToMessageBody();

      return message;
    }

    public void SendEmail(MimeMessage message)
    {
      using SmtpClient client = new SmtpClient();
      if (Secrets.EmailServer == EmailSenderServerEnum.Rackspace)
      {
        client.CheckCertificateRevocation = false;
        client.SslProtocols = SslProtocols.Tls;
      }

      try
      {
        client.Connect(Secrets.Host, Secrets.Port, MailKit.Security.SecureSocketOptions.SslOnConnect);
        client.Authenticate(Secrets.SenderEmail, Secrets.SenderPassword);
        client.Send(message);
        client.Disconnect(true);
      }
      catch (Exception e)
      {
        throw new Exception("There was a problem with either connecting or authenticating with the client", e);
      }
    }

    public string GetDoamin() => Secrets.Domain;

  }
}
