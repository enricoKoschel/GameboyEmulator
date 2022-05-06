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
		Logger.LogMessage("Program started", Logger.LogLevel.Info);

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

		Logger.LogMessage("Program terminated without errors", Logger.LogLevel.Info);
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