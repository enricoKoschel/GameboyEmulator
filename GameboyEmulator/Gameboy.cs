using System;
using System.IO;
using System.Collections.Generic;

namespace GameboyEmulatorCore
{
    class Gameboy
    {
        //Memory Map 
        private byte[] cartridge;                   //Complete cartridge with all banks

        private byte[] cartridgeRom;                //0x0000-0x7FFF | First two rom banks
        private readonly byte[] videoRam;           //0x8000-0x9FFF
        private readonly byte[] cartridgeRam;       //0xA000-0xBFFF
        private readonly byte[] workRam;            //0xC000-0xDFFF
                                                    //0xE000-0xFDFF (same as 0xC000-0xDDFF)
        private readonly byte[] spriteAtributes;    //0xFE00-0xFE9F
                                                    //0xFEA0-0xFEFF (unused)
        private readonly byte[] ioPorts;            //0xFF00-0xFF7F
        private readonly byte[] highRam;            //0xFF80-0xFFFE
        private byte interuptEnableReg;             //0xFFFF

        //Registers
        private int programCounter;

        //File paths
        private const string BOOT_ROM_FILE_PATH = "../../../roms/boot.gb";
        private const string gameRomFilePath = "../../../roms/tetris.gb"; //TODO - Accept game file path as console parameter

        private const int MAX_CPU_CYCLES_PER_FRAME = 70224;

        public Gameboy()
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
            Array.Resize(ref cartridgeRom, 0x8000);

            cartridge = File.ReadAllBytes(gameRomFilePath);
        }

        public void updateCPU()
        {
            int cyclesThisFrame = 0;

            while (cyclesThisFrame < MAX_CPU_CYCLES_PER_FRAME)
            {
                int cycles = ExecuteOpcode();
                cyclesThisFrame += cycles;
            }
        }

        private int ExecuteOpcode()
        {
            throw new NotImplementedException(); //TODO - implement opcodes
        }
    }
}
