using System;
using GameboyEmulatorCPU;
using GameboyEmulatorMemory;
using GameboyEmulatorScreen;

namespace GameboyEmulatorLCD
{
    class LCD
    {
        //Modules
        private readonly Memory memory;
        private readonly CPU cpu;
        private readonly Screen screen;

        public LCD(Memory memory, CPU cpu, Screen screen)
        {
            this.memory = memory;
            this.cpu = cpu;
            this.screen = screen;
        }

        //Constants
        private const int MODE_2_TIME = 80;
        private const int MODE_3_TIME = 252;

        private int drawScanlineCounter = 0;
        public bool shouldDrawScanline = false;
        public bool shouldIncreaseScanline = false;

        //Registers
        private byte Mode
        {
            get
            {
                return (byte)(StatusRegister & 0b00000011);
            }
            set
            {
                if (value > 0x3)
                {
                    throw new ArgumentOutOfRangeException("LCD Mode cannot be larger than 3!");
                }

                StatusRegister &= 0b11111100;
                StatusRegister |= value;
            }
        }
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
        public byte ScrollX
        {
            get
            {
                return memory.Read(0xFF43);
            }
            set
            {
                memory.Write(0xFF43, value);
            }
        }
        public byte ScrollY
        {
            get
            {
                return memory.Read(0xFF42);
            }
            set
            {
                memory.Write(0xFF42, value);
            }
        }
        public byte WindowX
        {
            get
            {
                return memory.Read(0xFF4B);
            }
            set
            {
                memory.Write(0xFF4B, value);
            }
        }
        public byte WindowY
        {
            get
            {
                return (byte)(memory.Read(0xFF4A) - 7);
            }
            set
            {
                memory.Write(0xFF4A, (byte)(value + 7));
            }
        }       
        public ushort WindowTileMapBaseAddress
        {
            get
            {
                if(cpu.GetBit(ControlRegister, 6))
                {
                    return 0x9C00;
                }
                else
                {
                    return 0x9800;
                }
            }
        }
        public ushort BackgroundTileMapBaseAddress
        {
            get
            {
                if (cpu.GetBit(ControlRegister, 3))
                {
                    return 0x9C00;
                }
                else
                {
                    return 0x9800;
                }
            }
        }
        public ushort TileDataBaseAddress
        {
            get
            {
                if (cpu.GetBit(ControlRegister, 4))
                {
                    return 0x8000;
                }
                else
                {
                    return 0x8800;
                }
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
        public bool WindowEnabled
        {
            get
            {
                return cpu.GetBit(ControlRegister, 5);
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

                    screen.Draw();
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
