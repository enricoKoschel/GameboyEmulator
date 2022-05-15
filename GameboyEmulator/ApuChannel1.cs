namespace GameboyEmulator;

public class ApuChannel1
{
	private byte internalFrequencySweepRegister;

	//NR10
	public byte FrequencySweepRegister
	{
		get => (byte)(internalFrequencySweepRegister & 0b0111_1111);
		set => internalFrequencySweepRegister = (byte)(value & 0b0111_1111);
	}

	private byte internalSoundLengthWavePatternRegister;

	//NR11
	public byte SoundLengthWavePatternRegister
	{
		get => internalSoundLengthWavePatternRegister;
		set
		{
			internalSoundLengthWavePatternRegister = value;
			SoundLengthWritten();
		}
	}

	//NR12
	public byte VolumeEnvelopeRegister { get; set; }

	//NR13
	public byte FrequencyRegisterLo { get; set; }

	private byte internalFrequencyRegisterHi;

	//NR14
	public byte FrequencyRegisterHi
	{
		get => (byte)(internalFrequencyRegisterHi & 0b1100_0111);
		set
		{
			internalFrequencyRegisterHi = (byte)(value & 0b1100_0111);
			TriggerWritten();
		}
	}

	private byte SoundLength     => (byte)(SoundLengthWavePatternRegister & 0b0011_1111);
	private byte WavePatternDuty => (byte)((SoundLengthWavePatternRegister & 0b1100_0000) >> 6);

	private byte InitialVolume           => (byte)((VolumeEnvelopeRegister & 0b1111_0000) >> 4);
	private bool VolumeEnvelopeDirection => Cpu.GetBit(VolumeEnvelopeRegister, 3);
	private byte VolumeSweepPeriod       => (byte)(VolumeEnvelopeRegister & 0b0000_0111);

	private byte FrequencySweepPeriod      => (byte)((FrequencySweepRegister & 0b0111_0000) >> 4);
	private bool FrequencySweepDirection   => Cpu.GetBit(FrequencySweepRegister, 3);
	private byte FrequencySweepShiftAmount => (byte)(FrequencySweepRegister & 0b0000_0111);

	private bool Trigger => Cpu.GetBit(FrequencyRegisterHi, 7);

	private bool EnableLength => Cpu.GetBit(FrequencyRegisterHi, 6);

	//Only the lower 3 bits of FrequencyRegisterHi are used
	private ushort FrequencyRegister
	{
		get => (ushort)(Cpu.MakeWord(FrequencyRegisterHi, FrequencyRegisterLo) & 0x7FF);
		set
		{
			FrequencyRegisterHi =
				(byte)((FrequencyRegisterHi & 0b1111_1000) | (Cpu.GetHiByte(value) & 0b0000_0111));

			FrequencyRegisterLo = Cpu.GetLoByte(value);
		}
	}

	private bool LeftEnabled  => Cpu.GetBit(apu.SoundOutputTerminalSelectRegister, 4);
	private bool RightEnabled => Cpu.GetBit(apu.SoundOutputTerminalSelectRegister, 0);

	private int frequencyTimer;

	private int waveDutyPosition;

	private int currentFrameSequencerTick;

	private int lengthTimer;

	private int currentEnvelopeVolume;
	private int volumePeriodTimer;

	private int  sweepTimer;
	private int  shadowFrequency;
	private bool sweepEnabled;

	public bool Playing { get; private set; }

	private readonly Apu apu;

	public ApuChannel1(Apu apu)
	{
		this.apu = apu;
	}

	public void Update(int cycles)
	{
		CheckDacEnabled();

		if (!Playing) return;

		if (apu.ShouldTickFrameSequencer) TickFrameSequencer();

		frequencyTimer -= cycles;
		if (frequencyTimer > 0) return;

		frequencyTimer += (2048 - FrequencyRegister) * 4;

		waveDutyPosition++;
		waveDutyPosition %= 8;
	}

	private void SoundLengthWritten()
	{
		lengthTimer = 64 - SoundLength;
	}

