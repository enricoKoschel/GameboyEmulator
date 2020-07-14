using System;
using System.IO;

namespace GameboyEmulator
{
	class Memory
	{
		// ReSharper disable CommentTypo
		//Memory Map
		//0x0000-0x7FFF		First two rom banks
		//0x8000-0x9FFF		Video Ram
		//0xA000-0xBFFF		Cartridge Ram
		//0xC000-0xDFFF		Work Ram
		//0xE000-0xFDFF		Echo Ram (same as 0xC000-0xDDFF)
		//0xFE00-0xFE9F		Sprite Attributes
		//0xFEA0-0xFEFF		Unused
		//0xFF00-0xFF7F		IO Ports
		//0xFF80-0xFFFE		High Ram
		//0xFFFF			Interrupt Enable Register
		// ReSharper restore CommentTypo

		private          byte[] cartridgeRomWithBanks;
		private          byte[] cartridgeRom;
		private readonly byte[] videoRam;
		private readonly byte[] cartridgeRam;
		private readonly byte[] workRam;
		private readonly byte[] spriteAttributes;
		private readonly byte[] ioPorts;
		private readonly byte[] highRam;
		private          byte   interruptEnableReg;


		private const int CARTRIDGE_ROM_BASE_ADDRESS     = 0x0000;
		private const int VIDEO_RAM_BASE_ADDRESS         = 0x8000;
		private const int CARTRIDGE_RAM_BASE_ADDRESS     = 0xA000;
		private const int WORK_RAM_BASE_ADDRESS          = 0xC000;
		private const int ECHO_RAM_BASE_ADDRESS          = 0xE000;
		private const int SPRITE_ATTRIBUTES_BASE_ADDRESS = 0xFE00;
		private const int UNUSED_BASE_ADDRESS            = 0xFEA0;
		private const int IO_PORTS_BASE_ADDRESS          = 0xFF00;
		private const int HIGH_RAM_BASE_ADDRESS          = 0xFF80;
		private const int INTERRUPT_ENABLE_REG_ADDRESS   = 0xFFFF;

		//File paths
		private const string BOOT_ROM_FILE_PATH = "../../../roms/boot.gb";

		//TODO - Accept game file path as console parameter / make into property
		private const string GAME_ROM_FILE_PATH = "../../../roms/tetris.gb";

		public Memory()
		{
			videoRam         = new byte[0x2000];
			cartridgeRam     = new byte[0x2000];
			workRam          = new byte[0x2000];
			spriteAttributes = new byte[0x100];
			ioPorts          = new byte[0x80];
			highRam          = new byte[0x7F];
		}

		public void LoadGame()
		{
			cartridgeRom = File.ReadAllBytes(BOOT_ROM_FILE_PATH);

			//Resize for Cartridge Header
			Array.Resize(ref cartridgeRom, 0x150);

			cartridgeRomWithBanks = File.ReadAllBytes(GAME_ROM_FILE_PATH);

			//Copy Cartridge Header
			for (int i = 0x100; i < 0x150; i++)
			{
				cartridgeRom[i] = cartridgeRomWithBanks[i];
			}
		}

		private void DisableBootRom()
		{
			//Overwrite Boot rom with Game rom, thus disabling it
			cartridgeRom = cartridgeRomWithBanks;
		}

		public byte Read(ushort address)
		{
			if (IsBetween(address, UNUSED_BASE_ADDRESS, IO_PORTS_BASE_ADDRESS))
			{
				//Unused Memory
				throw new IndexOutOfRangeException($"Cannot access Memory at Address {address}!");
			}

			if (IsBetween(address, CARTRIDGE_ROM_BASE_ADDRESS, VIDEO_RAM_BASE_ADDRESS)) return cartridgeRom[address];

			if (IsBetween(address, VIDEO_RAM_BASE_ADDRESS, CARTRIDGE_RAM_BASE_ADDRESS))
			{
				//IO Ports
				return videoRam[address - VIDEO_RAM_BASE_ADDRESS];
			}

			if (IsBetween(address, IO_PORTS_BASE_ADDRESS, HIGH_RAM_BASE_ADDRESS))
			{
				//IO Ports
				return ioPorts[address - IO_PORTS_BASE_ADDRESS];
			}

			if (IsBetween(address, HIGH_RAM_BASE_ADDRESS, INTERRUPT_ENABLE_REG_ADDRESS))
			{
				//High Ram
				return highRam[address - HIGH_RAM_BASE_ADDRESS];
			}

			throw new NotImplementedException($"Read Memory location: 0x{address:X} not implemented yet!");
		}

		public void Write(ushort address, byte data, bool dontReset = false)
		{
			if (IsBetween(address, UNUSED_BASE_ADDRESS, IO_PORTS_BASE_ADDRESS))
			{
				//Unused Memory / Out of Bounds
				throw new IndexOutOfRangeException($"Cannot access Memory at Address {address}!");
			}

			if (IsBetween(address, CARTRIDGE_ROM_BASE_ADDRESS, VIDEO_RAM_BASE_ADDRESS))
			{
				//Writing to Cartridge ROM triggers ROMBanking Controller
				throw new NotImplementedException("Rom-Banking not implemented yet!"); //TODO - handle Rom-Banking
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
					ioPorts[address - IO_PORTS_BASE_ADDRESS] = data;
				else
				{
					ioPorts[address - IO_PORTS_BASE_ADDRESS] = address switch
					{
						//Current Scanline register - Reset when written to
						0xFF44 => 0,

						//Default
						_ => data
					};
				}
			}
			else if (IsBetween(address, HIGH_RAM_BASE_ADDRESS, INTERRUPT_ENABLE_REG_ADDRESS))
			{
				//High Ram
				highRam[address - HIGH_RAM_BASE_ADDRESS] = data;
			}
			else
			{
				throw new NotImplementedException(
					$"Write Memory location: 0x{address:X} not implemented yet!"
				); //TODO - implement write memory
			}
		}

		//Utility functions
		/// <summary>
		/// lowerBound is inclusive, upperBound is exclusive
		/// </summary>
		private static bool IsBetween(int number, int lowerBound, int upperBound)
		{
			return (number >= lowerBound && number < upperBound);
		}
	}
}