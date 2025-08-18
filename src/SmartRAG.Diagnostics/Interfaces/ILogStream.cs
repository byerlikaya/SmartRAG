using System.Threading.Channels;

namespace SmartRAG.Diagnostics.Interfaces;

public interface ILogStream
{
	(Guid id, ChannelReader<string> reader) Subscribe();
	void Unsubscribe(Guid id);
	void Publish(string message);
}
