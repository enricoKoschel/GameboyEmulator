using System;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GameboyEmulator
{
	readonly struct Colors
	{
		public static Color black     = new Color(8, 24, 32);
		public static Color darkGray  = new Color(52, 104, 86);
		public static Color lightGray = new Color(136, 192, 112);
		public static Color white     = new Color(224, 248, 208);
	}

	class Screen
	{
		private const int SCREEN_GAME_WIDTH  = 160, SCREEN_GAME_HEIGHT = 144;
		private const int SCREEN_SCALE       = 8;
		private const int SCREEN_DRAW_WIDTH  = SCREEN_GAME_WIDTH * SCREEN_SCALE;
		private const int SCREEN_DRAW_HEIGHT = SCREEN_GAME_HEIGHT * SCREEN_SCALE;

		private readonly RenderWindow window;

		public RectangleShape[,] Buffer { get; }

		public Screen()
		{
			Buffer = new RectangleShape[SCREEN_GAME_WIDTH, SCREEN_GAME_HEIGHT];
			window = new RenderWindow(
				new VideoMode(SCREEN_DRAW_WIDTH, SCREEN_DRAW_HEIGHT), "GameBoy Emulator", Styles.Close
			);

			window.SetActive();

			//Gameboy runs at 60 fps
			window.SetFramerateLimit(60);

			Initialize();

			window.Closed += OnClose;
		}

		public bool IsOpen => window.IsOpen;

		public Window GetWindow()
		{
			return window;
		}

		private void Initialize()
		{
			for (int i = 0; i < SCREEN_GAME_WIDTH; i++)
			{
				for (int j = 0; j < SCREEN_GAME_HEIGHT; j++)
				{
					Buffer[i, j] = new RectangleShape
					{
						Size     = new Vector2f(SCREEN_SCALE, SCREEN_SCALE),
						Position = new Vector2f(i * SCREEN_SCALE, j * SCREEN_SCALE)
					};
				}
			}
		}

		public void DrawFrame()
		{
			if (!window.IsOpen) throw new InvalidOperationException("Cannot draw Screen when Window is closed!");

			window.Clear();
			window.DispatchEvents();

			foreach (RectangleShape rect in Buffer) window.Draw(rect);

			window.Display();
		}

		public void Clear()
		{
			for (int i = 0; i < SCREEN_GAME_WIDTH; i++)
			{
				for (int j = 0; j < SCREEN_GAME_HEIGHT; j++)
				{
					Buffer[i, j].FillColor = Colors.white;
					;
				}
			}
		}

		//Event handlers
		private static void OnClose(object sender, EventArgs e)
		{
			RenderWindow window = (RenderWindow)sender;
			window.Close();
		}
	}
}