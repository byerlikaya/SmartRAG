using System.Collections.Concurrent;
using System.Threading.Channels;
using SmartRAG.Diagnostics.Interfaces;

namespace SmartRAG.Diagnostics.Services;

public class LogStreamService : ILogStream
{
	private readonly ConcurrentDictionary<Guid, Channel<string>> _subscribers = new();

	public (Guid id, ChannelReader<string> reader) Subscribe()
	{
		var channel = Channel.CreateUnbounded<string>();
		var id = Guid.NewGuid();
		_subscribers[id] = channel;
		return (id, channel.Reader);
	}

	public void Unsubscribe(Guid id)
	{
		if (_subscribers.TryRemove(id, out var channel))
		{
			channel.Writer.TryComplete();
		}
	}

	public void Publish(string message)
	{
		foreach (var kv in _subscribers)
		{
			kv.Value.Writer.TryWrite(message);
		}
	}
}
