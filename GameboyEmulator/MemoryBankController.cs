namespace GameboyEmulator
{
	class MemoryBankController
	{
		private enum MemoryBankControllers
		{
			RomOnly = 0x00,
			Mbc1    = 0x01
		}

		private enum MemoryBankingModes
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

		private MemoryBankControllers currentMemoryBankController;
		private MemoryBankingModes    currentMemoryBankingMode;
		private int                   currentRomBank;
		private int                   currentRamBank;

		public int CurrentRamBank => currentRamBank;
		public int CurrentRomBank => currentRomBank;

		private bool isRamEnabled;

		public void DetectBankingMode()
		{
			currentMemoryBankController = (MemoryBankControllers)memory.Read(0x147);
			currentMemoryBankingMode    = MemoryBankingModes.RomBankingMode;
			currentRomBank              = 1;
			currentRamBank              = 0;
		}

		public void HandleBanking(ushort address, byte data)
		{
			switch (currentMemoryBankController)
			{
				case MemoryBankControllers.Mbc1:
				{
					if (Memory.IsBetween(address, 0x0000, 0x2000))
					{
						//Ram Enable
						isRamEnabled = (data & 0x0F) == 0xA;
					}
					else if (Memory.IsBetween(address, 0x2000, 0x4000))
					{
						//Change lower 5 bits of current Rombank
						currentRomBank = (currentRomBank & 0b11100000) | (data & 0b00011111);
						if (currentRomBank == 0x00 || currentRomBank == 0x20 || currentRomBank == 0x40 ||
							currentRomBank == 0x60)
							currentRomBank++;
					}
					else if (Memory.IsBetween(address, 0x4000, 0x6000))
					{
						//Change current Rambank or upper 2 bits of current Rombank
						if (currentMemoryBankingMode == MemoryBankingModes.RomBankingMode)
							currentRomBank = (currentRomBank & 0b00011111) | (data & 0b01100000);
						else
							currentRamBank = data & 0b00000011;
					}
					else if (Memory.IsBetween(address, 0x6000, 0x8000))
					{
						//Change Memory Banking Mode
						currentMemoryBankingMode = (MemoryBankingModes)data;
					}

					break;
				}
			}
		}
	}
}