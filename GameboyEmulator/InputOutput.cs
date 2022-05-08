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

	private const int GAME_WIDTH  = 160;
	private const int GAME_HEIGHT = 144;
	private const int SCALE       = 8;
	private const int DRAW_WIDTH  = GAME_WIDTH * SCALE;
	private const int DRAW_HEIGHT = GAME_HEIGHT * SCALE;

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

	private readonly byte[]  pixelBuffer;
	private readonly Texture texture;
	private readonly Sprite  sprite;

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

		pixelBuffer = new byte[GAME_WIDTH * GAME_HEIGHT * 4];
		texture     = new Texture(GAME_WIDTH, GAME_HEIGHT);
		sprite      = new Sprite(texture);

		sprite.Scale = new Vector2f(SCALE, SCALE);

		zBuffer = new bool[GAME_WIDTH, GAME_HEIGHT];

		InitialiseControls();
		InitialiseColors();

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
		upButton = ConvertStringToSfmlKey(Config.GetControlConfig("UP"));
		if (upButton == Keyboard.Key.Unknown) upButton = DEFAULT_UP_BUTTON;

		downButton = ConvertStringToSfmlKey(Config.GetControlConfig("DOWN"));
		if (downButton == Keyboard.Key.Unknown) downButton = DEFAULT_DOWN_BUTTON;

		leftButton = ConvertStringToSfmlKey(Config.GetControlConfig("LEFT"));
		if (leftButton == Keyboard.Key.Unknown) leftButton = DEFAULT_LEFT_BUTTON;

		rightButton = ConvertStringToSfmlKey(Config.GetControlConfig("RIGHT"));
		if (rightButton == Keyboard.Key.Unknown) rightButton = DEFAULT_RIGHT_BUTTON;

		startButton = ConvertStringToSfmlKey(Config.GetControlConfig("START"));
		if (startButton == Keyboard.Key.Unknown) startButton = DEFAULT_START_BUTTON;

		selectButton = ConvertStringToSfmlKey(Config.GetControlConfig("SELECT"));
		if (selectButton == Keyboard.Key.Unknown) selectButton = DEFAULT_SELECT_BUTTON;

		aButton = ConvertStringToSfmlKey(Config.GetControlConfig("A"));
		if (aButton == Keyboard.Key.Unknown) aButton = DEFAULT_A_BUTTON;

		bButton = ConvertStringToSfmlKey(Config.GetControlConfig("B"));
		if (bButton == Keyboard.Key.Unknown) bButton = DEFAULT_B_BUTTON;

		speedButton = ConvertStringToSfmlKey(Config.GetControlConfig("SPEED"));
		if (speedButton == Keyboard.Key.Unknown) speedButton = DEFAULT_SPEED_BUTTON;

		pauseButton = ConvertStringToSfmlKey(Config.GetControlConfig("PAUSE"));
		if (pauseButton == Keyboard.Key.Unknown) pauseButton = DEFAULT_PAUSE_BUTTON;

		resetButton = ConvertStringToSfmlKey(Config.GetControlConfig("RESET"));
		if (resetButton == Keyboard.Key.Unknown) resetButton = DEFAULT_RESET_BUTTON;

		audioChannel1Button = ConvertStringToSfmlKey(Config.GetControlConfig("AUDIO_CHANNEL_1"));
		if (audioChannel1Button == Keyboard.Key.Unknown) audioChannel1Button = DEFAULT_AUDIO_CHANNEL_1_BUTTON;

		audioChannel2Button = ConvertStringToSfmlKey(Config.GetControlConfig("AUDIO_CHANNEL_2"));
		if (audioChannel2Button == Keyboard.Key.Unknown) audioChannel2Button = DEFAULT_AUDIO_CHANNEL_2_BUTTON;

		audioChannel3Button = ConvertStringToSfmlKey(Config.GetControlConfig("AUDIO_CHANNEL_3"));
		if (audioChannel3Button == Keyboard.Key.Unknown) audioChannel3Button = DEFAULT_AUDIO_CHANNEL_3_BUTTON;

		audioChannel4Button = ConvertStringToSfmlKey(Config.GetControlConfig("AUDIO_CHANNEL_4"));
		if (audioChannel4Button == Keyboard.Key.Unknown) audioChannel4Button = DEFAULT_AUDIO_CHANNEL_4_BUTTON;
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

	private static Keyboard.Key ConvertStringToSfmlKey(string keyString)
	{
		if (keyString.Length == 1)
		{
			if (Memory.IsInRange(keyString[0], 'A', 'Z'))
				return keyString[0] - 'A' + Keyboard.Key.A;

			if (Memory.IsInRange(keyString[0], '0', '9'))
				return keyString[0] - '0' + Keyboard.Key.Num0;
		}
		else
		{
			return keyString switch
			{
				"ESC"            => Keyboard.Key.Escape,
				"LCTRL"          => Keyboard.Key.LControl,
				"LSHIFT"         => Keyboard.Key.LShift,
				"LALT"           => Keyboard.Key.LAlt,
				"SPACE"          => Keyboard.Key.Space,
				"ENTER"          => Keyboard.Key.Enter,
				"BACKSPACE"      => Keyboard.Key.Backspace,
				"TAB"            => Keyboard.Key.Tab,
				"PAGEUP"         => Keyboard.Key.PageUp,
				"PAGEDOWN"       => Keyboard.Key.PageDown,
				"END"            => Keyboard.Key.End,
				"HOME"           => Keyboard.Key.Home,
				"INSERT"         => Keyboard.Key.Insert,
				"DELETE"         => Keyboard.Key.Delete,
				"NUMPADADD"      => Keyboard.Key.Add,
				"NUMPADSUBTRACT" => Keyboard.Key.Subtract,
				"NUMPADMULTIPLY" => Keyboard.Key.Multiply,
				"NUMPADDIVIDE"   => Keyboard.Key.Divide,
				"PAUSE"          => Keyboard.Key.Pause,
				"LEFTARROW"      => Keyboard.Key.Left,
				"UPARROW"        => Keyboard.Key.Up,
				"RIGHTARROW"     => Keyboard.Key.Right,
				"DOWNARROW"      => Keyboard.Key.Down,
				"NUMPAD0"        => Keyboard.Key.Numpad0,
				"NUMPAD1"        => Keyboard.Key.Numpad1,
				"NUMPAD2"        => Keyboard.Key.Numpad2,
				"NUMPAD3"        => Keyboard.Key.Numpad3,
				"NUMPAD4"        => Keyboard.Key.Numpad4,
				"NUMPAD5"        => Keyboard.Key.Numpad5,
				"NUMPAD6"        => Keyboard.Key.Numpad6,
				"NUMPAD7"        => Keyboard.Key.Numpad7,
				"NUMPAD8"        => Keyboard.Key.Numpad8,
				"NUMPAD9"        => Keyboard.Key.Numpad9,
				"F1"             => Keyboard.Key.F1,
				"F2"             => Keyboard.Key.F2,
				"F3"             => Keyboard.Key.F3,
				"F4"             => Keyboard.Key.F4,
				"F5"             => Keyboard.Key.F5,
				"F6"             => Keyboard.Key.F6,
				"F7"             => Keyboard.Key.F7,
				"F8"             => Keyboard.Key.F8,
				"F9"             => Keyboard.Key.F9,
				"F10"            => Keyboard.Key.F10,
				"F11"            => Keyboard.Key.F11,
				"F12"            => Keyboard.Key.F12,
				_                => Keyboard.Key.Unknown
			};
		}

		return Keyboard.Key.Unknown;
	}

	public void Update()
	{
		CheckButtons();
		window.DispatchEvents();
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

		pixelBuffer[index + 0] = convertedColor.R;
		pixelBuffer[index + 1] = convertedColor.G;
		pixelBuffer[index + 2] = convertedColor.B;
		pixelBuffer[index + 3] = convertedColor.A;
	}

	public void UpdateZBuffer(int x, int y, bool behindSprite)
	{
		zBuffer[x, y] = behindSprite;
	}

	public bool GetZBufferAt(int x, int y)
	{
		return zBuffer[x, y];
	}

	public void DrawFrame()
	{
		if (!WindowIsOpen)
		{
			Logger.LogMessage("Cannot draw Screen when Window is closed!", Logger.LogLevel.Error);
			throw new InvalidOperationException("Cannot draw Screen when Window is closed!");
		}

		texture.Update(pixelBuffer);
		window.Draw(sprite);

		window.Display();
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
		if (sender is null)
		{
			Console.WriteLine("OnClosed sender is null");
			return;
		}

		RenderWindow window = (RenderWindow)sender;
		window.Close();
		Environment.Exit(0);
	}
}