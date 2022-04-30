using System;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GameboyEmulator;

public class InputOutput
{
	private readonly RenderWindow window;

	public bool WindowIsOpen   => window.IsOpen;
	public bool WindowHasFocus => window.HasFocus();

	private const int GAME_WIDTH         = 160;
	private const int GAME_HEIGHT        = 144;
	private const int NUMBER_OF_VERTICES = GAME_WIDTH * GAME_HEIGHT * 4;
	private const int SCALE              = 8;
	private const int DRAW_WIDTH         = GAME_WIDTH * SCALE;
	private const int DRAW_HEIGHT        = GAME_HEIGHT * SCALE;

	//Key mapping
	private const Keyboard.Key DEFAULT_UP_BUTTON              = Keyboard.Key.Up;
	private const Keyboard.Key DEFAULT_DOWN_BUTTON            = Keyboard.Key.Down;
	private const Keyboard.Key DEFAULT_LEFT_BUTTON            = Keyboard.Key.Left;
	private const Keyboard.Key DEFAULT_RIGHT_BUTTON           = Keyboard.Key.Right;
	private const Keyboard.Key DEFAULT_START_BUTTON           = Keyboard.Key.Enter;
	private const Keyboard.Key DEFAULT_SELECT_BUTTON          = Keyboard.Key.Space;
	private const Keyboard.Key DEFAULT_A_BUTTON               = Keyboard.Key.S;
	private const Keyboard.Key DEFAULT_B_BUTTON               = Keyboard.Key.A;
	private const Keyboard.Key DEFAULT_SPEED_BUTTON           = Keyboard.Key.LShift;
	private const Keyboard.Key DEFAULT_PAUSE_BUTTON           = Keyboard.Key.LControl;
	private const Keyboard.Key DEFAULT_RESET_BUTTON           = Keyboard.Key.Escape;
	private const Keyboard.Key DEFAULT_AUDIO_CHANNEL_1_BUTTON = Keyboard.Key.F5;
	private const Keyboard.Key DEFAULT_AUDIO_CHANNEL_2_BUTTON = Keyboard.Key.F6;
	private const Keyboard.Key DEFAULT_AUDIO_CHANNEL_3_BUTTON = Keyboard.Key.F7;
	private const Keyboard.Key DEFAULT_AUDIO_CHANNEL_4_BUTTON = Keyboard.Key.F8;

	private static Keyboard.Key upButton;
	private static Keyboard.Key downButton;
	private static Keyboard.Key leftButton;
	private static Keyboard.Key rightButton;
	private static Keyboard.Key startButton;
	private static Keyboard.Key selectButton;
	private static Keyboard.Key aButton;
	private static Keyboard.Key bButton;
	private static Keyboard.Key speedButton;
	private static Keyboard.Key pauseButton;
	private static Keyboard.Key resetButton;
	private static Keyboard.Key audioChannel1Button;
	private static Keyboard.Key audioChannel2Button;
	private static Keyboard.Key audioChannel3Button;
	private static Keyboard.Key audioChannel4Button;

	//Color mapping
	private static readonly Color DEFAULT_BLACK_COLOR      = new(8, 24, 32);
	private static readonly Color DEFAULT_DARK_GRAY_COLOR  = new(52, 104, 86);
	private static readonly Color DEFAULT_LIGHT_GRAY_COLOR = new(136, 192, 112);
	private static readonly Color DEFAULT_WHITE_COLOR      = new(224, 248, 208);

	private static Color blackColor;
	private static Color darkGrayColor;
	private static Color lightGrayColor;
	private static Color whiteColor;

	private readonly VertexBuffer vertexBuffer;
	private readonly Vertex[]     vertexArray;

	private readonly bool[,] zBuffer;

	private bool pauseButtonWasPressed;
	private bool audioChannel1ButtonWasPressed;
	private bool audioChannel2ButtonWasPressed;
	private bool audioChannel3ButtonWasPressed;
	private bool audioChannel4ButtonWasPressed;

	private readonly Emulator emulator;

	public InputOutput(Emulator emulator)
	{
		this.emulator = emulator;

		window = new RenderWindow(new VideoMode(DRAW_WIDTH, DRAW_HEIGHT), "GameBoy Emulator", Styles.Close);

		vertexArray  = new Vertex[NUMBER_OF_VERTICES];
		vertexBuffer = new VertexBuffer(NUMBER_OF_VERTICES, PrimitiveType.Quads, VertexBuffer.UsageSpecifier.Stream);

		zBuffer = new bool[GAME_WIDTH, GAME_HEIGHT];

		InitialiseControls();
		InitialiseColors();
		InitialiseVertexArray();

		window.SetActive();

		window.Closed += OnClosed;
	}

