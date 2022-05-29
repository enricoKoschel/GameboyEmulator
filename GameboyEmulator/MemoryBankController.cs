using System;

namespace GameboyEmulator;

public class MemoryBankController
{
	private readonly Emulator emulator;
	private          MbcBase  mbc;

	public MemoryBankController(Emulator emulator)
	{
		this.emulator = emulator;

		//mbc gets set by InitialiseBanking before it is accessed
		mbc = null!;
	}

	public bool HasRam           => mbc.HasRam;
	public bool RamEnabled       => mbc.RamEnabled;
	public bool RamAccessible    => HasRam && RamEnabled;
	public bool ShouldSaveToFile => HasRam && mbc.HasBattery;

	public byte NumberOfRamBanks => mbc.NumberOfRamBanks;

	public void InitialiseBanking()
	{
		MbcBase.BankControllerType type = (MbcBase.BankControllerType)emulator.memory.Read(0x147, true);
		if (!Enum.IsDefined(typeof(MbcBase.BankControllerType), type))
			Logger.ControlledCrash($"Cartridge uses invalid memory bank controller '0x{type:X2}'");

		Logger.LogInfo($"Memory bank controller: '{type}'");

		byte numberOfRomBanksRaw = emulator.memory.Read(0x148, true);
		if (numberOfRomBanksRaw > 0x08)
			Logger.ControlledCrash("Cartridge has an invalid number of ROM banks");

		byte numberOfRomBanks = (byte)Math.Pow(2, numberOfRomBanksRaw + 1);

		Logger.LogInfo($"Number of ROM banks: {numberOfRomBanks}");

		byte numberOfRamBanksRaw = emulator.memory.Read(0x149, true);
		byte numberOfRamBanks;
		switch (numberOfRamBanksRaw)
		{
			case 0x00:
			case 0x01:
				numberOfRamBanks = 0;
				break;
			case 0x02:
				numberOfRamBanks = 1;
				break;
			case 0x03:
				numberOfRamBanks = 4;
				break;
			case 0x04:
				numberOfRamBanks = 16;
				break;
			case 0x05:
				numberOfRamBanks = 8;
				break;
			default:
				Logger.ControlledCrash("Cartridge has an invalid number of RAM banks");
				return;
		}

		Logger.LogInfo($"Number of RAM banks: {numberOfRamBanks}");

		switch (type)
		{
			case MbcBase.BankControllerType.RomOnly:
				mbc = new MbcRomOnly(numberOfRomBanks, numberOfRamBanks);
				break;
			case MbcBase.BankControllerType.Mbc1:
			case MbcBase.BankControllerType.Mbc1Ram:
			case MbcBase.BankControllerType.Mbc1RamBattery:
				bool hasRam = type is MbcBase.BankControllerType.Mbc1Ram or MbcBase.BankControllerType.Mbc1RamBattery;
				bool hasBattery = type is MbcBase.BankControllerType.Mbc1RamBattery;

				mbc = new Mbc1(numberOfRomBanks, numberOfRamBanks, hasRam, hasBattery);
				break;
			default:
				Logger.ControlledCrash($"Memory Bank Controller '{type}' is not implemented yet");
				break;
		}
	}

	public void HandleBanking(ushort address, byte data)
	{
		mbc.HandleBanking(address, data);
	}

	public uint ConvertRomAddress(ushort address)
	{
		return mbc.ConvertRomAddress(address);
	}

	public uint ConvertRamAddress(ushort address)
	{
		return mbc.ConvertRamAddress(address);
	}
}