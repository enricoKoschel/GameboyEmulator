﻿using System;
using System.Collections.Generic;
using System.Threading;
using SFML.Audio;
using SFML.System;

namespace GameboyEmulator;

public abstract class ApuChannel : SoundStream
{
	private readonly List<short> sampleBuffer;
	private readonly int         bufferSize;
	private          int         sampleRate;
	private readonly Mutex       mutex;

	protected ApuChannel(int sampleRate, int bufferSize)
	{
		this.sampleRate = sampleRate;
		this.bufferSize = bufferSize * 2;

		sampleBuffer = new List<short>(bufferSize * 2);

		mutex = new Mutex();

		Initialize(2, (uint)sampleRate);
		Play();
	}

	private void AddSamplePair(short sampleLeft, short sampleRight)
	{
		mutex.WaitOne();
		sampleBuffer.Add(sampleLeft);
		sampleBuffer.Add(sampleRight);
		mutex.ReleaseMutex();
	}

	public void CollectSample()
	{
		AddSamplePair(GetCurrentAmplitudeLeft(), GetCurrentAmplitudeRight());
	}

	protected abstract short GetCurrentAmplitudeLeft();

	protected abstract short GetCurrentAmplitudeRight();

	protected override bool OnGetData(out short[] samples)
	{
		Console.WriteLine(sampleBuffer.Count);

		while (sampleBuffer.Count < bufferSize) Thread.Sleep(1);

		mutex.WaitOne();

		samples = sampleBuffer.ToArray();
		sampleBuffer.Clear();

		mutex.ReleaseMutex();

		return true;
	}

	protected override void OnSeek(Time timeOffset)
	{
	}
}