using System;

namespace GameboyEmulator;

public class Apu
{
	//Controls registers

	//NR50
	public byte ChannelControlRegister { get; set; }

	private bool VinLeftEnabled  => Cpu.GetBit(ChannelControlRegister, 7);
	private bool VinRightEnabled => Cpu.GetBit(ChannelControlRegister, 3);

	public byte LeftChannelVolume  => (byte)((ChannelControlRegister & 0b0111_0000) >> 4);
	public byte RightChannelVolume => (byte)(ChannelControlRegister & 0b0000_0111);

	//NR51
	public byte SoundOutputTerminalSelectRegister { get; set; }

	public bool Channel1Enabled { get; set; } = true;
	public bool Channel2Enabled { get; set; } = true;
	public bool Channel3Enabled { get; set; } = true;
	public bool Channel4Enabled { get; set; }

	//NR52
	public byte SoundOnOffRegister
	{
		get => Cpu.MakeByte(
			Enabled, true, true, true, channel4.Playing, channel3.Playing, channel2.Playing, channel1.Playing
		);
		set => Enabled = (value & 0b1000_0000) != 0;
	}

	public bool Enabled { get; private set; }

	/*
		00 - 12.5% (_-------_-------_-------)
		01 - 25%   (__------__------__------)
		10 - 50%   (____----____----____----)
		11 - 75%   (______--______--______--)
	*/
	public static readonly byte[,] WAVE_DUTY_TABLE =
	{
		{ 0, 1, 1, 1, 1, 1, 1, 1 },
		{ 0, 0, 1, 1, 1, 1, 1, 1 },
		{ 0, 0, 0, 0, 1, 1, 1, 1 },
		{ 0, 0, 0, 0, 0, 0, 1, 1 }
	};

	private const int SAMPLE_RATE        = 48000;
	private const int SAMPLE_BUFFER_SIZE = SAMPLE_RATE / 10;

	public const int VOLUME_MULTIPLIER = 50;

	private int internalMainApuCounter;

	private readonly Emulator emulator;

	public readonly ApuChannel1 channel1;
	public readonly ApuChannel2 channel2;
	public readonly ApuChannel3 channel3;
	public readonly ApuChannel4 channel4;

	public Apu(Emulator emulator)
	{
		this.emulator = emulator;

		channel1 = new ApuChannel1(this, SAMPLE_RATE, SAMPLE_BUFFER_SIZE);
		channel2 = new ApuChannel2(this, SAMPLE_RATE, SAMPLE_BUFFER_SIZE);
		channel3 = new ApuChannel3(this, SAMPLE_RATE, SAMPLE_BUFFER_SIZE);
		channel4 = new ApuChannel4(this, SAMPLE_RATE, SAMPLE_BUFFER_SIZE);
	}

	public void Update(int cycles)
	{
		if (!Enabled)
		{
			Reset();
			channel1.Reset();
			channel2.Reset();
			channel3.Reset();
			channel4.Reset();

			//Update is still required, because the internal frame sequencer still ticks when the apu is disabled
			channel1.Update(cycles);
			channel2.Update(cycles);
			channel3.Update(cycles);
			channel4.Update(cycles);

			return;
		}

		channel1.Update(cycles);
		channel2.Update(cycles);
		channel3.Update(cycles);
		channel4.Update(cycles);

		internalMainApuCounter += SAMPLE_RATE * cycles;

		if (internalMainApuCounter < Emulator.GAMEBOY_CLOCK_SPEED) return;

		internalMainApuCounter -= Emulator.GAMEBOY_CLOCK_SPEED;

		//TODO Play sound with adjusted sample rate when speed changes by changing what gets written into the sample list
		if (emulator.CurrentSpeed >= 105) return;

		if (Channel1Enabled) channel1.CollectSample();
		if (Channel2Enabled) channel2.CollectSample();
		if (Channel3Enabled) channel3.CollectSample();
		if (Channel4Enabled) channel4.CollectSample();
	}

	private void Reset()
	{
		ChannelControlRegister = 0;

		SoundOutputTerminalSelectRegister = 0;

		internalMainApuCounter = 0;
	}
}