	public static bool IsButtonPressed(Joypad.Button button)
	{
		return button switch
		{
			Joypad.Button.Up     => Keyboard.IsKeyPressed(upButton),
			Joypad.Button.Down   => Keyboard.IsKeyPressed(downButton),
			Joypad.Button.Left   => Keyboard.IsKeyPressed(leftButton),
			Joypad.Button.Right  => Keyboard.IsKeyPressed(rightButton),
			Joypad.Button.Start  => Keyboard.IsKeyPressed(startButton),
			Joypad.Button.Select => Keyboard.IsKeyPressed(selectButton),
			Joypad.Button.A      => Keyboard.IsKeyPressed(aButton),
			Joypad.Button.B      => Keyboard.IsKeyPressed(bButton),
			_                    => throw new ArgumentOutOfRangeException(nameof(button), button, "Invalid button!")
		};
	}

	private static void InitialiseControls()
	{
		upButton = Config.GetControlConfig("UP") == -1
					   ? DEFAULT_UP_BUTTON
					   : ConvertJsKeyCodeToSfml(Config.GetControlConfig("UP"));

		downButton = Config.GetControlConfig("DOWN") == -1
						 ? DEFAULT_DOWN_BUTTON
						 : ConvertJsKeyCodeToSfml(Config.GetControlConfig("DOWN"));

		leftButton = Config.GetControlConfig("LEFT") == -1
						 ? DEFAULT_LEFT_BUTTON
						 : ConvertJsKeyCodeToSfml(Config.GetControlConfig("LEFT"));

		rightButton = Config.GetControlConfig("RIGHT") == -1
						  ? DEFAULT_RIGHT_BUTTON
						  : ConvertJsKeyCodeToSfml(Config.GetControlConfig("RIGHT"));

		startButton = Config.GetControlConfig("START") == -1
						  ? DEFAULT_START_BUTTON
						  : ConvertJsKeyCodeToSfml(Config.GetControlConfig("START"));

		selectButton = Config.GetControlConfig("SELECT") == -1
						   ? DEFAULT_SELECT_BUTTON
						   : ConvertJsKeyCodeToSfml(Config.GetControlConfig("SELECT"));

		aButton = Config.GetControlConfig("A") == -1
					  ? DEFAULT_A_BUTTON
					  : ConvertJsKeyCodeToSfml(Config.GetControlConfig("A"));

		bButton = Config.GetControlConfig("B") == -1
					  ? DEFAULT_B_BUTTON
					  : ConvertJsKeyCodeToSfml(Config.GetControlConfig("B"));

		speedButton = Config.GetControlConfig("SPEED") == -1
						  ? DEFAULT_SPEED_BUTTON
						  : ConvertJsKeyCodeToSfml(Config.GetControlConfig("SPEED"));

		pauseButton = Config.GetControlConfig("PAUSE") == -1
						  ? DEFAULT_PAUSE_BUTTON
						  : ConvertJsKeyCodeToSfml(Config.GetControlConfig("PAUSE"));

		resetButton = Config.GetControlConfig("RESET") == -1
						  ? DEFAULT_RESET_BUTTON
						  : ConvertJsKeyCodeToSfml(Config.GetControlConfig("RESET"));

		audioChannel1Button = Config.GetControlConfig("AUDIO_CHANNEL_1") == -1
								  ? DEFAULT_AUDIO_CHANNEL_1_BUTTON
								  : ConvertJsKeyCodeToSfml(Config.GetControlConfig("AUDIO_CHANNEL_1"));

		audioChannel2Button = Config.GetControlConfig("AUDIO_CHANNEL_2") == -1
								  ? DEFAULT_AUDIO_CHANNEL_2_BUTTON
								  : ConvertJsKeyCodeToSfml(Config.GetControlConfig("AUDIO_CHANNEL_2"));

		audioChannel3Button = Config.GetControlConfig("AUDIO_CHANNEL_3") == -1
								  ? DEFAULT_AUDIO_CHANNEL_3_BUTTON
								  : ConvertJsKeyCodeToSfml(Config.GetControlConfig("AUDIO_CHANNEL_3"));

		audioChannel4Button = Config.GetControlConfig("AUDIO_CHANNEL_4") == -1
								  ? DEFAULT_AUDIO_CHANNEL_4_BUTTON
								  : ConvertJsKeyCodeToSfml(Config.GetControlConfig("AUDIO_CHANNEL_4"));
	}

