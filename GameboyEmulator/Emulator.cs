using System;
using System.IO;
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

		private const    int   NUMBER_OF_SPEEDS_TO_AVERAGE = 60;
		private readonly int[] lastSpeeds;

		public bool IsRunning => window.IsOpen;

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

			lastSpeeds = new int[NUMBER_OF_SPEEDS_TO_AVERAGE];

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

			//Very inefficient way to average the emulator speed over the last second (with NUMBER_OF_SPEEDS_TO_AVERAGE = 60)
			int speed = 1600 / Math.Max(frameTime.ElapsedTime.AsMilliseconds(), 1);

			int speedAverage = 0;

			for (int i = 0; i < NUMBER_OF_SPEEDS_TO_AVERAGE; i++)
			{
				speedAverage += lastSpeeds[i];

				if (i + 1 < NUMBER_OF_SPEEDS_TO_AVERAGE)
				{
					lastSpeeds[i] = lastSpeeds[i + 1];
					continue;
				}

				lastSpeeds[i] = speed;
			}

			speedAverage /= NUMBER_OF_SPEEDS_TO_AVERAGE;

			//TODO use file name provided by args from console
			window.SetTitle($"{Path.GetFileName(Memory.GAME_ROM_FILE_PATH)} | Speed: {speedAverage}%");
		}

		private static void OnClosed(object sender, EventArgs e)
		{
			RenderWindow window = (RenderWindow)sender;
			window.Close();
		}
	}
}