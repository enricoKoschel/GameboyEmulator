using System;
using System.Collections.Generic;
using System.IO;
using Mono.Options;

namespace GameboyEmulator;

static class Program
{
	private static readonly string PROGRAM_NAME = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);

	public static void Main(string[] args)
	{
		try
		{
			Logger.LogInfo("Program started");

			//Set timer granularity to 1ms on Windows, other platforms have finer granularity by default
			if (Environment.OSVersion.Platform == PlatformID.Win32NT && WinApi.TimeBeginPeriod(1) != 0)
				Logger.ControlledCrash("TimeBeginPeriod failed");

			string? gameRomFilePath = null;
			string? bootRomFilePath = null;
			bool    shouldShowHelp  = false;

			OptionSet options = new()
			{
				{ "g|game=", "Load the game rom from VALUE.", g => gameRomFilePath = g },
				{ "b|boot=", "Load the boot rom from VALUE.", b => bootRomFilePath = b },
				{ "h|help", "Show this message and exit.", h => shouldShowHelp = h != null }
			};

			List<string> extra;
			try
			{
				extra = options.Parse(args);
			}
			catch (OptionException e)
			{
				PrintArgumentError(e.Message);
				return;
			}

			if (shouldShowHelp)
			{
				ShowHelp(options);
				return;
			}

			if (extra.Count > 0)
			{
				PrintArgumentError($"Unknown option '{extra.ToArray()[0]}'.");
				return;
			}

			gameRomFilePath ??= Config.GetRomConfig("GAME");
			bootRomFilePath ??= Config.GetRomConfig("BOOT");

			Emulator emulator = new(gameRomFilePath, bootRomFilePath);

			emulator.Run();

			//Reset timer granularity to default on Windows
			if (Environment.OSVersion.Platform == PlatformID.Win32NT && WinApi.TimeEndPeriod(1) != 0)
				Logger.ControlledCrash("TimeEndPeriod failed");

			Logger.LogInfo("Program terminated without errors");
		}
		catch (Exception e)
		{
			Logger.ControlledCrash(e);
		}
	}

	private static void PrintArgumentError(string message)
	{
		Console.WriteLine(message);
		Console.WriteLine($"Try '{PROGRAM_NAME} --help' for more information.");
	}

	private static void ShowHelp(OptionSet options)
	{
		Console.WriteLine($"Usage: {PROGRAM_NAME} [options]");
		Console.WriteLine("Options:");

		options.WriteOptionDescriptions(Console.Out);

		Console.WriteLine(
			"(If no options are used, the game and boot rom file paths provided by settings.ini will be used)"
		);

		Console.WriteLine("(A boot rom is not required to use the emulator)");
	}
}