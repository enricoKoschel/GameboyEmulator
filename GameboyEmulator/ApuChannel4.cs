// ReSharper disable UnusedMember.Local
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable UnusedMember.Global
// TODO enable unused warnings again when channel 4 is implemented

namespace GameboyEmulator;

public class ApuChannel4 : ApuChannel
{
	private byte internalSoundLengthRegister;

	//NR41
	public byte SoundLengthRegister
	{
		get => (byte)(internalSoundLengthRegister & 0b0011_1111);
		set => internalSoundLengthRegister = (byte)(value & 0b0011_1111);
	}

	//NR42
	public byte VolumeEnvelopeRegister { get; set; }

	//NR43
	public byte PolynomialCounterRegister { get; set; }

	private byte internalCounterConsecutiveRegister;

	//NR44
	public byte CounterConsecutiveRegister
	{
		get => (byte)(internalCounterConsecutiveRegister & 0b1100_0000);
		set => internalCounterConsecutiveRegister = (byte)(value & 0b1100_0000);
	}

	private byte InitialVolume           => (byte)((VolumeEnvelopeRegister & 0b1111_0000) >> 4);
	private bool VolumeEnvelopeDirection => Cpu.GetBit(VolumeEnvelopeRegister, 3);
	private byte VolumeSweepPeriod       => (byte)(VolumeEnvelopeRegister & 0b0000_0111);

	private byte ShiftClockFrequency => (byte)((PolynomialCounterRegister & 0b1111_0000) >> 4);

	private bool CounterStepWidth => Cpu.GetBit(PolynomialCounterRegister, 3);

	private byte DividingRatio => (byte)(PolynomialCounterRegister & 0b0000_0111);

	private bool Trigger => Cpu.GetBit(CounterConsecutiveRegister, 7);

	private bool EnableLength => Cpu.GetBit(CounterConsecutiveRegister, 6);

	private bool LeftEnabled  => Cpu.GetBit(apu.SoundOutputTerminalSelectRegister, 7);
	private bool RightEnabled => Cpu.GetBit(apu.SoundOutputTerminalSelectRegister, 3);

	public bool Playing { get; private set; } = false;

	private readonly Apu apu;

	public ApuChannel4(Apu apu, int sampleRate, int bufferSize) : base(sampleRate, bufferSize)
	{
		this.apu = apu;
	}

	public void Update(int cycles)
	{
	}

	public void Reset()
	{
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