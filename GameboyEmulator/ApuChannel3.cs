using System.IO;

namespace GameboyEmulator;

public class ApuChannel3
{
	private bool internalSoundOnOffRegister;

	//NR30
	public byte SoundOnOffRegister
	{
		get => (byte)((internalSoundOnOffRegister ? 1 : 0) << 7);
		set => internalSoundOnOffRegister = (value & 0b1000_0000) != 0;
	}

	private byte internalSoundLengthRegister;

	//NR31
	public byte SoundLengthRegister
	{
		get => internalSoundLengthRegister;
		set
		{
			internalSoundLengthRegister = value;
			SoundLengthWritten();
		}
	}

	private byte internalSelectOutputLevelRegister;

	//NR32
	public byte SelectOutputLevelRegister
	{
		get => (byte)(internalSelectOutputLevelRegister & 0b0110_0000);
		set => internalSelectOutputLevelRegister = (byte)(value & 0b0110_0000);
	}

	//NR33
	public byte FrequencyRegisterLo { get; set; }

	private byte internalFrequencyRegisterHi;

	//NR34
	public byte FrequencyRegisterHi
	{
		get => (byte)(internalFrequencyRegisterHi & 0b1100_0111);
		set
		{
			internalFrequencyRegisterHi = (byte)(value & 0b1100_0111);
			TriggerWritten();
		}
	}

	//Only the lower 3 bits of FrequencyRegisterHi are used
	private ushort FrequencyRegister => (ushort)(Cpu.MakeWord(FrequencyRegisterHi, FrequencyRegisterLo) & 0x7FF);

	private bool SoundOnOff => Cpu.GetBit(SoundOnOffRegister, 7);

	private byte OutputLevel => (byte)((SelectOutputLevelRegister & 0b0110_0000) >> 5);

	private bool Trigger => Cpu.GetBit(FrequencyRegisterHi, 7);

	private bool EnableLength => Cpu.GetBit(FrequencyRegisterHi, 6);

	private int CurrentVolumeShiftAmount => OutputLevel switch
	{
		0b00 => 4,
		0b01 => 0,
		0b10 => 1,
		0b11 => 2,
		_    => throw new InvalidDataException($"{nameof(OutputLevel)} above 3 should be impossible!")
	};

	private bool LeftEnabled  => Cpu.GetBit(apu.SoundOutputTerminalSelectRegister, 6);
	private bool RightEnabled => Cpu.GetBit(apu.SoundOutputTerminalSelectRegister, 2);

	private int frequencyTimer;

	private int waveRamPosition;

	private int frameSequencerCounter;
	private int currentFrameSequencerTick;

	private int lengthTimer;

	public bool Playing { get; private set; }

	private readonly Apu apu;

	private readonly byte[] waveRam;

	public ApuChannel3(Apu apu)
	{
		this.apu = apu;

		waveRam = new byte[0x10];
	}

	public void Update(int cycles)
	{
		//The frame sequencer still gets ticked but no components get updated when the apu is disabled
		if (!apu.Enabled) UpdateFrameSequencer(cycles, true);

		CheckDacEnabled();

		if (!Playing) return;

		UpdateFrameSequencer(cycles);

		frequencyTimer -= cycles;
		if (frequencyTimer > 0) return;

		frequencyTimer += (2048 - FrequencyRegister) * 2;

		waveRamPosition++;
		waveRamPosition %= 0x20;
	}

	private void SoundLengthWritten()
	{
		lengthTimer = 256 - SoundLengthRegister;
	}

	private void TriggerWritten()
	{
		if (!apu.Enabled || !Trigger) return;

		Playing = true;

		if (lengthTimer == 0) lengthTimer = 256;

		CheckDacEnabled();
	}

	private void CheckDacEnabled()
	{
		if (!SoundOnOff) Playing = false;
	}

	public void Reset()
	{
		internalSoundOnOffRegister = false;
		SoundOnOffRegister         = 0;

		internalSoundLengthRegister = 0;
		SoundLengthRegister         = 0;

		internalSelectOutputLevelRegister = 0;
		SelectOutputLevelRegister         = 0;

		FrequencyRegisterLo = 0;

		internalFrequencyRegisterHi = 0;
		FrequencyRegisterHi         = 0;
	}

	private void UpdateFrameSequencer(int cycles, bool onlyTick = false)
	{
		if ((frameSequencerCounter += cycles) < 8192) return;

		frameSequencerCounter -= 8192;

		if (onlyTick) return;

		if (currentFrameSequencerTick % 2 == 0) UpdateLength();

		currentFrameSequencerTick++;
		currentFrameSequencerTick %= 8;
	}

	private void UpdateLength()
	{
		if (!EnableLength) return;

		if (lengthTimer <= 0 || --lengthTimer != 0) return;

		Playing = false;
	}

	private byte GetWaveRamSample(int index)
	{
		byte samplePair = waveRam[index / 2];

		int numberOfSample = index % 2;

		if (numberOfSample == 0) return (byte)((samplePair & 0b1111_0000) >> 4);

		return (byte)(samplePair & 0b0000_1111);
	}

	public byte GetWaveRamSamplePair(int index)
	{
		//TODO implement actual behaviour for CH3 enabled
		return Playing ? (byte)0xFF : waveRam[index];
	}

	public void SetWaveRamSamplePair(int index, byte data)
	{
		//TODO implement actual behaviour for CH3 enabled
		if (Playing) return;

		waveRam[index] = data;
	}

	public short GetCurrentAmplitudeLeft()
	{
		if (!apu.Enabled || !LeftEnabled) return 0;

		double volume = apu.LeftChannelVolume * Apu.VOLUME_MULTIPLIER;

		return (short)((GetWaveRamSample(waveRamPosition) >> CurrentVolumeShiftAmount) * volume);
	}

	public short GetCurrentAmplitudeRight()
	{
		if (!apu.Enabled || !RightEnabled) return 0;

		double volume = apu.RightChannelVolume * Apu.VOLUME_MULTIPLIER;

		return (short)((GetWaveRamSample(waveRamPosition) >> CurrentVolumeShiftAmount) * volume);
	}
}