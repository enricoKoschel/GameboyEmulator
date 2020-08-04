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
		private const int MODE_3_TIME = 252;

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
			get => memory.Read(0xFF4B);
			set => memory.Write(0xFF4B, value);
		}

		public byte WindowY
		{
			get => (byte)(memory.Read(0xFF4A) - 7);
			set => memory.Write(0xFF4A, (byte)(value + 7));
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
			set => Cpu.SetBit(StatusRegister, 2, value);
		}

		private bool CoincidenceInterruptEnabled
		{
			get => Cpu.GetBit(StatusRegister, 6);
			set => Cpu.SetBit(StatusRegister, 6, value);
		}

		private bool vBlankRequested = false;

		public void Update(int cycles)
		{
			drawScanlineCounter += cycles;

			shouldDrawScanline     = false;
			shouldIncreaseScanline = false;

			if (drawScanlineCounter >= 456)
			{
				//Increase Scanline every 456 Clockcycles, only draw if not in VBlank
				if (CurrentScanline < 144) shouldDrawScanline = true;

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

			interrupts.Request(Interrupts.InterruptTypes.LcdStat);
		}

		public void UpdateDisabled()
		{
			drawScanlineCounter = 0;
			CurrentScanline     = 0;
			Mode                = 1;

			screen.Clear();
			screen.DrawFrame();
		}

		private void SetStatus()
		{
			if (CurrentScanline >= 144)
			{
				//VBlank
				Mode = 1;

				if (!vBlankRequested)
				{
					interrupts.Request(Interrupts.InterruptTypes.VBlank);
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
				}
				else if (drawScanlineCounter < MODE_3_TIME)
				{
					//Accessing VRAM
					Mode = 3;
				}
				else
				{
					//HBlank
					Mode = 0;
				}
			}
		}
	}
}