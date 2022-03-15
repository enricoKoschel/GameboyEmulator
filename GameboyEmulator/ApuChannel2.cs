namespace GameboyEmulator;

public class ApuChannel2 : ApuChannel
{
	private byte SoundLength     => (byte)(apu.Channel2SoundLengthWavePatternRegister & 0b00111111);
	private byte WavePatternDuty => (byte)((apu.Channel2SoundLengthWavePatternRegister & 0b11000000) >> 6);

	private byte InitialVolume           => (byte)((apu.Channel2VolumeEnvelopeRegister & 0b11110000) >> 4);
	private bool VolumeEnvelopeDirection => Cpu.GetBit(apu.Channel2VolumeEnvelopeRegister, 3);
	private byte VolumeSweepPeriod       => (byte)(apu.Channel2VolumeEnvelopeRegister & 0b00000111);

	private bool Restart
	{
		get => Cpu.GetBit(apu.Channel2FrequencyRegisterHi, 7);
		set => apu.Channel2FrequencyRegisterHi = Cpu.SetBit(apu.Channel2FrequencyRegisterHi, 7, value);
	}

	private bool EnableLength => Cpu.GetBit(apu.Channel2FrequencyRegisterHi, 6);

	//Only the lower 3 bits of Channel2FrequencyRegisterHi are used
	private ushort FrequencyRegister =>
		(ushort)(Cpu.MakeWord(apu.Channel2FrequencyRegisterHi, apu.Channel2FrequencyRegisterLo) & 0x7FF);

	private bool LeftEnabled  => Cpu.GetBit(apu.SoundOutputTerminalSelectRegister, 5);
	private bool RightEnabled => Cpu.GetBit(apu.SoundOutputTerminalSelectRegister, 1);

	private int frequencyTimer;

	private int waveDutyPosition;

	private int frameSequencerCounter;
	private int currentFrameSequencerTick;

	private int lengthTimer;

	private int currentEnvelopeVolume;
	private int periodTimer;

	public  bool Playing { get; private set; }
	private bool enabled = true;

	private readonly Apu apu;

	public ApuChannel2(Apu apu, int sampleRate, int bufferSize) : base(sampleRate, bufferSize)
	{
		this.apu = apu;
	}

	public void Update(int cycles)
	{
		if (Restart)
		{
			Restart = false;

			enabled = true;
			Playing = true;

			if (lengthTimer == 0) lengthTimer = 64 - SoundLength;

			periodTimer           = VolumeSweepPeriod;
			currentEnvelopeVolume = InitialVolume;
		}

		if (!enabled) return;

		UpdateFrameSequencer(cycles);

		frequencyTimer -= cycles;

		if (frequencyTimer > 0) return;

		frequencyTimer += (2048 - FrequencyRegister) * 4;

		waveDutyPosition++;
		waveDutyPosition %= 8;
	}

	private void UpdateFrameSequencer(int cycles)
	{
		if ((frameSequencerCounter += cycles) < 8192) return;

		frameSequencerCounter -= 8192;

		if (currentFrameSequencerTick % 2 == 0) UpdateLength();
		if (currentFrameSequencerTick == 7) UpdateVolume();

		currentFrameSequencerTick++;
		currentFrameSequencerTick %= 8;
	}

	private void UpdateLength()
	{
		//TODO untested - very likely does not work
		if (!EnableLength) return;

		if (--lengthTimer != 0) return;

		Playing = false;
		enabled = false;
	}

	private void UpdateVolume()
	{
		if (VolumeSweepPeriod == 0) return;

		if (periodTimer > 0) periodTimer--;

		if (periodTimer != 0) return;

		periodTimer = VolumeSweepPeriod;

		if (VolumeEnvelopeDirection && currentEnvelopeVolume < 0xF) currentEnvelopeVolume++;
		else if (!VolumeEnvelopeDirection && currentEnvelopeVolume > 0) currentEnvelopeVolume--;
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