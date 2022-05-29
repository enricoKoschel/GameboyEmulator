namespace GameboyEmulator;

public sealed class Mbc5 : MbcBase
{
	public Mbc5(ushort numberOfRomBanks, byte numberOfRamBanks, BankControllerType type)
	{
		Type = type;

		if (numberOfRomBanks > 512)
		{
			Logger.ControlledCrash(
				$"Memory Bank Controller '{Type}' only supports up to 512 ROM banks, but {numberOfRomBanks} were requested"
			);
		}

		if (numberOfRamBanks > 16)
		{
			Logger.ControlledCrash(
				$"Memory Bank Controller '{Type}' only supports up to 16 RAM banks, but {numberOfRamBanks} were requested"
			);
		}

		HasRam = type is BankControllerType.Mbc5Ram or BankControllerType.Mbc5RamBattery
					 or BankControllerType.Mbc5RumbleRam or BankControllerType.Mbc5RumbleRamBattery;

		HasBattery = type is BankControllerType.Mbc5RamBattery or BankControllerType.Mbc5RumbleRamBattery;

		bool hasRumble = type is BankControllerType.Mbc5Rumble or BankControllerType.Mbc5RumbleRam
							 or BankControllerType.Mbc5RumbleRamBattery;

		if (hasRumble) Logger.LogInfo("Cartridge uses rumble, but rumble is not supported yet", true);

		NumberOfRomBanks = numberOfRomBanks;
		NumberOfRamBanks = numberOfRamBanks;

		CurrentRomBank = 1;
		CurrentRamBank = 0;
		RamEnabled     = false;
	}

	private bool currentRomBankUpper;
	private byte currentRomBankLower;

	public override bool   HasRam           { get; }
	public override bool   HasBattery       { get; }
	public override ushort NumberOfRomBanks { get; }
	public override byte   NumberOfRamBanks { get; }

	private ushort CurrentRomBank
	{
		get => Cpu.SetBit((ushort)currentRomBankLower, 8, currentRomBankUpper);
		init
		{
			currentRomBankLower = (byte)(value & 0b1111_1111);
			currentRomBankUpper = Cpu.GetBit(value, 8);
		}
	}

	private         byte CurrentRamBank { get; set; }
	public override bool RamEnabled     { get; protected set; }

	public override void HandleBanking(ushort address, byte data)
	{
		if (HasRam && Memory.IsInRange(address, 0x0000, 0x1FFF))
			RamEnabled = (data & 0x0F) == 0xA;
		else if (Memory.IsInRange(address, 0x2000, 0x2FFF))
			currentRomBankLower = data;
		else if (Memory.IsInRange(address, 0x3000, 0x3FFF))
			currentRomBankUpper = Cpu.GetBit(data, 0);
		else if (Memory.IsInRange(address, 0x4000, 0x5FFF))
			CurrentRamBank = (byte)(data & 0x0F);
	}

	public override uint ConvertRomAddress(ushort address)
	{
		if (address < 0x4000) return address;

		ushort currentActualRomBank = (ushort)(CurrentRomBank & (NumberOfRomBanks - 1));
		return (uint)(address + (currentActualRomBank - 1) * 0x4000);
	}

	public override uint ConvertRamAddress(ushort address)
	{
		return (uint)(address + 0x2000 * CurrentRamBank);
	}
}