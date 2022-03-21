namespace GameboyEmulator;

public class ApuChannel2 : ApuChannel
{
	//NR21
	public byte SoundLengthWavePatternRegister { get; set; }

	//NR22
	public byte VolumeEnvelopeRegister { get; set; }

	//NR23
	public byte FrequencyRegisterLo { get; set; }

	private byte internalFrequencyRegisterHi;

	//NR24
	public byte FrequencyRegisterHi
	{
		get => (byte)(internalFrequencyRegisterHi & 0b1100_0111);
		set => internalFrequencyRegisterHi = (byte)(value & 0b1100_0111);
	}

	private bool LeftEnabled  => Cpu.GetBit(apu.SoundOutputTerminalSelectRegister, 5);
	private bool RightEnabled => Cpu.GetBit(apu.SoundOutputTerminalSelectRegister, 1);

	public bool Playing { get; private set; } = false;

	private readonly Apu apu;

	public ApuChannel2(Apu apu, int sampleRate, int bufferSize) : base(sampleRate, bufferSize)
	{
		this.apu = apu;
	}

	public void Update(int cycles)
	{
	}

	public void Reset()
	{
		SoundLengthWavePatternRegister = 0;

		VolumeEnvelopeRegister = 0;

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