	private static void InitialiseColors()
	{
		//Shifting and oring is done to set the alpha component of the colors to FF
		blackColor = Config.GetColorConfig("BLACK") == -1
						 ? DEFAULT_BLACK_COLOR
						 : new Color((uint)((Config.GetColorConfig("BLACK") << 8) | 0xFF));

		darkGrayColor = Config.GetColorConfig("DARK_GRAY") == -1
							? DEFAULT_DARK_GRAY_COLOR
							: new Color((uint)((Config.GetColorConfig("DARK_GRAY") << 8) | 0xFF));

		lightGrayColor = Config.GetColorConfig("LIGHT_GRAY") == -1
							 ? DEFAULT_LIGHT_GRAY_COLOR
							 : new Color((uint)((Config.GetColorConfig("LIGHT_GRAY") << 8) | 0xFF));

		whiteColor = Config.GetColorConfig("WHITE") == -1
						 ? DEFAULT_WHITE_COLOR
						 : new Color((uint)((Config.GetColorConfig("WHITE") << 8) | 0xFF));
	}

	private void InitialiseVertexArray()
	{
		for (int i = 0; i < vertexArray.Length; i += 4)
		{
			int x = i % (GAME_WIDTH * 4);
			int y = i / (GAME_WIDTH * 4);

			int leftSide = x / 4 * SCALE;
			int topSide  = y * SCALE;

			//Vertex Direction
			//1******2
			//*		 *
			//*		 *
			//*		 *
			//*		 *
			//*		 *
			//*		 *
			//4******3

			Color white = ConvertGameboyToSfmlColor(Ppu.Color.White);

			vertexArray[i + 0] = new Vertex(
				new Vector2f(leftSide, topSide), white
			);

			vertexArray[i + 1] = new Vertex(
				new Vector2f(leftSide + SCALE, topSide), white
			);

			vertexArray[i + 2] = new Vertex(
				new Vector2f(leftSide + SCALE, topSide + SCALE), white
			);

			vertexArray[i + 3] = new Vertex(
				new Vector2f(leftSide, topSide + SCALE), white
			);
		}
	}

	private static Keyboard.Key ConvertJsKeyCodeToSfml(int keyCode)
	{
		if (Memory.IsInRange(keyCode, 65, 90))
		{
			//A-Z
			return (Keyboard.Key)(keyCode - 65);
		}

		if (Memory.IsInRange(keyCode, 48, 57))
		{
			//0-9
			return (Keyboard.Key)(keyCode - 22);
		}

		if (Memory.IsInRange(keyCode, 96, 105))
		{
			//Numpad0-Numpad9
			return (Keyboard.Key)(keyCode - 21);
		}

		if (Memory.IsInRange(keyCode, 112, 123))
		{
			//F1-F12
			return (Keyboard.Key)(keyCode - 27);
		}

		return keyCode switch
		{
			27  => Keyboard.Key.Escape,
			17  => Keyboard.Key.LControl,
			16  => Keyboard.Key.LShift,
			18  => Keyboard.Key.LAlt,
			32  => Keyboard.Key.Space,
			13  => Keyboard.Key.Enter,
			8   => Keyboard.Key.Backspace,
			9   => Keyboard.Key.Tab,
			33  => Keyboard.Key.PageUp,
			34  => Keyboard.Key.PageDown,
			35  => Keyboard.Key.End,
			36  => Keyboard.Key.Home,
			45  => Keyboard.Key.Insert,
			46  => Keyboard.Key.Delete,
			107 => Keyboard.Key.Add,
			109 => Keyboard.Key.Subtract,
			106 => Keyboard.Key.Multiply,
			111 => Keyboard.Key.Divide,
			19  => Keyboard.Key.Pause,
			37  => Keyboard.Key.Left,
			38  => Keyboard.Key.Up,
			39  => Keyboard.Key.Right,
			40  => Keyboard.Key.Down,
			_   => throw new ArgumentException($"Unsupported key code '{keyCode}'")
		};
	}

	public void Update()
	{
		CheckButtons();
	}

