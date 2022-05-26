﻿using System;
using System.Diagnostics.CodeAnalysis;

namespace GameboyEmulator;

public class MemoryBankController
{
	//Suppress warnings for not yet implemented mappers
	[SuppressMessage("ReSharper", "UnusedMember.Local")]
	private enum BankControllerType
	{
		//A list of games and their corresponding mappers can be found here: https://gbhwdb.gekkio.fi/cartridges/

		RomOnly = 0x00,

		//High priority - used by many games
		Mbc1           = 0x01,
		Mbc1Ram        = 0x02,
		Mbc1RamBattery = 0x03,

		//Medium priority - used by few games
		Mbc2        = 0x05,
		Mbc2Battery = 0x06,

		//Will not be implemented - not used by any licensed games
		RomRam        = 0x08,
		RomRamBattery = 0x09,

		//Will not be implemented - used by one licensed game
		Mmm01           = 0x0B,
		Mmm01Ram        = 0x0C,
		Mmm01RamBattery = 0x0D,

		//Medium priority - used mainly by Pokémon games
		Mbc3TimerBattery    = 0x0F,
		Mbc3TimerRamBattery = 0x10,
		Mbc3                = 0x11,
		Mbc3Ram             = 0x12,
		Mbc3RamBattery      = 0x13,

		//Medium priority - used by many GameBoy Color and Pokémon games
		Mbc5                 = 0x19,
		Mbc5Ram              = 0x1A,
		Mbc5RamBattery       = 0x1B,
		Mbc5Rumble           = 0x1C,
		Mbc5RumbleRam        = 0x1D,
		Mbc5RumbleRamBattery = 0x1E,

		//Will not be implemented - used by one licensed game
		Mbc6 = 0x20,

		//Will not be implemented - used by a few GameBoy Color games
		Mbc7SensorRumbleRamBattery = 0x22,

		//Will not be implemented - separate device used for taking photos
		PocketCamera = 0xFC,

		//Will not be implemented - used by one licensed game
		Tama5 = 0xFD,

		//Low priority - used by very few games
		Huc3 = 0xFE,

		//Low priority - used by very few games
		Huc1RamBattery = 0xFF
	}

	private enum MemoryBankingMode
	{
		SimpleRomBanking        = 0,
		AdvancedRomOrRamBanking = 1
	}

	private readonly Emulator emulator;

	public MemoryBankController(Emulator emulator)
	{
		this.emulator = emulator;
	}

	private BankControllerType currentBankControllerType;
	private MemoryBankingMode  currentMemoryBankingMode;
	private byte               numberOfRomBanks;
	private byte               currentRamBank;
	private byte               currentRomBankUpper;
	private byte               currentRomBankLower;

	public byte NumberOfRamBanks { get; private set; }

	public bool CartridgeRamExists => NumberOfRamBanks > 0;

	private byte CurrentRomBank
	{
		get => (byte)(((currentRomBankUpper & 0b0000_0011) << 5) | (currentRomBankLower & 0b0001_1111));
		set
		{
			currentRomBankLower = (byte)(value & 0b0001_1111);
			currentRomBankUpper = (byte)((value & 0b0110_0000) >> 5);
		}
	}

	public bool IsRamEnabled { get; private set; }

	public void InitialiseBanking()
	{
		currentBankControllerType = (BankControllerType)emulator.memory.Read(0x147, true);
		if (!Enum.IsDefined(typeof(BankControllerType), currentBankControllerType))
			Logger.ControlledCrash($"Cartridge uses invalid memory bank controller '{currentBankControllerType}'");

		Logger.LogInfo($"Memory bank controller: '{currentBankControllerType.ToString()}'");

		byte numberOfRomBanksRaw = emulator.memory.Read(0x148, true);
		if (numberOfRomBanksRaw > 0x08)
			Logger.ControlledCrash("Cartridge has an invalid number of ROM banks");

		numberOfRomBanks = (byte)Math.Pow(2, numberOfRomBanksRaw + 1);

		Logger.LogInfo($"Number of ROM banks: {numberOfRomBanks}");

		byte numberOfRamBanksRaw = emulator.memory.Read(0x149, true);
		switch (numberOfRamBanksRaw)
		{
			case 0x00:
			case 0x01:
				NumberOfRamBanks = 0;
				break;
			case 0x02:
				NumberOfRamBanks = 1;
				break;
			case 0x03:
				NumberOfRamBanks = 4;
				break;
			case 0x04:
				NumberOfRamBanks = 16;
				break;
			case 0x05:
				NumberOfRamBanks = 8;
				break;
			default:
				Logger.ControlledCrash("Cartridge has an invalid number of RAM banks");
				break;
		}

		Logger.LogInfo($"Number of RAM banks: {NumberOfRamBanks}");

		currentMemoryBankingMode = MemoryBankingMode.SimpleRomBanking;

		IsRamEnabled = false;

		CurrentRomBank = 1;
		currentRamBank = 0;
	}

