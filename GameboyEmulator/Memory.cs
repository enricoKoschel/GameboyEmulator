using System;
using System.Diagnostics;
using System.IO;

namespace GameboyEmulator;

public class Memory
{
	//ReSharper disable CommentTypo
	//Memory Map
	//0x0000-0x7FFF		First two rom banks
	//0x8000-0x9FFF		Video Ram
	//0xA000-0xBFFF		Cartridge Ram
	//0xC000-0xDFFF		Work Ram
	//0xE000-0xFDFF		Echo Ram (same as 0xC000-0xDDFF)
	//0xFE00-0xFE9F		Sprite Attributes
	//0xFEA0-0xFEFF		Unused
	//0xFF00-0xFF7F		IO Ports
	//0xFF80-0xFFFE		High Ram
	//0xFFFF			Interrupt Enable Register
	//ReSharper restore CommentTypo

	//bootRom, cartridgeRom and cartridgeRam are correctly set by LoadGame() before they are accessed
	private          byte[] bootRom      = null!;
	private          byte[] cartridgeRom = null!;
	private readonly byte[] videoRam;
	private          byte[] cartridgeRam = null!;
	private readonly byte[] workRam;
	private readonly byte[] spriteAttributes;
	private          byte   dmaRegister;
	private readonly byte[] highRam;

	private const int CARTRIDGE_ROM_BASE_ADDRESS = 0x0000;
	private const int CARTRIDGE_ROM_LAST_ADDRESS = 0x7FFF;

	private const int VIDEO_RAM_BASE_ADDRESS = 0x8000;
	private const int VIDEO_RAM_LAST_ADDRESS = 0x9FFF;

	private const int CARTRIDGE_RAM_BASE_ADDRESS = 0xA000;
	private const int CARTRIDGE_RAM_LAST_ADDRESS = 0xBFFF;

	private const int WORK_RAM_BASE_ADDRESS = 0xC000;
	private const int WORK_RAM_LAST_ADDRESS = 0xDFFF;

	private const int ECHO_RAM_BASE_ADDRESS = 0xE000;
	private const int ECHO_RAM_LAST_ADDRESS = 0xFDFF;

	private const int SPRITE_ATTRIBUTES_BASE_ADDRESS = 0xFE00;
	private const int SPRITE_ATTRIBUTES_LAST_ADDRESS = 0xFE9F;

	private const int UNUSED_BASE_ADDRESS = 0xFEA0;
	private const int UNUSED_LAST_ADDRESS = 0xFEFF;

	private const int IO_PORTS_BASE_ADDRESS = 0xFF00;
	private const int IO_PORTS_LAST_ADDRESS = 0xFF7F;

	private const int HIGH_RAM_BASE_ADDRESS = 0xFF80;
	private const int HIGH_RAM_LAST_ADDRESS = 0xFFFE;

	private const int INTERRUPT_ENABLE_REG_ADDRESS = 0xFFFF;

	private const ushort DMA_FINISH_ADDRESS = 0xFEA0;

	private bool bootRomEnabled;

	private readonly Stopwatch timeSinceLastRamSave;
	private          bool      ramChangedSinceLastSave = true;

	private readonly Emulator emulator;

	public Memory(Emulator emulator)
	{
		this.emulator = emulator;

		timeSinceLastRamSave = new Stopwatch();
		timeSinceLastRamSave.Start();

		videoRam         = new byte[0x2000];
		workRam          = new byte[0x2000];
		spriteAttributes = new byte[0x100];
		dmaRegister      = 0xFF;
		highRam          = new byte[0x7F];
	}

	private void AllocateCartridgeRam()
	{
		if (!emulator.memoryBankController.HasRam) return;

		//If a save file exists, load it into cartridge ram and resize ram to the actual size
		LoadCartridgeRam();
		Array.Resize(ref cartridgeRam, emulator.memoryBankController.NumberOfRamBanks * 0x2000);
	}

