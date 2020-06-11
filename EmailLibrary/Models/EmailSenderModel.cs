using EmailSenderLibrary.Utilities;

namespace EmailSenderLibrary.Models
{
  public class EmailSenderModel
  {
    public EmailSenderModel(EmailServer emailServer, string senderEmail, string host, int port, string emailPassword)
    {
      EmailServer = emailServer;
      SenderEmail = senderEmail;
      Host = host;
      Port = port;
      SenderPassword = emailPassword;
    }

    public EmailServer EmailServer { get; private set; }
    public string SenderEmail { get; private set; }
    public string Host { get; private set; }
    public int Port { get; private set; }
    public string SenderPassword { get; private set; }
  }
}
