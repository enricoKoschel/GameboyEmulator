using System;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GameboyEmulator
{
	public class Emulator
	{
		public readonly Cpu                  cpu;
		public readonly Interrupts           interrupts;
		public readonly Joypad               joypad;
		public readonly Memory               memory;
		public readonly MemoryBankController memoryBankController;
		public readonly Ppu                  ppu;
		public readonly Screen               screen;
		public readonly Timer                timer;
		public readonly RenderWindow         window;

		public bool IsRunning => window.IsOpen;

		private int lowestFps = int.MaxValue;
		private int highestFps;

		public Emulator()
		{
			cpu                  = new Cpu(this);
			interrupts           = new Interrupts(this);
			joypad               = new Joypad(this);
			memory               = new Memory(this);
			memoryBankController = new MemoryBankController(this);
			ppu                  = new Ppu(this);
			screen               = new Screen(this);
			timer                = new Timer(this);

			memory.LoadGame();

			window = new RenderWindow(
				new VideoMode(Screen.DRAW_WIDTH, Screen.DRAW_HEIGHT), "GameBoy Emulator", Styles.Close
			);

			window.SetActive();

			//Gameboy runs at 60 fps
			window.SetFramerateLimit(60);
			window.Closed += OnClosed;
		}

		public void Update()
		{
			Clock frameTime = new Clock();

			int cyclesThisFrame = 0;

			frameTime.Restart();

			while (cyclesThisFrame < Cpu.MAX_CYCLES_PER_FRAME)
			{
				int cycles = cpu.ExecuteOpcode();
				cyclesThisFrame += cycles;

				//Interrupts only get enabled when requested beforehand by the corresponding instruction
				interrupts.EnableInterrupts();

				ppu.Update(cycles);
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

		private static void OnClosed(object sender, EventArgs e)
		{
			RenderWindow window = (RenderWindow)sender;
			window.Close();
		}
	}
}