	public void LoadGame()
	{
		if (!String.IsNullOrWhiteSpace(emulator.bootRomFilePath))
		{
			bootRomEnabled = true;

			try
			{
				bootRom = File.ReadAllBytes(emulator.bootRomFilePath);
			}
			catch
			{
				Logger.ControlledCrash($"Boot rom '{emulator.bootRomFilePath}' could not be opened");
			}

			if (bootRom.Length != 0x100)
				Logger.ControlledCrash($"Selected boot rom '{emulator.bootRomFilePath}' has an invalid length");
		}
		else
		{
			bootRomEnabled = false;
			InitialiseRegisters();
		}

		try
		{
			cartridgeRom = File.ReadAllBytes(emulator.gameRomFilePath);
		}
		catch
		{
			Logger.ControlledCrash($"Game rom '{emulator.gameRomFilePath}' could not be opened");
		}

		//A cartridge has to be at least 0x150 bytes large to contain a full cartridge header
		if (cartridgeRom.Length < 0x150)
			Logger.ControlledCrash($"Selected game rom '{emulator.gameRomFilePath}' has an invalid length");

		//Pad the cartridge size to at least 0x8000 bytes (32KB)
		if (cartridgeRom.Length < 0x8000) Array.Resize(ref cartridgeRom, 0x8000);

		//Pad the cartridge size to the next highest power of 2
		double log = Math.Log2(cartridgeRom.Length);
		if (!(Math.Abs(log - (int)log) < Double.Epsilon))
			Array.Resize(ref cartridgeRom, (int)Math.Pow(2, Math.Ceiling(log)));


		//Detect current Memorybanking Mode
		emulator.memoryBankController.InitialiseBanking();

		AllocateCartridgeRam();

		Logger.LogInfo("Game loaded successfully");
	}

	public void SaveCartridgeRam()
	{
		if (!ramChangedSinceLastSave || !emulator.memoryBankController.HasRam ||
			!emulator.savingEnabled) return;

		if (timeSinceLastRamSave.Elapsed.TotalSeconds < 5) return;

		timeSinceLastRamSave.Restart();

		ramChangedSinceLastSave = false;

		File.WriteAllBytes(emulator.saveFilePath, cartridgeRam);
	}

	private void LoadCartridgeRam()
	{
		if (emulator.savingEnabled && File.Exists(emulator.saveFilePath))
			cartridgeRam = File.ReadAllBytes(emulator.saveFilePath);
	}

	private void InitialiseRegisters()
	{
		emulator.cpu.InitialiseRegisters();
		InitialiseMemory();
	}

	private void InitialiseMemory()
	{
		Write(0xFF05, 0x00);
		Write(0xFF06, 0x00);
		Write(0xFF07, 0x00);
		Write(0xFF10, 0x80);
		Write(0xFF11, 0xBF);
		Write(0xFF12, 0xF3);
		Write(0xFF14, 0xBF);
		Write(0xFF16, 0x3F);
		Write(0xFF17, 0x00);
		Write(0xFF19, 0xBF);
		Write(0xFF1A, 0x7F);
		Write(0xFF1B, 0xFF);
		Write(0xFF1C, 0x9F);
		Write(0xFF1E, 0xBF);
		Write(0xFF20, 0xFF);
		Write(0xFF21, 0x00);
		Write(0xFF22, 0x00);
		Write(0xFF23, 0xBF);
		Write(0xFF24, 0x77);
		Write(0xFF25, 0xF3);
		Write(0xFF26, 0xF1);
		Write(0xFF40, 0x91);
		Write(0xFF42, 0x00);
		Write(0xFF43, 0x00);
		Write(0xFF45, 0x00);
		Write(0xFF47, 0xFC);
		Write(0xFF48, 0xFF);
		Write(0xFF49, 0xFF);
		Write(0xFF4A, 0x00);
		Write(0xFF4B, 0x00);
		Write(0xFFFF, 0x00);
	}

	private void DisableBootRom()
	{
		bootRomEnabled = false;

		Logger.LogInfo("Boot rom was disabled");
	}

	public byte Read(ushort address, bool noRomBanking = false)
	{
		if (IsInRange(address, CARTRIDGE_ROM_BASE_ADDRESS, CARTRIDGE_ROM_LAST_ADDRESS))
			return ReadFromCartridgeRom(address, noRomBanking);

		if (IsInRange(address, VIDEO_RAM_BASE_ADDRESS, VIDEO_RAM_LAST_ADDRESS))
			return videoRam[address - VIDEO_RAM_BASE_ADDRESS];

		if (IsInRange(address, CARTRIDGE_RAM_BASE_ADDRESS, CARTRIDGE_RAM_LAST_ADDRESS))
			return ReadFromCartridgeRam(address);

		if (IsInRange(address, WORK_RAM_BASE_ADDRESS, WORK_RAM_LAST_ADDRESS))
			return workRam[address - WORK_RAM_BASE_ADDRESS];

		//Echo ram is identical to work ram
		if (IsInRange(address, ECHO_RAM_BASE_ADDRESS, ECHO_RAM_LAST_ADDRESS))
			return workRam[address - ECHO_RAM_BASE_ADDRESS];

		if (IsInRange(address, SPRITE_ATTRIBUTES_BASE_ADDRESS, SPRITE_ATTRIBUTES_LAST_ADDRESS))
			return spriteAttributes[address - SPRITE_ATTRIBUTES_BASE_ADDRESS];

		//Unused Memory / Out of Bounds
		if (IsInRange(address, UNUSED_BASE_ADDRESS, UNUSED_LAST_ADDRESS))
			return 0xFF;

		if (IsInRange(address, IO_PORTS_BASE_ADDRESS, IO_PORTS_LAST_ADDRESS))
			return ReadFromIoPorts(address);

		if (IsInRange(address, HIGH_RAM_BASE_ADDRESS, HIGH_RAM_LAST_ADDRESS))
			return highRam[address - HIGH_RAM_BASE_ADDRESS];

		if (address == INTERRUPT_ENABLE_REG_ADDRESS)
			return emulator.interrupts.InterruptEnableRegister;

		Logger.ControlledCrash($"Tried to access memory at invalid address: {address:X4}");
		return 0xFF;
	}

