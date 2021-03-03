using System;
using SFML.System;
using SFML.Window;

namespace GameboyEmulator
{
	static class Program
	{
		public static void Main(string[] args)
		{
			Logger.LogMessage("Program started");

			Cpu    emulator   = new Cpu();
			Window window     = emulator.GetGraphics().GetScreen().GetWindow();
			Clock  frameTime  = new Clock();
			int    lowestFps  = int.MaxValue;
			int    highestFps = 0;

			emulator.Start();

			while (emulator.IsRunning)
			{
				frameTime.Restart();
				emulator.Update();

				int fps                          = Convert.ToInt32(1 / frameTime.ElapsedTime.AsSeconds());
				highestFps = Math.Max(highestFps, fps);
				lowestFps  = Math.Min(lowestFps, fps);

				window.SetTitle($"GameBoy Emulator | FPS - {fps} | Lowest - {lowestFps} | Highest - {highestFps}");
			}

			Logger.LogMessage("Program terminated");
		}
	}
}