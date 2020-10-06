using System;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GameboyEmulator
{
	readonly struct Color
	{
		public static SFML.Graphics.Color black     = new SFML.Graphics.Color(8, 24, 32);
		public static SFML.Graphics.Color darkGray  = new SFML.Graphics.Color(52, 104, 86);
		public static SFML.Graphics.Color lightGray = new SFML.Graphics.Color(136, 192, 112);
		public static SFML.Graphics.Color white     = new SFML.Graphics.Color(224, 248, 208);
	}

	class Screen
	{
		private const int SCREEN_GAME_WIDTH  = 160, SCREEN_GAME_HEIGHT = 144;
		private const int NUMBER_OF_VERTICES = SCREEN_GAME_WIDTH * SCREEN_GAME_HEIGHT * 4;
		private const int SCREEN_SCALE       = 8;
		private const int SCREEN_DRAW_WIDTH  = SCREEN_GAME_WIDTH * SCREEN_SCALE;
		private const int SCREEN_DRAW_HEIGHT = SCREEN_GAME_HEIGHT * SCREEN_SCALE;

		private readonly RenderWindow window;
		
		private readonly VertexBuffer vertexBuffer;
		private readonly Vertex[]     vertexArray;

		public Screen()
		{
			vertexArray = new Vertex[NUMBER_OF_VERTICES];
			vertexBuffer = new VertexBuffer(
				NUMBER_OF_VERTICES, PrimitiveType.Quads, VertexBuffer.UsageSpecifier.Stream
			);

			window = new RenderWindow(
				new VideoMode(SCREEN_DRAW_WIDTH, SCREEN_DRAW_HEIGHT), "GameBoy Emulator", Styles.Close
			);

			window.SetActive();

			//Gameboy runs at 60 fps
			window.SetFramerateLimit(60);

			Initialize();

			window.Closed += OnClosed;
		}

		public void UpdateBuffer(int x, int y, SFML.Graphics.Color color)
		{
			int index = (x * 4) + y * SCREEN_GAME_WIDTH * 4;

			vertexArray[index + 0].Color = color;
			vertexArray[index + 1].Color = color;
			vertexArray[index + 2].Color = color;
			vertexArray[index + 3].Color = color;
		}

		public bool IsOpen => window.IsOpen;

		public Window GetWindow()
		{
			return window;
		}

		private void Initialize()
		{
			for (int i = 0; i < vertexArray.Length; i += 4)
			{
				int x = i % (SCREEN_GAME_WIDTH * 4);
				int y = i / (SCREEN_GAME_WIDTH * 4);

				int leftSide = x / 4 * SCREEN_SCALE;
				int topSide  = y * SCREEN_SCALE;

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
					new Vector2f(leftSide + SCREEN_SCALE, topSide), SFML.Graphics.Color.Blue
				);

				vertexArray[i + 2] = new Vertex(
					new Vector2f(leftSide + SCREEN_SCALE, topSide + SCREEN_SCALE), SFML.Graphics.Color.Blue
				);

				vertexArray[i + 3] = new Vertex(
					new Vector2f(leftSide, topSide + SCREEN_SCALE), SFML.Graphics.Color.Blue
				);
			}
		}

		public void DrawFrame()
		{
			if (!window.IsOpen) throw new InvalidOperationException("Cannot draw Screen when Window is closed!");
			
			window.DispatchEvents();
			
			vertexBuffer.Update(vertexArray);
			vertexBuffer.Draw(window, RenderStates.Default);

			window.Display();
		}

		//Event handlers
		private static void OnClosed(object sender, EventArgs e)
		{
			RenderWindow window = (RenderWindow)sender;
			window.Close();
		}
	}
}