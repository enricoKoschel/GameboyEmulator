using GameboyEmulatorMemory;
using GameboyEmulatorLCD;
using GameboyEmulatorCPU;

namespace GameboyEmulatorGraphics
{
    class Graphics
    {
        //Modules
        Memory memory;
        LCD lcd;

        public Graphics(Memory memory, CPU cpu)
        {
            this.memory = memory;

            lcd = new LCD(memory, cpu);
        }

        public void Update(int cycles)
        {
            lcd.Update(cycles);

            if (lcd.shouldDrawScanline)
            {

            }
        }
    }
}