	private void TriggerWritten()
	{
		if (!apu.Enabled || !Trigger) return;

		Playing = true;

		if (lengthTimer == 0) lengthTimer = 64;

		volumePeriodTimer     = VolumeSweepPeriod;
		currentEnvelopeVolume = InitialVolume;

		shadowFrequency = FrequencyRegister;
		sweepTimer      = FrequencySweepPeriod != 0 ? FrequencySweepPeriod : 8;
		sweepEnabled    = FrequencySweepPeriod != 0 || FrequencySweepShiftAmount != 0;

		//For overflow check
		if (FrequencySweepShiftAmount != 0) CalculateNewFrequency();

		CheckDacEnabled();
	}

	private void CheckDacEnabled()
	{
		bool dacEnabled          = InitialVolume != 0 || VolumeEnvelopeDirection;
		if (!dacEnabled) Playing = false;
	}

	public void Reset()
	{
		internalFrequencySweepRegister = 0;
		FrequencySweepRegister         = 0;

		internalSoundLengthWavePatternRegister = 0;
		SoundLengthWavePatternRegister         = 0;

		VolumeEnvelopeRegister = 0;

		FrequencyRegisterLo = 0;

		internalFrequencyRegisterHi = 0;
		FrequencyRegisterHi         = 0;

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

	private void TickFrameSequencer()
	{
		//Only the length gets updated when the channel is disabled
		if (currentFrameSequencerTick % 2 == 0) UpdateLength();
		if (Playing && currentFrameSequencerTick == 7) UpdateVolume();
		if (Playing && currentFrameSequencerTick is 2 or 6) UpdateSweep();

		currentFrameSequencerTick++;
		currentFrameSequencerTick %= 8;
	}

	private void UpdateLength()
	{
		if (!EnableLength) return;

		if (lengthTimer <= 0 || --lengthTimer != 0) return;

		Playing = false;
	}

	private void UpdateVolume()
	{
		if (volumePeriodTimer > 0) volumePeriodTimer--;
		if (volumePeriodTimer != 0) return;

		if (VolumeSweepPeriod == 0) return;

		volumePeriodTimer = VolumeSweepPeriod;

		int newVolume = currentEnvelopeVolume + (VolumeEnvelopeDirection ? 1 : -1);

		if (newVolume is >= 0 and < 16) currentEnvelopeVolume = newVolume;
	}

	private void UpdateSweep()
	{
		if (sweepTimer > 0) sweepTimer--;
		if (sweepTimer != 0) return;

		sweepTimer = FrequencySweepPeriod != 0 ? FrequencySweepPeriod : 8;

		if (!sweepEnabled || FrequencySweepPeriod == 0) return;

		int newFrequency = CalculateNewFrequency();

		if (newFrequency >= 2048 || FrequencySweepShiftAmount <= 0) return;

		FrequencyRegister = (ushort)newFrequency;
		shadowFrequency   = newFrequency;

		//For overflow check
		CalculateNewFrequency();
	}

	private int CalculateNewFrequency()
	{
		int newFrequency = shadowFrequency >> FrequencySweepShiftAmount;

		if (FrequencySweepDirection) newFrequency = -newFrequency;

		newFrequency += shadowFrequency;

		if (newFrequency >= 2048) Playing = false;

		return newFrequency;
	}

	public short GetCurrentAmplitudeLeft()
	{
		if (!apu.Enabled || !LeftEnabled) return 0;

		double volume = currentEnvelopeVolume * apu.LeftChannelVolume * Apu.VOLUME_MULTIPLIER;

		return (short)(Apu.WAVE_DUTY_TABLE[WavePatternDuty, waveDutyPosition] * volume);
	}

	public short GetCurrentAmplitudeRight()
	{
		if (!apu.Enabled || !RightEnabled) return 0;

		double volume = currentEnvelopeVolume * apu.RightChannelVolume * Apu.VOLUME_MULTIPLIER;

		return (short)(Apu.WAVE_DUTY_TABLE[WavePatternDuty, waveDutyPosition] * volume);
	}
}