using System;

namespace GameboyEmulator
{
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

		private byte timerControl;

		public byte TimerControl
		{
			get => (byte)(timerControl & 0b00000111);
			set => timerControl = (byte)(value & 0b00000111);
		}

		private int InternalMainTimerCounterResetValue => (TimerControl & 0b00000011) switch
		{
			0 => 1024,
			1 => 16,
			2 => 64,
			3 => 256,
			_ => throw new ArgumentOutOfRangeException(
					 nameof(TimerControl) + " & 0b00000011", TimerControl & 0b00000011,
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
			internalDividerRegisterCounter = counterWithCycles - 256; //TODO maybe reset to 0
			DividerRegister++;
		}

		private void UpdateMainTimer(int cycles)
		{
			//TODO add remaining counter back after below 0
			//int counterWithCycles = 
			if ((internalMainTimerCounter -= cycles) > 0) return;

			internalMainTimerCounter = InternalMainTimerCounterResetValue;

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
}