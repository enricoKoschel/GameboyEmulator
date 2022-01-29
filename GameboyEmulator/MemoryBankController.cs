﻿using System;
using System.IO;

namespace GameboyEmulator
{
	class MemoryBankController
	{
		//A list of games and their corresponding mappers can be found here: https://gbhwdb.gekkio.fi/cartridges/
		private enum BankControllerType
		{
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

		//Modules
		private readonly Memory memory;

		public MemoryBankController(Memory memory)
		{
			this.memory = memory;
		}

		private BankControllerType currentBankControllerType;
		private MemoryBankingMode  currentMemoryBankingMode;
		private byte               numberOfRomBanks;
		private byte               numberOfRamBanks;
		private byte               currentRamBank;
		private byte               currentRamBankHidden;
		private byte               currentRomBank;

		private bool isRamEnabled;

		public void InitialiseBanking()
		{
			currentBankControllerType = (BankControllerType)memory.Read(0x147, true);
			if (!Enum.IsDefined(typeof(BankControllerType), currentBankControllerType))
			{
				Logger.LogMessage(
					$"Invalid memory bank controller with id '{currentBankControllerType}'", Logger.LogLevel.Error
				);

				throw new InvalidDataException(
					$"Invalid memory bank controller with id '{currentBankControllerType}'"
				);
			}

			Logger.LogMessage(
				$"Memory bank controller '{currentBankControllerType.ToString()}' was determined.",
				Logger.LogLevel.Info, true
			);

			byte numberOfRomBanksRaw = memory.Read(0x148, true);
			if (numberOfRomBanksRaw > 0x08)
			{
				Logger.LogMessage("Cartridge has invalid number of ROM banks!", Logger.LogLevel.Error);
				throw new InvalidDataException("Cartridge has invalid number of ROM banks!");
			}

			numberOfRomBanks = (byte)Math.Pow(2, numberOfRomBanksRaw + 1);

			Logger.LogMessage($"{numberOfRomBanks} ROM bank(s) was/were determined.", Logger.LogLevel.Info, true);

			byte numberOfRamBanksRaw = memory.Read(0x149, true);
			if (numberOfRamBanksRaw == 0x00 || numberOfRamBanksRaw == 0x01)
				numberOfRamBanks = 0;
			else if (numberOfRamBanksRaw < 0x06)
				numberOfRamBanks = (byte)(numberOfRamBanksRaw - 1);
			else
			{
				Logger.LogMessage("Cartridge has invalid number of RAM banks!", Logger.LogLevel.Error);
				throw new InvalidDataException("Cartridge has invalid number of RAM banks!");
			}

			Logger.LogMessage($"{numberOfRamBanks} RAM bank(s) was/were determined.", Logger.LogLevel.Info, true);

			currentMemoryBankingMode = MemoryBankingMode.SimpleRomBanking;

			isRamEnabled = false;

			currentRomBank       = 1;
			currentRamBank       = 0;
			currentRamBankHidden = 0;
		}

		public void HandleBanking(ushort address, byte data)
		{
			byte previousRomBank = currentRomBank;

			switch (currentBankControllerType)
			{
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
					Logger.LogMessage(
						$"Memory Bank Controller '{currentBankControllerType.ToString()}' is not implemented yet!",
						Logger.LogLevel.Error
					);

					throw new NotImplementedException(
						$"Memory Bank Controller '{currentBankControllerType.ToString()}' is not implemented yet!"
					);
				}
			}

			if (previousRomBank != currentRomBank)
			{
				Logger.LogMessage(
					$"Rom bank was changed from '0x{previousRomBank:X}' to '0x{currentRomBank:X}'",
					Logger.LogLevel.Info,
					true
				);
			}
		}

		private void HandleBankingMbc1(ushort address, byte data, bool hasRam)
		{
			//Create mask for rom bank number, so that only the necessary amount of bits are used
			int  numberOfBitsForRomBank = (int)Math.Ceiling(Math.Log2(numberOfRomBanks));
			byte romBankBitMask         = (byte)(0xFF >> (8 - numberOfBitsForRomBank));

			//Ensure that not more than 5 bits are used
			romBankBitMask &= 0b00011111;

			if (hasRam && Memory.IsInRange(address, 0x0000, 0x1FFF))
				isRamEnabled = (data & 0x0F) == 0xA;

			if (Memory.IsInRange(address, 0x2000, 0x3FFF))
			{
				//Set up to the lower 5 bits of current rom bank number
				currentRomBank = (byte)(data & romBankBitMask);
			}
			else if (Memory.IsInRange(address, 0x4000, 0x5FFF))
			{
				//Set bits 5 and 6 of current rom bank number if enough banks are available
				if (numberOfRomBanks >= 64)
					currentRomBank = (byte)((currentRomBank & 0b00011111) | ((data & 0b00000011) << 5));

				//Set current hidden ram bank number if enough banks are available
				if (currentMemoryBankingMode == MemoryBankingMode.SimpleRomBanking && numberOfRamBanks >= 4)
					currentRamBankHidden = (byte)(data & 0b00000011);

				//Set current ram bank number if enough banks are available
				if (currentMemoryBankingMode == MemoryBankingMode.AdvancedRomOrRamBanking && numberOfRamBanks >= 4)
					currentRamBank = (byte)(data & 0b00000011);
			}
			else if (Memory.IsInRange(address, 0x6000, 0x7FFF))
			{
				//Change Memory Banking Mode
				switch (data)
				{
					case 0:
						currentMemoryBankingMode = MemoryBankingMode.SimpleRomBanking;
						currentRamBank           = 0;
						break;
					case 1:
						currentMemoryBankingMode = MemoryBankingMode.AdvancedRomOrRamBanking;
						currentRamBank           = currentRamBankHidden; //TODO maybe only do this if ram is enabled?
						break;
					default:
						Logger.LogMessage("Invalid memory banking mode!", Logger.LogLevel.Error);
						throw new InvalidDataException("Invalid memory banking mode!");
				}
			}

			if (currentRomBank == 0x00 || currentRomBank == 0x20 || currentRomBank == 0x40 ||
				currentRomBank == 0x60)
				currentRomBank++;
		}

		public ushort ConvertAddressInRomBank(ushort address)
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
					if (currentMemoryBankingMode == MemoryBankingMode.AdvancedRomOrRamBanking)
					{
						if (address < 0x4000)
							return (ushort)(address + (currentRomBank & 0b01100000) * 0x4000);

						return (ushort)(address + ((currentRomBank & 0b00011111) - 1) * 0x4000);
					}

					if (address < 0x4000) return address;

					return (ushort)(address + (currentRomBank - 1) * 0x4000);
				}
				default:
				{
					Logger.LogMessage(
						$"Memory Bank Controller '{currentBankControllerType.ToString()}' is not implemented yet!",
						Logger.LogLevel.Error
					);

					throw new NotImplementedException(
						$"Memory Bank Controller '{currentBankControllerType.ToString()}' is not implemented yet!"
					);
				}
			}
		}

		public ushort ConvertAddressInRamBank(ushort address)
		{
			//TODO adapt to all mbcs
			return (ushort)(address + 0x2000 * currentRamBank);
		}

		public byte GetNumberOfRamBanks()
		{
			return numberOfRamBanks;
		}

		public bool GetIsRamEnabled()
		{
			return isRamEnabled;
		}
	}
}