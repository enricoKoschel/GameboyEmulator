using GameboyEmulatorMemory;
using GameboyEmulatorLCD;
using GameboyEmulatorCPU;

namespace GameboyEmulatorGraphics
{
    class Graphics
    {
        //Modules
        readonly Memory memory; //TODO - Maybe remove this
        readonly LCD lcd;

        public Graphics(Memory memory, CPU cpu)
        {
            this.memory = memory;

            lcd = new LCD(memory, cpu);
        }

        public void Update(int cycles)
        {
            if (!lcd.IsEnabled)
            {
                return;
            }

            lcd.Update(cycles);

            if (lcd.shouldDrawScanline)
            {
                DrawScanline();
            }

            if (lcd.shouldIncreaseScanline)
            {
                lcd.CurrentScanline++;
            }           
        }

        private void DrawScanline()
        {
            if (lcd.TilesEnabled)
            {
                RenderTiles();
            }
            if (lcd.SpritesEnabled)
            {
                RenderSprites();
            }
        }

        private void RenderTiles()
        {

        }
        private void RenderSprites()
        {

        }
    }
}
