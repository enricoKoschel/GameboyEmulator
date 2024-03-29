﻿using System;

namespace GameboyEmulator;

public class Interrupts
{
	[Flags]
	public enum InterruptType
	{
		VBlank  = 1,
		LcdStat = 2,
		Timer   = 4,
		Serial  = 8,
		Joypad  = 16
	}

	public enum EnableInterruptsStatus
	{
		ThisCycle,
		NextCycle,
		None
	}

	private readonly Emulator emulator;

	public Interrupts(Emulator emulator)
	{
		this.emulator = emulator;

		InterruptMasterEnable = true;
	}

	private byte internalInterruptEnableRegister;

	public byte InterruptEnableRegister
	{
		//Unused bits are 1
		get => (byte)(internalInterruptEnableRegister | 0b1110_0000);
		set => internalInterruptEnableRegister = (byte)(value | 0b1110_0000);
	}

	private byte internalInterruptFlagRegister;

	public byte InterruptFlagRegister
	{
		//Unused bits are 1
		get => (byte)(internalInterruptFlagRegister | 0b1110_0000);
		set => internalInterruptFlagRegister = (byte)(value | 0b1110_0000);
	}

	private bool VBlankEnabled => Cpu.GetBit(InterruptEnableRegister, 0);

	private bool LcdStatEnabled => Cpu.GetBit(InterruptEnableRegister, 1);

	private bool TimerEnabled => Cpu.GetBit(InterruptEnableRegister, 2);

	private bool SerialEnabled => Cpu.GetBit(InterruptEnableRegister, 3);

	private bool JoypadEnabled => Cpu.GetBit(InterruptEnableRegister, 4);

	private bool VBlankRequested
	{
		get => Cpu.GetBit(InterruptFlagRegister, 0);
		set => InterruptFlagRegister = Cpu.SetBit(InterruptFlagRegister, 0, value);
	}

	private bool LcdStatRequested
	{
		get => Cpu.GetBit(InterruptFlagRegister, 1);
		set => InterruptFlagRegister = Cpu.SetBit(InterruptFlagRegister, 1, value);
	}

	private bool TimerRequested
	{
		get => Cpu.GetBit(InterruptFlagRegister, 2);
		set => InterruptFlagRegister = Cpu.SetBit(InterruptFlagRegister, 2, value);
	}

	private bool SerialRequested
	{
		get => Cpu.GetBit(InterruptFlagRegister, 3);
		set => InterruptFlagRegister = Cpu.SetBit(InterruptFlagRegister, 3, value);
	}

	private bool JoypadRequested
	{
		get => Cpu.GetBit(InterruptFlagRegister, 4);
		set => InterruptFlagRegister = Cpu.SetBit(InterruptFlagRegister, 4, value);
	}

	public bool HasPendingInterrupts => Cpu.ToBool(InterruptFlagRegister & InterruptEnableRegister & 0x1F);

	public bool InterruptMasterEnable { get; set; }

	public EnableInterruptsStatus enableInterruptsStatus = EnableInterruptsStatus.None;

	private void CheckEnable()
	{
		//Interrupts only get enabled when requested beforehand by the corresponding instruction
		//When enabling is requested, there is a delay of one cycle until they are actually enabled
		switch (enableInterruptsStatus)
		{
			case EnableInterruptsStatus.NextCycle:
				enableInterruptsStatus = EnableInterruptsStatus.ThisCycle;
				break;
			case EnableInterruptsStatus.ThisCycle:
				enableInterruptsStatus = EnableInterruptsStatus.None;
				InterruptMasterEnable  = true;
				break;
			case EnableInterruptsStatus.None:
			default:
				break;
		}
	}

	public void Request(InterruptType interrupt)
	{
		switch (interrupt)
		{
			case InterruptType.VBlank:
				VBlankRequested = true;
				break;
			case InterruptType.LcdStat:
				LcdStatRequested = true;
				break;
			case InterruptType.Timer:
				TimerRequested = true;
				break;
			case InterruptType.Serial:
				SerialRequested = true;
				break;
			case InterruptType.Joypad:
				JoypadRequested = true;
				break;
			default:
				Logger.ControlledCrash($"Invalid interrupt '{interrupt}' requested");
				break;
		}

		if (((byte)interrupt & InterruptEnableRegister) != 0)
		{
			//Halt mode is exited when an enabled interrupt is requested, master interrupt enable is ignored
			emulator.cpu.ExitHaltMode();
		}
	}

	public void Check()
	{
		CheckEnable();

		if (!InterruptMasterEnable || InterruptFlagRegister == 0 || InterruptEnableRegister == 0) return;

		//Ordered in decreasing Priority so that highest Priority always gets executed
		if (VBlankEnabled && VBlankRequested) Service(InterruptType.VBlank);
		else if (LcdStatEnabled && LcdStatRequested) Service(InterruptType.LcdStat);
		else if (TimerEnabled && TimerRequested) Service(InterruptType.Timer);
		else if (SerialEnabled && SerialRequested) Service(InterruptType.Serial);
		else if (JoypadEnabled && JoypadRequested) Service(InterruptType.Joypad);
	}

	private void Service(InterruptType interrupt)
	{
		InterruptMasterEnable = false;

		switch (interrupt)
		{
			case InterruptType.VBlank:
				VBlankRequested = false;
				emulator.cpu.ServiceInterrupt(0x40);
				break;
			case InterruptType.LcdStat:
				LcdStatRequested = false;
				emulator.cpu.ServiceInterrupt(0x48);
				break;
			case InterruptType.Timer:
				TimerRequested = false;
				emulator.cpu.ServiceInterrupt(0x50);
				break;
			case InterruptType.Serial:
				SerialRequested = false;
				emulator.cpu.ServiceInterrupt(0x58);
				break;
			case InterruptType.Joypad:
				JoypadRequested = false;
				emulator.cpu.ServiceInterrupt(0x60);
				break;
			default:
				Logger.ControlledCrash($"Tried to service invalid interrupt '{interrupt}'");
				break;
		}
	}
}