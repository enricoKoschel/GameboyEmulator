using System;
using GameboyEmulatorScreen;
using GameboyEmulatorCore;

namespace GameboyEmulatorMain
{
    class Program
    {
        static void Main(string[] args)
        {
            Screen screen = new Screen();
            Gameboy emulator = new Gameboy();

            emulator.LoadGame();

            while (screen.IsOpen)
            {
                emulator.updateCPU();
                screen.DrawScreen();
            }            
        }
    }
}
