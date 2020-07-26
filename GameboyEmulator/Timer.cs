using System;

namespace GameboyEmulator
{
	class Timer
	{
		//Modules
		private readonly Memory     memory;
		private readonly Interrupts interrupts;

		public Timer(Memory memory, Interrupts interrupts)
		{
			this.memory     = memory;
			this.interrupts = interrupts;
		}

		//Registers
		private byte DividerRegister
		{
			get => memory.Read(0xFF04);
			set => memory.Write(0xFF04, value, true);
		}

		private byte TimerRegister
		{
			get => memory.Read(0xFF05);
			set => memory.Write(0xFF05, value);
		}

		private byte TimerModulo
		{
			get => memory.Read(0xFF06);
			set => memory.Write(0xFF06, value);
		}

		private byte TimerControl
		{
			get => memory.Read(0xFF07);
			set => memory.Write(0xFF07, value);
		}

		private byte Frequency
		{
			get => (byte)(TimerControl & 0b00000011);
			set
			{
				if (value > 0x3)
					throw new ArgumentOutOfRangeException(nameof(value), "Frequency cannot be larger than 3!");

				TimerControl &= 0b11111100;
				TimerControl |= value;
			}
		}

		private int updateDividerRegisterCounter;

		//Main Timer Frequency starts at 4096 Hz (Increment every 1024 Clock Cycles)
		private int updateMainTimerCounter;

		//Flags
		private bool MainTimerEnabled => Cpu.GetBit(TimerControl, 2);

		public void Update(int cycles)
		{
			UpdateDividerRegister(cycles);
			if (MainTimerEnabled) UpdateMainTimer(cycles);
		}

		private void UpdateDividerRegister(int cycles)
		{
			if ((updateDividerRegisterCounter += cycles) < 256) return;

			//Increment Divider Register every 256 Clock Cycles
			updateDividerRegisterCounter = 0;
			DividerRegister++;
		}

		private void UpdateMainTimer(int cycles)
		{
			if ((updateMainTimerCounter -= cycles) > 0) return;

			SetMainTimerCounter();

			if (TimerRegister == 255)
			{
				//Timer Overflow, reset to Value in Timer Modulo and request Interrupt
				TimerRegister = TimerModulo;
				interrupts.Request(Interrupts.InterruptTypes.Timer);
			}
			else
				TimerRegister++;
		}

		private void SetMainTimerCounter()
		{
			updateMainTimerCounter = Frequency switch
			{
				0 => 1024,
				1 => 16,
				2 => 64,
				3 => 256,
				_ => throw new ArgumentOutOfRangeException(nameof(Frequency), "Frequency cannot be larger than 3!")
			};
		}
	}
}