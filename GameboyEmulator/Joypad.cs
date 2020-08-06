using SFML.Window;

namespace GameboyEmulator
{
	class Joypad
	{
		//Modules
		private readonly Memory     memory;
		private readonly Interrupts interrupts;

		public Joypad(Memory memory, Interrupts interrupts)
		{
			this.memory     = memory;
			this.interrupts = interrupts;
		}

		//Registers
		private byte JoypadRegister
		{
			get => memory.Read(0xFF00);
			set => memory.Write(0xFF00, (byte)(value & 0b00111111));
		}

		//Flags
		private bool ButtonKeysSelected    => !Cpu.GetBit(JoypadRegister, 5);
		private bool DirectionKeysSelected => !Cpu.GetBit(JoypadRegister, 4);

		private bool DownOrStartPressed
		{
			get => !Cpu.GetBit(JoypadRegister, 3);
			set => JoypadRegister = Cpu.SetBit(JoypadRegister, 3, !value);
		}

		private bool UpOrSelectPressed
		{
			get => !Cpu.GetBit(JoypadRegister, 2);
			set => JoypadRegister = Cpu.SetBit(JoypadRegister, 2, !value);
		}

		private bool LeftOrButtonBPressed
		{
			get => !Cpu.GetBit(JoypadRegister, 1);
			set => JoypadRegister = Cpu.SetBit(JoypadRegister, 1, !value);
		}

		private bool RightOrButtonAPressed
		{
			get => !Cpu.GetBit(JoypadRegister, 0);
			set => JoypadRegister = Cpu.SetBit(JoypadRegister, 0, !value);
		}

		public void Update()
		{
			UpdateJoypadRegister();
		}
		
		public void ButtonPressed(object sender, KeyEventArgs e)
		{
			//FIXME Interrupt causes Issues, will fix later
			//interrupts.Request(Interrupts.InterruptTypes.Joypad);
			UpdateJoypadRegister();
		}

		public void ButtonReleased(object sender, KeyEventArgs e)
		{
			UpdateJoypadRegister();
		}

		private void UpdateJoypadRegister()
		{
			DownOrStartPressed    = false;
			UpOrSelectPressed     = false;
			LeftOrButtonBPressed  = false;
			RightOrButtonAPressed = false;
			
			if (ButtonKeysSelected)
			{
				DownOrStartPressed    = Keyboard.IsKeyPressed(Keyboard.Key.Enter);
				UpOrSelectPressed     = Keyboard.IsKeyPressed(Keyboard.Key.Space);
				LeftOrButtonBPressed  = Keyboard.IsKeyPressed(Keyboard.Key.A);
				RightOrButtonAPressed = Keyboard.IsKeyPressed(Keyboard.Key.S);
			}

			if (!DirectionKeysSelected) return;

			DownOrStartPressed    |= Keyboard.IsKeyPressed(Keyboard.Key.Down);
			UpOrSelectPressed     |= Keyboard.IsKeyPressed(Keyboard.Key.Up);
			LeftOrButtonBPressed  |= Keyboard.IsKeyPressed(Keyboard.Key.Left);
			RightOrButtonAPressed |= Keyboard.IsKeyPressed(Keyboard.Key.Right);
		}
	}
}