﻿using System;
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

		//4194304/70224 is the exact fps of the Gameboy
		public const double GAMEBOY_FPS = 4194304 / 70224.0;

		public  double MaxFps          { get; set; }
		private double MinTimePerFrame => MaxFps != 0 ? 1000 / MaxFps : 0;

		private const    int   NUMBER_OF_SPEEDS_TO_AVERAGE = 60;
		private readonly int[] lastSpeeds;

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
			inputOutput          = new InputOutput(this);

			lastSpeeds = new int[NUMBER_OF_SPEEDS_TO_AVERAGE];

			MaxFps = GAMEBOY_FPS;

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
			inputOutput.Update();

			//Thread.Sleep is too imprecise for this use case, thus a busy loop has to be used
			while (frameTime.Elapsed.TotalMilliseconds < MinTimePerFrame)
			{
			}

			UpdateWindowTitle(frameTime.Elapsed.TotalMilliseconds);
		}

		private void UpdateWindowTitle(double elapsedTime)
		{
			//One frame on the Gameboy takes about 16.74 milliseconds to render
			//Dividing 1674 by the frame time gives us the emulation speed out of 100%
			int speed = Convert.ToInt32(1674 / Math.Max(elapsedTime, 1));

			int speedAverage = 0;

			//Very inefficient way to average the emulator speed
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