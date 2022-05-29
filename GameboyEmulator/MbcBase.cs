using System.Diagnostics.CodeAnalysis;

namespace GameboyEmulator;

public abstract class MbcBase
{
	//Suppress warnings for not yet implemented mappers
	[SuppressMessage("ReSharper", "UnusedMember.Local")]
	public enum BankControllerType
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

	public BankControllerType Type { get; protected init; }

	public abstract bool HasRam           { get; } //=> NumberOfRamBanks > 0
	public abstract bool HasBattery       { get; }
	public abstract byte NumberOfRomBanks { get; }
	public abstract byte NumberOfRamBanks { get; }

	public abstract byte CurrentRomBank { get; protected set; }
	public abstract byte CurrentRamBank { get; protected set; }
	public abstract bool RamEnabled     { get; protected set; }

	public abstract void HandleBanking(ushort address, byte data);

	public abstract uint ConvertRomAddress(ushort address);

	public abstract uint ConvertRamAddress(ushort address);
}