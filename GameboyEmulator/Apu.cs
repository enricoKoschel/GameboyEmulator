using System;
using System.Collections.Generic;
using System.Threading;

namespace GameboyEmulator;

public class Apu
{
	//Channel 1 registers
	private byte internalChannel1SweepRegister;

	public byte Channel1SweepRegister
	{
		get => (byte)(internalChannel1SweepRegister & 0b01111111);
		set => internalChannel1SweepRegister = (byte)(value & 0b01111111);
	}

	public byte Channel1SoundLengthWavePatternRegister { get; set; }
	public byte Channel1VolumeEnvelopeRegister         { get; set; }
	public byte Channel1FrequencyRegisterLo            { get; set; }

	private byte internalChannel1FrequencyRegisterHi;

	public byte Channel1FrequencyRegisterHi
	{
		get => (byte)(internalChannel1FrequencyRegisterHi & 0b11000111);
		set => internalChannel1FrequencyRegisterHi = (byte)(value & 0b11000111);
	}

	//Only the lower 3 bits of Channel1FrequencyRegisterHi are used
	public ushort Channel1FrequencyRegister =>
		(ushort)(Cpu.MakeWord(Channel1FrequencyRegisterHi, Channel1FrequencyRegisterLo) & 0x7FF);

	//Channel 2 registers
	public byte Channel2SoundLengthWavePatternRegister { get; set; }
	public byte Channel2VolumeEnvelopeRegister         { get; set; }
	public byte Channel2FrequencyRegisterLo            { get; set; }

	private byte internalChannel2FrequencyRegisterHi;

	public byte Channel2FrequencyRegisterHi
	{
		get => (byte)(internalChannel2FrequencyRegisterHi & 0b11000111);
		set => internalChannel2FrequencyRegisterHi = (byte)(value & 0b11000111);
	}

	//Only the lower 3 bits of Channel2FrequencyRegisterHi are used
	private ushort Channel2FrequencyRegister =>
		(ushort)(Cpu.MakeWord(Channel2FrequencyRegisterHi, Channel2FrequencyRegisterLo) & 0x7FF);

	private byte Channel2WavePatternDuty => (byte)((Channel2SoundLengthWavePatternRegister & 0b11000000) >> 6);

	private int channel2FrequencyTimer;
	private int channel2WaveDutyPosition;

	//Channel 3 registers
	private bool internalChannel3SoundOnOffRegister;

	public byte Channel3SoundOnOffRegister
	{
		get => (byte)((internalChannel3SoundOnOffRegister ? 1 : 0) << 7);
		set => internalChannel3SoundOnOffRegister = (value & 0b10000000) != 0;
	}

	public byte Channel3SoundLengthRegister { get; set; }

	private byte internalChannel3SelectOutputLevelRegister;

	public byte Channel3SelectOutputLevelRegister
	{
		get => (byte)(internalChannel3SelectOutputLevelRegister & 0b01100000);
		set => internalChannel3SelectOutputLevelRegister = (byte)(value & 0b01100000);
	}

	public byte Channel3FrequencyRegisterLo { get; set; }

	private byte internalChannel3FrequencyRegisterHi;

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

	public byte Channel4SoundLengthRegister
	{
		get => (byte)(internalChannel4SoundLengthRegister & 0b00111111);
		set => internalChannel4SoundLengthRegister = (byte)(value & 0b00111111);
	}

	public byte Channel4VolumeEnvelopeRegister    { get; set; }
	public byte Channel4PolynomialCounterRegister { get; set; }

	private byte internalChannel4CounterConsecutiveRegister;

	public byte Channel4CounterConsecutiveRegister
	{
		get => (byte)(internalChannel4CounterConsecutiveRegister & 0b11000000);
		set => internalChannel4CounterConsecutiveRegister = (byte)(value & 0b11000000);
	}

	//Controls registers
	public byte ChannelControlRegister            { get; set; }
	public byte SoundOutputTerminalSelectRegister { get; set; }

	private byte internalSoundOnOffRegister;

	public byte SoundOnOffRegister
	{
		get => (byte)(internalSoundOnOffRegister & 0b10001111);
		set => internalSoundOnOffRegister = (byte)(value & 0b10000000);
	}

	private readonly byte[] wavePatternRam;

