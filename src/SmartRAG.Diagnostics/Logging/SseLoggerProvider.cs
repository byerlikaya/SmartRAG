using Microsoft.Extensions.Logging;
using SmartRAG.Diagnostics.Interfaces;

namespace SmartRAG.Diagnostics.Logging;

public class SseLoggerProvider : ILoggerProvider
{
	private readonly ILogStream _stream;
	public SseLoggerProvider(ILogStream stream) => _stream = stream;
	public ILogger CreateLogger(string categoryName) => new SseLogger(categoryName, _stream);
	public void Dispose() { }

	private sealed class SseLogger : ILogger
	{
		private readonly string _category;
		private readonly ILogStream _stream;
		public SseLogger(string category, ILogStream stream) { _category = category; _stream = stream; }
		public IDisposable BeginScope<TState>(TState state) => default!;
		public bool IsEnabled(LogLevel logLevel) => true;
		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			if (!IsEnabled(logLevel)) return;
			var msg = formatter(state, exception);
			var line = $"[{DateTime.Now:HH:mm:ss}] {logLevel,-7} {_category}: {msg}";
			_stream.Publish(line);
		}
	}
}
