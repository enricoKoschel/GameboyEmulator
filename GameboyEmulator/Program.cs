using System;
using GameboyEmulatorScreen;

namespace GameboyEmulatorMain
{
    class Program
    {
        static void Main(string[] args)
        {
            Screen screen = new Screen();

            while (screen.IsOpen)
            {
                screen.DrawScreen();
            }            
        }
    }
}
