using System;
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

		private bool downPressedThisFrame;
		private bool upPressedThisFrame;
		private bool leftPressedThisFrame;
		private bool rightPressedThisFrame;
		private bool startPressedThisFrame;
		private bool selectPressedThisFrame;
		private bool buttonBPressedThisFrame;
		private bool buttonAPressedThisFrame;

		private bool buttonPressedLastFrame;

		private bool ButtonPressedThisFrame => downPressedThisFrame || upPressedThisFrame || leftPressedThisFrame ||
											   rightPressedThisFrame || startPressedThisFrame ||
											   selectPressedThisFrame || buttonBPressedThisFrame ||
											   buttonAPressedThisFrame;

		public void Update(bool frameDone)
		{
			if (frameDone)
			{
				hadFocusLastFrame = window.HasFocus();

				downPressedThisFrame    = Keyboard.IsKeyPressed(Keyboard.Key.Down);
				upPressedThisFrame      = Keyboard.IsKeyPressed(Keyboard.Key.Up);
				leftPressedThisFrame    = Keyboard.IsKeyPressed(Keyboard.Key.Left);
				rightPressedThisFrame   = Keyboard.IsKeyPressed(Keyboard.Key.Right);
				startPressedThisFrame   = Keyboard.IsKeyPressed(Keyboard.Key.Enter);
				selectPressedThisFrame  = Keyboard.IsKeyPressed(Keyboard.Key.Space);
				buttonBPressedThisFrame = Keyboard.IsKeyPressed(Keyboard.Key.A);
				buttonAPressedThisFrame = Keyboard.IsKeyPressed(Keyboard.Key.S);

				//Only request Interrupt if Button was pressed this Frame
				if (ButtonPressedThisFrame && ButtonPressedThisFrame != buttonPressedLastFrame)
					interrupts.Request(Interrupts.InterruptType.Joypad);

				buttonPressedLastFrame = ButtonPressedThisFrame;
			}

			DownOrStartPressed    = false;
			UpOrSelectPressed     = false;
			LeftOrButtonBPressed  = false;
			RightOrButtonAPressed = false;

			//Do not accept Key Presses when Emulator is not in Focus
			if (!hadFocusLastFrame) return;

			if (ButtonKeysSelected)
			{
				DownOrStartPressed    = startPressedThisFrame;
				UpOrSelectPressed     = selectPressedThisFrame;
				LeftOrButtonBPressed  = buttonBPressedThisFrame;
				RightOrButtonAPressed = buttonAPressedThisFrame;
			}

			if (DirectionKeysSelected)
			{
				DownOrStartPressed    = downPressedThisFrame;
				UpOrSelectPressed     = upPressedThisFrame;
				LeftOrButtonBPressed  = leftPressedThisFrame;
				RightOrButtonAPressed = rightPressedThisFrame;
			}
		}
	}
}