	/*
		00 - 12.5% (_-------_-------_-------)
		01 - 25%   (__------__------__------)
		10 - 50%   (____----____----____----)
		11 - 75%   (______--______--______--)
	*/
	private static readonly bool[,] WAVE_DUTY_TABLE =
	{
		{ false, true, true, true, true, true, true, true },
		{ false, false, true, true, true, true, true, true },
		{ false, false, false, false, true, true, true, true },
		{ false, false, false, false, false, false, true, true }
	};

	private const int SAMPLE_RATE = 44100;

	private int internalMainApuCounter;
	private int internalFrameSequencerCounter;

	private Emulator emulator;

	public Apu(Emulator emulator)
	{
		this.emulator = emulator;

		wavePatternRam = new byte[0x10];
	}

	public void Update(int cycles)
	{
		UpdateChannel1();
		UpdateChannel2(cycles);
		UpdateChannel3();
		UpdateChannel4();

		UpdateFrameSequencer(cycles);

		internalMainApuCounter += SAMPLE_RATE * cycles;

		if (internalMainApuCounter >= Emulator.GAMEBOY_CLOCK_SPEED)
		{
			internalMainApuCounter -= Emulator.GAMEBOY_CLOCK_SPEED;

			//TODO Save current state of APU as sample

			//TODO Play sound with adjusted sample rate when speed changes by changing what gets written into the sample list
			if (emulator.CurrentSpeed == 100) ch2.AddSample(GetCurrentChannel2AmplitudeLeft());
		}
	}

	private ApuChannel ch2 = new(1, SAMPLE_RATE, SAMPLE_RATE / 10);

	private void UpdateFrameSequencer(int cycles)
	{
		if ((internalFrameSequencerCounter += cycles) >= 8192)
		{
			internalFrameSequencerCounter -= 8192;

			//TODO Update Length, envelope and sweep functions
		}
	}

	private void UpdateChannel1()
	{
	}

	private void UpdateChannel2(int cycles)
	{
		channel2FrequencyTimer -= cycles;

		if (channel2FrequencyTimer > 0) return;

		channel2FrequencyTimer += (2048 - Channel2FrequencyRegister) * 4;

		channel2WaveDutyPosition++;
		channel2WaveDutyPosition %= 8;
	}

	private void UpdateChannel3()
	{
	}

	private void UpdateChannel4()
	{
	}

	private void GetCurrentChannel1AmplitudeLeft()
	{
	}

	private void GetCurrentChannel1AmplitudeRight()
	{
	}

	private short GetCurrentChannel2AmplitudeLeft()
	{
		if (!Cpu.GetBit(SoundOutputTerminalSelectRegister, 5) &&
			!Cpu.GetBit(SoundOutputTerminalSelectRegister, 1) &&
			!Cpu.GetBit(SoundOnOffRegister, 7)) return 0;

		return (short)((WAVE_DUTY_TABLE[Channel2WavePatternDuty, channel2WaveDutyPosition] ? 1 : 0) * 3000);
	}

	private short GetCurrentChannel2AmplitudeRight()
	{
		if (!Cpu.GetBit(SoundOutputTerminalSelectRegister, 5) &&
			!Cpu.GetBit(SoundOutputTerminalSelectRegister, 1) &&
			!Cpu.GetBit(SoundOnOffRegister, 7)) return 0;

		return (short)((WAVE_DUTY_TABLE[Channel2WavePatternDuty, channel2WaveDutyPosition] ? 1 : 0) * 3000);
	}

	private void GetCurrentChannel3AmplitudeLeft()
	{
	}

	private void GetCurrentChannel3AmplitudeRight()
	{
	}

	private void GetCurrentChannel4AmplitudeLeft()
	{
	}

	private void GetCurrentChannel4AmplitudeRight()
	{
	}

	public byte GetWavePatternRamAtIndex(int index)
	{
		//TODO implement actual behaviour for CH3 enabled
		return internalChannel3SoundOnOffRegister ? (byte)0xFF : wavePatternRam[index];
	}

	public void SetWavePatternRamAtIndex(int index, byte data)
	{
		//TODO implement actual behaviour for CH3 enabled
		if (internalChannel3SoundOnOffRegister) return;

		wavePatternRam[index] = data;
	}
}