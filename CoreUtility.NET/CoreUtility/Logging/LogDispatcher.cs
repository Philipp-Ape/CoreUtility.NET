using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoreUtility.Logging
{

	public class LogDispatcher
	{

		private struct SinkFunc
		{

			public Stream Stream;
			public Func<Stream, byte[], Task> Func;

		}

		public Encoding Encoding { get; set; } = Encoding.UTF8;
		public LogLevel LogLevel { get; set; } = LogLevel.All;
		public string DateTimeFormat { get; set; } = string.Join(' ',
			CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern, CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern);
		public ConsoleColor DateTimeColor { get; set; } = ConsoleColor.Gray;
		public bool EnableConsole { get; set; }
		public bool EnableDebug { get; set; }

		private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);
		private readonly List<SinkFunc> Sinks = new List<SinkFunc>();
		private readonly Hashtable Messages = new Hashtable();

		public LogDispatcher AddSink(string filePath) => AddSink(new FileStream(filePath, FileMode.Append, FileAccess.Write));

		public LogDispatcher AddSink(Stream stream)
		{
			lock (Sinks) Sinks.Add(new SinkFunc()
			{
				Stream = stream,
				Func = async (s, b) =>
				{
					await Semaphore.WaitAsync();
					
					try
					{
						if (s?.CanWrite ?? false) await s.WriteAsync(b);
					}
					finally
					{
						Semaphore.Release();
					}
				}
			});

			return this;
		}

		public LogDispatcher RemoveSink(Stream stream)
		{
			lock (Sinks) Sinks.RemoveAll(x => x.Stream == stream);
			return this;
		}

		public void Close()
		{
			lock (Sinks)
			{
				for (int i = 0; i < Sinks.Count; i++)
				{
					try
					{
						Sinks[i].Stream.Close();
					}
					catch { }
				}
			}
		}

		public LogDispatcher RegisterMessage(object key, string message)
		{
			lock (Messages) Messages.Add(key, message);
			return this;
		}

		public async Task AppendLog(Log log)
		{
			if (!LogLevel.HasFlag(log.LogLevel)) return;

			string dateTimeString = DateTime.Now.ToString(DateTimeFormat);
			string message = log.MessageIndex != null ? string.Format(
				(string?)Messages[log.MessageIndex] ?? throw new KeyNotFoundException(nameof(log.MessageIndex)), log.IndexArguments ?? new string[] { }) :
				log.InitialMessage ?? string.Empty;

			StringBuilder builder = new StringBuilder();
			builder.Append(dateTimeString);
			builder.Append(" [");
			builder.Append(log.LogLevel.ToString());
			builder.Append("] ");
			builder.AppendLine(message);

			string builderString = builder.ToString();
			Task writerTask = WriteMessage(builderString);

			if (EnableConsole || log.EnableConsole)
				lock (Console.Out)
				{
					ConsoleColor prevColor = Console.ForegroundColor;

					Console.ForegroundColor = DateTimeColor;
					Console.Write(dateTimeString);

					Console.ForegroundColor = log.LogLevel switch
					{
						LogLevel.Fatal => ConsoleColor.DarkRed,
						LogLevel.Error => ConsoleColor.Red,
						LogLevel.Warn => ConsoleColor.Yellow,
						LogLevel.Info => ConsoleColor.Cyan,
						LogLevel.Debug => ConsoleColor.DarkGray,
						LogLevel.Trace => ConsoleColor.White,
						_ => prevColor
					};

					Console.Write($" [{log.LogLevel}] ");

					Console.ForegroundColor = log.ConsoleColor ?? prevColor;
					Console.Write(message);
					Console.ForegroundColor = prevColor;
					Console.WriteLine();
				}

			if (EnableDebug || log.EnableDebug) Debug.WriteLine(builderString);

			await writerTask;
		}

		public async Task WriteIndexedMessage(object key, params string[] args)
		{
			string message;
			lock (Messages) message = (string?)Messages[key] ?? string.Empty;
			await WriteMessage(string.Format(message, args));
		}

		public async Task WriteMessage(string message)
		{
			byte[] encoded = Encoding.GetBytes(message);
			await Task.WhenAll(Sinks.ConvertAll(x => x.Func.Invoke(x.Stream, encoded)));
		}

	}

}