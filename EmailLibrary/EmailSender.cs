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
    private string _websiteName;
    private MimeMessage message = new MimeMessage();

    public void SetConnectionInfo(EmailSenderServerEnum emailServer, string host, int port, string senderEmail, string senderPassword, string websiteName)
    {
      _emailServer = emailServer;
      _host = host;
      _port = port;
      _senderEmail = senderEmail;
      _senderPassword = senderPassword;
      _websiteName = websiteName;
    }

    public void GenerateContent(string toName, string toEmail, string subject, string body)
    {
      MailboxAddress from = new MailboxAddress($"{ _websiteName } Admin", _senderEmail);
      MailboxAddress to = new MailboxAddress(toName, toEmail);

      BodyBuilder bodyBuilder = new BodyBuilder()
      {
        HtmlBody = body
      };

      message.To.Add(to);
      message.From.Add(from);
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
