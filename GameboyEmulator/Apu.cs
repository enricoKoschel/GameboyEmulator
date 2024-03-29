﻿using System.Collections.Generic;
using SFML.Audio;
using SFML.System;

namespace GameboyEmulator;

public class Apu : SoundStream
{
	//Controls registers
	//NR50
	public byte ChannelControlRegister { get; set; }

	public byte LeftChannelVolume  => (byte)((ChannelControlRegister & 0b0111_0000) >> 4);
	public byte RightChannelVolume => (byte)(ChannelControlRegister & 0b0000_0111);

	//NR51
	public byte SoundOutputTerminalSelectRegister { get; set; }

	public bool Channel1Enabled { get; set; } = true;
	public bool Channel2Enabled { get; set; } = true;
	public bool Channel3Enabled { get; set; } = true;
	public bool Channel4Enabled { get; set; } = true;

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
	public static readonly sbyte[,] WAVE_DUTY_TABLE =
	{
		{ -1, 1, 1, 1, 1, 1, 1, 1 },
		{ -1, -1, 1, 1, 1, 1, 1, 1 },
		{ -1, -1, -1, -1, 1, 1, 1, 1 },
		{ -1, -1, -1, -1, -1, -1, 1, 1 }
	};

	private const int SAMPLE_RATE                        = 48000;
	private const int SAMPLE_BUFFER_SIZE_IN_MILLISECONDS = 50;
	private const int CHANNEL_COUNT                      = 2;

	public const int SAMPLE_BUFFER_SIZE =
		(int)(SAMPLE_RATE * CHANNEL_COUNT * (SAMPLE_BUFFER_SIZE_IN_MILLISECONDS / 1000f));

	public const int VOLUME_MULTIPLIER = 25;

	public int AmountOfSamples
	{
		get
		{
			lock (sampleBuffer)
			{
				return sampleBuffer.Count;
			}
		}
	}


	private int internalMainApuCounter;

	public readonly ApuChannel1 channel1;
	public readonly ApuChannel2 channel2;
	public readonly ApuChannel3 channel3;
	public readonly ApuChannel4 channel4;

	private readonly List<short> sampleBuffer;

	private readonly Emulator emulator;

	public Apu(Emulator emulator)
	{
		this.emulator = emulator;

		channel1 = new ApuChannel1(this);
		channel2 = new ApuChannel2(this);
		channel3 = new ApuChannel3(this);
		channel4 = new ApuChannel4(this);

		sampleBuffer = new List<short>(SAMPLE_BUFFER_SIZE);

		Initialize(CHANNEL_COUNT, SAMPLE_RATE);
		Play();
	}

	public bool ShouldTickFrameSequencer { get; private set; }

	public void TickFrameSequencer()
	{
		if (!Enabled) return;

		ShouldTickFrameSequencer = true;
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

			return;
		}

		channel1.Update(cycles);
		channel2.Update(cycles);
		channel3.Update(cycles);
		channel4.Update(cycles);

		if (ShouldTickFrameSequencer) ShouldTickFrameSequencer = false;

		internalMainApuCounter += SAMPLE_RATE * cycles;

		if (internalMainApuCounter < Emulator.GAMEBOY_CLOCK_SPEED) return;

		internalMainApuCounter -= Emulator.GAMEBOY_CLOCK_SPEED;

		lock (sampleBuffer)
		{
			if (sampleBuffer.Count >= SAMPLE_BUFFER_SIZE && emulator.MaxFps == 0) return;
		}

		short leftSample  = 0;
		short rightSample = 0;

		if (Channel1Enabled)
		{
			leftSample  += channel1.GetCurrentAmplitudeLeft();
			rightSample += channel1.GetCurrentAmplitudeRight();
		}

		if (Channel2Enabled)
		{
			leftSample  += channel2.GetCurrentAmplitudeLeft();
			rightSample += channel2.GetCurrentAmplitudeRight();
		}

		if (Channel3Enabled)
		{
			leftSample  += channel3.GetCurrentAmplitudeLeft();
			rightSample += channel3.GetCurrentAmplitudeRight();
		}

		if (Channel4Enabled)
		{
			leftSample  += channel4.GetCurrentAmplitudeLeft();
			rightSample += channel4.GetCurrentAmplitudeRight();
		}

		lock (sampleBuffer)
		{
			sampleBuffer.Add(leftSample);
			sampleBuffer.Add(rightSample);
		}
	}

	protected override bool OnGetData(out short[] samples)
	{
		lock (sampleBuffer)
		{
			if (sampleBuffer.Count >= SAMPLE_BUFFER_SIZE)
			{
				samples = sampleBuffer.GetRange(0, SAMPLE_BUFFER_SIZE).ToArray();

				sampleBuffer.RemoveRange(0, SAMPLE_BUFFER_SIZE);
			}
			else
			{
				//Dispatching events (i.e. moving the window) causes emulator to freeze so play silence
				if (emulator.inputOutput.DispatchingEvents || sampleBuffer.Count == 0)
					samples = new short[SAMPLE_BUFFER_SIZE];
				else
				{
					//Repeat full part of the buffer
					samples = new short[SAMPLE_BUFFER_SIZE];
					for (int i = 0; i < samples.Length; i++) samples[i] = sampleBuffer[i % sampleBuffer.Count];
				}
			}
		}

		return true;
	}

	protected override void OnSeek(Time timeOffset)
	{
		//Function is unused
	}

	public void ClearSampleBuffer()
	{
		lock (sampleBuffer)
		{
			sampleBuffer.Clear();
		}
	}

	private void Reset()
	{
		ChannelControlRegister = 0;

		SoundOutputTerminalSelectRegister = 0;

		internalMainApuCounter = 0;
	}
}