	public void HandleBanking(ushort address, byte data)
	{
		switch (currentBankControllerType)
		{
			case BankControllerType.RomOnly:
				return;
			case BankControllerType.Mbc1:
			{
				HandleBankingMbc1(address, data, false);
				break;
			}
			case BankControllerType.Mbc1Ram:
			case BankControllerType.Mbc1RamBattery:
			{
				HandleBankingMbc1(address, data, true);
				break;
			}
			default:
			{
				Logger.ControlledCrash($"Memory Bank Controller '{currentBankControllerType}' is not implemented yet");
				break;
			}
		}
	}

	private void HandleBankingMbc1(ushort address, byte data, bool hasRam)
	{
		if (hasRam && Memory.IsInRange(address, 0x0000, 0x1FFF))
			IsRamEnabled = (data & 0x0F) == 0xA;
		else if (Memory.IsInRange(address, 0x2000, 0x3FFF))
		{
			//Set the lower 5 bits of current rom bank number
			currentRomBankLower = (byte)(data & 0b0001_1111);
		}
		else if (Memory.IsInRange(address, 0x4000, 0x5FFF))
		{
			//Set bits 5 and 6 of current rom bank number if enough banks are available
			if (numberOfRomBanks >= 64) currentRomBankUpper = (byte)(data & 0b0000_0011);

			//Set current ram bank number if enough banks are available
			if (currentMemoryBankingMode == MemoryBankingMode.AdvancedRomOrRamBanking && NumberOfRamBanks >= 4)
				currentRamBank = (byte)(data & 0b0000_0011);
		}
		else if (Memory.IsInRange(address, 0x6000, 0x7FFF))
		{
			//Change Memory Banking Mode
			switch (data)
			{
				case 0:
					currentMemoryBankingMode = MemoryBankingMode.SimpleRomBanking;
					break;
				case 1:
					currentMemoryBankingMode = MemoryBankingMode.AdvancedRomOrRamBanking;
					break;
				default:
					Logger.ControlledCrash($"Invalid memory banking mode '{currentMemoryBankingMode}'");
					break;
			}
		}

		if (currentRomBankLower == 0) currentRomBankLower++;
	}

	public uint ConvertAddressInRomBank(ushort address)
	{
		switch (currentBankControllerType)
		{
			case BankControllerType.RomOnly:
			{
				return address;
			}
			case BankControllerType.Mbc1:
			case BankControllerType.Mbc1Ram:
			case BankControllerType.Mbc1RamBattery:
			{
				byte currentActualRomBank = (byte)(CurrentRomBank & (numberOfRomBanks - 1));

				if (currentMemoryBankingMode == MemoryBankingMode.AdvancedRomOrRamBanking)
				{
					if (address < 0x4000) return (uint)(address + (currentActualRomBank & 0b0110_0000) * 0x4000);

					return (uint)(address + (currentActualRomBank - 1) * 0x4000);
				}

				if (address < 0x4000) return address;

				return (uint)(address + (currentActualRomBank - 1) * 0x4000);
			}
			default:
			{
				Logger.ControlledCrash($"Memory Bank Controller '{currentBankControllerType}' is not implemented yet");
				return 0;
			}
		}
	}

	public uint ConvertAddressInRamBank(ushort address)
	{
		//TODO adapt to all mbcs
		if (currentMemoryBankingMode == MemoryBankingMode.SimpleRomBanking) return address;

		return (uint)(address + 0x2000 * currentRamBank);
	}
}