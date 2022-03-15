using System.Collections.Generic;
using System.Threading;
using SFML.Audio;
using SFML.System;

namespace GameboyEmulator;

public abstract class ApuChannel : SoundStream
{
	private List<short> sampleBuffer;
	private int         bufferSize;
	private int         sampleRate;
	private Mutex       mutex;

	protected ApuChannel(int sampleRate, int bufferSize)
	{
		this.sampleRate = sampleRate;
		this.bufferSize = bufferSize * 2;

		sampleBuffer = new List<short>(bufferSize * 2);

		mutex = new Mutex();

		Initialize(2, (uint)sampleRate);
		Play();
	}

	protected void AddSamplePair(short sampleLeft, short sampleRight)
	{
		mutex.WaitOne();
		sampleBuffer.Add(sampleLeft);
		sampleBuffer.Add(sampleRight);
		mutex.ReleaseMutex();
	}

	protected override bool OnGetData(out short[] samples)
	{
		while (sampleBuffer.Count < bufferSize)
		{
		}

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