	private byte ReadFromCartridgeRom(ushort address, bool noRomBanking)
	{
		if (bootRomEnabled && address < 0x100) return bootRom[address];

		return noRomBanking
				   ? cartridgeRom[address]
				   : cartridgeRom[emulator.memoryBankController.ConvertRomAddress(address)];
	}

	private byte ReadFromCartridgeRam(ushort address)
	{
		if (!emulator.memoryBankController.HasRam ||
			!emulator.memoryBankController.RamEnabled) return 0xFF;

		uint addressWithBanking =
			emulator.memoryBankController.ConvertRamAddress((ushort)(address - CARTRIDGE_RAM_BASE_ADDRESS));

		return cartridgeRam[addressWithBanking];
	}

	private byte ReadFromIoPorts(ushort address)
	{
		if (IsInRange(address, 0xFF30, 0xFF3F))
			return emulator.apu.channel3.GetWaveRamSamplePair(address & 0xF);

		switch (address & 0xFF)
		{
			case 0x00:
				return emulator.joypad.JoypadRegister;
			case 0x01:
			case 0x02:
				//TODO Serial transfer
				return 0xFF;
			case 0x04:
				return emulator.timer.DividerRegister;
			case 0x05:
				return emulator.timer.TimerRegister;
			case 0x06:
				return emulator.timer.TimerModulo;
			case 0x07:
				return emulator.timer.TimerControl;
			case 0x0F:
				return emulator.interrupts.InterruptFlagRegister;
			case 0x10:
				return emulator.apu.channel1.FrequencySweepRegister;
			case 0x11:
				return emulator.apu.channel1.SoundLengthWavePatternRegister;
			case 0x12:
				return emulator.apu.channel1.VolumeEnvelopeRegister;
			case 0x13:
				//Write only
				return 0xFF;
			case 0x14:
				return emulator.apu.channel1.FrequencyRegisterHi;
			case 0x16:
				return emulator.apu.channel2.SoundLengthWavePatternRegister;
			case 0x17:
				return emulator.apu.channel2.VolumeEnvelopeRegister;
			case 0x18:
				//Write only
				return 0xFF;
			case 0x19:
				return emulator.apu.channel2.FrequencyRegisterHi;
			case 0x1A:
				return emulator.apu.channel3.SoundOnOffRegister;
			case 0x1B:
				//Write only
				return 0xFF;
			case 0x1C:
				return emulator.apu.channel3.SelectOutputLevelRegister;
			case 0x1D:
				//Write only
				return 0xFF;
			case 0x1E:
				return emulator.apu.channel3.FrequencyRegisterHi;
			case 0x20:
				//Write only
				return 0xFF;
			case 0x21:
				return emulator.apu.channel4.VolumeEnvelopeRegister;
			case 0x22:
				return emulator.apu.channel4.PolynomialCounterRegister;
			case 0x23:
				return emulator.apu.channel4.CounterConsecutiveRegister;
			case 0x24:
				return emulator.apu.ChannelControlRegister;
			case 0x25:
				return emulator.apu.SoundOutputTerminalSelectRegister;
			case 0x26:
				return emulator.apu.SoundOnOffRegister;
			case 0x40:
				return emulator.ppu.LcdControlRegister;
			case 0x41:
				return emulator.ppu.LcdStatusRegister;
			case 0x42:
				return emulator.ppu.ScrollY;
			case 0x43:
				return emulator.ppu.ScrollX;
			case 0x44:
				return emulator.ppu.CurrentScanline;
			case 0x45:
				return emulator.ppu.CurrentScanlineCompare;
			case 0x46:
				return dmaRegister;
			case 0x47:
				return emulator.ppu.TilePalette;
			case 0x48:
				return emulator.ppu.SpritePalette0;
			case 0x49:
				return emulator.ppu.SpritePalette1;
			case 0x4A:
				return emulator.ppu.WindowY;
			case 0x4B:
				return emulator.ppu.WindowX;
			case 0x4D:
				//0xFF4D is used to detect a GameBoy Color, a regular GameBoy always returns 0xFF
				return 0xFF;
			case 0x50:
				//Reading from 0xFF50 returns 1 for bits 7-1 and the boot rom state for bit 0
				return Cpu.MakeByte(true, true, true, true, true, true, true, !bootRomEnabled);
			default:
				//Unused IO ports return 0xFF
				return 0xFF;
		}
	}

