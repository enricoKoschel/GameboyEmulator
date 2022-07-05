using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace GameboyEmulator;

public static class Logger
{
	private enum LogLevel
	{
		Info,
		Warn,
		Error
	}

	private static readonly StreamWriter LOG_FILE;

	private static string CurrentTime              => DateTime.Now.ToString("HH:mm:ss.fff");
	private static string CurrentTimeFileFormatted => DateTime.Now.ToString("yyyy-MM-dd__HH_mm_ss");

	private const string DEFAULT_LOG_DIRECTORY  = "./logs";
	private const bool   ENABLE_CONSOLE_LOGGING = true;

	private static readonly string LOG_DIRECTORY;

	static Logger()
	{
		string? logDirectory = Path.GetDirectoryName(Config.GetLogLocationConfig() + "/");

		bool defaultDirectoryUsed = false;
		if (String.IsNullOrWhiteSpace(logDirectory))
		{
			LOG_DIRECTORY        = DEFAULT_LOG_DIRECTORY;
			defaultDirectoryUsed = true;
		}
		else LOG_DIRECTORY = logDirectory;

		LOG_DIRECTORY += "/";

		LOG_FILE = CreateLogFile();

		LOG_FILE.AutoFlush = true;

		if (defaultDirectoryUsed)
			LogInvalidConfigValue("[Logging].LOCATION", Config.GetLogLocationConfig(), DEFAULT_LOG_DIRECTORY);
	}

	private static void LogMessage(string message, LogLevel loglevel, bool logToConsole = false)
	{
		string logMessage = $"[{CurrentTime}][{LogLevelToString(loglevel)}] {message}";

		if (logToConsole && ENABLE_CONSOLE_LOGGING) Console.WriteLine(logMessage);

		LOG_FILE.WriteLine(logMessage);
	}

	public static void LogError(string message, bool logToConsole = false)
	{
		LogMessage(message, LogLevel.Error, logToConsole);
	}

	public static void LogWarn(string message, bool logToConsole = false)
	{
		LogMessage(message, LogLevel.Warn, logToConsole);
	}

	public static void LogInfo(string message, bool logToConsole = false)
	{
		LogMessage(message, LogLevel.Info, logToConsole);
	}

	public static void LogInvalidConfigValue<T1, T2>(string configKey, T1? value, T2 defaultValue)
	{
		LogWarn(
			value is null
				? $"{configKey} could not be found in config file. Defaulting to {defaultValue}."
				: $"Invalid value '{value}' for {configKey} in config file. Defaulting to {defaultValue}.",
			true
		);
	}

	[DoesNotReturn]
	public static void ControlledCrash(string message, int skipFrames = 0)
	{
		LogError($"{message}\n{new StackTrace(skipFrames + 1)}", true);
		Environment.Exit(1);
	}

	[DoesNotReturn]
	public static void ControlledCrash(Exception e)
	{
		LogError($"{e.Message}\n{e.StackTrace}", true);
		Environment.Exit(1);
	}

	[DoesNotReturn]
	public static void Unreachable()
	{
		ControlledCrash("Unreachable code reached", 1);
	}

	private static StreamWriter CreateLogFile()
	{
		Directory.CreateDirectory(LOG_DIRECTORY);

		string logFilePath = $"{LOG_DIRECTORY}{CurrentTimeFileFormatted}-{{0}}.txt";
		int    fileNumber  = 1;

		while (File.Exists(String.Format(logFilePath, fileNumber))) fileNumber++;

		string uniqueLogFilePath = String.Format(logFilePath, fileNumber);

		return new StreamWriter(File.Create(uniqueLogFilePath));
	}

	private static string LogLevelToString(LogLevel logLevel)
	{
		switch (logLevel)
		{
			case LogLevel.Info:
				return "Info";
			case LogLevel.Warn:
				return "Warn";
			case LogLevel.Error:
				return "Error";
			default:
				ControlledCrash($"Invalid log level {logLevel}");
				return "";
		}
	}
}