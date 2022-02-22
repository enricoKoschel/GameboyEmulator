namespace GameboyEmulator
{
	static class Program
	{
		public static void Main(string[] args)
		{
			Logger.LogMessage("Program started", Logger.LogLevel.Info);

			Emulator emulator = new Emulator();

			while (emulator.IsRunning) emulator.Update();

			Logger.LogMessage("Program terminated without errors", Logger.LogLevel.Info);
		}
	}
}