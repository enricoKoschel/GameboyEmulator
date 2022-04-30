namespace GameboyEmulator;

public class ApuChannel2
{
	private byte internalSoundLengthWavePatternRegister;

	//NR21
	public byte SoundLengthWavePatternRegister
	{
		get => internalSoundLengthWavePatternRegister;
		set
		{
			internalSoundLengthWavePatternRegister = value;
			SoundLengthWritten();
		}
	}

	//NR22
	public byte VolumeEnvelopeRegister { get; set; }

	//NR23
	public byte FrequencyRegisterLo { get; set; }

	private byte internalFrequencyRegisterHi;

	//NR24
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

	private bool Trigger => Cpu.GetBit(FrequencyRegisterHi, 7);

	private bool EnableLength => Cpu.GetBit(FrequencyRegisterHi, 6);

	//Only the lower 3 bits of FrequencyRegisterHi are used
	private ushort FrequencyRegister => (ushort)(Cpu.MakeWord(FrequencyRegisterHi, FrequencyRegisterLo) & 0x7FF);

	private bool LeftEnabled  => Cpu.GetBit(apu.SoundOutputTerminalSelectRegister, 5);
	private bool RightEnabled => Cpu.GetBit(apu.SoundOutputTerminalSelectRegister, 1);

	private int frequencyTimer;

	private int waveDutyPosition;

	private int frameSequencerCounter;
	private int currentFrameSequencerTick;

	private int lengthTimer;

	private int currentEnvelopeVolume;
	private int volumePeriodTimer;

	public bool Playing { get; private set; }

	private readonly Apu apu;

	public ApuChannel2(Apu apu)
	{
		this.apu = apu;
	}

	public void Update(int cycles)
	{
		//The frame sequencer still gets ticked but no components get updated when the apu is disabled
		if (!apu.Enabled) UpdateFrameSequencer(cycles, false, true);

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

		CheckDacEnabled();
	}

	private void CheckDacEnabled()
	{
		bool dacEnabled          = InitialVolume != 0 || VolumeEnvelopeDirection;
		if (!dacEnabled) Playing = false;
	}

	public void Reset()
	{
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
	}

	private void UpdateFrameSequencer(int cycles, bool onlyLength = false, bool onlyTick = false)
	{
		if ((frameSequencerCounter += cycles) < 8192) return;

		frameSequencerCounter -= 8192;

		if (onlyTick) return;

		if (currentFrameSequencerTick % 2 == 0) UpdateLength();
		if (!onlyLength && currentFrameSequencerTick == 7) UpdateVolume();

		currentFrameSequencerTick++;
		currentFrameSequencerTick %= 8;
	}

	private void UpdateLength()
	{
		//TODO probably works
		if (!EnableLength) return;

		if (lengthTimer <= 0 || --lengthTimer != 0) return;

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