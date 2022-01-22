using System;

namespace GameboyEmulator
{
	class Lcd
	{
		//Modules
		private readonly Memory     memory;
		private readonly Screen     screen;
		private readonly Interrupts interrupts;

		public Lcd(Memory memory, Screen screen, Interrupts interrupts)
		{
			this.memory     = memory;
			this.screen     = screen;
			this.interrupts = interrupts;
		}

		//Constants
		private const int MODE_2_TIME = 80;
		private const int MODE_3_TIME = 390; //Maybe 390? Was 252 before

		public const ushort SPRITE_PALETTE_0_ADDRESS = 0xFF48;
		public const ushort SPRITE_PALETTE_1_ADDRESS = 0xFF49;

		private int  drawScanlineCounter;
		public  bool shouldDrawScanline;
		public  bool shouldIncreaseScanline;

		//Registers
		private byte Mode
		{
			get => (byte)(StatusRegister & 0b00000011);
			set
			{
				if (value > 0x3)
					throw new ArgumentOutOfRangeException(nameof(value), "LCD Mode cannot be larger than 3!");

				StatusRegister &= 0b11111100;
				StatusRegister |= value;
			}
		}

		private byte ControlRegister
		{
			get => memory.Read(0xFF40);
			set => memory.Write(0xFF40, value);
		}

		private byte StatusRegister
		{
			get => memory.Read(0xFF41);
			set => memory.Write(0xFF41, value);
		}

		public byte CurrentScanline
		{
			get => memory.Read(0xFF44);
			set => memory.Write(0xFF44, value, true);
		}

		private byte CurrentScanlineCompare
		{
			get => memory.Read(0xFF45);
			set => memory.Write(0xFF45, value, true);
		}

		public byte ScrollX
		{
			get => memory.Read(0xFF43);
			set => memory.Write(0xFF43, value);
		}

		public byte ScrollY
		{
			get => memory.Read(0xFF42);
			set => memory.Write(0xFF42, value);
		}

		public byte WindowX
		{
			get => (byte)(memory.Read(0xFF4B) - 7);
			set => memory.Write(0xFF4B, (byte)(value + 7));
		}

		public byte WindowY
		{
			get => memory.Read(0xFF4A);
			set => memory.Write(0xFF4A, value);
		}

		public ushort WindowTileMapBaseAddress => Cpu.GetBit(ControlRegister, 6) ? (ushort)0x9C00 : (ushort)0x9800;

		public ushort BackgroundTileMapBaseAddress => Cpu.GetBit(ControlRegister, 3) ? (ushort)0x9C00 : (ushort)0x9800;

		public ushort TileDataBaseAddress => Cpu.GetBit(ControlRegister, 4) ? (ushort)0x8000 : (ushort)0x8800;

		public byte TilePalette => memory.Read(0xFF47);

		public int SpriteSize => Cpu.GetBit(ControlRegister, 2) ? 16 : 8;

		//Flags
		public bool IsEnabled => Cpu.GetBit(ControlRegister, 7);

		public bool TilesEnabled => Cpu.GetBit(ControlRegister, 0);

		public bool SpritesEnabled => Cpu.GetBit(ControlRegister, 1);

		public bool WindowEnabled => Cpu.GetBit(ControlRegister, 5);

		public bool TileDataIsSigned => !Cpu.GetBit(ControlRegister, 4);

		private bool CoincidenceFlag
		{
			get => Cpu.GetBit(StatusRegister, 2);
			set => StatusRegister = Cpu.SetBit(StatusRegister, 2, value);
		}

		private bool CoincidenceInterruptEnabled
		{
			get => Cpu.GetBit(StatusRegister, 6);
			set => StatusRegister = Cpu.SetBit(StatusRegister, 6, value);
		}

		private bool Mode2InterruptEnabled
		{
			get => Cpu.GetBit(StatusRegister, 5);
			set => StatusRegister = Cpu.SetBit(StatusRegister, 5, value);
		}

		private bool Mode1InterruptEnabled
		{
			get => Cpu.GetBit(StatusRegister, 4);
			set => StatusRegister = Cpu.SetBit(StatusRegister, 4, value);
		}

		private bool Mode0InterruptEnabled
		{
			get => Cpu.GetBit(StatusRegister, 3);
			set => StatusRegister = Cpu.SetBit(StatusRegister, 3, value);
		}

		private bool vBlankRequested;
		private bool coincidenceRequested;
		private bool mode2Requested;
		private bool mode1Requested;
		private bool mode0Requested;

		public void Update(int cycles)
		{
			drawScanlineCounter += cycles;

			shouldDrawScanline     = false;
			shouldIncreaseScanline = false;

			if (drawScanlineCounter >= 456)
			{
				//Increase Scanline every 456 Clockcycles, only draw if not in VBlank
				if (CurrentScanline < 144) shouldDrawScanline = true;

				coincidenceRequested = false;
				CoincidenceFlag      = false;

				shouldIncreaseScanline = true;
				drawScanlineCounter    = 0;
			}

			SetStatus();
			CompareLyWithLyc();
		}

		private void CompareLyWithLyc()
		{
			if (CurrentScanline != CurrentScanlineCompare) return;

			CoincidenceFlag = true;

			if (!CoincidenceInterruptEnabled) return;

			if (coincidenceRequested) return;

			interrupts.Request(Interrupts.InterruptType.LcdStat);
			coincidenceRequested = true;
		}

		private void SetStatus()
		{
			if (CurrentScanline >= 144)
			{
				//VBlank
				Mode = 1;

				//TODO Stat Interrupts still untested
				mode2Requested = false;
				mode0Requested = false;

				if (Mode1InterruptEnabled && !mode1Requested)
				{
					interrupts.Request(Interrupts.InterruptType.LcdStat);
					mode1Requested = true;
				}

				if (!vBlankRequested)
				{
					interrupts.Request(Interrupts.InterruptType.VBlank);
					vBlankRequested = true;
				}

				if (CurrentScanline <= 153) return;

				vBlankRequested = false;

				//One Frame done
				CurrentScanline     = 0;
				drawScanlineCounter = 0;

				screen.DrawFrame();
			}
			else
			{
				if (drawScanlineCounter < MODE_2_TIME)
				{
					//Accessing OAM
					Mode = 2;

					//TODO Stat Interrupts still untested
					mode1Requested = false;
					mode0Requested = false;

					if (!Mode2InterruptEnabled || mode2Requested) return;

					interrupts.Request(Interrupts.InterruptType.LcdStat);
					mode2Requested = true;
				}
				else if (drawScanlineCounter < MODE_3_TIME)
				{
					//Accessing VRAM
					Mode = 3;

					mode0Requested = false;
					mode1Requested = false;
					mode2Requested = false;
				}
				else
				{
					//HBlank
					Mode = 0;

					//TODO Stat Interrupts still untested
					mode2Requested = false;
					mode1Requested = false;

					if (!Mode0InterruptEnabled || mode0Requested) return;

					interrupts.Request(Interrupts.InterruptType.LcdStat);
					mode0Requested = true;
				}
			}
		}
	}
}