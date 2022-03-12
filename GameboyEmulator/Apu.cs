using System;
using System.Collections.Generic;
using SFML.Audio;

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
	private int channel2waveDutyPosition;

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

	private int internalApuCounter;

	public Apu()
	{
		wavePatternRam = new byte[0x10];
	}

	private List<short> a = new();

	public void Update(int cycles, bool frameDone)
	{
		UpdateChannel1();
		UpdateChannel2(cycles);
		UpdateChannel3();
		UpdateChannel4();

		internalApuCounter += SAMPLE_RATE * cycles;

		if (internalApuCounter >= Emulator.GAMEBOY_CLOCK_SPEED)
		{
			internalApuCounter -= Emulator.GAMEBOY_CLOCK_SPEED;

			//TODO Save current state of APU as sample


			a.Add((short)((GetCurrentChannel2Amplitude() ? 1 : 0) * 3000));
		}

		if (frameDone)
		{
			SoundBuffer b = new(a.ToArray(), 1, SAMPLE_RATE);
			a.Clear();

			Sound s = new(b);
			s.Play();

			while (s.Status == SoundStatus.Playing) ;
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

		channel2waveDutyPosition += 1;
		channel2waveDutyPosition %= 8;
	}

	private void UpdateChannel3()
	{
	}

	private void UpdateChannel4()
	{
	}

	private void GetCurrentChannel1Amplitude()
	{
	}

	private bool GetCurrentChannel2Amplitude()
	{
		return WAVE_DUTY_TABLE[Channel2WavePatternDuty, channel2waveDutyPosition];
	}

	private void GetCurrentChannel3Amplitude()
	{
	}

	private void GetCurrentChannel4Amplitude()
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