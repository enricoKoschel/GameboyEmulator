﻿using System;
using System.IO;

namespace GameboyEmulator
{
	public class Memory
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

		private          byte[] bootRom;
		private          byte[] cartridgeRom;
		private readonly byte[] videoRam;
		private          byte[] cartridgeRam;
		private readonly byte[] workRam;
		private readonly byte[] spriteAttributes;
		private          byte   dmaRegister;
		private readonly byte[] highRam;

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

		//File paths
		private const string BOOT_ROM_FILE_PATH = "../../../roms/boot.gb";
		private       bool   useBootRom         = false;
		private       bool   bootRomEnabled;

		public const string GAME_ROM_FILE_PATH = "../../../roms/zelda.gb";
		//private const string GAME_ROM_FILE_PATH = "../../../roms/test/blargg/instr_timing/instr_timing.gb";

		private readonly Emulator emulator;

		public Memory(Emulator emulator)
		{
			this.emulator = emulator;

			videoRam         = new byte[0x2000];
			workRam          = new byte[0x2000];
			spriteAttributes = new byte[0x100];
			dmaRegister      = 0xFF;
			highRam          = new byte[0x7F];
		}

		private void AllocateCartridgeRam(byte numberOfRamBanks)
		{
			if (numberOfRamBanks > 0) cartridgeRam = new byte[numberOfRamBanks * 0x2000];
		}

		public void LoadGame()
		{
			if (useBootRom)
			{
				bootRomEnabled = true;

				try
				{
					bootRom = File.ReadAllBytes(BOOT_ROM_FILE_PATH);
				}
				catch (Exception e)
				{
					Logger.LogMessage($"Boot rom '{BOOT_ROM_FILE_PATH}' could not be opened!", Logger.LogLevel.Error);
					throw new Exception("", e);
				}

				if (bootRom.Length != 0x100)
				{
					Logger.LogMessage("Invalid boot rom selected!", Logger.LogLevel.Error);
					throw new InvalidDataException("Invalid boot rom selected!");
				}
			}
			else
			{
				bootRomEnabled = false;
				InitializeRegisters();
			}

			try
			{
				cartridgeRom = File.ReadAllBytes(GAME_ROM_FILE_PATH);
			}
			catch (Exception e)
			{
				Logger.LogMessage($"Game rom '{GAME_ROM_FILE_PATH}' could not be opened!", Logger.LogLevel.Error);
				throw new Exception("", e);
			}

			//Detect current Memorybanking Mode
			emulator.memoryBankController.InitializeBanking();

			AllocateCartridgeRam(emulator.memoryBankController.NumberOfRamBanks);

			Logger.LogMessage("Game was loaded.", Logger.LogLevel.Info);
		}

		private void InitializeRegisters()
		{
			emulator.cpu.InitializeRegisters();
			InitializeMemory();
		}

		private void InitializeMemory()
		{
			Write(0xFF05, 0x00);
			Write(0xFF06, 0x00);
			Write(0xFF07, 0x00);
			Write(0xFF10, 0x80);
			Write(0xFF11, 0xBF);
			Write(0xFF12, 0xF3);
			Write(0xFF14, 0xBF);
			Write(0xFF16, 0x3F);
			Write(0xFF17, 0x00);
			Write(0xFF19, 0xBF);
			Write(0xFF1A, 0x7F);
			Write(0xFF1B, 0xFF);
			Write(0xFF1C, 0x9F);
			Write(0xFF1E, 0xBF);
			Write(0xFF20, 0xFF);
			Write(0xFF21, 0x00);
			Write(0xFF22, 0x00);
			Write(0xFF23, 0xBF);
			Write(0xFF24, 0x77);
			Write(0xFF25, 0xF3);
			Write(0xFF26, 0xF1);
			Write(0xFF40, 0x91);
			Write(0xFF42, 0x00);
			Write(0xFF43, 0x00);
			Write(0xFF45, 0x00);
			Write(0xFF47, 0xFC);
			Write(0xFF48, 0xFF);
			Write(0xFF49, 0xFF);
			Write(0xFF4A, 0x00);
			Write(0xFF4B, 0x00);
			Write(0xFFFF, 0x00);
		}

