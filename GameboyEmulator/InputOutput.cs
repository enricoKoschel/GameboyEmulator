﻿using System;
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
		upButton = ConvertStringToSfmlKey(Config.GetControlConfig("UP"));
		if (upButton == Keyboard.Key.Unknown)
		{
			Logger.LogInvalidConfigValue("[Controls].UP", Config.GetControlConfig("UP"), DEFAULT_UP_BUTTON);

			upButton = DEFAULT_UP_BUTTON;
		}

		downButton = ConvertStringToSfmlKey(Config.GetControlConfig("DOWN"));
		if (downButton == Keyboard.Key.Unknown)
		{
			Logger.LogInvalidConfigValue("[Controls].DOWN", Config.GetControlConfig("DOWN"), DEFAULT_DOWN_BUTTON);

			downButton = DEFAULT_DOWN_BUTTON;
		}

		leftButton = ConvertStringToSfmlKey(Config.GetControlConfig("LEFT"));
		if (leftButton == Keyboard.Key.Unknown)
		{
			Logger.LogInvalidConfigValue("[Controls].LEFT", Config.GetControlConfig("LEFT"), DEFAULT_LEFT_BUTTON);

			leftButton = DEFAULT_LEFT_BUTTON;
		}

		rightButton = ConvertStringToSfmlKey(Config.GetControlConfig("RIGHT"));
		if (rightButton == Keyboard.Key.Unknown)
		{
			Logger.LogInvalidConfigValue("[Controls].RIGHT", Config.GetControlConfig("RIGHT"), DEFAULT_RIGHT_BUTTON);

			rightButton = DEFAULT_RIGHT_BUTTON;
		}

		startButton = ConvertStringToSfmlKey(Config.GetControlConfig("START"));
		if (startButton == Keyboard.Key.Unknown)
		{
			Logger.LogInvalidConfigValue("[Controls].START", Config.GetControlConfig("START"), DEFAULT_START_BUTTON);

			startButton = DEFAULT_START_BUTTON;
		}

		selectButton = ConvertStringToSfmlKey(Config.GetControlConfig("SELECT"));
		if (selectButton == Keyboard.Key.Unknown)
		{
			Logger.LogInvalidConfigValue("[Controls].SELECT", Config.GetControlConfig("SELECT"), DEFAULT_SELECT_BUTTON);

			selectButton = DEFAULT_SELECT_BUTTON;
		}

		aButton = ConvertStringToSfmlKey(Config.GetControlConfig("A"));
		if (aButton == Keyboard.Key.Unknown)
		{
			Logger.LogInvalidConfigValue("[Controls].A", Config.GetControlConfig("A"), DEFAULT_A_BUTTON);

			aButton = DEFAULT_A_BUTTON;
		}

		bButton = ConvertStringToSfmlKey(Config.GetControlConfig("B"));
		if (bButton == Keyboard.Key.Unknown)
		{
			Logger.LogInvalidConfigValue("[Controls].B", Config.GetControlConfig("B"), DEFAULT_B_BUTTON);

			bButton = DEFAULT_B_BUTTON;
		}

		speedButton = ConvertStringToSfmlKey(Config.GetControlConfig("SPEED"));
		if (speedButton == Keyboard.Key.Unknown)
		{
			Logger.LogInvalidConfigValue("[Controls].SPEED", Config.GetControlConfig("SPEED"), DEFAULT_SPEED_BUTTON);

			speedButton = DEFAULT_SPEED_BUTTON;
		}

		pauseButton = ConvertStringToSfmlKey(Config.GetControlConfig("PAUSE"));
		if (pauseButton == Keyboard.Key.Unknown)
		{
			Logger.LogInvalidConfigValue("[Controls].PAUSE", Config.GetControlConfig("PAUSE"), DEFAULT_PAUSE_BUTTON);

			pauseButton = DEFAULT_PAUSE_BUTTON;
		}

		resetButton = ConvertStringToSfmlKey(Config.GetControlConfig("RESET"));
		if (resetButton == Keyboard.Key.Unknown)
		{
			Logger.LogInvalidConfigValue("[Controls].RESET", Config.GetControlConfig("RESET"), DEFAULT_RESET_BUTTON);

			resetButton = DEFAULT_RESET_BUTTON;
		}

		audioChannel1Button = ConvertStringToSfmlKey(Config.GetControlConfig("AUDIO_CHANNEL_1"));
		if (audioChannel1Button == Keyboard.Key.Unknown)
		{
			Logger.LogInvalidConfigValue(
				"[Controls].AUDIO_CHANNEL_1", Config.GetControlConfig("AUDIO_CHANNEL_1"), DEFAULT_AUDIO_CHANNEL_1_BUTTON
			);

			audioChannel1Button = DEFAULT_AUDIO_CHANNEL_1_BUTTON;
		}

		audioChannel2Button = ConvertStringToSfmlKey(Config.GetControlConfig("AUDIO_CHANNEL_2"));
		if (audioChannel2Button == Keyboard.Key.Unknown)
		{
			Logger.LogInvalidConfigValue(
				"[Controls].AUDIO_CHANNEL_2", Config.GetControlConfig("AUDIO_CHANNEL_2"), DEFAULT_AUDIO_CHANNEL_2_BUTTON
			);

			audioChannel2Button = DEFAULT_AUDIO_CHANNEL_2_BUTTON;
		}

		audioChannel3Button = ConvertStringToSfmlKey(Config.GetControlConfig("AUDIO_CHANNEL_3"));
		if (audioChannel3Button == Keyboard.Key.Unknown)
		{
			Logger.LogInvalidConfigValue(
				"[Controls].AUDIO_CHANNEL_3", Config.GetControlConfig("AUDIO_CHANNEL_3"), DEFAULT_AUDIO_CHANNEL_3_BUTTON
			);

			audioChannel3Button = DEFAULT_AUDIO_CHANNEL_3_BUTTON;
		}

		audioChannel4Button = ConvertStringToSfmlKey(Config.GetControlConfig("AUDIO_CHANNEL_4"));
		if (audioChannel4Button == Keyboard.Key.Unknown)
		{
			Logger.LogInvalidConfigValue(
				"[Controls].AUDIO_CHANNEL_4", Config.GetControlConfig("AUDIO_CHANNEL_4"), DEFAULT_AUDIO_CHANNEL_4_BUTTON
			);

			audioChannel4Button = DEFAULT_AUDIO_CHANNEL_4_BUTTON;
		}
	}

	private static void InitialiseColors()
	{
		blackColor = ConvertIntToSfmlColor(Config.GetColorConfig("BLACK"));
		if (blackColor == Color.Transparent)
		{
			Logger.LogInvalidConfigValue(
				"[Colors].BLACK", Config.GetColorConfig("BLACK"), ConvertSfmlColorToInt(DEFAULT_BLACK_COLOR)
			);

			blackColor = DEFAULT_BLACK_COLOR;
		}

		darkGrayColor = ConvertIntToSfmlColor(Config.GetColorConfig("DARK_GRAY"));
		if (darkGrayColor == Color.Transparent)
		{
			Logger.LogInvalidConfigValue(
				"[Colors].DARK_GRAY", Config.GetColorConfig("DARK_GRAY"), ConvertSfmlColorToInt(DEFAULT_DARK_GRAY_COLOR)
			);

			darkGrayColor = DEFAULT_DARK_GRAY_COLOR;
		}

		lightGrayColor = ConvertIntToSfmlColor(Config.GetColorConfig("LIGHT_GRAY"));
		if (lightGrayColor == Color.Transparent)
		{
			Logger.LogInvalidConfigValue(
				"[Colors].LIGHT_GRAY", Config.GetColorConfig("LIGHT_GRAY"),
				ConvertSfmlColorToInt(DEFAULT_LIGHT_GRAY_COLOR)
			);

			lightGrayColor = DEFAULT_LIGHT_GRAY_COLOR;
		}

		whiteColor = ConvertIntToSfmlColor(Config.GetColorConfig("WHITE"));
		if (whiteColor == Color.Transparent)
		{
			Logger.LogInvalidConfigValue(
				"[Colors].WHITE", Config.GetColorConfig("WHITE"), ConvertSfmlColorToInt(DEFAULT_WHITE_COLOR)
			);

			whiteColor = DEFAULT_WHITE_COLOR;
		}
	}

	private static int ConvertSfmlColorToInt(Color color)
	{
		return (color.R << 16) | (color.G << 8) | color.B;
	}

	private static Color ConvertIntToSfmlColor(int? color)
	{
		if (color is null) return Color.Transparent;

		//Shifting and oring is done to set the alpha component of the colors to 0xFF
		//This is done to make the colors fully opaque
		return color == -1 ? Color.Transparent : new Color((uint)((color << 8) | 0xFF));
	}

	private static Keyboard.Key ConvertStringToSfmlKey(string? keyString)
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