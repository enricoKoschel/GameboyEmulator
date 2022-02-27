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

		//Key mapping
		private const Keyboard.Key UP_BUTTON     = Keyboard.Key.Up;
		private const Keyboard.Key DOWN_BUTTON   = Keyboard.Key.Down;
		private const Keyboard.Key LEFT_BUTTON   = Keyboard.Key.Left;
		private const Keyboard.Key RIGHT_BUTTON  = Keyboard.Key.Right;
		private const Keyboard.Key START_BUTTON  = Keyboard.Key.Enter;
		private const Keyboard.Key SELECT_BUTTON = Keyboard.Key.Space;
		private const Keyboard.Key A_BUTTON      = Keyboard.Key.S;
		private const Keyboard.Key B_BUTTON      = Keyboard.Key.A;
		private const Keyboard.Key SPEED_BUTTON  = Keyboard.Key.Add;

		//Color mapping
		private static readonly Color BLACK_COLOR      = new Color(8, 24, 32);
		private static readonly Color DARK_GRAY_COLOR  = new Color(52, 104, 86);
		private static readonly Color LIGHT_GRAY_COLOR = new Color(136, 192, 112);
		private static readonly Color WHITE_COLOR      = new Color(224, 248, 208);

		private readonly VertexBuffer vertexBuffer;
		private readonly Vertex[]     vertexArray;

		private readonly bool[,] zBuffer;

		private readonly Emulator emulator;

		public InputOutput(Emulator emulator)
		{
			this.emulator = emulator;

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

			window.Closed += OnClosed;
		}

		public static bool IsButtonPressed(Joypad.Button button)
		{
			return button switch
			{
				Joypad.Button.Up     => Keyboard.IsKeyPressed(UP_BUTTON),
				Joypad.Button.Down   => Keyboard.IsKeyPressed(DOWN_BUTTON),
				Joypad.Button.Left   => Keyboard.IsKeyPressed(LEFT_BUTTON),
				Joypad.Button.Right  => Keyboard.IsKeyPressed(RIGHT_BUTTON),
				Joypad.Button.Start  => Keyboard.IsKeyPressed(START_BUTTON),
				Joypad.Button.Select => Keyboard.IsKeyPressed(SELECT_BUTTON),
				Joypad.Button.A      => Keyboard.IsKeyPressed(A_BUTTON),
				Joypad.Button.B      => Keyboard.IsKeyPressed(B_BUTTON),
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

		public void Update()
		{
			if (Keyboard.IsKeyPressed(SPEED_BUTTON)) emulator.MaxFps = 0;
			else emulator.MaxFps                                     = Emulator.GAMEBOY_FPS;
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

		private static Color ConvertGameboyToSfmlColor(Ppu.Color color)
		{
			return color switch
			{
				Ppu.Color.Black     => BLACK_COLOR,
				Ppu.Color.DarkGray  => DARK_GRAY_COLOR,
				Ppu.Color.LightGray => LIGHT_GRAY_COLOR,
				Ppu.Color.White     => WHITE_COLOR,
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