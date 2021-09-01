using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NewAgeUI.BackgroundServices
{
  public class QueuedBackgroundService : BackgroundService
  {
    private readonly IBackgroundTaskQueue _queue;

    public QueuedBackgroundService(IBackgroundTaskQueue queue)
    {
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
        }
        catch (Exception ex)
        {
          Console.WriteLine(ex.Message);
        }
      }
    }
  }
}