	public void Write(ushort address, byte data)
	{
		//Writing to Cartridge Rom triggers Memorybanking Controller
		if (IsInRange(address, CARTRIDGE_ROM_BASE_ADDRESS, CARTRIDGE_ROM_LAST_ADDRESS))
			emulator.memoryBankController.HandleBanking(address, data);

		else if (IsInRange(address, VIDEO_RAM_BASE_ADDRESS, VIDEO_RAM_LAST_ADDRESS))
			videoRam[address - VIDEO_RAM_BASE_ADDRESS] = data;

		else if (IsInRange(address, CARTRIDGE_RAM_BASE_ADDRESS, CARTRIDGE_RAM_LAST_ADDRESS))
			WriteToCartridgeRam(address, data);

		else if (IsInRange(address, WORK_RAM_BASE_ADDRESS, WORK_RAM_LAST_ADDRESS))
			workRam[address - WORK_RAM_BASE_ADDRESS] = data;

		//Echo ram is identical to work ram
		else if (IsInRange(address, ECHO_RAM_BASE_ADDRESS, ECHO_RAM_LAST_ADDRESS))
			workRam[address - ECHO_RAM_BASE_ADDRESS] = data;

		else if (IsInRange(address, SPRITE_ATTRIBUTES_BASE_ADDRESS, SPRITE_ATTRIBUTES_LAST_ADDRESS))
			spriteAttributes[address - SPRITE_ATTRIBUTES_BASE_ADDRESS] = data;

		//Unused Memory / Out of Bounds
		else if (IsInRange(address, UNUSED_BASE_ADDRESS, UNUSED_LAST_ADDRESS))
		{
			/*Empty*/
		}

		else if (IsInRange(address, IO_PORTS_BASE_ADDRESS, IO_PORTS_LAST_ADDRESS))
			WriteToIoPorts(address, data);

		else if (IsInRange(address, HIGH_RAM_BASE_ADDRESS, HIGH_RAM_LAST_ADDRESS))
			highRam[address - HIGH_RAM_BASE_ADDRESS] = data;

		else if (address == INTERRUPT_ENABLE_REG_ADDRESS)
			emulator.interrupts.InterruptEnableRegister = data;

		else
			Logger.ControlledCrash($"Tried to access memory at invalid address: {address:X4}");
	}

	private void WriteToCartridgeRam(ushort address, byte data)
	{
		if (!emulator.memoryBankController.HasRam ||
			!emulator.memoryBankController.RamEnabled) return;

		uint addressWithBanking =
			emulator.memoryBankController.ConvertRamAddress((ushort)(address - CARTRIDGE_RAM_BASE_ADDRESS));

		cartridgeRam[addressWithBanking] = data;

		ramChangedSinceLastSave = true;
	}

