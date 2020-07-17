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

		//Modules
		private readonly Cpu        cpu;
		private readonly Interrupts interrupts;

		//File paths
		private const string BOOT_ROM_FILE_PATH = "../../../roms/boot.gb";
		private       bool   bootRomEnabled     = true;

		//TODO - Accept game file path as console parameter / make into property
		private const string GAME_ROM_FILE_PATH = "../../../roms/test/cpu_instrs/individual/01-special.gb";
		//private const string GAME_ROM_FILE_PATH = "../../../roms/test/cpu_instrs/cpu_instrs.gb";

		public Memory(Cpu cpu, Interrupts interrupts)
		{
			this.cpu        = cpu;
			this.interrupts = interrupts;

			videoRam         = new byte[0x2000];
			cartridgeRam     = new byte[0x2000];
			workRam          = new byte[0x2000];
			spriteAttributes = new byte[0x100];
			ioPorts          = new byte[0x80];
			highRam          = new byte[0x7F];
		}

		public void LoadGame()
		{
			if (bootRomEnabled)
			{
				cartridgeRom = File.ReadAllBytes(BOOT_ROM_FILE_PATH);

				//Resize for Cartridge Header
				Array.Resize(ref cartridgeRom, 0x150);

				cartridgeRomWithBanks = File.ReadAllBytes(GAME_ROM_FILE_PATH);

				//Copy Cartridge Header
				for (int i = 0x100; i < 0x150; i++) cartridgeRom[i] = cartridgeRomWithBanks[i];
			}
			else
			{
				InitializeRegisters();

				cartridgeRom = File.ReadAllBytes(GAME_ROM_FILE_PATH);
			}
		}

		private void InitializeRegisters()
		{
			cpu.InitializeRegisters();
			InitializeMemory();
		}

		private void InitializeMemory()
		{
			Write(0xFF05, 0x00, true);
			Write(0xFF06, 0x00, true);
			Write(0xFF07, 0x00, true);
			Write(0xFF10, 0x80, true);
			Write(0xFF11, 0xBF, true);
			Write(0xFF12, 0xF3, true);
			Write(0xFF14, 0xBF, true);
			Write(0xFF16, 0x3F, true);
			Write(0xFF17, 0x00, true);
			Write(0xFF19, 0xBF, true);
			Write(0xFF1A, 0x7F, true);
			Write(0xFF1B, 0xFF, true);
			Write(0xFF1C, 0x9F, true);
			Write(0xFF1E, 0xBF, true);
			Write(0xFF20, 0xFF, true);
			Write(0xFF21, 0x00, true);
			Write(0xFF22, 0x00, true);
			Write(0xFF23, 0xBF, true);
			Write(0xFF24, 0x77, true);
			Write(0xFF25, 0xF3, true);
			Write(0xFF26, 0xF1, true);
			Write(0xFF40, 0x91, true);
			Write(0xFF42, 0x00, true);
			Write(0xFF43, 0x00, true);
			Write(0xFF45, 0x00, true);
			Write(0xFF47, 0xFC, true);
			Write(0xFF48, 0xFF, true);
			Write(0xFF49, 0xFF, true);
			Write(0xFF4A, 0x00, true);
			Write(0xFF4B, 0x00, true);
			Write(0xFFFF, 0x00, true);
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
				throw new IndexOutOfRangeException($"Cannot access Memory at Address 0x{address:X}!");
			}

			if (IsBetween(address, CARTRIDGE_ROM_BASE_ADDRESS, VIDEO_RAM_BASE_ADDRESS))
			{
				//Cartridge Rom
				return cartridgeRom[address];

				//TODO - Implement Banking
			}

			if (IsBetween(address, WORK_RAM_BASE_ADDRESS, ECHO_RAM_BASE_ADDRESS))
			{
				//Work Ram
				return workRam[address - WORK_RAM_BASE_ADDRESS];
			}

			if (IsBetween(address, ECHO_RAM_BASE_ADDRESS, SPRITE_ATTRIBUTES_BASE_ADDRESS))
			{
				//Echo Ram - Same as Work Ram
				return workRam[address - ECHO_RAM_BASE_ADDRESS];
			}

			if (IsBetween(address, VIDEO_RAM_BASE_ADDRESS, CARTRIDGE_RAM_BASE_ADDRESS))
			{
				//Video Ram
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
				throw new IndexOutOfRangeException($"Cannot access Memory at Address 0x{address:X}!");
			}

			if (IsBetween(address, CARTRIDGE_ROM_BASE_ADDRESS, VIDEO_RAM_BASE_ADDRESS))
			{
				//Writing to Cartridge ROM triggers ROMBanking Controller
				throw new NotImplementedException("Rom-Banking not implemented yet!"); //TODO - handle Rom-Banking
			}
			else if (IsBetween(address, VIDEO_RAM_BASE_ADDRESS, CARTRIDGE_RAM_BASE_ADDRESS))
			{
				//Video Ram
				videoRam[address - VIDEO_RAM_BASE_ADDRESS] = data;
			}
			else if (IsBetween(address, WORK_RAM_BASE_ADDRESS, ECHO_RAM_BASE_ADDRESS))
			{
				//Work Ram
				workRam[address - WORK_RAM_BASE_ADDRESS] = data;
			}
			else if (IsBetween(address, ECHO_RAM_BASE_ADDRESS, SPRITE_ATTRIBUTES_BASE_ADDRESS))
			{
				//Echo Ram - Same as Work RAM
				workRam[address - ECHO_RAM_BASE_ADDRESS] = data;
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
			else if (address == INTERRUPT_ENABLE_REG_ADDRESS)
			{
				interrupts.interruptEnableRegister = data;
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