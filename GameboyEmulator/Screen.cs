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

		private readonly RectangleShape[,] buffer;
		private readonly RenderWindow      window;

		public Screen()
		{
			buffer = new RectangleShape[SCREEN_GAME_WIDTH, SCREEN_GAME_HEIGHT];
			window = new RenderWindow(
				new VideoMode(SCREEN_DRAW_WIDTH, SCREEN_DRAW_HEIGHT), "GameBoy Emulator", Styles.Close
			);

			window.SetActive();
			Initialize();

			//Assign event handlers
			window.Closed += OnClose;
		}

		public bool IsOpen => window.IsOpen;

		private void Initialize()
		{
			for (int i = 0; i < SCREEN_GAME_WIDTH; i++)
			{
				for (int j = 0; j < SCREEN_GAME_HEIGHT; j++)
				{
					buffer[i, j] = new RectangleShape
					{
						Size     = new Vector2f(SCREEN_SCALE, SCREEN_SCALE),
						Position = new Vector2f(i * SCREEN_SCALE, j * SCREEN_SCALE)
					};

					//TODO - Remove Debug pattern
					switch ((i % 4) + (j % 4))
					{
						case 0:
						case 4:
							buffer[i, j].FillColor = Colors.white;
							break;
						case 1:
						case 5:
							buffer[i, j].FillColor = Colors.lightGray;
							break;
						case 2:
						case 6:
							buffer[i, j].FillColor = Colors.darkGray;
							break;
						case 3:
							buffer[i, j].FillColor = Colors.black;
							break;
					}
				}
			}
		}

		public void Draw()
		{
			if (!window.IsOpen) throw new InvalidOperationException("Cannot draw Screen when Window is closed!");

			window.Clear();
			window.DispatchEvents();

			foreach (RectangleShape rect in buffer) window.Draw(rect);

			window.Display();
		}


		//Event handlers
		private static void OnClose(object sender, EventArgs e)
		{
			RenderWindow window = (RenderWindow)sender;
			window.Close();
		}
	}
}