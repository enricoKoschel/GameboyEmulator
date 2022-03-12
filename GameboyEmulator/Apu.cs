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
	public byte Channel2VolumeEnvelopeRegister         { get; set; }
	public byte Channel2FrequencyRegisterLo            { get; set; }

	private byte internalChannel2FrequencyRegisterHi;

	public byte Channel2FrequencyRegisterHi
	{
		get => (byte)(internalChannel2FrequencyRegisterHi & 0b11000111);
		set => internalChannel2FrequencyRegisterHi = (byte)(value & 0b11000111);
	}

	//Only the lower 3 bits of Channel2FrequencyRegisterHi are used
	public ushort Channel2FrequencyRegister =>
		(ushort)(Cpu.MakeWord(Channel2FrequencyRegisterHi, Channel2FrequencyRegisterLo) & 0x7FF);

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
	public byte ChannelControlRegister            { get; set; }
	public byte SoundOutputTerminalSelectRegister { get; set; }

	private byte internalSoundOnOffRegister;

	public byte SoundOnOffRegister
	{
		get => (byte)(internalSoundOnOffRegister & 0b10001111);
		set => internalSoundOnOffRegister = (byte)(value & 0b10000000);
	}

	private readonly byte[] wavePatternRam;

	private const int SAMPLE_RATE = 44100;

	private int internalApuCounter;

	public Apu()
	{
		wavePatternRam = new byte[0x10];
	}

	public void Update(int cycles)
	{
		UpdateChannel1();
		UpdateChannel2();
		UpdateChannel3();
		UpdateChannel4();

		internalApuCounter += SAMPLE_RATE * cycles;

		if (internalApuCounter >= Emulator.GAMEBOY_CLOCK_SPEED)
		{
			internalApuCounter -= Emulator.GAMEBOY_CLOCK_SPEED;

			//TODO Save current state of APU as sample
		}
	}

	private void UpdateChannel1()
	{
	}

	private void UpdateChannel2()
	{
	}

	private void UpdateChannel3()
	{
	}

	private void UpdateChannel4()
	{
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