using System;

namespace CoreUtility.Logging
{

	[Flags]
	public enum LogLevel
	{

		None = 0,
		Fatal = 1,
		Error = 2,
		Warn = 4,
		Info = 8,
		Debug = 16,
		Trace = 32,
		All = 63

	}

}