using System;

namespace GameboyEmulator
{
	class Lcd
	{
		//Modules
		private readonly Memory memory;
		private readonly Cpu    cpu;
		private readonly Screen screen;

		public Lcd(Memory memory, Cpu cpu, Screen screen)
		{
			this.memory = memory;
			this.cpu    = cpu;
			this.screen = screen;
		}

		//Constants
		private const int MODE_2_TIME = 80;
		private const int MODE_3_TIME = 252;

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

		public ushort WindowTileMapBaseAddress => cpu.GetBit(ControlRegister, 6) ? (ushort)0x9C00 : (ushort)0x9800;

		public ushort BackgroundTileMapBaseAddress => cpu.GetBit(ControlRegister, 3) ? (ushort)0x9C00 : (ushort)0x9800;

		public ushort TileDataBaseAddress => cpu.GetBit(ControlRegister, 4) ? (ushort)0x8000 : (ushort)0x8800;

		//Flags
		public bool IsEnabled => cpu.GetBit(ControlRegister, 7);

		public bool TilesEnabled => cpu.GetBit(ControlRegister, 0);

		public bool SpritesEnabled => cpu.GetBit(ControlRegister, 1);

		public bool WindowEnabled => cpu.GetBit(ControlRegister, 5);

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
		}

		private void SetStatus()
		{
			//TODO - Maybe add this
			//if (!lcdEnabled())
			//{
			//    scanlineCounter = 456;
			//    mainMem[SCANLINE_REG] = 0;
			//    status = (status & 0b11111100) | 1;
			//    writeMem(LCD_STATUS_REG, status);
			//    return;
			//}

			//TODO - Interrupts  

			if (CurrentScanline >= 144)
			{
				//VBlank
				Mode = 1;

				if (CurrentScanline <= 153) return;

				//One Frame done
				CurrentScanline     = 0;
				drawScanlineCounter = 0;

				screen.Draw();
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