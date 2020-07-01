using GameboyEmulatorScreen;
using GameboyEmulatorCPU;

namespace GameboyEmulatorMain
{
    class Program
    {
        static void Main(string[] args)
        {
            Screen screen = new Screen();
            CPU emulator = new CPU();

            emulator.Start();

            while (screen.IsOpen)
            {
                emulator.Update();
                screen.DrawScreen();
            }            
        }
    }
}
