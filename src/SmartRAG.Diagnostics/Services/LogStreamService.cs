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
		// Mesajı JSON formatında gönder
		var jsonMessage = System.Text.Json.JsonSerializer.Serialize(new { 
			message = message, 
			level = "info", 
			timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
		});
		
		var deadSubscribers = new List<Guid>();
		
		foreach (var kv in _subscribers)
		{
			try
			{
				if (!kv.Value.Writer.TryWrite(jsonMessage))
				{
					// Channel kapalıysa subscriber'ı temizle
					deadSubscribers.Add(kv.Key);
				}
			}
			catch (Exception)
			{
				// Channel hatası varsa subscriber'ı temizle
				deadSubscribers.Add(kv.Key);
			}
		}
		
		// Ölü subscriber'ları temizle
		foreach (var deadId in deadSubscribers)
		{
			Unsubscribe(deadId);
		}
	}
}
