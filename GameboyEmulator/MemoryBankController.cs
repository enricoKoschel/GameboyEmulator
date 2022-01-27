using System;

namespace GameboyEmulator
{
	class MemoryBankController
	{
		private enum BankControllerType
		{
			RomOnly = 0x00,
			Mbc1    = 0x01
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
		private int                currentRamBank;

		public int CurrentRomBank { get; private set; }

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

			currentMemoryBankingMode = MemoryBankingMode.RomBankingMode;

			CurrentRomBank = 1;
			currentRamBank = 0;
		}

		public void HandleBanking(ushort address, byte data)
		{
			switch (currentBankControllerType)
			{
				case BankControllerType.Mbc1:
				{
					if (Memory.IsInRange(address, 0x0000, 0x1FFF))
					{
						//Ram Enable
						isRamEnabled = (data & 0x0F) == 0xA;
					}
					else if (Memory.IsInRange(address, 0x2000, 0x3FFF))
					{
						//Change lower 5 bits of current Rombank
						CurrentRomBank = (CurrentRomBank & 0b11100000) | (data & 0b00011111);
						if (CurrentRomBank == 0x00 || CurrentRomBank == 0x20 || CurrentRomBank == 0x40 ||
							CurrentRomBank == 0x60)
							CurrentRomBank++;
					}
					else if (Memory.IsInRange(address, 0x4000, 0x5FFF))
					{
						//Change current Rambank or upper 2 bits of current Rombank
						if (currentMemoryBankingMode == MemoryBankingMode.RomBankingMode)
							CurrentRomBank = (CurrentRomBank & 0b00011111) | (data & 0b01100000);
						else
							currentRamBank = data & 0b00000011;
					}
					else if (Memory.IsInRange(address, 0x6000, 0x7FFF))
					{
						//Change Memory Banking Mode
						currentMemoryBankingMode = (MemoryBankingMode)data;
					}

					break;
				}
			}
		}

		public ushort ConvertAddressInRomBank(ushort address)
		{
			//TODO adapt to all mbcs
			if (address < 0x4000) return address;

			return (ushort)(address + (CurrentRomBank - 1) * 0x4000);
		}
	}
}