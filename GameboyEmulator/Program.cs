namespace GameboyEmulator
{
	static class Program
	{
		public static void Main(string[] args)
		{
			Cpu emulator = new Cpu();

			emulator.Start();

			while (emulator.IsRunning)
			{
				emulator.Update();
			}
		}
	}
}