	private void CheckButtons()
	{
		if (!WindowHasFocus) return;

		//Check for speed button
		if (Keyboard.IsKeyPressed(speedButton)) emulator.MaxFps = 0;
		else emulator.MaxFps                                    = Emulator.GAMEBOY_FPS;

		//Check for pause button
		if (Keyboard.IsKeyPressed(pauseButton) && !pauseButtonWasPressed)
		{
			emulator.isPaused     = !emulator.isPaused;
			pauseButtonWasPressed = true;
		}
		else if (!Keyboard.IsKeyPressed(pauseButton)) pauseButtonWasPressed = false;

		//Check for reset button
		if (Keyboard.IsKeyPressed(resetButton)) emulator.Reset();

		//Check for audio channel buttons
		if (Keyboard.IsKeyPressed(audioChannel1Button) && !audioChannel1ButtonWasPressed)
		{
			emulator.apu.Channel1Enabled  = !emulator.apu.Channel1Enabled;
			audioChannel1ButtonWasPressed = true;
		}
		else if (!Keyboard.IsKeyPressed(audioChannel1Button)) audioChannel1ButtonWasPressed = false;

		if (Keyboard.IsKeyPressed(audioChannel2Button) && !audioChannel2ButtonWasPressed)
		{
			emulator.apu.Channel2Enabled  = !emulator.apu.Channel2Enabled;
			audioChannel2ButtonWasPressed = true;
		}
		else if (!Keyboard.IsKeyPressed(audioChannel2Button)) audioChannel2ButtonWasPressed = false;

		if (Keyboard.IsKeyPressed(audioChannel3Button) && !audioChannel3ButtonWasPressed)
		{
			emulator.apu.Channel3Enabled  = !emulator.apu.Channel3Enabled;
			audioChannel3ButtonWasPressed = true;
		}
		else if (!Keyboard.IsKeyPressed(audioChannel3Button)) audioChannel3ButtonWasPressed = false;

		if (Keyboard.IsKeyPressed(audioChannel4Button) && !audioChannel4ButtonWasPressed)
		{
			emulator.apu.Channel4Enabled  = !emulator.apu.Channel4Enabled;
			audioChannel4ButtonWasPressed = true;
		}
		else if (!Keyboard.IsKeyPressed(audioChannel4Button)) audioChannel4ButtonWasPressed = false;
	}

	public void UpdatePixelBuffer(int x, int y, Ppu.Color color)
	{
		int index = x * 4 + y * GAME_WIDTH * 4;

		Color convertedColor = ConvertGameboyToSfmlColor(color);

		vertexArray[index + 0].Color = convertedColor;
		vertexArray[index + 1].Color = convertedColor;
		vertexArray[index + 2].Color = convertedColor;
		vertexArray[index + 3].Color = convertedColor;
	}

	public void UpdateZBuffer(int x, int y, bool behindSprite)
	{
		zBuffer[x, y] = behindSprite;
	}

	public bool GetZBufferAt(int x, int y)
	{
		return zBuffer[x, y];
	}

	public void DrawFrame(bool paused = false)
	{
		if (!WindowIsOpen)
		{
			Logger.LogMessage("Cannot draw Screen when Window is closed!", Logger.LogLevel.Error);
			throw new InvalidOperationException("Cannot draw Screen when Window is closed!");
		}

		window.DispatchEvents();

		//Dont update vertexBuffer if paused, so no unfinished frame gets drawn
		if (!paused) vertexBuffer.Update(vertexArray);
		vertexBuffer.Draw(window, RenderStates.Default);

		window.Display();
	}

	public void ClearFrame(Ppu.Color color)
	{
		for (int i = 0; i < NUMBER_OF_VERTICES; i++) vertexArray[i].Color = ConvertGameboyToSfmlColor(color);
	}

	private static Color ConvertGameboyToSfmlColor(Ppu.Color color)
	{
		return color switch
		{
			Ppu.Color.Black     => blackColor,
			Ppu.Color.DarkGray  => darkGrayColor,
			Ppu.Color.LightGray => lightGrayColor,
			Ppu.Color.White     => whiteColor,
			_                   => throw new ArgumentOutOfRangeException(nameof(color), color, "Invalid color!")
		};
	}

	public void SetWindowTitle(string title)
	{
		window.SetTitle(title);
	}

	public void CloseWindow()
	{
		window.Close();
	}

	private static void OnClosed(object? sender, EventArgs e)
	{
		if (sender is null) return;

		RenderWindow window = (RenderWindow)sender;
		window.Close();
	}
}