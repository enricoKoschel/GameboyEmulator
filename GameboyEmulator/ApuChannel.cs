using System;
using System.Collections.Generic;
using System.Threading;
using SFML.Audio;
using SFML.System;

namespace GameboyEmulator;

public class ApuChannel : SoundStream
{
	private List<short> sampleBuffer;
	private int         bufferSize;
	private uint        sampleRate;
	private Mutex       mutex;

	public ApuChannel(uint channels, uint sampleRate, int bufferSize)
	{
		this.sampleRate = sampleRate;
		this.bufferSize = bufferSize;

		sampleBuffer = new List<short>(bufferSize);

		mutex = new Mutex();

		Initialize(channels, sampleRate);
		Play();
	}

	public void AddSample(short sample)
	{
		mutex.WaitOne();
		sampleBuffer.Add(sample);
		mutex.ReleaseMutex();
	}

	protected override bool OnGetData(out short[] samples)
	{
		Console.WriteLine("shesh");

		while (sampleBuffer.Count < bufferSize) ;

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