namespace GameboyEmulator;

public class Joypad
{
	public enum Button
	{
		Up,
		Down,
		Left,
		Right,
		Start,
		Select,
		A,
		B
	}

	private readonly Emulator emulator;

	public Joypad(Emulator emulator)
	{
		this.emulator = emulator;
	}

	//Registers
	public byte JoypadRegister
	{
		get
		{
			bool downOrStartPressed    = false;
			bool upOrSelectPressed     = false;
			bool leftOrButtonBPressed  = false;
			bool rightOrButtonAPressed = false;

			if (actionButtonsSelected)
			{
				downOrStartPressed    |= startPressedThisFrame;
				upOrSelectPressed     |= selectPressedThisFrame;
				leftOrButtonBPressed  |= buttonBPressedThisFrame;
				rightOrButtonAPressed |= buttonAPressedThisFrame;
			}

			if (directionButtonsSelected)
			{
				downOrStartPressed    |= downPressedThisFrame;
				upOrSelectPressed     |= upPressedThisFrame;
				leftOrButtonBPressed  |= leftPressedThisFrame;
				rightOrButtonAPressed |= rightPressedThisFrame;
			}

			//Unused IO bits are 1
			return Cpu.MakeByte(
				true, true, !actionButtonsSelected, !directionButtonsSelected,
				!downOrStartPressed, !upOrSelectPressed, !leftOrButtonBPressed, !rightOrButtonAPressed
			);
		}
		set
		{
			actionButtonsSelected    = !Cpu.GetBit(value, 5);
			directionButtonsSelected = !Cpu.GetBit(value, 4);
		}
	}

	//Flags
	private bool actionButtonsSelected;
	private bool directionButtonsSelected;

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

	public void CaptureInput()
	{
		//Do not allow any keys to be pressed when Emulator is out of focus
		if (!emulator.inputOutput.WindowHasFocus)
		{
			upPressedThisFrame    = false;
			downPressedThisFrame  = false;
			leftPressedThisFrame  = false;
			rightPressedThisFrame = false;

			startPressedThisFrame   = false;
			selectPressedThisFrame  = false;
			buttonBPressedThisFrame = false;
			buttonAPressedThisFrame = false;

			return;
		}

		upPressedThisFrame    = InputOutput.IsButtonPressed(Button.Up);
		downPressedThisFrame  = InputOutput.IsButtonPressed(Button.Down);
		leftPressedThisFrame  = InputOutput.IsButtonPressed(Button.Left);
		rightPressedThisFrame = InputOutput.IsButtonPressed(Button.Right);

		//Don't allow "impossible" inputs (🠕+🠗/🠔+🠖)
		if (upPressedThisFrame && downPressedThisFrame) upPressedThisFrame      = downPressedThisFrame  = false;
		if (leftPressedThisFrame && rightPressedThisFrame) leftPressedThisFrame = rightPressedThisFrame = false;

		startPressedThisFrame   = InputOutput.IsButtonPressed(Button.Start);
		selectPressedThisFrame  = InputOutput.IsButtonPressed(Button.Select);
		buttonBPressedThisFrame = InputOutput.IsButtonPressed(Button.B);
		buttonAPressedThisFrame = InputOutput.IsButtonPressed(Button.A);

		//Request interrupt if a button was pressed this frame
		if (ButtonPressedThisFrame && !buttonPressedLastFrame)
			emulator.interrupts.Request(Interrupts.InterruptType.Joypad);

		buttonPressedLastFrame = ButtonPressedThisFrame;
	}
}