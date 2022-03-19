﻿namespace GameboyEmulator;

public class Apu
{
	//Channel 1 registers
	private byte internalChannel1SweepRegister;

	//NR10
	public byte Channel1SweepRegister
	{
		get => (byte)(internalChannel1SweepRegister & 0b01111111);
		set => internalChannel1SweepRegister = (byte)(value & 0b01111111);
	}

	private byte internalChannel1SoundLengthWavePatternRegister;

	//NR11
	public byte Channel1SoundLengthWavePatternRegister
	{
		get => internalChannel1SoundLengthWavePatternRegister;
		set
		{
			internalChannel1SoundLengthWavePatternRegister = value;
			channel1.SoundLengthWritten();
		}
	}

	//NR12
	public byte Channel1VolumeEnvelopeRegister { get; set; }

	//NR13
	public byte Channel1FrequencyRegisterLo { get; set; }

	private byte internalChannel1FrequencyRegisterHi;

	//NR14
	public byte Channel1FrequencyRegisterHi
	{
		get => (byte)(internalChannel1FrequencyRegisterHi & 0b11000111);
		set
		{
			internalChannel1FrequencyRegisterHi = (byte)(value & 0b11000111);
			channel1.TriggerWritten();
		}
	}

	//Channel 2 registers

	//NR21
	public byte Channel2SoundLengthWavePatternRegister { get; set; }

	//NR22
	public byte Channel2VolumeEnvelopeRegister { get; set; }

	//NR23
	public byte Channel2FrequencyRegisterLo { get; set; }

	private byte internalChannel2FrequencyRegisterHi;

	//NR24
	public byte Channel2FrequencyRegisterHi
	{
		get => (byte)(internalChannel2FrequencyRegisterHi & 0b11000111);
		set => internalChannel2FrequencyRegisterHi = (byte)(value & 0b11000111);
	}

	//Channel 3 registers
	private bool internalChannel3SoundOnOffRegister;

	//NR30
	public byte Channel3SoundOnOffRegister
	{
		get => (byte)((internalChannel3SoundOnOffRegister ? 1 : 0) << 7);
		set => internalChannel3SoundOnOffRegister = (value & 0b10000000) != 0;
	}

	//NR31
	public byte Channel3SoundLengthRegister { get; set; }

	private byte internalChannel3SelectOutputLevelRegister;

	//NR32
	public byte Channel3SelectOutputLevelRegister
	{
		get => (byte)(internalChannel3SelectOutputLevelRegister & 0b01100000);
		set => internalChannel3SelectOutputLevelRegister = (byte)(value & 0b01100000);
	}

	//NR33
	public byte Channel3FrequencyRegisterLo { get; set; }

	private byte internalChannel3FrequencyRegisterHi;

	//NR34
	public byte Channel3FrequencyRegisterHi
	{
		get => (byte)(internalChannel3FrequencyRegisterHi & 0b11000111);
		set => internalChannel3FrequencyRegisterHi = (byte)(value & 0b11000111);
	}

	//Only the lower 3 bits of Channel3FrequencyRegisterHi are used
	public ushort Channel3FrequencyRegister =>
		(ushort)(Cpu.MakeWord(Channel3FrequencyRegisterHi, Channel3FrequencyRegisterLo) & 0x7FF);

	//Channel 4 registers
	private byte internalChannel4SoundLengthRegister;

	//NR41
	public byte Channel4SoundLengthRegister
	{
		get => (byte)(internalChannel4SoundLengthRegister & 0b00111111);
		set => internalChannel4SoundLengthRegister = (byte)(value & 0b00111111);
	}

	//NR42
	public byte Channel4VolumeEnvelopeRegister { get; set; }

	//NR43
	public byte Channel4PolynomialCounterRegister { get; set; }

	private byte internalChannel4CounterConsecutiveRegister;

	//NR44
	public byte Channel4CounterConsecutiveRegister
	{
		get => (byte)(internalChannel4CounterConsecutiveRegister & 0b11000000);
		set => internalChannel4CounterConsecutiveRegister = (byte)(value & 0b11000000);
	}

	//Controls registers

	//NR50
	public byte ChannelControlRegister { get; set; }

	private bool VinLeftEnabled  => Cpu.GetBit(ChannelControlRegister, 7);
	private bool VinRightEnabled => Cpu.GetBit(ChannelControlRegister, 3);

	public byte LeftChannelVolume  => (byte)((ChannelControlRegister & 0b01110000) >> 4);
	public byte RightChannelVolume => (byte)(ChannelControlRegister & 0b00000111);

