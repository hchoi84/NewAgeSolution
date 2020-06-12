using EmailSenderLibrary.Utilities;
using MimeKit;
using MailKit.Net.Smtp;
using System.Security.Authentication;
using System;

namespace EmailSenderLibrary
{
  // TODO: Should this inherit from IDisposable so that the senderPassword isn't persisting?
  // TODO: Figure out how to log error messages to a file. ILogger and NLog? How to setup ILogger and NLog? What other options are available?
  public class EmailSender
  {
    private readonly EmailSenderServerEnum _emailServer;
    private readonly string _host;
    private readonly int _port;
    private readonly string _senderEmail;
    private readonly string _senderPassword;
    private readonly string _websiteName;

    public EmailSender(EmailSenderServerEnum emailServer, string host, int port, string senderEmail, string senderPassword, string websiteName)
    {
      _emailServer = emailServer;
      _host = host;
      _port = port;
      _senderEmail = senderEmail;
      _senderPassword = senderPassword;
      _websiteName = websiteName;
    }

    public (EmailSenderConfirmationEnum, string) SendConfirmationToken(EmailSenderTypeEnum emailType, string fromName, string toName, string toEmail, string tokenLink)
    {
      MailboxAddress from = new MailboxAddress(fromName, _senderEmail);
      MailboxAddress to = new MailboxAddress(toName, toEmail);

      MimeMessage message = new MimeMessage();
      message.From.Add(from);
      message.To.Add(to);
      
      GenerateEmail(emailType, toName, tokenLink, message);

      try
      {
        SendEmail(message);
      }
      catch (Exception e)
      {
        return (EmailSenderConfirmationEnum.Failed, e.Message);
      }

      return (EmailSenderConfirmationEnum.Success, "Email sent successfully");
    }

    private void GenerateEmail(EmailSenderTypeEnum emailType, string toName, string tokenLink, MimeMessage message)
    {
      string subject;
      BodyBuilder bodyBuilder = new BodyBuilder();

      if (emailType == EmailSenderTypeEnum.EmailConfirmation)
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
        if (_emailServer == EmailSenderServerEnum.Rackspace)
        {
          client.CheckCertificateRevocation = false;
          client.SslProtocols = SslProtocols.Tls;
        }
        try
        {
          client.Connect(_host, _port, MailKit.Security.SecureSocketOptions.SslOnConnect);
          client.Authenticate(_senderEmail, _senderPassword);
          client.Send(message);
          client.Disconnect(true);
        }
        catch (Exception e)
        {
          throw new Exception("There was a problem with either connecting or authenticating with the client", e);
        }
      }
    }
  }
}
