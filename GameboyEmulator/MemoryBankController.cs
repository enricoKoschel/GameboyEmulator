﻿namespace GameboyEmulator
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
			currentMemoryBankingMode  = MemoryBankingMode.RomBankingMode;
			CurrentRomBank            = 1;
			currentRamBank            = 0;
		}

		public void HandleBanking(ushort address, byte data)
		{
			switch (currentBankControllerType)
			{
				case BankControllerType.Mbc1:
				{
					if (Memory.IsBetween(address, 0x0000, 0x2000))
					{
						//Ram Enable
						isRamEnabled = (data & 0x0F) == 0xA;
					}
					else if (Memory.IsBetween(address, 0x2000, 0x4000))
					{
						//Change lower 5 bits of current Rombank
						CurrentRomBank = (CurrentRomBank & 0b11100000) | (data & 0b00011111);
						if (CurrentRomBank == 0x00 || CurrentRomBank == 0x20 || CurrentRomBank == 0x40 ||
							CurrentRomBank == 0x60)
							CurrentRomBank++;
					}
					else if (Memory.IsBetween(address, 0x4000, 0x6000))
					{
						//Change current Rambank or upper 2 bits of current Rombank
						if (currentMemoryBankingMode == MemoryBankingMode.RomBankingMode)
							CurrentRomBank = (CurrentRomBank & 0b00011111) | (data & 0b01100000);
						else
							currentRamBank = data & 0b00000011;
					}
					else if (Memory.IsBetween(address, 0x6000, 0x8000))
					{
						//Change Memory Banking Mode
						currentMemoryBankingMode = (MemoryBankingMode)data;
					}

					break;
				}
			}
		}
	}
}