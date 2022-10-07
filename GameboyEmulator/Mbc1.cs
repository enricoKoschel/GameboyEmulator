namespace GameboyEmulator;

public sealed class Mbc1 : MbcBase
{
	private enum BankingMode
	{
		Simple   = 0,
		Advanced = 1
	}

	public Mbc1(ushort numberOfRomBanks, byte numberOfRamBanks, BankControllerType type)
	{
		Type = type;

		if (numberOfRomBanks > 128)
		{
			Logger.ControlledCrash(
				$"Memory Bank Controller '{Type}' only supports up to 128 ROM banks, but {numberOfRomBanks} were requested"
			);
		}

		if (numberOfRamBanks > 4)
		{
			Logger.ControlledCrash(
				$"Memory Bank Controller '{Type}' only supports up to 4 RAM banks, but {numberOfRamBanks} were requested"
			);
		}

		HasRam     = type is BankControllerType.Mbc1Ram or BankControllerType.Mbc1RamBattery;
		HasBattery = type is BankControllerType.Mbc1RamBattery;

		NumberOfRomBanks = numberOfRomBanks;
		NumberOfRamBanks = numberOfRamBanks;

		currentBankingMode = BankingMode.Simple;

		CurrentRomBank = 1;
		CurrentRamBank = 0;
		RamEnabled     = false;
	}

	private BankingMode currentBankingMode;
	private byte        currentRomBankUpper;
	private byte        currentRomBankLower;

	public override bool   HasRam           { get; }
	public override bool   HasBattery       { get; }
	public override ushort NumberOfRomBanks { get; }
	public override byte   NumberOfRamBanks { get; }

	private byte CurrentRomBank
	{
		get => (byte)(((currentRomBankUpper & 0b0000_0011) << 5) | (currentRomBankLower & 0b0001_1111));
		init
		{
			currentRomBankLower = (byte)(value & 0b0001_1111);
			currentRomBankUpper = (byte)((value & 0b0110_0000) >> 5);
		}
	}

	private         byte CurrentRamBank { get; set; }
	public override bool RamEnabled     { get; protected set; }

	public override void HandleBanking(ushort address, byte data)
	{
		if (HasRam && Memory.IsInRange(address, 0x0000, 0x1FFF))
			RamEnabled = (data & 0x0F) == 0xA;
		else if (Memory.IsInRange(address, 0x2000, 0x3FFF))
		{
			//Set the lower 5 bits of current rom bank number
			currentRomBankLower = (byte)(data & 0b0001_1111);
		}
		else if (Memory.IsInRange(address, 0x4000, 0x5FFF))
		{
			//Set bits 5 and 6 of current rom bank number if enough banks are available
			if (NumberOfRomBanks >= 64) currentRomBankUpper = (byte)(data & 0b0000_0011);

			//Set current ram bank number if enough banks are available
			if (currentBankingMode == BankingMode.Advanced && NumberOfRamBanks >= 4)
				CurrentRamBank = (byte)(data & 0b0000_0011);
		}
		else if (Memory.IsInRange(address, 0x6000, 0x7FFF))
		{
			//Change Memory Banking Mode
			switch (data)
			{
				case 0:
					currentBankingMode = BankingMode.Simple;
					break;
				case 1:
					currentBankingMode = BankingMode.Advanced;
					break;
				default:
					Logger.ControlledCrash($"Invalid memory banking mode '{(BankingMode)data}'");
					break;
			}
		}

		if (currentRomBankLower == 0) currentRomBankLower++;
	}

	public override uint ConvertRomAddress(ushort address)
	{
		byte currentActualRomBank = (byte)(CurrentRomBank & (NumberOfRomBanks - 1));

		if (currentBankingMode == BankingMode.Advanced)
		{
			if (address < 0x4000) return (uint)(address + (currentActualRomBank & 0b0110_0000) * 0x4000);

			return (uint)(address + (currentActualRomBank - 1) * 0x4000);
		}

		if (address < 0x4000) return address;

		return (uint)(address + (currentActualRomBank - 1) * 0x4000);
	}

	public override uint ConvertRamAddress(ushort address)
	{
		if (currentBankingMode == BankingMode.Simple) return address;

		return (uint)(address + 0x2000 * CurrentRamBank);
	}
}