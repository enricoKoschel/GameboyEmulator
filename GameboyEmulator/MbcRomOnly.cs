﻿namespace GameboyEmulator;

public sealed class MbcRomOnly : MbcBase
{
	public MbcRomOnly(ushort numberOfRomBanks, byte numberOfRamBanks)
	{
		Type = BankControllerType.RomOnly;

		if (numberOfRomBanks != 2)
		{
			Logger.ControlledCrash(
				$"Memory Bank Controller '{Type}' only supports 2 ROM banks, but {numberOfRomBanks} were requested"
			);
		}

		if (numberOfRamBanks != 0)
		{
			Logger.ControlledCrash(
				$"Memory Bank Controller '{Type}' does not support RAM banks, but {numberOfRamBanks} were requested"
			);
		}

		RamEnabled = false;
	}

	public override bool HasRam => false;

	public override bool   HasBattery       => false;
	public override ushort NumberOfRomBanks => 2;
	public override byte   NumberOfRamBanks => 0;

	public override bool RamEnabled { get; protected set; }

	public override void HandleBanking(ushort address, byte data)
	{
		//Rom only does not have banking
	}

	public override uint ConvertRomAddress(ushort address)
	{
		return address;
	}

	public override uint ConvertRamAddress(ushort address)
	{
		return address;
	}
}