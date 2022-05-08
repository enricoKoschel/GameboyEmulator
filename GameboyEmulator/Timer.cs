using System;

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

	private int InternalMainTimerCounterResetValue => (TimerControl & 0b0000_0011) switch
	{
		0 => 1024,
		1 => 16,
		2 => 64,
		3 => 256,
		_ => throw new ArgumentOutOfRangeException(
				 nameof(TimerControl) + " & 0b0000_0011", TimerControl & 0b0000_0011,
				 "Something has gone horribly wrong and the fabric of space time is rupturing as we speak."
			 )
	};

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
		int counterWithCycles = internalDividerRegisterCounter += cycles;

		if (counterWithCycles < 256) return;

		//Increment Divider Register every 256 Clock Cycles
		internalDividerRegisterCounter = counterWithCycles - 256; //Maybe reset to 0

		byte previousDividerRegister = DividerRegister;
		DividerRegister++;

		if (Cpu.ToBool(previousDividerRegister & 0b0010_0000) && !Cpu.ToBool(DividerRegister & 0b0010_0000))
		{
			//Falling edge of bit 5 of Divider Register clocks the APU's Frame Sequencer
			emulator.apu.UpdateFrameSequencer();
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