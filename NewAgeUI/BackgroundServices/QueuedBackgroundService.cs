using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NewAgeUI.BackgroundServices
{
  public class QueuedBackgroundService : BackgroundService
  {
    private readonly ILogger<QueuedBackgroundService> _logger;
    private readonly IBackgroundTaskQueue _queue;

    public QueuedBackgroundService(ILogger<QueuedBackgroundService> logger, IBackgroundTaskQueue queue)
    {
      _logger = logger;
      _queue = queue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        var item = await _queue.Dequeue(stoppingToken);

        try
        {
          await item(_queue.GetFile(), _queue.GetEmail());
          _queue.RemoveEmail();
          _queue.RemoveFile();
          _logger.LogInformation("Process completed");
        }
        catch (Exception ex)
        {
          _queue.RemoveEmail();
          _queue.RemoveFile();
          _logger.LogError(ex, ex.Message);
        }
      }
    }
  }
}
