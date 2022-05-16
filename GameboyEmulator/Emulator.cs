using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace GameboyEmulator;

public class Emulator
{
	public Cpu                  cpu;
	public Interrupts           interrupts;
	public Joypad               joypad;
	public Memory               memory;
	public MemoryBankController memoryBankController;
	public Ppu                  ppu;
	public Timer                timer;
	public InputOutput          inputOutput;
	public Apu                  apu;

	public const int GAMEBOY_CLOCK_SPEED = 4194304;

	//Clock speed/70224 is the exact fps of the Gameboy
	public const double GAMEBOY_FPS = GAMEBOY_CLOCK_SPEED / 70224.0;

	public  double MaxFps                  { get; set; }
	private double MinMillisecondsPerFrame => MaxFps != 0 ? 1000 / MaxFps : 0;

	private double sleepErrorInMilliseconds;

	private const    int   NUMBER_OF_SPEEDS_TO_AVERAGE = 60;
	private readonly int[] speedHistory;
	private          int   speedAverage = 100;

	public readonly bool savingEnabled;

	public readonly string bootRomFilePath;
	public readonly string gameRomFilePath;
	public readonly string saveFilePath;

	private bool IsRunning => inputOutput.WindowIsOpen;
	public  bool isPaused;

	public Emulator(string gameRomFilePath, string bootRomFilePath)
	{
		cpu                  = new Cpu(this);
		interrupts           = new Interrupts(this);
		joypad               = new Joypad(this);
		memory               = new Memory(this);
		memoryBankController = new MemoryBankController(this);
		ppu                  = new Ppu(this);
		timer                = new Timer(this);
		inputOutput          = new InputOutput(this);
		apu                  = new Apu(this);

		this.gameRomFilePath = gameRomFilePath;
		this.bootRomFilePath = bootRomFilePath;

		string? saveFileDirectory = Path.GetDirectoryName(Config.GetSaveLocationConfig() + "/");
		if (String.IsNullOrWhiteSpace(saveFileDirectory))
		{
			saveFileDirectory = "./saves/";

			Logger.LogMessage(
				$"Invalid value for [Saving].LOCATION in config file. Defaulting to {saveFileDirectory}.",
				Logger.LogLevel.Warn, true
			);
		}

		string saveFileName = Path.ChangeExtension(Path.GetFileName(gameRomFilePath), ".sav");
		saveFilePath = $"{saveFileDirectory}/{saveFileName}";

		speedHistory = new int[NUMBER_OF_SPEEDS_TO_AVERAGE];

		MaxFps = GAMEBOY_FPS;

		memory.LoadGame();

		savingEnabled = Config.GetSaveEnabledConfig();

		//Only create save directory if saving is enabled and cartridge ram exists
		if (savingEnabled && memoryBankController.CartridgeRamExists)
			Directory.CreateDirectory(saveFileDirectory);
	}

	public void Reset()
	{
		inputOutput.CloseWindow();
		apu.Stop();

		cpu                  = new Cpu(this);
		interrupts           = new Interrupts(this);
		joypad               = new Joypad(this);
		memory               = new Memory(this);
		memoryBankController = new MemoryBankController(this);
		ppu                  = new Ppu(this);
		timer                = new Timer(this);
		inputOutput          = new InputOutput(this);
		apu                  = new Apu(this);

		MaxFps   = GAMEBOY_FPS;
		isPaused = false;

		memory.LoadGame();
	}

	public void Run()
	{
		while (IsRunning) Update();
	}

	private void Update()
	{
		while (IsRunning && isPaused)
		{
			inputOutput.Update();
			apu.ClearSampleBuffer();
			Thread.Sleep(16);
		}

		Stopwatch frameTime = new();

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
			apu.Update(cycles);
			interrupts.Update();
		}

		joypad.CaptureInput();
		inputOutput.Update();

		//Save cartridge ram at the end of every frame so that no data is lost
		memory.SaveCartridgeRam();

		double elapsedMilliseconds = frameTime.Elapsed.TotalMilliseconds;
		double sleepNeeded         = MinMillisecondsPerFrame - elapsedMilliseconds - sleepErrorInMilliseconds;

		double timeSlept = 0;

		if (sleepNeeded > 0 && apu.AmountOfSamples > Apu.SAMPLE_BUFFER_SIZE)
		{
			TimeOnly timeBeforeSleep = TimeOnly.FromDateTime(DateTime.Now);
			Thread.Sleep((int)sleepNeeded);
			TimeOnly timeAfterSleep = TimeOnly.FromDateTime(DateTime.Now);

			timeSlept = (timeAfterSleep - timeBeforeSleep).TotalMilliseconds;

			//Calculate the error from sleeping too much/not enough
			sleepErrorInMilliseconds = sleepNeeded - timeSlept;
		}
		else sleepErrorInMilliseconds = 0;

		UpdateWindowTitle(elapsedMilliseconds + timeSlept);
	}

	private void UpdateWindowTitle(double elapsedTime)
	{
		//One frame on the Gameboy takes about 16.74 milliseconds to render
		//Dividing 1674 by the frame time gives us the emulation speed out of 100%
		int speed = Convert.ToInt32(1674 / Math.Max(elapsedTime, 1));

		if (Math.Abs(speed - speedHistory[NUMBER_OF_SPEEDS_TO_AVERAGE - 1]) > 100)
		{
			//If the speed is significantly different from the previous one, dont consider it
			speedHistory[NUMBER_OF_SPEEDS_TO_AVERAGE - 1] = speed;
			return;
		}

		//Very inefficient way to average the emulator speed
		for (int i = 0; i < NUMBER_OF_SPEEDS_TO_AVERAGE; i++)
		{
			speedAverage += speedHistory[i];

			if (i + 1 < NUMBER_OF_SPEEDS_TO_AVERAGE)
			{
				speedHistory[i] = speedHistory[i + 1];
				continue;
			}

			speedHistory[i] = speed;
		}

		speedAverage /= NUMBER_OF_SPEEDS_TO_AVERAGE;

		inputOutput.SetWindowTitle(
			isPaused
				? $"{Path.GetFileName(gameRomFilePath)} | paused"
				: $"{Path.GetFileName(gameRomFilePath)} | Speed: {speedAverage}%"
		);
	}
}