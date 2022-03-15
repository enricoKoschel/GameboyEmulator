namespace GameboyEmulator;

public class Apu
{
	//Channel 1 registers
	private byte internalChannel1SweepRegister;

	public byte Channel1SweepRegister
	{
		get => (byte)(internalChannel1SweepRegister & 0b01111111);
		set => internalChannel1SweepRegister = (byte)(value & 0b01111111);
	}

	public byte Channel1SoundLengthWavePatternRegister { get; set; }
	public byte Channel1VolumeEnvelopeRegister         { get; set; }
	public byte Channel1FrequencyRegisterLo            { get; set; }

	private byte internalChannel1FrequencyRegisterHi;

	public byte Channel1FrequencyRegisterHi
	{
		get => (byte)(internalChannel1FrequencyRegisterHi & 0b11000111);
		set => internalChannel1FrequencyRegisterHi = (byte)(value & 0b11000111);
	}

	//Only the lower 3 bits of Channel1FrequencyRegisterHi are used
	public ushort Channel1FrequencyRegister =>
		(ushort)(Cpu.MakeWord(Channel1FrequencyRegisterHi, Channel1FrequencyRegisterLo) & 0x7FF);

	//Channel 2 registers
	public byte Channel2SoundLengthWavePatternRegister { get; set; }

	public byte Channel2VolumeEnvelopeRegister { get; set; }

	public byte Channel2FrequencyRegisterLo { get; set; }

	private byte internalChannel2FrequencyRegisterHi;

	public byte Channel2FrequencyRegisterHi
	{
		get => (byte)(internalChannel2FrequencyRegisterHi & 0b11000111);
		set => internalChannel2FrequencyRegisterHi = (byte)(value & 0b11000111);
	}

	//Channel 3 registers
	private bool internalChannel3SoundOnOffRegister;

	public byte Channel3SoundOnOffRegister
	{
		get => (byte)((internalChannel3SoundOnOffRegister ? 1 : 0) << 7);
		set => internalChannel3SoundOnOffRegister = (value & 0b10000000) != 0;
	}

	public byte Channel3SoundLengthRegister { get; set; }

	private byte internalChannel3SelectOutputLevelRegister;

	public byte Channel3SelectOutputLevelRegister
	{
		get => (byte)(internalChannel3SelectOutputLevelRegister & 0b01100000);
		set => internalChannel3SelectOutputLevelRegister = (byte)(value & 0b01100000);
	}

	public byte Channel3FrequencyRegisterLo { get; set; }

	private byte internalChannel3FrequencyRegisterHi;

	public byte Channel3FrequencyRegisterHi
	{
		get => (byte)(internalChannel3FrequencyRegisterHi & 0b11000111);
		set => internalChannel3FrequencyRegisterHi = (byte)(value & 0b11000111);
	}

	//Only the lower 3 bits of Channel3FrequencyRegisterHi are used
	public ushort Channel3FrequencyRegister =>
		(ushort)(Cpu.MakeWord(Channel3FrequencyRegisterHi, Channel3FrequencyRegisterLo) & 0x7FF);

	//Channel 4 registers
	private byte internalChannel4SoundLengthRegister;

	public byte Channel4SoundLengthRegister
	{
		get => (byte)(internalChannel4SoundLengthRegister & 0b00111111);
		set => internalChannel4SoundLengthRegister = (byte)(value & 0b00111111);
	}

	public byte Channel4VolumeEnvelopeRegister    { get; set; }
	public byte Channel4PolynomialCounterRegister { get; set; }

	private byte internalChannel4CounterConsecutiveRegister;

	public byte Channel4CounterConsecutiveRegister
	{
		get => (byte)(internalChannel4CounterConsecutiveRegister & 0b11000000);
		set => internalChannel4CounterConsecutiveRegister = (byte)(value & 0b11000000);
	}

	//Controls registers
	public byte ChannelControlRegister { get; set; }

	private bool VinLeftEnabled  => Cpu.GetBit(ChannelControlRegister, 7);
	private bool VinRightEnabled => Cpu.GetBit(ChannelControlRegister, 3);

	public byte LeftChannelVolume  => (byte)((ChannelControlRegister & 0b01110000) >> 4);
	public byte RightChannelVolume => (byte)(ChannelControlRegister & 0b00000111);

	public byte SoundOutputTerminalSelectRegister { get; set; }

	public bool SoundEnabled { get; private set; }

	private bool channel1Enabled = true;
	private bool channel2Enabled = true;
	private bool channel3Enabled = false;
	private bool channel4Enabled = false;

	public byte SoundOnOffRegister
	{
		get => Cpu.MakeByte(
			SoundEnabled, true, true, true, false /*channel4.Playing*/, false /*channel3.Playing*/, channel2.Playing,
			channel1.Playing
		);
		set => SoundEnabled = (value & 0b10000000) != 0;
	}

	private readonly byte[] wavePatternRam;

	/*
		00 - 12.5% (_-------_-------_-------)
		01 - 25%   (__------__------__------)
		10 - 50%   (____----____----____----)
		11 - 75%   (______--______--______--)
	*/
	public static readonly byte[,] WAVE_DUTY_TABLE =
	{
		{ 0, 1, 1, 1, 1, 1, 1, 1 },
		{ 0, 0, 1, 1, 1, 1, 1, 1 },
		{ 0, 0, 0, 0, 1, 1, 1, 1 },
		{ 0, 0, 0, 0, 0, 0, 1, 1 }
	};

	private const int SAMPLE_RATE       = 48000;
	public const  int VOLUME_MULTIPLIER = 50;

	private int internalMainApuCounter;

	private readonly Emulator emulator;

	private readonly ApuChannel1 channel1;
	private readonly ApuChannel2 channel2;

	//private ApuChannel channel3;
	//private ApuChannel channel4;

	public Apu(Emulator emulator)
	{
		this.emulator = emulator;

		wavePatternRam = new byte[0x10];

		if (channel1Enabled) channel1 = new ApuChannel1(this, SAMPLE_RATE, SAMPLE_RATE / 10);
		if (channel2Enabled) channel2 = new ApuChannel2(this, SAMPLE_RATE, SAMPLE_RATE / 10);
	}

	public void Update(int cycles)
	{
		if (channel1Enabled) channel1.Update(cycles);
		if (channel2Enabled) channel2.Update(cycles);

		internalMainApuCounter += SAMPLE_RATE * cycles;

		if (internalMainApuCounter < Emulator.GAMEBOY_CLOCK_SPEED) return;

		internalMainApuCounter -= Emulator.GAMEBOY_CLOCK_SPEED;

		//TODO Play sound with adjusted sample rate when speed changes by changing what gets written into the sample list
		if (emulator.CurrentSpeed >= 105) return;

		if (channel1Enabled) channel1.CollectSample();
		if (channel2Enabled) channel2.CollectSample();
	}

	public byte GetWavePatternRamAtIndex(int index)
	{
		//TODO implement actual behaviour for CH3 enabled
		return internalChannel3SoundOnOffRegister ? (byte)0xFF : wavePatternRam[index];
	}

	public void SetWavePatternRamAtIndex(int index, byte data)
	{
		//TODO implement actual behaviour for CH3 enabled
		if (internalChannel3SoundOnOffRegister) return;

		wavePatternRam[index] = data;
	}
}