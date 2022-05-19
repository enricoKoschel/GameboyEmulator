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

	public bool DispatchingEvents { get; private set; }

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

	private bool speedButtonWasPressed;
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

		window.SetFramerateLimit(40);

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
		if (upButton == Keyboard.Key.Unknown)
		{
			upButton = DEFAULT_UP_BUTTON;

			Logger.LogMessage(
				$"Invalid value for [Controls].UP in config file. Defaulting to {upButton}.", Logger.LogLevel.Warn, true
			);
		}

		downButton = ConvertStringToSfmlKey(Config.GetControlConfig("DOWN"));
		if (downButton == Keyboard.Key.Unknown)
		{
			downButton = DEFAULT_DOWN_BUTTON;

			Logger.LogMessage(
				$"Invalid value for [Controls].DOWN in config file. Defaulting to {downButton}.", Logger.LogLevel.Warn,
				true
			);
		}

		leftButton = ConvertStringToSfmlKey(Config.GetControlConfig("LEFT"));
		if (leftButton == Keyboard.Key.Unknown)
		{
			leftButton = DEFAULT_LEFT_BUTTON;

			Logger.LogMessage(
				$"Invalid value for [Controls].LEFT in config file. Defaulting to {leftButton}.", Logger.LogLevel.Warn,
				true
			);
		}

		rightButton = ConvertStringToSfmlKey(Config.GetControlConfig("RIGHT"));
		if (rightButton == Keyboard.Key.Unknown)
		{
			rightButton = DEFAULT_RIGHT_BUTTON;

			Logger.LogMessage(
				$"Invalid value for [Controls].RIGHT in config file. Defaulting to {rightButton}.",
				Logger.LogLevel.Warn, true
			);
		}

		startButton = ConvertStringToSfmlKey(Config.GetControlConfig("START"));
		if (startButton == Keyboard.Key.Unknown)
		{
			startButton = DEFAULT_START_BUTTON;

			Logger.LogMessage(
				$"Invalid value for [Controls].START in config file. Defaulting to {startButton}.",
				Logger.LogLevel.Warn, true
			);
		}

		selectButton = ConvertStringToSfmlKey(Config.GetControlConfig("SELECT"));
		if (selectButton == Keyboard.Key.Unknown)
		{
			selectButton = DEFAULT_SELECT_BUTTON;

			Logger.LogMessage(
				$"Invalid value for [Controls].SELECT in config file. Defaulting to {selectButton}.",
				Logger.LogLevel.Warn, true
			);
		}

		aButton = ConvertStringToSfmlKey(Config.GetControlConfig("A"));
		if (aButton == Keyboard.Key.Unknown)
		{
			aButton = DEFAULT_A_BUTTON;

			Logger.LogMessage(
				$"Invalid value for [Controls].A in config file. Defaulting to {aButton}.", Logger.LogLevel.Warn, true
			);
		}

		bButton = ConvertStringToSfmlKey(Config.GetControlConfig("B"));
		if (bButton == Keyboard.Key.Unknown)
		{
			bButton = DEFAULT_B_BUTTON;

			Logger.LogMessage(
				$"Invalid value for [Controls].B in config file. Defaulting to {bButton}.", Logger.LogLevel.Warn, true
			);
		}

		speedButton = ConvertStringToSfmlKey(Config.GetControlConfig("SPEED"));
		if (speedButton == Keyboard.Key.Unknown)
		{
			speedButton = DEFAULT_SPEED_BUTTON;

			Logger.LogMessage(
				$"Invalid value for [Controls].SPEED in config file. Defaulting to {speedButton}.",
				Logger.LogLevel.Warn, true
			);
		}

		pauseButton = ConvertStringToSfmlKey(Config.GetControlConfig("PAUSE"));
		if (pauseButton == Keyboard.Key.Unknown)
		{
			pauseButton = DEFAULT_PAUSE_BUTTON;

			Logger.LogMessage(
				$"Invalid value for [Controls].PAUSE in config file. Defaulting to {pauseButton}.",
				Logger.LogLevel.Warn, true
			);
		}

		resetButton = ConvertStringToSfmlKey(Config.GetControlConfig("RESET"));
		if (resetButton == Keyboard.Key.Unknown)
		{
			resetButton = DEFAULT_RESET_BUTTON;

			Logger.LogMessage(
				$"Invalid value for [Controls].RESET in config file. Defaulting to {resetButton}.",
				Logger.LogLevel.Warn, true
			);
		}

		audioChannel1Button = ConvertStringToSfmlKey(Config.GetControlConfig("AUDIO_CHANNEL_1"));
		if (audioChannel1Button == Keyboard.Key.Unknown)
		{
			audioChannel1Button = DEFAULT_AUDIO_CHANNEL_1_BUTTON;

			Logger.LogMessage(
				$"Invalid value for [Controls].AUDIO_CHANNEL_1 in config file. Defaulting to {audioChannel1Button}.",
				Logger.LogLevel.Warn, true
			);
		}

		audioChannel2Button = ConvertStringToSfmlKey(Config.GetControlConfig("AUDIO_CHANNEL_2"));
		if (audioChannel2Button == Keyboard.Key.Unknown)
		{
			audioChannel2Button = DEFAULT_AUDIO_CHANNEL_2_BUTTON;

			Logger.LogMessage(
				$"Invalid value for [Controls].AUDIO_CHANNEL_2 in config file. Defaulting to {audioChannel2Button}.",
				Logger.LogLevel.Warn, true
			);
		}

		audioChannel3Button = ConvertStringToSfmlKey(Config.GetControlConfig("AUDIO_CHANNEL_3"));
		if (audioChannel3Button == Keyboard.Key.Unknown)
		{
			audioChannel3Button = DEFAULT_AUDIO_CHANNEL_3_BUTTON;

			Logger.LogMessage(
				$"Invalid value for [Controls].AUDIO_CHANNEL_3 in config file. Defaulting to {audioChannel3Button}.",
				Logger.LogLevel.Warn, true
			);
		}

		audioChannel4Button = ConvertStringToSfmlKey(Config.GetControlConfig("AUDIO_CHANNEL_4"));
		if (audioChannel4Button == Keyboard.Key.Unknown)
		{
			audioChannel4Button = DEFAULT_AUDIO_CHANNEL_4_BUTTON;

			Logger.LogMessage(
				$"Invalid value for [Controls].AUDIO_CHANNEL_4 in config file. Defaulting to {audioChannel4Button}.",
				Logger.LogLevel.Warn, true
			);
		}
	}

	private static void InitialiseColors()
	{
		blackColor = ConvertIntToSfmlColor(Config.GetColorConfig("BLACK"));
		if (blackColor == Color.Transparent)
		{
			blackColor = DEFAULT_BLACK_COLOR;

			Logger.LogMessage(
				$"Invalid value for [Colors].BLACK in config file. Defaulting to {ConvertSfmlColorToInt(blackColor):X}.",
				Logger.LogLevel.Warn, true
			);
		}

		darkGrayColor = ConvertIntToSfmlColor(Config.GetColorConfig("DARK_GRAY"));
		if (darkGrayColor == Color.Transparent)
		{
			darkGrayColor = DEFAULT_DARK_GRAY_COLOR;

			Logger.LogMessage(
				$"Invalid value for [Colors].DARK_GRAY in config file. Defaulting to {ConvertSfmlColorToInt(darkGrayColor):X}.",
				Logger.LogLevel.Warn, true
			);
		}

		lightGrayColor = ConvertIntToSfmlColor(Config.GetColorConfig("LIGHT_GRAY"));
		if (lightGrayColor == Color.Transparent)
		{
			lightGrayColor = DEFAULT_LIGHT_GRAY_COLOR;

			Logger.LogMessage(
				$"Invalid value for [Colors].LIGHT_GRAY in config file. Defaulting to {ConvertSfmlColorToInt(lightGrayColor):X}.",
				Logger.LogLevel.Warn, true
			);
		}

		whiteColor = ConvertIntToSfmlColor(Config.GetColorConfig("WHITE"));
		if (whiteColor == Color.Transparent)
		{
			whiteColor = DEFAULT_WHITE_COLOR;

			Logger.LogMessage(
				$"Invalid value for [Colors].WHITE in config file. Defaulting to {ConvertSfmlColorToInt(whiteColor):X}.",
				Logger.LogLevel.Warn, true
			);
		}
	}

	private static int ConvertSfmlColorToInt(Color color)
	{
		return (color.R << 16) | (color.G << 8) | color.B;
	}

	private static Color ConvertIntToSfmlColor(int color)
	{
		//Shifting and oring is done to set the alpha component of the colors to 0xFF
		//This is done to make the colors fully opaque
		return color == -1 ? Color.Transparent : new Color((uint)((color << 8) | 0xFF));
	}

	private static Keyboard.Key ConvertStringToSfmlKey(string keyString)
	{
		//Only allow enum value names and not underlying integer
		if (Int32.TryParse(keyString, out _)) return Keyboard.Key.Unknown;

		return Enum.TryParse(keyString, true, out Keyboard.Key key) ? key : Keyboard.Key.Unknown;
	}

	public void Update()
	{
		CheckButtons();

		DispatchingEvents = true;
		window.DispatchEvents();
		DispatchingEvents = false;
	}

	private void CheckButtons()
	{
		if (!WindowHasFocus) return;

		//Check for speed button
		if (!speedButtonWasPressed && Keyboard.IsKeyPressed(speedButton))
		{
			speedButtonWasPressed = true;
			emulator.MaxFps       = 0;
		}
		else if (speedButtonWasPressed && !Keyboard.IsKeyPressed(speedButton))
		{
			speedButtonWasPressed = false;
			emulator.MaxFps       = Emulator.GAMEBOY_FPS;
			emulator.apu.ClearSampleBuffer();
		}

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