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

		private static readonly StreamWriter logFile;

		private static string CurrentTime              => DateTime.Now.ToString("HH:mm:ss.fff");
		private static string CurrentTimeFileFormatted => DateTime.Now.ToString("dd.MM.yyyy_HH_mm");

		private const string LOG_DIRECTORY_PATH = "../../../logs/";

		static Logger()
		{
			logFile           = CreateLogFile();
			logFile.AutoFlush = true;
		}

		public static void LogMessage(string message, LogLevel loglevel = LogLevel.Info)
		{
			logFile.WriteLine($"[{CurrentTime}][{LogLevelToString(loglevel)}] {message}");
		}

		private static StreamWriter CreateLogFile()
		{
			Directory.CreateDirectory(LOG_DIRECTORY_PATH);

			string logFilePath = $"{LOG_DIRECTORY_PATH}{CurrentTimeFileFormatted}-{{0}}.txt";
			int    fileNumber  = 1;

			while (File.Exists(string.Format(logFilePath, fileNumber))) fileNumber++;

			string uniqueLogFilePath = string.Format(logFilePath, fileNumber);

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