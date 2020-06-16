using EmailSenderLibrary.Utilities;
using MimeKit;
using MailKit.Net.Smtp;
using System.Security.Authentication;
using System;

namespace EmailSenderLibrary
{
  // TODO: Should this inherit from IDisposable so that the senderPassword isn't persisting?
  // TODO: Figure out how to log error messages to a file. ILogger and NLog? How to setup ILogger and NLog? What other options are available?
  public class EmailSender : IEmailSender
  {
    private EmailSenderServerEnum _emailServer;
    private string _host;
    private int _port;
    private string _senderEmail;
    private string _senderPassword;
    private string _senderName;
    private string _websiteName;
    private MimeMessage message = new MimeMessage();

    public void SetConnectionInfo(EmailSenderServerEnum emailServer, string host, int port, string senderEmail, string senderPassword, string senderName, string websiteName)
    {
      _emailServer = emailServer;
      _host = host;
      _port = port;
      _senderEmail = senderEmail;
      _senderPassword = senderPassword;
      _senderName = senderName;
      _websiteName = websiteName;
    }

    public void GenerateTokenConfirmationContent(EmailSenderTypeEnum emailType, string toName, string toEmail, string tokenLink)
    {
      MailboxAddress from = new MailboxAddress(_senderName, _senderEmail);
      MailboxAddress to = new MailboxAddress(toName, toEmail);
      
      message.To.Add(to);
      message.From.Add(from);

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

    public void SendEmail()
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