	//NR51
	public byte SoundOutputTerminalSelectRegister { get; set; }

	public bool SoundEnabled { get; private set; }

	private bool channel1Enabled = true;
	private bool channel2Enabled;
	private bool channel3Enabled;
	private bool channel4Enabled;

	//NR52
	public byte SoundOnOffRegister
	{
		get => Cpu.MakeByte(
			SoundEnabled, true, true, true, false /*channel4.Playing*/, false /*channel3.Playing*/,
			false /*channel2.Playing*/, channel1.Playing
		);
		set => SoundEnabled = (value & 0b10000000) != 0;
	}

	private readonly byte[] wavePatternRam;

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

	private const int SAMPLE_RATE       = 48000;
	public const  int VOLUME_MULTIPLIER = 50;

	private int internalMainApuCounter;

	private readonly Emulator emulator;

	private readonly ApuChannel1 channel1;

	//private readonly ApuChannel2 channel2;
	//private ApuChannel channel3;
	//private ApuChannel channel4;

	public Apu(Emulator emulator)
	{
		this.emulator = emulator;

		wavePatternRam = new byte[0x10];

		channel1 = new ApuChannel1(this, SAMPLE_RATE, SAMPLE_RATE / 10);
		//channel2 = new ApuChannel2(this, SAMPLE_RATE, SAMPLE_RATE / 10);
	}

	public void Update(int cycles)
	{
		if (!SoundEnabled)
		{
			Reset();
			channel1.Reset();

			//Update is still required, because the internal frame sequencer still ticks when the apu is disabled
			channel1.Update(cycles);
		}

		channel1.Update(cycles);
		//channel2.Update(cycles);

		internalMainApuCounter += SAMPLE_RATE * cycles;

		if (internalMainApuCounter < Emulator.GAMEBOY_CLOCK_SPEED) return;

		internalMainApuCounter -= Emulator.GAMEBOY_CLOCK_SPEED;

		//TODO Play sound with adjusted sample rate when speed changes by changing what gets written into the sample list
		if (emulator.CurrentSpeed >= 105) return;

		if (channel1Enabled) channel1.CollectSample();
		//if (channel2Enabled) channel2.CollectSample();
	}

	private void Reset()
	{
		internalChannel1SweepRegister = 0;
		Channel1SweepRegister         = 0;

		internalChannel1SoundLengthWavePatternRegister = 0;
		Channel1SoundLengthWavePatternRegister         = 0;

		Channel1VolumeEnvelopeRegister = 0;

		Channel1FrequencyRegisterLo = 0;

		internalChannel1FrequencyRegisterHi = 0;
		Channel1FrequencyRegisterHi         = 0;

		Channel2SoundLengthWavePatternRegister = 0;

		Channel2VolumeEnvelopeRegister = 0;

		Channel2FrequencyRegisterLo = 0;

		internalChannel2FrequencyRegisterHi = 0;
		Channel2FrequencyRegisterHi         = 0;

		internalChannel3SoundOnOffRegister = false;
		Channel3SoundOnOffRegister         = 0;

		Channel3SoundLengthRegister = 0;

		internalChannel3SelectOutputLevelRegister = 0;
		Channel3SelectOutputLevelRegister         = 0;

		Channel3FrequencyRegisterLo = 0;

		internalChannel3FrequencyRegisterHi = 0;
		Channel3FrequencyRegisterHi         = 0;

		internalChannel4SoundLengthRegister = 0;
		Channel4SoundLengthRegister         = 0;

		Channel4VolumeEnvelopeRegister = 0;

		Channel4PolynomialCounterRegister = 0;

		internalChannel4CounterConsecutiveRegister = 0;
		Channel4CounterConsecutiveRegister         = 0;

		ChannelControlRegister = 0;

		SoundOutputTerminalSelectRegister = 0;

		channel1Enabled = false;
		channel2Enabled = false;
		channel3Enabled = false;
		channel4Enabled = false;

		internalMainApuCounter = 0;
	}

	public byte GetWavePatternRamAtIndex(int index)
	{
		//TODO implement actual behaviour for CH3 enabled
		return /*internalChannel3SoundOnOffRegister ? (byte)0xFF : */wavePatternRam[index];
	}

	public void SetWavePatternRamAtIndex(int index, byte data)
	{
		//TODO implement actual behaviour for CH3 enabled
		//if (internalChannel3SoundOnOffRegister) return;

		wavePatternRam[index] = data;
	}
}