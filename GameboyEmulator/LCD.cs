using System;
using GameboyEmulatorCPU;
using GameboyEmulatorMemory;

namespace GameboyEmulatorLCD
{
    class LCD
    {
        //Modules
        Memory memory;
        CPU cpu;

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
        private byte CurrentScanline
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
        private bool IsEnabled
        {
            get
            {
                return cpu.GetBit(ControlRegister, 7);
            }
            set
            {
                ControlRegister = cpu.SetBit(ControlRegister, 7, value);
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

        public void Update(int cycles)
        {
            if (IsEnabled)
            {
                drawScanlineCounter += cycles;
            }
            else
            {
                return;
            }

            shouldDrawScanline = false;

            if(drawScanlineCounter >= 456)
            {
                //Draw Scanline every 456 Clockcycles
                shouldDrawScanline = true;
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
