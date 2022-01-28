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
		private          byte[] cartridgeRam;
		private readonly byte[] workRam;
		private readonly byte[] spriteAttributes;
		private readonly byte[] ioPorts;
		private readonly byte[] highRam;
		private          byte   interruptEnableRegister;

		private const int CARTRIDGE_ROM_BASE_ADDRESS = 0x0000;
		private const int CARTRIDGE_ROM_LAST_ADDRESS = 0x7FFF;

		private const int VIDEO_RAM_BASE_ADDRESS = 0x8000;
		private const int VIDEO_RAM_LAST_ADDRESS = 0x9FFF;

		private const int CARTRIDGE_RAM_BASE_ADDRESS = 0xA000;
		private const int CARTRIDGE_RAM_LAST_ADDRESS = 0xBFFF;

		private const int WORK_RAM_BASE_ADDRESS = 0xC000;
		private const int WORK_RAM_LAST_ADDRESS = 0xDFFF;

		private const int ECHO_RAM_BASE_ADDRESS = 0xE000;
		private const int ECHO_RAM_LAST_ADDRESS = 0xFDFF;

		private const int SPRITE_ATTRIBUTES_BASE_ADDRESS = 0xFE00;
		private const int SPRITE_ATTRIBUTES_LAST_ADDRESS = 0xFE9F;

		private const int UNUSED_BASE_ADDRESS = 0xFEA0;
		private const int UNUSED_LAST_ADDRESS = 0xFEFF;

		private const int IO_PORTS_BASE_ADDRESS = 0xFF00;
		private const int IO_PORTS_LAST_ADDRESS = 0xFF7F;

		private const int HIGH_RAM_BASE_ADDRESS = 0xFF80;
		private const int HIGH_RAM_LAST_ADDRESS = 0xFFFE;

		private const int INTERRUPT_ENABLE_REG_ADDRESS = 0xFFFF;

		private const ushort DMA_FINISH_ADDRESS = 0xFEA0;

		//Modules
		private readonly Cpu                  cpu;
		private readonly MemoryBankController mbc;

		//File paths
		private const string BOOT_ROM_FILE_PATH = "../../../roms/boot.gb";
		private       bool   bootRomEnabled     = false;

		//TODO - Accept game file path as console parameter / make into property
		private const string GAME_ROM_FILE_PATH = "../../../roms/mario.gb";
		//private const string GAME_ROM_FILE_PATH = "../../../roms/test/ppu/dmg-acid2.gb";
		//private const string GAME_ROM_FILE_PATH = "../../../roms/test/tearoom/m2_win_en_toggle.gb";
		//private const string GAME_ROM_FILE_PATH = "../../../roms/test/ppu/lycscx.gb";
		//private const string GAME_ROM_FILE_PATH = "../../../roms/demos/pocket.gb";

		public Memory(Cpu cpu)
		{
			this.cpu = cpu;
			mbc      = new MemoryBankController(this);

			videoRam         = new byte[0x2000];
			workRam          = new byte[0x2000];
			spriteAttributes = new byte[0x100];
			ioPorts          = new byte[0x80];
			highRam          = new byte[0x7F];
		}

		private void AllocateCartridgeRam(byte numberOfRamBanks)
		{
			if (numberOfRamBanks > 0)
				cartridgeRam = new byte[numberOfRamBanks * 0x2000];
		}

		public void LoadGame()
		{
			//TODO refactor this method
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

			//Detect current Memorybanking Mode
			mbc.DetectBankingMode();

			AllocateCartridgeRam(mbc.GetNumberOfRamBanks());
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

		public byte Read(ushort address, bool noRomBanking = false)
		{
			if (IsInRange(address, CARTRIDGE_ROM_BASE_ADDRESS, CARTRIDGE_ROM_LAST_ADDRESS))
				return noRomBanking ? cartridgeRom[address] : cartridgeRom[mbc.ConvertAddressInRomBank(address)];

			if (IsInRange(address, VIDEO_RAM_BASE_ADDRESS, VIDEO_RAM_LAST_ADDRESS))
				return videoRam[address - VIDEO_RAM_BASE_ADDRESS];

			if (IsInRange(address, CARTRIDGE_RAM_BASE_ADDRESS, CARTRIDGE_RAM_LAST_ADDRESS))
			{
				return cartridgeRam != null && mbc.GetIsRamEnabled()
						   ? cartridgeRam[mbc.ConvertAddressInRamBank(address) - CARTRIDGE_RAM_BASE_ADDRESS]
						   : (byte)0x00;
			}

			if (IsInRange(address, WORK_RAM_BASE_ADDRESS, WORK_RAM_LAST_ADDRESS))
				return workRam[address - WORK_RAM_BASE_ADDRESS];

			//Echo ram is identical to work ram
			if (IsInRange(address, ECHO_RAM_BASE_ADDRESS, ECHO_RAM_LAST_ADDRESS))
				return workRam[address - ECHO_RAM_BASE_ADDRESS];

			if (IsInRange(address, SPRITE_ATTRIBUTES_BASE_ADDRESS, SPRITE_ATTRIBUTES_LAST_ADDRESS))
				return spriteAttributes[address - SPRITE_ATTRIBUTES_BASE_ADDRESS];

			//Unused Memory / Out of Bounds
			if (IsInRange(address, UNUSED_BASE_ADDRESS, UNUSED_LAST_ADDRESS))
				return 0xFF;

			if (IsInRange(address, IO_PORTS_BASE_ADDRESS, IO_PORTS_LAST_ADDRESS))
				return ioPorts[address - IO_PORTS_BASE_ADDRESS];

			if (IsInRange(address, HIGH_RAM_BASE_ADDRESS, HIGH_RAM_LAST_ADDRESS))
				return highRam[address - HIGH_RAM_BASE_ADDRESS];

			if (address == INTERRUPT_ENABLE_REG_ADDRESS)
				return interruptEnableRegister;

			throw new ArgumentOutOfRangeException(nameof(address), address, "Address out of range!");
		}

		public void Write(ushort address, byte data, bool dontReset = false)
		{
			//Writing to Cartridge Rom triggers Memorybanking Controller
			if (IsInRange(address, CARTRIDGE_ROM_BASE_ADDRESS, CARTRIDGE_ROM_LAST_ADDRESS))
				mbc.HandleBanking(address, data);

			else if (IsInRange(address, VIDEO_RAM_BASE_ADDRESS, VIDEO_RAM_LAST_ADDRESS))
				videoRam[address - VIDEO_RAM_BASE_ADDRESS] = data;

			else if (IsInRange(address, CARTRIDGE_RAM_BASE_ADDRESS, CARTRIDGE_RAM_LAST_ADDRESS))
			{
				if (cartridgeRam != null && mbc.GetIsRamEnabled())
					cartridgeRam[mbc.ConvertAddressInRamBank(address) - CARTRIDGE_RAM_BASE_ADDRESS] = data;
			}

			else if (IsInRange(address, WORK_RAM_BASE_ADDRESS, WORK_RAM_LAST_ADDRESS))
				workRam[address - WORK_RAM_BASE_ADDRESS] = data;

			//Echo ram is identical to work ram
			else if (IsInRange(address, ECHO_RAM_BASE_ADDRESS, ECHO_RAM_LAST_ADDRESS))
				workRam[address - ECHO_RAM_BASE_ADDRESS] = data;

			else if (IsInRange(address, SPRITE_ATTRIBUTES_BASE_ADDRESS, SPRITE_ATTRIBUTES_LAST_ADDRESS))
				spriteAttributes[address - SPRITE_ATTRIBUTES_BASE_ADDRESS] = data;

			//Unused Memory / Out of Bounds
			else if (IsInRange(address, UNUSED_BASE_ADDRESS, UNUSED_LAST_ADDRESS))
			{
				/*Empty*/
			}

			else if (IsInRange(address, IO_PORTS_BASE_ADDRESS, IO_PORTS_LAST_ADDRESS))
			{
				//Special Addresses
				switch (address)
				{
					case 0xFF46:
						//DMA - Direct Memory Access
						DirectMemoryAccess(data);
						break;
					case 0xFF50:
						//Boot Rom disable Register
						DisableBootRom();
						break;
				}

				//IO Ports
				//During normal Gameboy operation, some registers get reset when written to
				//The dontReset flag is for internal use, when these registers have to be written to
				if (dontReset)
					ioPorts[address - IO_PORTS_BASE_ADDRESS] = data;
				else
				{
					ioPorts[address - IO_PORTS_BASE_ADDRESS] = address switch
					{
						//Reset when written to
						0xFF04 => 0,
						0xFF44 => 0,

						//Default
						_ => data
					};
				}
			}

			else if (IsInRange(address, HIGH_RAM_BASE_ADDRESS, HIGH_RAM_LAST_ADDRESS))
				highRam[address - HIGH_RAM_BASE_ADDRESS] = data;

			else if (address == INTERRUPT_ENABLE_REG_ADDRESS)
				interruptEnableRegister = data;

			else
				throw new ArgumentOutOfRangeException(nameof(address), address, "Address out of range!");
		}

		private void DirectMemoryAccess(byte sourceAddressLo)
		{
			if (sourceAddressLo > 0xF1) return;

			ushort sourceAddress      = (ushort)(sourceAddressLo * 0x100);
			ushort destinationAddress = 0xFE00;

			while (destinationAddress < DMA_FINISH_ADDRESS)
				Write(destinationAddress++, Read(sourceAddress++));
		}

		public static bool IsInRange(int number, int lowerBoundInclusive, int upperBoundInclusive)
		{
			return number >= lowerBoundInclusive && number <= upperBoundInclusive;
		}
	}
}