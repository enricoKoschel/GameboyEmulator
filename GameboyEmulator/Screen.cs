using System;
using SFML.Graphics;

namespace GameboyEmulator
{
	readonly struct Color
	{
		public static SFML.Graphics.Color black     = new SFML.Graphics.Color(8, 24, 32);
		public static SFML.Graphics.Color darkGray  = new SFML.Graphics.Color(52, 104, 86);
		public static SFML.Graphics.Color lightGray = new SFML.Graphics.Color(136, 192, 112);
		public static SFML.Graphics.Color white     = new SFML.Graphics.Color(224, 248, 208);
	}

	public class Screen
	{
		private const int GAME_WIDTH         = 160, GAME_HEIGHT = 144;
		private const int NUMBER_OF_VERTICES = GAME_WIDTH * GAME_HEIGHT * 4;
		private const int SCALE              = 8;
		public const  int DRAW_WIDTH         = GAME_WIDTH * SCALE;
		public const  int DRAW_HEIGHT        = GAME_HEIGHT * SCALE;

		private readonly VertexBuffer vertexBuffer;
		private readonly Vertex[]     vertexArray;

		private readonly bool[,] zBuffer;

		private readonly Emulator emulator;

		public Screen(Emulator emulator)
		{
			this.emulator = emulator;

			vertexArray = new Vertex[NUMBER_OF_VERTICES];
			vertexBuffer = new VertexBuffer(
				NUMBER_OF_VERTICES, PrimitiveType.Quads, VertexBuffer.UsageSpecifier.Stream
			);

			zBuffer = new bool[GAME_WIDTH, GAME_HEIGHT];

			window.SetActive();

			//Gameboy runs at 60 fps
			window.SetFramerateLimit(60);

			Initialize();

			window.Closed += OnClosed;
		}

		public void UpdatePixelBuffer(int x, int y, SFML.Graphics.Color color)
		{
			int index = x * 4 + y * GAME_WIDTH * 4;

			vertexArray[index + 0].Color = color;
			vertexArray[index + 1].Color = color;
			vertexArray[index + 2].Color = color;
			vertexArray[index + 3].Color = color;
		}

		public void UpdateZBuffer(int x, int y, bool behindSprite)
		{
			zBuffer[x, y] = behindSprite;
		}

		public bool GetZBufferAt(int x, int y)
		{
			return zBuffer[x, y];
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

				vertexArray[i + 0] = new Vertex(
					new Vector2f(leftSide, topSide), SFML.Graphics.Color.Blue
				);

				vertexArray[i + 1] = new Vertex(
					new Vector2f(leftSide + SCALE, topSide), SFML.Graphics.Color.Blue
				);

				vertexArray[i + 2] = new Vertex(
					new Vector2f(leftSide + SCALE, topSide + SCALE), SFML.Graphics.Color.Blue
				);

				vertexArray[i + 3] = new Vertex(
					new Vector2f(leftSide, topSide + SCALE), SFML.Graphics.Color.Blue
				);
			}
		}

		public void DrawFrame()
		{
			if (!window.IsOpen)
			{
				Logger.LogMessage("Cannot draw Screen when Window is closed!", Logger.LogLevel.Error);
				throw new InvalidOperationException("Cannot draw Screen when Window is closed!");
			}

			window.DispatchEvents();

			vertexBuffer.Update(vertexArray);
			vertexBuffer.Draw(window, RenderStates.Default);

			window.Display();
		}

		public void Clear(SFML.Graphics.Color color)
		{
			for (int i = 0; i < NUMBER_OF_VERTICES; i++) vertexArray[i].Color = color;
		}

		//Event handlers
		private static void OnClosed(object sender, EventArgs e)
		{
			RenderWindow window = (RenderWindow)sender;
			window.Close();
		}
	}
}