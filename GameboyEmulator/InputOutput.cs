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

	private const int BASE_RESET_TIMER_DELAY = 50;

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

	private int ResetTimerDelay => BASE_RESET_TIMER_DELAY * emulator.SpeedAverage / 100;

	private int resetTimer;

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

		window.Closed += OnClosed;
	}

	public static bool IsButtonPressed(Joypad.Button button)
	{
		switch (button)
		{
			case Joypad.Button.Up:
				return Keyboard.IsKeyPressed(upButton);
			case Joypad.Button.Down:
				return Keyboard.IsKeyPressed(downButton);
			case Joypad.Button.Left:
				return Keyboard.IsKeyPressed(leftButton);
			case Joypad.Button.Right:
				return Keyboard.IsKeyPressed(rightButton);
			case Joypad.Button.Start:
				return Keyboard.IsKeyPressed(startButton);
			case Joypad.Button.Select:
				return Keyboard.IsKeyPressed(selectButton);
			case Joypad.Button.A:
				return Keyboard.IsKeyPressed(aButton);
			case Joypad.Button.B:
				return Keyboard.IsKeyPressed(bButton);
			default:
				Logger.ControlledCrash($"Invalid button '{button}'");
				return false;
		}
	}

	private static void InitialiseControls()
	{
		upButton = ConvertStringToSfmlKeyOrDefault("UP", DEFAULT_UP_BUTTON);

		downButton = ConvertStringToSfmlKeyOrDefault("DOWN", DEFAULT_DOWN_BUTTON);

		leftButton = ConvertStringToSfmlKeyOrDefault("LEFT", DEFAULT_LEFT_BUTTON);

		rightButton = ConvertStringToSfmlKeyOrDefault("RIGHT", DEFAULT_RIGHT_BUTTON);

		startButton = ConvertStringToSfmlKeyOrDefault("START", DEFAULT_START_BUTTON);

		selectButton = ConvertStringToSfmlKeyOrDefault("SELECT", DEFAULT_SELECT_BUTTON);

		aButton = ConvertStringToSfmlKeyOrDefault("A", DEFAULT_A_BUTTON);

		bButton = ConvertStringToSfmlKeyOrDefault("B", DEFAULT_B_BUTTON);

		speedButton = ConvertStringToSfmlKeyOrDefault("SPEED", DEFAULT_SPEED_BUTTON);

		pauseButton = ConvertStringToSfmlKeyOrDefault("PAUSE", DEFAULT_PAUSE_BUTTON);

		resetButton = ConvertStringToSfmlKeyOrDefault("RESET", DEFAULT_RESET_BUTTON);

		audioChannel1Button = ConvertStringToSfmlKeyOrDefault("AUDIO_CHANNEL_1", DEFAULT_AUDIO_CHANNEL_1_BUTTON);

		audioChannel2Button = ConvertStringToSfmlKeyOrDefault("AUDIO_CHANNEL_2", DEFAULT_AUDIO_CHANNEL_2_BUTTON);

		audioChannel3Button = ConvertStringToSfmlKeyOrDefault("AUDIO_CHANNEL_3", DEFAULT_AUDIO_CHANNEL_3_BUTTON);

		audioChannel4Button = ConvertStringToSfmlKeyOrDefault("AUDIO_CHANNEL_4", DEFAULT_AUDIO_CHANNEL_4_BUTTON);
	}

	private static void InitialiseColors()
	{
		blackColor = ConvertIntToSfmlColorOrDefault("BLACK", DEFAULT_BLACK_COLOR);

		darkGrayColor = ConvertIntToSfmlColorOrDefault("DARK_GRAY", DEFAULT_DARK_GRAY_COLOR);

		lightGrayColor = ConvertIntToSfmlColorOrDefault("LIGHT_GRAY", DEFAULT_LIGHT_GRAY_COLOR);

		whiteColor = ConvertIntToSfmlColorOrDefault("WHITE", DEFAULT_WHITE_COLOR);
	}

	private static int ConvertSfmlColorToInt(Color color)
	{
		return (color.R << 16) | (color.G << 8) | color.B;
	}

	private static Color ConvertIntToSfmlColorOrDefault(string colorName, Color defaultColor)
	{
		int? color = Config.GetColorConfig(colorName);

		//Shifting and ORing is done to set the alpha component of the colors to 0xFF, making them fully opaque
		if (color is not (null or -1)) return new Color((uint)((color << 8) | 0xFF));

		string? colorString = color is null ? null : $"#{color:X6}";
		Logger.LogInvalidConfigValue(
			$"[Colors].{colorName}", colorString, $"#{ConvertSfmlColorToInt(defaultColor):X6}"
		);

		return defaultColor;
	}

	private static Keyboard.Key ConvertStringToSfmlKeyOrDefault(string keyName, Keyboard.Key defaultKey)
	{
		string? keyString = Config.GetControlConfig(keyName);

		//Only allow enum value names and not underlying integer
		if (!Int32.TryParse(keyString, out _) && Enum.TryParse(keyString, true, out Keyboard.Key key)) return key;

		Logger.LogInvalidConfigValue($"[Controls].{keyName}", keyString, defaultKey);
		return defaultKey;
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
		if (Keyboard.IsKeyPressed(speedButton) && !speedButtonWasPressed)
		{
			speedButtonWasPressed = true;
			emulator.MaxFps       = emulator.MaxFps == 0 ? Emulator.GAMEBOY_FPS : 0;
		}
		else if (!Keyboard.IsKeyPressed(speedButton)) speedButtonWasPressed = false;

		//Check for pause button
		if (Keyboard.IsKeyPressed(pauseButton) && !pauseButtonWasPressed)
		{
			emulator.isPaused     = !emulator.isPaused;
			pauseButtonWasPressed = true;
		}
		else if (!Keyboard.IsKeyPressed(pauseButton)) pauseButtonWasPressed = false;

		//Check for reset button
		if (Keyboard.IsKeyPressed(resetButton))
		{
			if (resetTimer++ > ResetTimerDelay)
			{
				resetTimer = 0;
				emulator.Reset();
			}
		}
		else resetTimer = 0;

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
		if (!WindowIsOpen) Logger.ControlledCrash("Cannot display window if it's closed");

		texture.Update(pixelBuffer);
		window.Draw(sprite);

		window.Display();
	}

	private static Color ConvertGameboyToSfmlColor(Ppu.Color color)
	{
		switch (color)
		{
			case Ppu.Color.Black:
				return blackColor;
			case Ppu.Color.DarkGray:
				return darkGrayColor;
			case Ppu.Color.LightGray:
				return lightGrayColor;
			case Ppu.Color.White:
				return whiteColor;
			default:
				Logger.ControlledCrash($"Invalid color '{color}'");
				return Color.Transparent;
		}
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