using System;

namespace GameboyEmulator;

public class ApuChannel1 : ApuChannel
{
	private byte SoundLength     => (byte)(apu.Channel1SoundLengthWavePatternRegister & 0b00111111);
	private byte WavePatternDuty => (byte)((apu.Channel1SoundLengthWavePatternRegister & 0b11000000) >> 6);

	private byte InitialVolume           => (byte)((apu.Channel1VolumeEnvelopeRegister & 0b11110000) >> 4);
	private bool VolumeEnvelopeDirection => Cpu.GetBit(apu.Channel1VolumeEnvelopeRegister, 3);
	private byte VolumeSweepPeriod       => (byte)(apu.Channel1VolumeEnvelopeRegister & 0b00000111);

	private byte FrequencySweepPeriod => (byte)((apu.Channel1SweepRegister & 0b01110000) >> 4);
	private bool SweepDirection       => Cpu.GetBit(apu.Channel1SweepRegister, 3);
	private byte SweepAmount          => (byte)(apu.Channel1SweepRegister & 0b00000111);

	private bool Trigger => Cpu.GetBit(apu.Channel1FrequencyRegisterHi, 7);


	private bool EnableLength => Cpu.GetBit(apu.Channel1FrequencyRegisterHi, 6);

	//Only the lower 3 bits of Channel1FrequencyRegisterHi are used
	private ushort FrequencyRegister
	{
		get => (ushort)(Cpu.MakeWord(apu.Channel1FrequencyRegisterHi, apu.Channel1FrequencyRegisterLo) & 0x7FF);
		set
		{
			apu.Channel1FrequencyRegisterHi =
				(byte)((apu.Channel1FrequencyRegisterHi & 0b11111000) | (Cpu.GetHiByte(value) & 0b00000111));

			apu.Channel1FrequencyRegisterLo = Cpu.GetLoByte(value);
		}
	}

	private bool LeftEnabled  => Cpu.GetBit(apu.SoundOutputTerminalSelectRegister, 4);
	private bool RightEnabled => Cpu.GetBit(apu.SoundOutputTerminalSelectRegister, 0);

	private int frequencyTimer;

	private int waveDutyPosition;

	private int frameSequencerCounter;
	private int currentFrameSequencerTick;

	private int lengthTimer;

	private int currentEnvelopeVolume;
	private int volumePeriodTimer;

	private int  sweepTimer;
	private int  shadowFrequency;
	private bool sweepEnabled;

	public bool Playing { get; private set; }

	private readonly Apu apu;

	public ApuChannel1(Apu apu, int sampleRate, int bufferSize) : base(sampleRate, bufferSize)
	{
		this.apu = apu;
	}

	public void Update(int cycles)
	{
		//The frame sequencer still gets ticked but no components get updated when the apu is disabled
		if (!apu.SoundEnabled) UpdateFrameSequencer(cycles, false, true);

		CheckDacEnabled();

		if (!Playing)
		{
			//The length still gets updated when the channel is disabled
			UpdateFrameSequencer(cycles, true);
			return;
		}

		UpdateFrameSequencer(cycles);

		frequencyTimer -= cycles;
		if (frequencyTimer > 0) return;

		frequencyTimer += (2048 - FrequencyRegister) * 4;

		waveDutyPosition++;
		waveDutyPosition %= 8;
	}

	public void SoundLengthWritten()
	{
		lengthTimer = 64 - SoundLength;
	}

	public void TriggerWritten()
	{
		if (!apu.SoundEnabled || !Trigger) return;

		Playing = true;

		if (lengthTimer == 0) lengthTimer = 64;

		volumePeriodTimer     = VolumeSweepPeriod;
		currentEnvelopeVolume = InitialVolume;

		shadowFrequency = FrequencyRegister;
		sweepTimer      = FrequencySweepPeriod != 0 ? FrequencySweepPeriod : 8;
		sweepEnabled    = FrequencySweepPeriod != 0 || SweepAmount != 0;

		//For overflow check
		if (SweepAmount != 0) CalculateNewFrequency();

		CheckDacEnabled();
	}

	private void CheckDacEnabled()
	{
		bool dacEnabled          = InitialVolume != 0 || VolumeEnvelopeDirection;
		if (!dacEnabled) Playing = false;
	}

	public void Reset()
	{
		frequencyTimer = 0;

		waveDutyPosition = 0;

		currentFrameSequencerTick = 0;

		lengthTimer = 0;

		currentEnvelopeVolume = 0;
		volumePeriodTimer     = 0;

		sweepTimer      = 0;
		shadowFrequency = 0;
		sweepEnabled    = false;
	}

	private void UpdateFrameSequencer(int cycles, bool onlyLength = false, bool onlyTick = false)
	{
		if ((frameSequencerCounter += cycles) < 8192) return;

		frameSequencerCounter -= 8192;

		if (!onlyTick && currentFrameSequencerTick % 2 == 0) UpdateLength();
		if (!onlyTick && !onlyLength && currentFrameSequencerTick == 7) UpdateVolume();
		if (!onlyTick && !onlyLength && currentFrameSequencerTick is 2 or 6) UpdateSweep();

		currentFrameSequencerTick++;
		currentFrameSequencerTick %= 8;
	}

	private void UpdateLength()
	{
		//TODO probably works
		if (!EnableLength) return;

		if (--lengthTimer != 0) return;

		Playing = false;
	}

	private void UpdateVolume()
	{
		//TODO maybe works
		if (volumePeriodTimer > 0) volumePeriodTimer--;
		if (volumePeriodTimer != 0) return;

		if (VolumeSweepPeriod == 0) return;

		volumePeriodTimer = VolumeSweepPeriod;

		int newVolume = currentEnvelopeVolume + (VolumeEnvelopeDirection ? 1 : -1);

		if (newVolume is >= 0 and < 16) currentEnvelopeVolume = newVolume;
	}

	private void UpdateSweep()
	{
		//TODO does not work
		if (sweepTimer > 0) sweepTimer--;
		if (sweepTimer != 0) return;

		sweepTimer = FrequencySweepPeriod != 0 ? FrequencySweepPeriod : 8;

		if (!sweepEnabled || FrequencySweepPeriod == 0) return;

		int newFrequency = CalculateNewFrequency();

		if (newFrequency >= 2048 || SweepAmount <= 0) return;

		FrequencyRegister = (ushort)newFrequency;
		shadowFrequency   = newFrequency;

		//For overflow check
		CalculateNewFrequency();
	}

	private int CalculateNewFrequency()
	{
		int newFrequency = shadowFrequency >> SweepAmount;

		if (SweepDirection) newFrequency = -newFrequency;

		newFrequency += shadowFrequency;

		if (newFrequency >= 2048) Playing = false;

		return newFrequency;
	}

	public void CollectSample()
	{
		AddSamplePair(GetCurrentAmplitudeLeft(), GetCurrentAmplitudeRight());
	}

	private short GetCurrentAmplitudeLeft()
	{
		if (!apu.SoundEnabled || !LeftEnabled) return 0;

		double volume = currentEnvelopeVolume * apu.LeftChannelVolume * Apu.VOLUME_MULTIPLIER;

		return (short)(Apu.WAVE_DUTY_TABLE[WavePatternDuty, waveDutyPosition] * volume);
	}

	private short GetCurrentAmplitudeRight()
	{
		if (!apu.SoundEnabled || !RightEnabled) return 0;

		double volume = currentEnvelopeVolume * apu.RightChannelVolume * Apu.VOLUME_MULTIPLIER;

		return (short)(Apu.WAVE_DUTY_TABLE[WavePatternDuty, waveDutyPosition] * volume);
	}
}