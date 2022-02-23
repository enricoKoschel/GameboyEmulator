using System;
using System.Diagnostics;
using System.IO;

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
		public readonly Timer                timer;
		public readonly InputOutput          inputOutput;

		private const    int    NUMBER_OF_SPEEDS_TO_AVERAGE = 60;
		private readonly long[] lastSpeeds;

		public bool IsRunning => inputOutput.WindowIsOpen;

		public Emulator()
		{
			cpu                  = new Cpu(this);
			interrupts           = new Interrupts(this);
			joypad               = new Joypad(this);
			memory               = new Memory(this);
			memoryBankController = new MemoryBankController(this);
			ppu                  = new Ppu(this);
			timer                = new Timer(this);
			inputOutput          = new InputOutput();

			lastSpeeds = new long[NUMBER_OF_SPEEDS_TO_AVERAGE];

			memory.LoadGame();
		}

		public void Update()
		{
			Stopwatch frameTime = new Stopwatch();

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
			long speed = 1600 / Math.Max(frameTime.ElapsedMilliseconds, 1);

			long speedAverage = 0;

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
			inputOutput.SetWindowTitle($"{Path.GetFileName(Memory.GAME_ROM_FILE_PATH)} | Speed: {speedAverage}%");
		}
	}
}