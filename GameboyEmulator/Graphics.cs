using GameboyEmulatorMemory;
using GameboyEmulatorLCD;
using GameboyEmulatorCPU;
using GameboyEmulatorScreen;

namespace GameboyEmulatorGraphics
{
    class Graphics
    {
        //Modules
        private readonly Memory memory; //TODO - Maybe remove this
        private readonly LCD lcd;
        private readonly Screen screen;

        public Graphics(Memory memory, CPU cpu)
        {
            this.memory = memory;

            screen = new Screen();
            lcd = new LCD(memory, cpu, screen);
        }

        public bool IsScreenOpen
        {
            get
            {
                return screen.IsOpen;
            }
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
