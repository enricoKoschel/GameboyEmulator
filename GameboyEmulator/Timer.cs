namespace GameboyEmulator;

public class Timer
{
	private readonly Emulator emulator;

	public Timer(Emulator emulator)
	{
		this.emulator = emulator;
	}

	//Registers
	public byte DividerRegister { get; private set; }

	public byte TimerRegister { get; set; }

	public byte TimerModulo { get; set; }

	private byte internalTimerControl;

	public byte TimerControl
	{
		get => (byte)(internalTimerControl & 0b0000_0111);
		set => internalTimerControl = (byte)(value & 0b0000_0111);
	}

	private int InternalMainTimerCounterResetValue
	{
		get
		{
			switch (TimerControl & 0b0000_0011)
			{
				case 0:
					return 1024;
				case 1:
					return 16;
				case 2:
					return 64;
				case 3:
					return 256;
				default:
					Logger.Unreachable();
					return 0;
			}
		}
	}

	private int internalDividerRegisterCounter;

	private int internalMainTimerCounter;

	//Flags
	private bool MainTimerEnabled => Cpu.GetBit(TimerControl, 2);

	public void Update(int cycles)
	{
		UpdateDividerRegister(cycles);
		if (MainTimerEnabled) UpdateMainTimer(cycles);
	}

	public void ResetDividerRegister()
	{
		DividerRegister                = 0;
		internalDividerRegisterCounter = 0;
	}

	private void UpdateDividerRegister(int cycles)
	{
		internalDividerRegisterCounter += cycles;

		if (internalDividerRegisterCounter < 256) return;

		//Increment Divider Register every 256 Clock Cycles
		internalDividerRegisterCounter -= 256; //Maybe reset to 0?

		byte previousDividerRegister = DividerRegister;
		DividerRegister++;

		if (Cpu.ToBool(previousDividerRegister & 0b0001_0000) && !Cpu.ToBool(DividerRegister & 0b0001_0000))
		{
			//A falling edge of bit 4 of the Divider Register ticks the APU's Frame Sequencer
			emulator.apu.TickFrameSequencer();
		}
	}

	private void UpdateMainTimer(int cycles)
	{
		int counterWithCycles = internalMainTimerCounter -= cycles;
		if (counterWithCycles > 0) return;

		internalMainTimerCounter = InternalMainTimerCounterResetValue;
		UpdateMainTimer(-counterWithCycles);

		if (TimerRegister == 255)
		{
			//Timer Overflow, reset to Value in Timer Modulo and request Interrupt
			TimerRegister = TimerModulo;
			emulator.interrupts.Request(Interrupts.InterruptType.Timer);
		}
		else
			TimerRegister++;
	}
}