using System;
using System.IO;

namespace GameboyEmulatorMemory
{
    class Memory
    {
        //Memory Map 
        private byte[] cartridgeRomWithBanks;       //Complete cartridge with all banks

        private byte[] cartridgeRom;                //0x0000-0x7FFF | First two rom banks
        private readonly byte[] videoRam;           //0x8000-0x9FFF
        private readonly byte[] cartridgeRam;       //0xA000-0xBFFF
        private readonly byte[] workRam;            //0xC000-0xDFFF
                                                    //0xE000-0xFDFF (same as 0xC000-0xDDFF)
        private readonly byte[] spriteAtributes;    //0xFE00-0xFE9F
                                                    //0xFEA0-0xFEFF (unused)
        private readonly byte[] ioPorts;            //0xFF00-0xFF7F
        private readonly byte[] highRam;            //0xFF80-0xFFFE
        private byte interruptEnableReg;            //0xFFFF

        private const int CARTRIDGE_ROM_BASE_ADDRESS = 0x0000;
        private const int VIDEO_RAM_BASE_ADDRESS = 0x8000;
        private const int CARTRIDGE_RAM_BASE_ADDRESS = 0xA000;
        private const int WORK_RAM_BASE_ADDRESS = 0xC000;
        private const int ECHO_RAM_BASE_ADDRESS = 0xE000;
        private const int SPRITE_ATTRIBUTES_BASE_ADDRESS = 0xFE00;
        private const int UNUSED_BASE_ADDRESS = 0xFEA0;
        private const int IO_PORTS_BASE_ADDRESS = 0xFF00;
        private const int HIGH_RAM_BASE_ADDRESS = 0xFF80;
        private const int INTERRUPT_ENABLE_REG_ADDRESS = 0xFFFF;

        //File paths
        private const string BOOT_ROM_FILE_PATH = "../../../roms/boot.gb";
        private const string gameRomFilePath = "../../../roms/tetris.gb"; //TODO - Accept game file path as console parameter / make into property

        public Memory()
        {
            videoRam = new byte[0x2000];
            cartridgeRam = new byte[0x2000];
            workRam = new byte[0x2000];
            spriteAtributes = new byte[0x100];
            ioPorts = new byte[0x80];
            highRam = new byte[0x7F];
        }

        public void LoadGame()
        {
            cartridgeRom = File.ReadAllBytes(BOOT_ROM_FILE_PATH);

            //Resize for Cartridge Header
            Array.Resize(ref cartridgeRom, 0x150);

            cartridgeRomWithBanks = File.ReadAllBytes(gameRomFilePath);

            //Copy Cartridge Header
            for (int i = 0x100; i < 0x150; i++)
            {
                cartridgeRom[i] = cartridgeRomWithBanks[i];
            }
        }

        public void DisableBootRom()
        {
            //Overwrite Boot rom with Game rom, thus disabling it
            cartridgeRom = cartridgeRomWithBanks;
        }

        public byte Read(ushort address)
        {
            if (address < CARTRIDGE_ROM_BASE_ADDRESS || IsBetween(address, UNUSED_BASE_ADDRESS, IO_PORTS_BASE_ADDRESS))
            {
                //Unused Memory / Out of Bounds
                throw new IndexOutOfRangeException($"Cannot access Memory at Address {address}!");
            }

            if (IsBetween(address, CARTRIDGE_ROM_BASE_ADDRESS, VIDEO_RAM_BASE_ADDRESS))
            {
                return cartridgeRom[address];
            }
            else if (IsBetween(address, IO_PORTS_BASE_ADDRESS, HIGH_RAM_BASE_ADDRESS))
            {
                //IO Ports
                //TODO - Implement IO Ports
                return ioPorts[address - IO_PORTS_BASE_ADDRESS];
            }
            else if (IsBetween(address, HIGH_RAM_BASE_ADDRESS, INTERRUPT_ENABLE_REG_ADDRESS))
            {
                //High Ram
                return highRam[address - HIGH_RAM_BASE_ADDRESS];
            }
            else
            {
                throw new NotImplementedException($"Read Memory location: 0x{address:X} not implemented yet!"); //TODO - implement read memory
            }           
        }

        public void Write(ushort address, byte data, bool dontReset = false)
        {
            if (address < CARTRIDGE_ROM_BASE_ADDRESS || IsBetween(address, UNUSED_BASE_ADDRESS, IO_PORTS_BASE_ADDRESS))
            {
                //Unused Memory / Out of Bounds
                throw new IndexOutOfRangeException($"Cannot access Memory at Address {address}!");
            }

            if (IsBetween(address, CARTRIDGE_ROM_BASE_ADDRESS, VIDEO_RAM_BASE_ADDRESS))
            {                
                //Writing to Cartridge ROM triggers ROMBanking Controller
                throw new NotImplementedException("Rombanking not implemented yet!"); //TODO - handle rombanking
            }
            else if (IsBetween(address, VIDEO_RAM_BASE_ADDRESS, CARTRIDGE_RAM_BASE_ADDRESS))
            {
                //Video RAM
                videoRam[address - VIDEO_RAM_BASE_ADDRESS] = data;
            }
            else if (IsBetween(address, IO_PORTS_BASE_ADDRESS, HIGH_RAM_BASE_ADDRESS))
            {
                if (address == 0xFF50)
                {
                    //Boot Rom done
                    DisableBootRom();
                }

                //IO Ports
                //TODO - Implement IO Ports
                if (dontReset)
                {
                    ioPorts[address - IO_PORTS_BASE_ADDRESS] = data;
                }

                switch (address)
                {
                    case 0xFF44:
                        {
                            //Current Scanline register - Reset when written to
                            ioPorts[address - IO_PORTS_BASE_ADDRESS] = 0;
                            break;
                        }
                    default:
                        {
                            ioPorts[address - IO_PORTS_BASE_ADDRESS] = data;
                            break;
                        }
                }
            }
            else if (IsBetween(address, HIGH_RAM_BASE_ADDRESS, INTERRUPT_ENABLE_REG_ADDRESS))
            {
                //High Ram
                highRam[address - HIGH_RAM_BASE_ADDRESS] = data;
            }           
            else
            {
                throw new NotImplementedException($"Write Memory location: 0x{address:X} not implemented yet!"); //TODO - implement write memory
            }           
        }

        //Utility functions
        /// <summary>
        /// lowerBound is inclusive, upperBound is exclusive
        /// </summary>
        private bool IsBetween(int number, int lowerBound, int upperBound)
        {
            return (number >= lowerBound && number < upperBound);
        }
    }
}
