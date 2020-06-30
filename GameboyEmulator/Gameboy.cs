using System;
using System.IO;

namespace GameboyEmulatorCore
{
    class Gameboy
    {
        //Memory Map
        byte[] cartridgeRom;    //0x0000-0x7FFF
        byte[] videoRam;        //0x8000-0x9FFF
        byte[] cartridgeRam;    //0xA000-0xBFFF
        byte[] workRam;         //0xC000-0xDFFF
                                //0xE000-0xFDFF (same as 0xC000-0xDDFF)
        byte[] spriteAtributes; //0xFE00-0xFE9F
                                //0xFEA0-0xFEFF (unused)
        byte[] ioPorts;         //0xFF00-0xFF7F
        byte[] highRam;         //0xFF80-0xFFFE
        byte interuptEnableReg; //0xFFFF

        string bootRomFilePath = "";

        public Gameboy()
        {
            cartridgeRom = new byte[0x8000];
            videoRam = new byte[0x2000];
            cartridgeRam = new byte[0x2000];
            workRam = new byte[0x2000];
            spriteAtributes = new byte[0x100];
            ioPorts = new byte[0x80];
            highRam = new byte[0x7F];
        }

        public void LoadGame()
        {
            File.ReadAllBytes("");
        }
    }
}
