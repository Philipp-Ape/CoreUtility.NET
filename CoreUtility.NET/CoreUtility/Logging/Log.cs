using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CoreUtility.Logging
{

	public class Log
	{

		public string? InitialMessage;
		public LogLevel LogLevel = LogLevel.Trace;
		public object? MessageIndex;
		public string?[]? IndexArguments;
		public bool EnableConsole;
		public ConsoleColor? ConsoleColor;
		public bool EnableDebug;

		private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions { WriteIndented = true };

		private Log() { }

		public static Log Create(string message) => new Log() { InitialMessage = message };
		public static Log Create(string message, LogLevel level) => new Log() { InitialMessage = message, LogLevel = level };

		public static Log Create(Exception exception) => Create(exception, true);

		public static Log Create(Exception exception, bool includeStackTrace)
		{
			StringBuilder builder = new StringBuilder();
			builder.AppendLine(exception.Message);

			if (includeStackTrace) builder.Append(exception.StackTrace);

			return new Log()
			{
				InitialMessage = builder.ToString(),
				LogLevel = LogLevel.Error
			};
		}

		public static Log Create(object obj)
		{
			string? message;

			try
			{
				message = JsonSerializer.Serialize(obj, SerializerOptions);
			}
			catch
			{
				message = obj?.ToString();
			}

			return new Log() { InitialMessage = message };
		}

		public static Log CreateIndexed(object value, params object?[]? args)
		{
			string?[]? stringArgs = null;

			if (args != null)
			{
				stringArgs = new string[args.Length];

				for (int i = 0; i < args.Length; i++)
				{
					try
					{
						stringArgs[i] = JsonSerializer.Serialize(args[i], SerializerOptions);
					}
					catch
					{
						stringArgs[i] = args[i]?.ToString();
					}
				}
			}

			return new Log()
			{
				MessageIndex = value ?? throw new ArgumentNullException(nameof(value)),
				IndexArguments = stringArgs
			};
		}

		public Log Level(LogLevel level)
		{
			LogLevel = level;
			return this;
		}

		public Log Console()
		{
			EnableConsole = true;
			return this;
		}

		public Log Console(ConsoleColor color)
		{
			EnableConsole = true;
			ConsoleColor = color;
			return this;
		}

		public Log Debug()
		{
			EnableDebug = true;
			return this;
		}

		public async Task Append(LogDispatcher dispatcher) => await dispatcher.AppendLog(this);

	}

}