	private void WriteToIoPorts(ushort address, byte data)
	{
		if (IsInRange(address, 0xFF30, 0xFF3F))
			emulator.apu.channel3.SetWaveRamSamplePair(address & 0xF, data);

		switch (address & 0xFF)
		{
			case 0x00:
				//Only bits 4 and 5 of the joypad register are writeable
				emulator.joypad.JoypadRegister =
					(byte)((emulator.joypad.JoypadRegister & 0b1100_1111) | (data & 0b0011_0000));

				break;
			case 0x01:
			case 0x02:
				//TODO Serial transfer
				break;
			case 0x04:
				//Writing to the divider register resets it
				emulator.timer.ResetDividerRegister();
				break;
			case 0x05:
				emulator.timer.TimerRegister = data;
				break;
			case 0x06:
				emulator.timer.TimerModulo = data;
				break;
			case 0x07:
				emulator.timer.TimerControl = data;
				break;
			case 0x0F:
				emulator.interrupts.InterruptFlagRegister = data;
				break;
			case 0x10:
				emulator.apu.channel1.FrequencySweepRegister = data;
				break;
			case 0x11:
				emulator.apu.channel1.SoundLengthWavePatternRegister = data;
				break;
			case 0x12:
				emulator.apu.channel1.VolumeEnvelopeRegister = data;
				break;
			case 0x13:
				emulator.apu.channel1.FrequencyRegisterLo = data;
				break;
			case 0x14:
				emulator.apu.channel1.FrequencyRegisterHi = data;
				break;
			case 0x16:
				emulator.apu.channel2.SoundLengthWavePatternRegister = data;
				break;
			case 0x17:
				emulator.apu.channel2.VolumeEnvelopeRegister = data;
				break;
			case 0x18:
				emulator.apu.channel2.FrequencyRegisterLo = data;
				break;
			case 0x19:
				emulator.apu.channel2.FrequencyRegisterHi = data;
				break;
			case 0x1A:
				emulator.apu.channel3.SoundOnOffRegister = data;
				break;
			case 0x1B:
				emulator.apu.channel3.SoundLengthRegister = data;
				break;
			case 0x1C:
				emulator.apu.channel3.SelectOutputLevelRegister = data;
				break;
			case 0x1D:
				emulator.apu.channel3.FrequencyRegisterLo = data;
				break;
			case 0x1E:
				emulator.apu.channel3.FrequencyRegisterHi = data;
				break;
			case 0x20:
				emulator.apu.channel4.SoundLengthRegister = data;
				break;
			case 0x21:
				emulator.apu.channel4.VolumeEnvelopeRegister = data;
				break;
			case 0x22:
				emulator.apu.channel4.PolynomialCounterRegister = data;
				break;
			case 0x23:
				emulator.apu.channel4.CounterConsecutiveRegister = data;
				break;
			case 0x24:
				emulator.apu.ChannelControlRegister = data;
				break;
			case 0x25:
				emulator.apu.SoundOutputTerminalSelectRegister = data;
				break;
			case 0x26:
				emulator.apu.SoundOnOffRegister = data;
				break;
			case 0x40:
				emulator.ppu.LcdControlRegister = data;
				break;
			case 0x41:
				emulator.ppu.LcdStatusRegister = data;
				break;
			case 0x42:
				emulator.ppu.ScrollY = data;
				break;
			case 0x43:
				emulator.ppu.ScrollX = data;
				break;
			case 0x45:
				emulator.ppu.CurrentScanlineCompare = data;
				break;
			case 0x46:
				dmaRegister = data;
				DirectMemoryAccess(data);
				break;
			case 0x47:
				emulator.ppu.TilePalette = data;
				break;
			case 0x48:
				emulator.ppu.SpritePalette0 = data;
				break;
			case 0x49:
				emulator.ppu.SpritePalette1 = data;
				break;
			case 0x4A:
				emulator.ppu.WindowY = data;
				break;
			case 0x4B:
				emulator.ppu.WindowX = data;
				break;
			case 0x50:
				//Writing to this address disables the boot rom
				if (bootRomEnabled && Cpu.GetBit(data, 0)) DisableBootRom();
				break;
		}
	}

	private void DirectMemoryAccess(byte sourceAddressLo)
	{
		//TODO Maybe all addresses are allowed and some are treated as echo ram?
		/*"what happens when the source address for OAM DMA is FE00?
		passing all the other mooneye oam tests including timing, just failing sources-GS"
		"it's treated as echo ram of DE00
		C000-FFFF oam dma source is always treated as wram"
		"oh so it's essentially just like an extended echo region?"
		"in most cases echo ram is only E000-FDFF. oam dma is one of the exceptions here which have the entire E000-FFFF region as echo ram for dma source"
		"i see, thanks!
		just got that test passing now"*/
		if (sourceAddressLo > 0xF1) return;

		ushort sourceAddress      = (ushort)(sourceAddressLo * 0x100);
		ushort destinationAddress = 0xFE00;

		while (destinationAddress < DMA_FINISH_ADDRESS)
			Write(destinationAddress++, Read(sourceAddress++));
	}

	public static bool IsInRange(int number, int lowerBoundInclusive, int upperBoundInclusive)
	{
		if (lowerBoundInclusive > upperBoundInclusive)
			(upperBoundInclusive, lowerBoundInclusive) = (lowerBoundInclusive, upperBoundInclusive);

		return number >= lowerBoundInclusive && number <= upperBoundInclusive;
	}
}