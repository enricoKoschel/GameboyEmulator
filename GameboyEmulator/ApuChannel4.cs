namespace GameboyEmulator;

public class ApuChannel4
{
	private byte internalSoundLengthRegister;

	//NR41
	public byte SoundLengthRegister
	{
		set
		{
			internalSoundLengthRegister = (byte)(value & 0b0011_1111);
			SoundLengthWritten();
		}
	}

	//NR42
	public byte VolumeEnvelopeRegister { get; set; }

	//NR43
	public byte PolynomialCounterRegister { get; set; }

	private byte internalCounterConsecutiveRegister;

	//NR44
	public byte CounterConsecutiveRegister
	{
		get => (byte)(internalCounterConsecutiveRegister | 0b1011_1111);
		set
		{
			internalCounterConsecutiveRegister = (byte)(value & 0b1100_0000);
			TriggerWritten();
		}
	}

	private byte SoundLength => (byte)(internalSoundLengthRegister & 0b0011_1111);

	private byte InitialVolume           => (byte)((VolumeEnvelopeRegister & 0b1111_0000) >> 4);
	private bool VolumeEnvelopeDirection => Cpu.GetBit(VolumeEnvelopeRegister, 3);
	private byte VolumeSweepPeriod       => (byte)(VolumeEnvelopeRegister & 0b0000_0111);

	private byte ShiftClockFrequency => (byte)((PolynomialCounterRegister & 0b1111_0000) >> 4);

	private bool CounterStepWidth => Cpu.GetBit(PolynomialCounterRegister, 3);

	private byte DividingRatio => (byte)(PolynomialCounterRegister & 0b0000_0111);

	private byte Divisor
	{
		get
		{
			switch (DividingRatio)
			{
				case 0: return 8;
				case 1: return 16;
				case 2: return 32;
				case 3: return 48;
				case 4: return 64;
				case 5: return 80;
				case 6: return 96;
				case 7: return 112;
				default:
					Logger.Unreachable();
					return 0;
			}
		}
	}

	private bool Trigger => Cpu.GetBit(internalCounterConsecutiveRegister, 7);

	private bool EnableLength => Cpu.GetBit(internalCounterConsecutiveRegister, 6);

	private bool LeftEnabled  => Cpu.GetBit(apu.SoundOutputTerminalSelectRegister, 7);
	private bool RightEnabled => Cpu.GetBit(apu.SoundOutputTerminalSelectRegister, 3);

	private int frequencyTimer;

	private int currentFrameSequencerTick;

	private int lengthTimer;

	private int currentEnvelopeVolume;
	private int volumePeriodTimer;

	private ushort shiftRegister;

	public bool Playing { get; private set; }

	private readonly Apu apu;

	public ApuChannel4(Apu apu)
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

		frequencyTimer += Divisor << ShiftClockFrequency;

		ShiftRight();
	}

	private void ShiftRight()
	{
		bool xor = Cpu.GetBit(shiftRegister, 1) ^ Cpu.GetBit(shiftRegister, 0);

		shiftRegister >>= 1;
		shiftRegister =   Cpu.SetBit(shiftRegister, 14, xor);
		if (CounterStepWidth) shiftRegister = Cpu.SetBit(shiftRegister, 6, xor);
	}

	private void CheckDacEnabled()
	{
		bool dacEnabled          = InitialVolume != 0 || VolumeEnvelopeDirection;
		if (!dacEnabled) Playing = false;
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

		shiftRegister = 0b0111_1111_1111_1111;

		CheckDacEnabled();
	}

	private void TickFrameSequencer()
	{
		//Only the length gets updated when the channel is disabled
		if (currentFrameSequencerTick % 2 == 0) UpdateLength();
		if (Playing && currentFrameSequencerTick == 7) UpdateVolume();

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

	public void Reset()
	{
		internalSoundLengthRegister = 0;

		VolumeEnvelopeRegister = 0;

		PolynomialCounterRegister = 0;

		internalCounterConsecutiveRegister = 0;

		frequencyTimer = 0;

		currentFrameSequencerTick = 0;

		lengthTimer = 0;

		currentEnvelopeVolume = 0;
		volumePeriodTimer     = 0;

		shiftRegister = 0;
	}

	public short GetCurrentAmplitudeLeft()
	{
		if (!apu.Enabled || !LeftEnabled) return 0;

		double volume = currentEnvelopeVolume * apu.LeftChannelVolume * Apu.VOLUME_MULTIPLIER;

		return (short)((Cpu.GetBit(shiftRegister, 0) ? -1 : 1) * volume);
	}

	public short GetCurrentAmplitudeRight()
	{
		if (!apu.Enabled || !RightEnabled) return 0;

		double volume = currentEnvelopeVolume * apu.RightChannelVolume * Apu.VOLUME_MULTIPLIER;

		return (short)((Cpu.GetBit(shiftRegister, 0) ? -1 : 1) * volume);
	}
}