		private void DisableBootRom()
		{
			bootRomEnabled = false;

			Logger.LogMessage("Boot rom was disabled.", Logger.LogLevel.Info);
		}

		public byte Read(ushort address, bool noRomBanking = false)
		{
			if (IsInRange(address, CARTRIDGE_ROM_BASE_ADDRESS, CARTRIDGE_ROM_LAST_ADDRESS))
			{
				if (bootRomEnabled && address < 0x100) return bootRom[address];

				return noRomBanking
						   ? cartridgeRom[address]
						   : cartridgeRom[emulator.memoryBankController.ConvertAddressInRomBank(address)];
			}

			if (IsInRange(address, VIDEO_RAM_BASE_ADDRESS, VIDEO_RAM_LAST_ADDRESS))
				return videoRam[address - VIDEO_RAM_BASE_ADDRESS];

			if (IsInRange(address, CARTRIDGE_RAM_BASE_ADDRESS, CARTRIDGE_RAM_LAST_ADDRESS))
			{
				if (cartridgeRam != null && emulator.memoryBankController.IsRamEnabled)
				{
					return cartridgeRam[
						emulator.memoryBankController.ConvertAddressInRamBank(
							(ushort)(address - CARTRIDGE_RAM_BASE_ADDRESS)
						)];
				}

				return 0xFF;
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
			{
				switch (address & 0xFF)
				{
					case 0x00:
						return emulator.joypad.JoypadRegister;
					case 0x01:
					case 0x02:
						//TODO Serial transfer
						return 0xFF;
					case 0x04:
						return emulator.timer.DividerRegister;
					case 0x05:
						return emulator.timer.TimerRegister;
					case 0x06:
						return emulator.timer.TimerModulo;
					case 0x07:
						return emulator.timer.TimerControl;
					case 0x0F:
						return emulator.interrupts.InterruptFlagRegister;
					case 0x10:
					case 0x11:
					case 0x12:
					case 0x13:
					case 0x14:
					case 0x16:
					case 0x17:
					case 0x18:
					case 0x19:
					case 0x1A:
					case 0x1B:
					case 0x1C:
					case 0x1D:
					case 0x1E:
					case 0x20:
					case 0x21:
					case 0x22:
					case 0x23:
					case 0x24:
					case 0x25:
					case 0x26:
					case 0x30:
					case 0x31:
					case 0x32:
					case 0x33:
					case 0x34:
					case 0x35:
					case 0x36:
					case 0x37:
					case 0x38:
					case 0x39:
					case 0x3A:
					case 0x3B:
					case 0x3C:
					case 0x3D:
					case 0x3E:
					case 0x3F:
						//TODO implement audio
						return 0xFF;
					case 0x40:
						return emulator.ppu.LcdControlRegister;
					case 0x41:
						return emulator.ppu.LcdStatusRegister;
					case 0x42:
						return emulator.ppu.ScrollY;
					case 0x43:
						return emulator.ppu.ScrollX;
					case 0x44:
						return emulator.ppu.CurrentScanline;
					case 0x45:
						return emulator.ppu.CurrentScanlineCompare;
					case 0x46:
						return dmaRegister;
					case 0x47:
						return emulator.ppu.TilePalette;
					case 0x48:
						return emulator.ppu.SpritePalette0;
					case 0x49:
						return emulator.ppu.SpritePalette1;
					case 0x4A:
						return emulator.ppu.WindowY;
					case 0x4B:
						return emulator.ppu.WindowX;
					case 0x4D:
						//0xFF4D is used to detect a GameBoy Color, a regular GameBoy always returns 0xFF
						return 0xFF;
					default:
						//Unused IO ports return 0xFF
						return 0xFF;
				}
			}

			if (IsInRange(address, HIGH_RAM_BASE_ADDRESS, HIGH_RAM_LAST_ADDRESS))
				return highRam[address - HIGH_RAM_BASE_ADDRESS];

			if (address == INTERRUPT_ENABLE_REG_ADDRESS)
				return emulator.interrupts.InterruptEnableRegister;

			throw new ArgumentOutOfRangeException(nameof(address), address, "Address out of range!");
		}

		public void Write(ushort address, byte data)
		{
			//Writing to Cartridge Rom triggers Memorybanking Controller
			if (IsInRange(address, CARTRIDGE_ROM_BASE_ADDRESS, CARTRIDGE_ROM_LAST_ADDRESS))
				emulator.memoryBankController.HandleBanking(address, data);

			else if (IsInRange(address, VIDEO_RAM_BASE_ADDRESS, VIDEO_RAM_LAST_ADDRESS))
				videoRam[address - VIDEO_RAM_BASE_ADDRESS] = data;

			else if (IsInRange(address, CARTRIDGE_RAM_BASE_ADDRESS, CARTRIDGE_RAM_LAST_ADDRESS))
			{
				if (cartridgeRam != null && emulator.memoryBankController.IsRamEnabled)
				{
					cartridgeRam[
						emulator.memoryBankController.ConvertAddressInRamBank(
							(ushort)(address - CARTRIDGE_RAM_BASE_ADDRESS)
						)] = data;
				}
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
				switch (address & 0xFF)
				{
					case 0x00:
						emulator.joypad.JoypadRegister = data;
						break;
					case 0x01:
					case 0x02:
						//TODO Serial transfer
						break;
					case 0x04:
						//Writing to the divider register resets it
						emulator.timer.ResetDividerRegister();
						break;
					case 0x05:
						emulator.timer.TimerRegister = data;
						break;
					case 0x06:
						emulator.timer.TimerModulo = data;
						break;
					case 0x07:
						emulator.timer.TimerControl = data;
						break;
					case 0x0F:
						emulator.interrupts.InterruptFlagRegister = data;
						break;
					case 0x10:
					case 0x11:
					case 0x12:
					case 0x13:
					case 0x14:
					case 0x16:
					case 0x17:
					case 0x18:
					case 0x19:
					case 0x1A:
					case 0x1B:
					case 0x1C:
					case 0x1D:
					case 0x1E:
					case 0x20:
					case 0x21:
					case 0x22:
					case 0x23:
					case 0x24:
					case 0x25:
					case 0x26:
					case 0x30:
					case 0x31:
					case 0x32:
					case 0x33:
					case 0x34:
					case 0x35:
					case 0x36:
					case 0x37:
					case 0x38:
					case 0x39:
					case 0x3A:
					case 0x3B:
					case 0x3C:
					case 0x3D:
					case 0x3E:
					case 0x3F:
						//TODO implement audio
						break;
					case 0x40:
						emulator.ppu.LcdControlRegister = data;
						break;
					case 0x41:
						emulator.ppu.LcdStatusRegister = data;
						break;
					case 0x42:
						emulator.ppu.ScrollY = data;
						break;
					case 0x43:
						emulator.ppu.ScrollX = data;
						break;
					case 0x45:
						emulator.ppu.CurrentScanlineCompare = data;
						break;
					case 0x46:
						dmaRegister = data;
						DirectMemoryAccess(data);
						break;
					case 0x47:
						emulator.ppu.TilePalette = data;
						break;
					case 0x48:
						emulator.ppu.SpritePalette0 = data;
						break;
					case 0x49:
						emulator.ppu.SpritePalette1 = data;
						break;
					case 0x4A:
						emulator.ppu.WindowY = data;
						break;
					case 0x4B:
						emulator.ppu.WindowX = data;
						break;
					case 0x50:
						//Writing to this address disables the boot rom
						DisableBootRom();
						break;
				}
			}

			else if (IsInRange(address, HIGH_RAM_BASE_ADDRESS, HIGH_RAM_LAST_ADDRESS))
				highRam[address - HIGH_RAM_BASE_ADDRESS] = data;

			else if (address == INTERRUPT_ENABLE_REG_ADDRESS)
				emulator.interrupts.InterruptEnableRegister = data;

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