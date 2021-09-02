using ChannelAdvisorLibrary;
using EmailSenderLibrary;
using FileReaderLibrary;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace NewAgeUI.BackgroundServices
{
  public interface IBackgroundTaskQueue
  {
    ValueTask Enqueue(Dictionary<string, int> activeBufferFile, string email);
    ValueTask<Func<Dictionary<string, int>, string, ValueTask>> Dequeue(CancellationToken cancellationToken);
    Dictionary<string, int> GetFile();
    void RemoveFile();
    string GetEmail();
    void RemoveEmail();
  }

  public class BackgroundTaskQueue : IBackgroundTaskQueue
  {
    private readonly Channel<Func<Dictionary<string, int>, string, ValueTask>> _queue;
    private readonly ILogger<BackgroundTaskQueue> _logger;
    private readonly IServiceProvider _serviceProvider;

    public Dictionary<string, int> File { get; private set; }
    public string Email { get; private set; }

    public BackgroundTaskQueue(ILogger<BackgroundTaskQueue> logger, IServiceProvider serviceProvider)
    {
      var options = new BoundedChannelOptions(10)
      {
        FullMode = BoundedChannelFullMode.Wait
      };

      _queue = Channel.CreateBounded<Func<Dictionary<string, int>, string, ValueTask>>(options);
      _logger = logger;
      _serviceProvider = serviceProvider;
    }

    public async ValueTask Enqueue(Dictionary<string, int> activeBufferFile, string email)
    {
      if (File != null || Email != null)
      {
        throw new Exception($"There's already a task in progress for {Email}");
      }

      File = activeBufferFile;
      Email = email;
      await _queue.Writer.WriteAsync(GenerateBufferImportFile);
    }

    private async ValueTask GenerateBufferImportFile(Dictionary<string, int> file, string email)
    {
      using var scope = _serviceProvider.CreateScope();
      var _channelAdvisor = scope.ServiceProvider.GetRequiredService<IChannelAdvisor>();
      _logger.LogInformation("Beginning CA fetch");
      List<JObject> fromCA = await _channelAdvisor.GetForBufferAsync();
      _logger.LogInformation("Ended CA fetch");

      _logger.LogInformation("Beginning Import File Generator");
      var _fileReader = scope.ServiceProvider.GetRequiredService<IFileReader>();
      StringBuilder sb = _fileReader.GenerateBufferImportSB(file, fromCA);
      _logger.LogInformation("Ended Import File Generator");

      byte[] fileContent = new UTF8Encoding().GetBytes(sb.ToString());

      _logger.LogInformation($"Sending email to {email}");
      string subject = "Your buffer import file is ready";
      var _emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
      _emailSender.SendEmail(
        _emailSender.GenerateContent("Importer", email, "Buffer Import File", subject, "StoreBufferImport.csv", fileContent));
    }

    public async ValueTask<Func<Dictionary<string, int>, string, ValueTask>> Dequeue(CancellationToken cancellationToken)
    {
      return await _queue.Reader.ReadAsync(cancellationToken);
    }

    public Dictionary<string, int> GetFile() => File;

    public string GetEmail() => Email;

    public void RemoveFile()
    {
      File = null;
    }

    public void RemoveEmail()
    {
      Email = null;
    }
  }
}
