using System;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace GameboyEmulatorScreen
{
    readonly struct Colors
    {
        static public Color BLACK = new Color(8, 24, 32);
        static public Color DARK_GRAY = new Color(52, 104, 86);
        static public Color LIGHT_GRAY = new Color(136, 192, 112);
        static public Color WHITE = new Color(224, 248, 208);
    }

    class Screen
    {
        private const int SCREEN_GAME_WIDTH = 160, SCREEN_GAME_HEIGHT = 144;
        private const int SCREEN_SCALE = 8;
        private const int SCREEN_DRAW_WIDTH = SCREEN_GAME_WIDTH * SCREEN_SCALE;
        private const int SCREEN_DRAW_HEIGHT = SCREEN_GAME_HEIGHT * SCREEN_SCALE;

        private readonly RectangleShape[,] buffer;
        private readonly RenderWindow window;
        
        public Screen()
        {
            buffer = new RectangleShape[SCREEN_GAME_WIDTH, SCREEN_GAME_HEIGHT];
            window = new RenderWindow(new VideoMode(SCREEN_DRAW_WIDTH, SCREEN_DRAW_HEIGHT), "GameBoy Emulator", Styles.Close);
            window.SetActive();
            Initialize();


            //Assign event handlers
            window.Closed += OnClose;
        }

        public bool IsOpen
        {
            get { return window.IsOpen; }
        }

        private void Initialize()
        {
            for (int i = 0; i < SCREEN_GAME_WIDTH; i++)
            {
                for (int j = 0; j < SCREEN_GAME_HEIGHT; j++)
                {
                    buffer[i, j] = new RectangleShape();
                    buffer[i, j].Size = new Vector2f(SCREEN_SCALE, SCREEN_SCALE);
                    buffer[i, j].Position = new Vector2f(i * SCREEN_SCALE, j * SCREEN_SCALE);

                    //TODO - Remove Debug pattern
                    switch ((i % 4) + (j % 4))
                    {
                        case 0:
                        case 4:
                            buffer[i, j].FillColor = Colors.WHITE;
                            break;
                        case 1:
                        case 5:
                            buffer[i, j].FillColor = Colors.LIGHT_GRAY;
                            break;
                        case 2:
                        case 6:
                            buffer[i, j].FillColor = Colors.DARK_GRAY;
                            break;
                        case 3:
                            buffer[i, j].FillColor = Colors.BLACK;
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

            foreach (RectangleShape rect in buffer)
            {
                window.Draw(rect);
            }

            window.Display();
        }


        //Event handlers
        private void OnClose(object sender, EventArgs e)
        {
            RenderWindow window = (RenderWindow)sender;
            window.Close();
        }
    }
}
