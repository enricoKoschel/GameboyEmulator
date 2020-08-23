using SFML.Window;

namespace GameboyEmulator
{
	class Joypad
	{
		//Modules
		private readonly Memory     memory;
		private readonly Interrupts interrupts;
		private readonly Window     window;

		public Joypad(Memory memory, Interrupts interrupts, Graphics graphics)
		{
			this.memory     = memory;
			this.interrupts = interrupts;

			window = graphics.GetScreen().GetWindow();
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

		private bool hadFocusLastFrame;
		private bool downOrStartPressedLastCycle;
		private bool upOrSelectPressedLastCycle;
		private bool leftOrButtonBPressedLastCycle;
		private bool rightOrButtonAPressedLastCycle;

		public void Update(bool frameDone)
		{
			if (frameDone) hadFocusLastFrame = window.HasFocus();

			DownOrStartPressed    = false;
			UpOrSelectPressed     = false;
			LeftOrButtonBPressed  = false;
			RightOrButtonAPressed = false;

			//Do not accept Key Presses when Emulator is not in Focus
			if (!hadFocusLastFrame) return;

			if (ButtonKeysSelected)
			{
				DownOrStartPressed    = Keyboard.IsKeyPressed(Keyboard.Key.Enter);
				UpOrSelectPressed     = Keyboard.IsKeyPressed(Keyboard.Key.Space);
				LeftOrButtonBPressed  = Keyboard.IsKeyPressed(Keyboard.Key.A);
				RightOrButtonAPressed = Keyboard.IsKeyPressed(Keyboard.Key.S);
			}

			if (DirectionKeysSelected)
			{
				DownOrStartPressed    |= Keyboard.IsKeyPressed(Keyboard.Key.Down);
				UpOrSelectPressed     |= Keyboard.IsKeyPressed(Keyboard.Key.Up);
				LeftOrButtonBPressed  |= Keyboard.IsKeyPressed(Keyboard.Key.Left);
				RightOrButtonAPressed |= Keyboard.IsKeyPressed(Keyboard.Key.Right);
			}


			//Request Interrupt if Button was pressed this Cycle
			bool requestInterrupt = false;

			requestInterrupt |= DownOrStartPressed && (DownOrStartPressed != downOrStartPressedLastCycle);
			requestInterrupt |= UpOrSelectPressed && (UpOrSelectPressed != upOrSelectPressedLastCycle);
			requestInterrupt |= LeftOrButtonBPressed && (LeftOrButtonBPressed != leftOrButtonBPressedLastCycle);
			requestInterrupt |= RightOrButtonAPressed && (RightOrButtonAPressed != rightOrButtonAPressedLastCycle);

			//FIXME Joypad Interrupt causes weird behaviour
			//if (requestInterrupt) interrupts.Request(Interrupts.InterruptTypes.Joypad);

			downOrStartPressedLastCycle    = DownOrStartPressed;
			upOrSelectPressedLastCycle     = UpOrSelectPressed;
			leftOrButtonBPressedLastCycle  = LeftOrButtonBPressed;
			rightOrButtonAPressedLastCycle = RightOrButtonAPressed;
		}
	}
}