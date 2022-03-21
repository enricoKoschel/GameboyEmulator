namespace GameboyEmulator;

public class ApuChannel3 : ApuChannel
{
	private bool internalSoundOnOffRegister;

	//NR30
	public byte SoundOnOffRegister
	{
		get => (byte)((internalSoundOnOffRegister ? 1 : 0) << 7);
		set => internalSoundOnOffRegister = (value & 0b1000_0000) != 0;
	}

	//NR31
	public byte SoundLengthRegister { get; set; }

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
		set => internalFrequencyRegisterHi = (byte)(value & 0b1100_0111);
	}

	//Only the lower 3 bits of FrequencyRegisterHi are used
	public ushort FrequencyRegister => (ushort)(Cpu.MakeWord(FrequencyRegisterHi, FrequencyRegisterLo) & 0x7FF);

	private bool LeftEnabled  => Cpu.GetBit(apu.SoundOutputTerminalSelectRegister, 6);
	private bool RightEnabled => Cpu.GetBit(apu.SoundOutputTerminalSelectRegister, 2);

	public bool Playing { get; private set; } = false;

	private readonly Apu apu;

	public ApuChannel3(Apu apu, int sampleRate, int bufferSize) : base(sampleRate, bufferSize)
	{
		this.apu = apu;
	}

	public void Update(int cycles)
	{
	}

	public void Reset()
	{
		internalSoundOnOffRegister = false;
		SoundOnOffRegister         = 0;

		SoundLengthRegister = 0;

		internalSelectOutputLevelRegister = 0;
		SelectOutputLevelRegister         = 0;

		FrequencyRegisterLo = 0;

		internalFrequencyRegisterHi = 0;
		FrequencyRegisterHi         = 0;
	}

	protected override short GetCurrentAmplitudeLeft()
	{
		return 0;
	}

	protected override short GetCurrentAmplitudeRight()
	{
		return 0;
	}
}