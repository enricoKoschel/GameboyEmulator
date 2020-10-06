namespace GameboyEmulator
{
	class Interrupts
	{
		public enum InterruptType
		{
			VBlank,
			LcdStat,
			Timer,
			Serial,
			Joypad
		}

		//Modules
		private readonly Memory memory;
		private readonly Cpu    cpu;

		public Interrupts(Memory memory, Cpu cpu)
		{
			this.memory = memory;
			this.cpu    = cpu;
		}

		private byte InterruptEnableRegister => memory.Read(0xFFFF);

		private byte InterruptFlagRegister
		{
			get => memory.Read(0xFF0F);
			set => memory.Write(0xFF0F, (byte)(value & 0b00011111));
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

		public bool masterInterruptEnable = true;

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
			}
		}

		public void Update()
		{
			if (!masterInterruptEnable || InterruptFlagRegister == 0 || InterruptEnableRegister == 0) return;

			//Ordered in increasing Priority so that highest Priority always get's executed
			if (JoypadEnabled && JoypadRequested) Service(InterruptType.Joypad);
			if (SerialEnabled && SerialRequested) Service(InterruptType.Serial);
			if (TimerEnabled && TimerRequested) Service(InterruptType.Timer);
			if (LcdStatEnabled && LcdStatRequested) Service(InterruptType.LcdStat);
			if (VBlankEnabled && VBlankRequested) Service(InterruptType.VBlank);
		}

		private void Service(InterruptType interrupt)
		{
			masterInterruptEnable = false;

			switch (interrupt)
			{
				case InterruptType.VBlank:
					VBlankRequested = false;
					cpu.ServiceInterrupt(0x40);
					break;
				case InterruptType.LcdStat:
					LcdStatRequested = false;
					cpu.ServiceInterrupt(0x48);
					break;
				case InterruptType.Timer:
					TimerRequested = false;
					cpu.ServiceInterrupt(0x50);
					break;
				case InterruptType.Serial:
					SerialRequested = false;
					cpu.ServiceInterrupt(0x58);
					break;
				case InterruptType.Joypad:
					JoypadRequested = false;
					cpu.ServiceInterrupt(0x60);
					break;
			}
		}
	}
}