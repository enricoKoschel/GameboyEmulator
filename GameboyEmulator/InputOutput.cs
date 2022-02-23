using System;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GameboyEmulator
{
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

		private readonly VertexBuffer vertexBuffer;
		private readonly Vertex[]     vertexArray;

		private readonly bool[,] zBuffer;

		public InputOutput()
		{
			window = new RenderWindow(
				new VideoMode(DRAW_WIDTH, DRAW_HEIGHT), "GameBoy Emulator", Styles.Close
			);

			vertexArray = new Vertex[NUMBER_OF_VERTICES];
			vertexBuffer = new VertexBuffer(
				NUMBER_OF_VERTICES, PrimitiveType.Quads, VertexBuffer.UsageSpecifier.Stream
			);

			zBuffer = new bool[GAME_WIDTH, GAME_HEIGHT];

			Initialize();

			window.SetActive();

			//Gameboy runs at 60 fps
			window.SetFramerateLimit(60);
			window.Closed += OnClosed;
		}

		public static bool IsButtonPressed(Joypad.Button button)
		{
			return button switch
			{
				Joypad.Button.Up     => Keyboard.IsKeyPressed(Keyboard.Key.Up),
				Joypad.Button.Down   => Keyboard.IsKeyPressed(Keyboard.Key.Down),
				Joypad.Button.Left   => Keyboard.IsKeyPressed(Keyboard.Key.Left),
				Joypad.Button.Right  => Keyboard.IsKeyPressed(Keyboard.Key.Right),
				Joypad.Button.Start  => Keyboard.IsKeyPressed(Keyboard.Key.Enter),
				Joypad.Button.Select => Keyboard.IsKeyPressed(Keyboard.Key.Space),
				Joypad.Button.A      => Keyboard.IsKeyPressed(Keyboard.Key.S),
				Joypad.Button.B      => Keyboard.IsKeyPressed(Keyboard.Key.A),
				_                    => throw new ArgumentOutOfRangeException(nameof(button), button, "Invalid button!")
			};
		}

		private void Initialize()
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

		public void DrawFrame()
		{
			if (!WindowIsOpen)
			{
				Logger.LogMessage("Cannot draw Screen when Window is closed!", Logger.LogLevel.Error);
				throw new InvalidOperationException("Cannot draw Screen when Window is closed!");
			}

			window.DispatchEvents();

			vertexBuffer.Update(vertexArray);
			vertexBuffer.Draw(window, RenderStates.Default);

			window.Display();
		}

		public void ClearFrame(Ppu.Color color)
		{
			for (int i = 0; i < NUMBER_OF_VERTICES; i++) vertexArray[i].Color = ConvertGameboyToSfmlColor(color);
		}

		private Color ConvertGameboyToSfmlColor(Ppu.Color color)
		{
			return color switch
			{
				Ppu.Color.Black     => new Color(8, 24, 32),
				Ppu.Color.DarkGray  => new Color(52, 104, 86),
				Ppu.Color.LightGray => new Color(136, 192, 112),
				Ppu.Color.White     => new Color(224, 248, 208),
				_                   => throw new ArgumentOutOfRangeException(nameof(color), color, "Invalid color!")
			};
		}

		public void SetWindowTitle(string title)
		{
			window.SetTitle(title);
		}

		private static void OnClosed(object sender, EventArgs e)
		{
			RenderWindow window = (RenderWindow)sender;
			window.Close();
		}
	}
}