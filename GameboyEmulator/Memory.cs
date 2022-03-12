﻿using System;
using System.IO;

namespace GameboyEmulator;

public class Memory
{
	//ReSharper disable CommentTypo
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
	//ReSharper restore CommentTypo

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

	private bool bootRomEnabled;

	private DateTime lastTimeRamWasSaved     = DateTime.Now;
	private bool     ramChangedSinceLastSave = true;

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
		if (numberOfRamBanks == 0) return;

		//If a save file exists, load it into cartridge ram and resize ram to the actual size
		LoadCartridgeRam();
		Array.Resize(ref cartridgeRam, numberOfRamBanks * 0x2000);
	}

	public void LoadGame()
	{
		if (!String.IsNullOrWhiteSpace(emulator.bootRomFilePath))
		{
			bootRomEnabled = true;

			try
			{
				bootRom = File.ReadAllBytes(emulator.bootRomFilePath);
			}
			catch
			{
				Logger.LogMessage(
					$"Boot rom '{emulator.bootRomFilePath}' could not be opened!", Logger.LogLevel.Error, true
				);

				Environment.Exit(1);
			}

			if (bootRom.Length != 0x100)
			{
				Logger.LogMessage("Invalid boot rom selected!", Logger.LogLevel.Error, true);

				Environment.Exit(1);
			}
		}
		else
		{
			bootRomEnabled = false;
			InitialiseRegisters();
		}

		try
		{
			cartridgeRom = File.ReadAllBytes(emulator.gameRomFilePath);
		}
		catch
		{
			Logger.LogMessage(
				$"Game rom '{emulator.gameRomFilePath}' could not be opened!", Logger.LogLevel.Error, true
			);

			Environment.Exit(1);
		}

		//A cartridge has to be at least 0x150 bytes large to contain a full cartridge header
		if (cartridgeRom.Length < 0x150)
		{
			Logger.LogMessage("Invalid game rom selected!", Logger.LogLevel.Error, true);

			Environment.Exit(1);
		}

		//Pad the cartridge size to at least 0x8000 bytes (32KB)
		if (cartridgeRom.Length < 0x8000) Array.Resize(ref cartridgeRom, 0x8000);

		//Pad the cartridge size to the next highest power of 2
		double log = Math.Log2(cartridgeRom.Length);
		if (!(Math.Abs(log - (int)log) < Double.Epsilon))
			Array.Resize(ref cartridgeRom, (int)Math.Pow(2, Math.Ceiling(log)));


		//Detect current Memorybanking Mode
		emulator.memoryBankController.InitialiseBanking();

		AllocateCartridgeRam(emulator.memoryBankController.NumberOfRamBanks);

		Logger.LogMessage("Game was loaded.", Logger.LogLevel.Info);
	}

	public void SaveCartridgeRam()
	{
		if (!ramChangedSinceLastSave || !emulator.memoryBankController.CartridgeRamExists ||
			!Config.GetSaveEnabledConfig()) return;

		if (DateTime.Now < lastTimeRamWasSaved.AddSeconds(1)) return;

		lastTimeRamWasSaved     = DateTime.Now;
		ramChangedSinceLastSave = false;

		File.WriteAllBytes(emulator.saveFilePath, cartridgeRam);
	}

	private void LoadCartridgeRam()
	{
		if (Config.GetSaveEnabledConfig() && File.Exists(emulator.saveFilePath))
			cartridgeRam = File.ReadAllBytes(emulator.saveFilePath);
	}

	private void InitialiseRegisters()
	{
		emulator.cpu.InitialiseRegisters();
		InitialiseMemory();
	}

	private void InitialiseMemory()
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
			if (emulator.memoryBankController.CartridgeRamExists && emulator.memoryBankController.IsRamEnabled)
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
			if (IsInRange(address, 0xFF30, 0xFF3F))
				return emulator.apu.GetWavePatternRamAtIndex(address & 0xF);

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
					return emulator.apu.Channel1SweepRegister;
				case 0x11:
					return emulator.apu.Channel1SoundLengthWavePatternRegister;
				case 0x12:
					return emulator.apu.Channel1VolumeEnvelopeRegister;
				case 0x13:
					//Write only
					return 0xFF;
				case 0x14:
					return emulator.apu.Channel1FrequencyRegisterHi;
				case 0x16:
					return emulator.apu.Channel2SoundLengthWavePatternRegister;
				case 0x17:
					return emulator.apu.Channel2VolumeEnvelopeRegister;
				case 0x18:
					//Write only
					return 0xFF;
				case 0x19:
					return emulator.apu.Channel2FrequencyRegisterHi;
				case 0x1A:
					return emulator.apu.Channel3SoundOnOffRegister;
				case 0x1B:
					//Write only
					return 0xFF;
				case 0x1C:
					return emulator.apu.Channel3SelectOutputLevelRegister;
				case 0x1D:
					//Write only
					return 0xFF;
				case 0x1E:
					return emulator.apu.Channel3FrequencyRegisterHi;
				case 0x20:
					//Write only
					return 0xFF;
				case 0x21:
					return emulator.apu.Channel4VolumeEnvelopeRegister;
				case 0x22:
					return emulator.apu.Channel4PolynomialCounterRegister;
				case 0x23:
					return emulator.apu.Channel4CounterConsecutiveRegister;
				case 0x24:
					return emulator.apu.ChannelControlRegister;
				case 0x25:
					return emulator.apu.SoundOutputTerminalSelectRegister;
				case 0x26:
					return emulator.apu.SoundOnOffRegister;
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
				case 0x50:
					//Reading from 0xFF50 returns 1 for bits 7-1 and the boot rom state for bit 0
					return (byte)(0b11111110 | (bootRomEnabled ? 0 : 1));
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
			if (!emulator.memoryBankController.CartridgeRamExists ||
				!emulator.memoryBankController.IsRamEnabled) return;

			cartridgeRam[
				emulator.memoryBankController.ConvertAddressInRamBank(
					(ushort)(address - CARTRIDGE_RAM_BASE_ADDRESS)
				)] = data;

			ramChangedSinceLastSave = true;
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
			if (IsInRange(address, 0xFF30, 0xFF3F))
				emulator.apu.SetWavePatternRamAtIndex(address & 0xF, data);

			switch (address & 0xFF)
			{
				case 0x00:
					//Only bits 4 and 5 of the joypad register are writeable
					emulator.joypad.JoypadRegister =
						(byte)((emulator.joypad.JoypadRegister & 0b11001111) | (data & 0b00110000));

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
					emulator.apu.Channel1SweepRegister = data;
					break;
				case 0x11:
					emulator.apu.Channel1SoundLengthWavePatternRegister = data;
					break;
				case 0x12:
					emulator.apu.Channel1VolumeEnvelopeRegister = data;
					break;
				case 0x13:
					emulator.apu.Channel1FrequencyRegisterLo = data;
					break;
				case 0x14:
					emulator.apu.Channel1FrequencyRegisterHi = data;
					break;
				case 0x16:
					emulator.apu.Channel2SoundLengthWavePatternRegister = data;
					break;
				case 0x17:
					emulator.apu.Channel2VolumeEnvelopeRegister = data;
					break;
				case 0x18:
					emulator.apu.Channel2FrequencyRegisterLo = data;
					break;
				case 0x19:
					emulator.apu.Channel2FrequencyRegisterHi = data;
					break;
				case 0x1A:
					emulator.apu.Channel3SoundOnOffRegister = data;
					break;
				case 0x1B:
					emulator.apu.Channel3SoundLengthRegister = data;
					break;
				case 0x1C:
					emulator.apu.Channel3SelectOutputLevelRegister = data;
					break;
				case 0x1D:
					emulator.apu.Channel3FrequencyRegisterLo = data;
					break;
				case 0x1E:
					emulator.apu.Channel3FrequencyRegisterHi = data;
					break;
				case 0x20:
					emulator.apu.Channel4SoundLengthRegister = data;
					break;
				case 0x21:
					emulator.apu.Channel4VolumeEnvelopeRegister = data;
					break;
				case 0x22:
					emulator.apu.Channel4PolynomialCounterRegister = data;
					break;
				case 0x23:
					emulator.apu.Channel4CounterConsecutiveRegister = data;
					break;
				case 0x24:
					emulator.apu.ChannelControlRegister = data;
					break;
				case 0x25:
					emulator.apu.SoundOutputTerminalSelectRegister = data;
					break;
				case 0x26:
					emulator.apu.SoundOnOffRegister = data;
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
					if (bootRomEnabled && (data & 0b00000001) == 1) DisableBootRom();
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
		//TODO Maybe all addresses are allowed and some are treated as echo ram?
		/*"what happens when the source address for OAM DMA is FE00?
		passing all the other mooneye oam tests including timing, just failing sources-GS"
		"it's treated as echo ram of DE00
		C000-FFFF oam dma source is always treated as wram"
		"oh so it's essentially just like an extended echo region?"
		"in most cases echo ram is only E000-FDFF. oam dma is one of the exceptions here which have the entire E000-FFFF region as echo ram for dma source"
		"i see, thanks!
		just got that test passing now"*/
		if (sourceAddressLo > 0xF1) return;

		ushort sourceAddress      = (ushort)(sourceAddressLo * 0x100);
		ushort destinationAddress = 0xFE00;

		while (destinationAddress < DMA_FINISH_ADDRESS)
			Write(destinationAddress++, Read(sourceAddress++));
	}

	public static bool IsInRange(int number, int lowerBoundInclusive, int upperBoundInclusive)
	{
		if (lowerBoundInclusive > upperBoundInclusive)
			(upperBoundInclusive, lowerBoundInclusive) = (lowerBoundInclusive, upperBoundInclusive);

		return number >= lowerBoundInclusive && number <= upperBoundInclusive;
	}
}