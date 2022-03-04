using System;
using System.IO;

namespace GameboyEmulator
{
	public static class Logger
	{
		public enum LogLevel
		{
			Info,
			Warn,
			Error
		}

		private static readonly StreamWriter LOG_FILE;

		private static string CurrentTime              => DateTime.Now.ToString("HH:mm:ss.fff");
		private static string CurrentTimeFileFormatted => DateTime.Now.ToString("yyyy-MM-dd__HH_mm_ss");

		private static readonly string LOG_DIRECTORY_PATH;
		private const           bool   ENABLE_CONSOLE_LOGGING = true;

		static Logger()
		{
			if (!Config.GetLogEnabledConfig()) return;

			LOG_DIRECTORY_PATH = Path.GetDirectoryName(Config.GetLogLocationConfig() + "/");
			if (String.IsNullOrWhiteSpace(LOG_DIRECTORY_PATH)) LOG_DIRECTORY_PATH = "./logs";
			LOG_DIRECTORY_PATH += "/";

			LOG_FILE           = CreateLogFile();
			LOG_FILE.AutoFlush = true;
		}

		public static void LogMessage(string message, LogLevel loglevel, bool logToConsole = false)
		{
			if (!Config.GetLogEnabledConfig()) return;

			string logMessage = $"[{CurrentTime}][{LogLevelToString(loglevel)}] {message}";

			if (logToConsole && ENABLE_CONSOLE_LOGGING) Console.WriteLine(logMessage);

			LOG_FILE.WriteLine(logMessage);
		}

		private static StreamWriter CreateLogFile()
		{
			Directory.CreateDirectory(LOG_DIRECTORY_PATH);

			string logFilePath = $"{LOG_DIRECTORY_PATH}{CurrentTimeFileFormatted}-{{0}}.txt";
			int    fileNumber  = 1;

			while (File.Exists(String.Format(logFilePath, fileNumber))) fileNumber++;

			string uniqueLogFilePath = String.Format(logFilePath, fileNumber);

			return new StreamWriter(File.Create(uniqueLogFilePath));
		}

		private static string LogLevelToString(LogLevel logLevel)
		{
			return logLevel switch
			{
				LogLevel.Info  => "Info",
				LogLevel.Warn  => "Warn",
				LogLevel.Error => "Error",
				_              => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, "Invalid Log Level")
			};
		}
	}
}