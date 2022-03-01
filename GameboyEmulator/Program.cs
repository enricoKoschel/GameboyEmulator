using System;
using System.IO;

namespace GameboyEmulator
{
	static class Program
	{
		public static void Main(string[] args)
		{
			Logger.LogMessage("Program started", Logger.LogLevel.Info);

			string gameRomFilePath, bootRomFilePath;

			switch (args.Length)
			{
				case 0:
					gameRomFilePath = Config.GetRomConfig("GAME");
					bootRomFilePath = Config.GetRomConfig("BOOT");
					break;
				case 1:
					gameRomFilePath = args[0];
					bootRomFilePath = Config.GetRomConfig("BOOT");
					break;
				case 2:
					gameRomFilePath = args[0];
					bootRomFilePath = args[1];
					break;
				default:
					Logger.LogMessage("Program was called incorrectly, refer to usage prompt!", Logger.LogLevel.Error);
					PrintUsage();
					return;
			}

			Emulator emulator = new Emulator(gameRomFilePath, bootRomFilePath);

			while (emulator.IsRunning) emulator.Update();

			Logger.LogMessage("Program terminated without errors", Logger.LogLevel.Info);
		}

		private static void PrintUsage()
		{
			string programName = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);

			Console.WriteLine($"Usage: {programName} <path_to_game_rom> [<path_to_boot_rom>]");
		}
	}
}