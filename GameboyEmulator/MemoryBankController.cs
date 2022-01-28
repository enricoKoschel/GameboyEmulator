using System;
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
			RomBankingMode = 0,
			RamBankingMode = 1
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
		private byte               currentRomBank;

		private bool isRamEnabled;

		public void DetectBankingMode()
		{
			currentBankControllerType = (BankControllerType)memory.Read(0x147);
			if (!Enum.IsDefined(typeof(BankControllerType), currentBankControllerType))
			{
				throw new NotImplementedException(
					$"Invalid memory bank controller with id '{currentBankControllerType}'"
				);
			}

			byte numberOfRomBanksRaw = memory.Read(0x148);
			if (numberOfRomBanksRaw > 0x08)
				throw new InvalidDataException("Cartridge has invalid number of ROM banks!");

			numberOfRomBanks = (byte)Math.Pow(2, numberOfRomBanksRaw + 1);

			byte numberOfRamBanksRaw = memory.Read(0x149);
			if (numberOfRamBanksRaw == 0x00 || numberOfRamBanksRaw == 0x01)
				numberOfRamBanks = 0;
			else if (numberOfRamBanksRaw < 0x06)
				numberOfRamBanks = (byte)(numberOfRamBanksRaw - 1);
			else
				throw new InvalidDataException("Cartridge has invalid number of RAM banks!");

			currentMemoryBankingMode = MemoryBankingMode.RomBankingMode;

			isRamEnabled = false;

			currentRomBank = 1;
			currentRamBank = 0;
		}

		public void HandleBanking(ushort address, byte data)
		{
			switch (currentBankControllerType)
			{
				case BankControllerType.Mbc1:
				{
					HandleBankingMbc1(address, data);
					break;
				}
				default:
				{
					throw new NotImplementedException(
						$"Memory Bank Controller '{currentBankControllerType.ToString()}' is not implemented yet!"
					);
				}
			}
		}

		private void HandleBankingMbc1(ushort address, byte data)
		{
			if (Memory.IsInRange(address, 0x0000, 0x1FFF))
			{
				//Ram Enable
				isRamEnabled = (data & 0x0F) == 0xA;
			}
			else if (Memory.IsInRange(address, 0x2000, 0x3FFF))
			{
				//Change lower 5 bits of current Rombank
				currentRomBank = (byte)((currentRomBank & 0b11100000) | (data & 0b00011111));
				if (currentRomBank == 0x00 || currentRomBank == 0x20 || currentRomBank == 0x40 ||
					currentRomBank == 0x60)
					currentRomBank++;
			}
			else if (Memory.IsInRange(address, 0x4000, 0x5FFF))
			{
				//Change current Rambank or upper 2 bits of current Rombank
				if (currentMemoryBankingMode == MemoryBankingMode.RomBankingMode)
					currentRomBank = (byte)((currentRomBank & 0b00011111) | (data & 0b01100000));
				else
					currentRamBank = (byte)(data & 0b00000011);
			}
			else if (Memory.IsInRange(address, 0x6000, 0x7FFF))
			{
				//Change Memory Banking Mode
				currentMemoryBankingMode = (MemoryBankingMode)data;
			}
		}

		public ushort ConvertAddressInRomBank(ushort address)
		{
			//TODO adapt to all mbcs
			if (address < 0x4000) return address;

			return (ushort)(address + (currentRomBank - 1) * 0x4000);
		}

		public ushort ConvertAddressInRamBank(ushort address)
		{
			//TODO adapt to all mbcs
			return address;
		}

		public byte GetNumberOfRamBanks()
		{
			return numberOfRamBanks;
		}
	}
}