using GameboyEmulatorCPU;

namespace GameboyEmulatorMain
{
    class Program
    {
        static void Main(string[] args)
        {
            CPU emulator = new CPU();

            emulator.Start();

            while (emulator.IsRunning)
            {
                emulator.Update();
            }            
        }
    }
}
