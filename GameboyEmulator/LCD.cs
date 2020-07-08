using System;
using GameboyEmulatorCPU;
using GameboyEmulatorMemory;

namespace GameboyEmulatorLCD
{
    class LCD
    {
        //Modules
        readonly Memory memory;
        readonly CPU cpu;

        public LCD(Memory memory, CPU cpu)
        {
            this.memory = memory;
            this.cpu = cpu;
        }

        //Constants
        private const int MODE_2_TIME = 80;
        private const int MODE_3_TIME = 252;

        private int drawScanlineCounter = 0;
        public bool shouldDrawScanline = false;
        public bool shouldIncreaseScanline = false;
        public byte CurrentScanline
        {
            get
            {
                return memory.Read(0xFF44);
            }
            set
            {
                memory.Write(0xFF44, value, true);
            }
        }

        //Registers
        private byte ControlRegister
        {
            get
            {
                return memory.Read(0xFF40);
            }
            set
            {
                memory.Write(0xFF40, value);
            }
        }
        private byte StatusRegister
        {
            get
            {
                return memory.Read(0xFF41);
            }
            set
            {
                memory.Write(0xFF41, value);
            }
        }

        //Flags
        public bool IsEnabled
        {
            get
            {
                return cpu.GetBit(ControlRegister, 7);
            }
        }
        private byte Mode
        {
            get
            {
                return (byte)(StatusRegister & 0b00000011);
            }
            set
            {
                if(value > 0x3)
                {
                    throw new ArgumentOutOfRangeException("LCD Mode cannot be larger than 3!");
                }

                StatusRegister &= 0b11111100;
                StatusRegister |= value;
            }
        }
        public bool TilesEnabled
        {
            get
            {
                return cpu.GetBit(ControlRegister, 0);
            }
        }
        public bool SpritesEnabled
        {
            get
            {
                return cpu.GetBit(ControlRegister, 1);
            }
        }

        public void Update(int cycles)
        {
            drawScanlineCounter += cycles;

            shouldDrawScanline = false;
            shouldIncreaseScanline = false;

            if (drawScanlineCounter >= 456)
            {
                //Increase Scanline every 456 Clockcycles, only draw if not in VBlank
                if (CurrentScanline < 144)
                {
                    shouldDrawScanline = true;
                }
                
                shouldIncreaseScanline = true;
                drawScanlineCounter = 0;
            }

            SetStatus();
        }

        private void SetStatus()
        {
            //TODO - Maybe add this
            //if (!lcdEnabled())
            //{
            //    scanlineCounter = 456;
            //    mainMem[SCANLINE_REG] = 0;
            //    status = (status & 0b11111100) | 1;
            //    writeMem(LCD_STATUS_REG, status);
            //    return;
            //}

            //TODO - Interrupts  

            if(CurrentScanline >= 144)
            {
                //VBlank
                Mode = 1;

                if (CurrentScanline > 153)
                {
                    //One Frame done
                    CurrentScanline = 0;
                    drawScanlineCounter = 0;
                }
            }
            else
            {
                if(drawScanlineCounter < MODE_2_TIME)
                {
                    //Accessing OAM
                    Mode = 2;
                }
                else if (drawScanlineCounter < MODE_3_TIME)
                {
                    //Accessing VRAM
                    Mode = 3;
                }
                else
                {
                    //HBlank
                    Mode = 0;
                }
            }
        }
    }
}
