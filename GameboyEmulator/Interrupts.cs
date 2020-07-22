using System;

namespace GameboyEmulator
{
	class Interrupts
	{
		public enum InterruptTypes
		{
			VBlank,
			LcdStat,
			Timer,
			Serial,
			Joypad
		}

		//Modules
		private readonly Memory memory;

		public Interrupts(Memory memory)
		{
			this.memory = memory;
		}

		public byte InterruptEnableRegister
		{
			get => memory.Read(0xFFFF);
			set => memory.Write(0xFFFF, (byte)(value & 0b00011111));
		}

		public byte InterruptFlagRegister
		{
			get => memory.Read(0xFF0F);
			set => memory.Write(0xFF0F, (byte)(value & 0b00011111));
		}

		private bool VBlankEnabled
		{
			get => Cpu.GetBit(InterruptEnableRegister, 0);
			set => InterruptEnableRegister = Cpu.SetBit(InterruptEnableRegister, 0, value);
		}

		private bool LcdStatEnabled
		{
			get => Cpu.GetBit(InterruptEnableRegister, 1);
			set => InterruptEnableRegister = Cpu.SetBit(InterruptEnableRegister, 1, value);
		}

		private bool TimerEnabled
		{
			get => Cpu.GetBit(InterruptEnableRegister, 2);
			set => InterruptEnableRegister = Cpu.SetBit(InterruptEnableRegister, 2, value);
		}

		private bool SerialEnabled
		{
			get => Cpu.GetBit(InterruptEnableRegister, 3);
			set => InterruptEnableRegister = Cpu.SetBit(InterruptEnableRegister, 3, value);
		}

		private bool JoypadEnabled
		{
			get => Cpu.GetBit(InterruptEnableRegister, 4);
			set => InterruptEnableRegister = Cpu.SetBit(InterruptEnableRegister, 4, value);
		}

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
		
		public bool masterInterruptEnable;

		public void RequestInterrupt(InterruptTypes interrupt)
		{
			switch (interrupt)
			{
				case InterruptTypes.VBlank:
					VBlankRequested = true;
					break;
				case InterruptTypes.LcdStat:
					LcdStatRequested = true;
					break;
				case InterruptTypes.Timer:
					TimerRequested = true;
					break;
				case InterruptTypes.Serial:
					SerialRequested = true;
					break;
				case InterruptTypes.Joypad:
					SerialRequested = true;
					break;
			}
		}

		public void ServiceInterrupts()
		{
		}
	}
}