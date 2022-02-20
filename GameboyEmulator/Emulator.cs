using System;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GameboyEmulator
{
	public class Emulator
	{
		public readonly Cpu                  cpu;
		public readonly Graphics             graphics;
		public readonly Interrupts           interrupts;
		public readonly Joypad               joypad;
		public readonly Lcd                  lcd;
		public readonly Memory               memory;
		public readonly MemoryBankController memoryBankController;
		public readonly Screen               screen;
		public readonly Timer                timer;
		public readonly RenderWindow         window;

		public bool IsRunning => window.IsOpen;

		public Emulator()
		{
			cpu                  = new Cpu(this);
			graphics             = new Graphics(this);
			interrupts           = new Interrupts(this);
			joypad               = new Joypad(this);
			lcd                  = new Lcd(this);
			memory               = new Memory(this);
			memoryBankController = new MemoryBankController(this);
			screen               = new Screen(this);
			timer                = new Timer(this);

			window = new RenderWindow(
				new VideoMode(Screen.DRAW_WIDTH, Screen.DRAW_HEIGHT), "GameBoy Emulator", Styles.Close
			);
		}

		public void Update()
		{
			Clock frameTime       = new Clock();
			int   lowestFps       = int.MaxValue;
			int   highestFps      = 0;
			int   cyclesThisFrame = 0;

			frameTime.Restart();

			while (cyclesThisFrame < Cpu.MAX_CYCLES_PER_FRAME)
			{
				int cycles = cpu.ExecuteOpcode();

				//Interrupts only get enabled when requested beforehand by the corresponding instruction
				interrupts.EnableInterrupts();

				cyclesThisFrame += cycles;

				graphics.Update(cycles);

				timer.Update(cycles);

				joypad.Update(false);

				interrupts.Update();
			}

			joypad.Update(true);

			int fps = Convert.ToInt32(1 / frameTime.ElapsedTime.AsSeconds());
			highestFps = Math.Max(highestFps, fps);
			lowestFps  = Math.Min(lowestFps, fps);

			window.SetTitle($"GameBoy Emulator | FPS - {fps} | Lowest - {lowestFps} | Highest - {highestFps}");
		}
	}
}