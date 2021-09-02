using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace NewAgeUI.BackgroundServices
{
  public interface IBackgroundTaskQueue
  {
    ValueTask Enqueue(Func<Dictionary<string, int>, string, ValueTask> item, Dictionary<string, int> activeBufferFile, string email);
    ValueTask<Func<Dictionary<string, int>, string, ValueTask>> Dequeue(CancellationToken cancellationToken);
    Dictionary<string, int> GetFile();
    string GetEmail();
  }

  public class BackgroundTaskQueue : IBackgroundTaskQueue
  {
    private readonly Channel<Func<Dictionary<string, int>, string, ValueTask>> _queue;
    public Dictionary<string, int> File { get; private set; }
    public string Email { get; private set; }

    public BackgroundTaskQueue()
    {
      var options = new BoundedChannelOptions(10)
      {
        FullMode = BoundedChannelFullMode.Wait
      };

      _queue = Channel.CreateBounded<Func<Dictionary<string, int>, string, ValueTask>>(options);
    }

    public async ValueTask Enqueue(Func<Dictionary<string, int>, string, ValueTask> item, Dictionary<string, int> activeBufferFile, string email)
    {
      if (item == null)
      {
        throw new ArgumentNullException(nameof(item));
      }

      File = activeBufferFile;
      Email = email;
      await _queue.Writer.WriteAsync(item);
    }

    public async ValueTask<Func<Dictionary<string, int>, string, ValueTask>> Dequeue(CancellationToken cancellationToken)
    {
      return await _queue.Reader.ReadAsync(cancellationToken);
    }

    public Dictionary<string, int> GetFile() => File;

    public string GetEmail() => Email;
  }
}
