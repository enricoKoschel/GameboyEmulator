using System;
using System.Collections.Generic;
using System.Linq;

namespace GameboyEmulator
{
	public class Ppu
	{
		private readonly struct Sprite
		{
			public readonly ushort oamAddress;
			public readonly short  x;
			public readonly short  y;
			public readonly byte   tileNumber;
			public readonly byte   attributes;

			public Sprite(ushort oamAddress, short x, short y, byte tileNumber, byte attributes)
			{
				this.oamAddress = oamAddress;

				//Using short to allow for Sprites to touch the Sides of the Screen
				this.x = x;
				this.y = y;

				this.tileNumber = tileNumber;
				this.attributes = attributes;
			}
		}

		//Constants
		private const int MODE_2_TIME = 80;
		private const int MODE_3_TIME = 390; //Maybe 390? Was 252 before

		public const ushort SPRITE_PALETTE_0_ADDRESS = 0xFF48;
		public const ushort SPRITE_PALETTE_1_ADDRESS = 0xFF49;

		private int drawScanlineCounter;

		//Registers
		private byte Mode
		{
			get => (byte)(StatusRegister & 0b00000011);
			set
			{
				if (value > 0x3)
				{
					Logger.LogMessage("LCD Mode cannot be larger than 3!", Logger.LogLevel.Error);
					throw new ArgumentOutOfRangeException(nameof(value), "LCD Mode cannot be larger than 3!");
				}

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

		private readonly Emulator emulator;

		public Ppu(Emulator emulator)
		{
			this.emulator = emulator;
		}

		private int internalWindowCounter;

		public void Update(int cycles)
		{
			bool shouldDrawScanline     = false;
			bool shouldIncreaseScanline = false;

			drawScanlineCounter += cycles;

			if (drawScanlineCounter >= 456)
			{
				//Increase Scanline every 456 Clockcycles, only draw if not in VBlank
				if (CurrentScanline < 144) shouldDrawScanline = true;

				coincidenceRequested = false;
				CoincidenceFlag      = false;

				shouldIncreaseScanline = true;
				drawScanlineCounter    = 0; //TODO maybe add remaining drawScanlineCounter above 456
			}

			SetStatus();
			CompareLyWithLyc();

			if (shouldDrawScanline) DrawScanline();

			//Reset window counter at the beginning of every frame
			if (shouldIncreaseScanline && CurrentScanline++ == 0) internalWindowCounter = 0;

			//TODO maybe remove the two bools and inline if bodies
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

		public void DrawScanline()
		{
			if (TilesEnabled) RenderTiles();
			if (SpritesEnabled) RenderSprites();
		}

		private void RenderTiles()
		{
			//Background
			int backgroundTileMapY = (CurrentScanline + ScrollY) / 8 * 32;

			//If the Screen Overflows at the bottom of the Screen, start at the top again
			backgroundTileMapY %= 0x400;

			for (int backgroundPixel = 0; backgroundPixel < 160; backgroundPixel++)
			{
				int backgroundTileMapX = (backgroundPixel + ScrollX) / 8;

				//If the Screen Overflows at the right of the Screen, start at the left again
				backgroundTileMapX %= 32;

				ushort backgroundTileMapIndex =
					(ushort)(BackgroundTileMapBaseAddress + backgroundTileMapY + backgroundTileMapX);

				ushort backgroundTileDataIndex = TileDataBaseAddress;

				if (TileDataIsSigned)
					backgroundTileDataIndex += (ushort)(((sbyte)memory.Read(backgroundTileMapIndex) + 128) * 16);
				else
					backgroundTileDataIndex += (ushort)(memory.Read(backgroundTileMapIndex) * 16);

				int currentTileLine          = (CurrentScanline + ScrollY) % 8 * 2;
				int currentTileColumn        = (backgroundPixel + ScrollX) % 8;
				int currentTileColumnReverse = (currentTileColumn - 7) * -1;

				byte tileDataLo = memory.Read((ushort)(backgroundTileDataIndex + currentTileLine));
				byte tileDataHi = memory.Read((ushort)(backgroundTileDataIndex + currentTileLine + 1));

				int  paletteIndexLo = Cpu.GetBit(tileDataLo, currentTileColumnReverse) ? 1 : 0;
				int  paletteIndexHi = Cpu.GetBit(tileDataHi, currentTileColumnReverse) ? 1 : 0;
				byte paletteIndex   = (byte)((paletteIndexHi << 1) | paletteIndexLo);

				int bufferXIndex = backgroundPixel;
				int bufferYIndex = CurrentScanline;

				screen.UpdateZBuffer(bufferXIndex, bufferYIndex, paletteIndex == 0);
				screen.UpdatePixelBuffer(bufferXIndex, bufferYIndex, GetColor(TilePalette, paletteIndex));
			}

			//Window
			if (!WindowEnabled || WindowY > CurrentScanline || WindowX > 159) return;

			int windowTileMapY = internalWindowCounter++ / 8 * 32;

			for (int windowPixel = WindowX; windowPixel < 160; windowPixel++)
			{
				int windowTileMapX = (windowPixel - WindowX) / 8;

				ushort windowTileMapIndex = (ushort)(WindowTileMapBaseAddress + windowTileMapX + windowTileMapY);

				ushort windowTileDataIndex = TileDataBaseAddress;

				if (TileDataIsSigned)
					windowTileDataIndex += (ushort)(((sbyte)memory.Read(windowTileMapIndex) + 128) * 16);
				else
					windowTileDataIndex += (ushort)(memory.Read(windowTileMapIndex) * 16);

				int currentTileLine          = (CurrentScanline - WindowY) % 8 * 2;
				int currentTileColumn        = (windowPixel - WindowX) % 8;
				int currentTileColumnReverse = (currentTileColumn - 7) * -1;

				byte tileDataLo = memory.Read((ushort)(windowTileDataIndex + currentTileLine));
				byte tileDataHi = memory.Read((ushort)(windowTileDataIndex + currentTileLine + 1));

				int  paletteIndexLo = Cpu.GetBit(tileDataLo, currentTileColumnReverse) ? 1 : 0;
				int  paletteIndexHi = Cpu.GetBit(tileDataHi, currentTileColumnReverse) ? 1 : 0;
				byte paletteIndex   = (byte)((paletteIndexHi << 1) | paletteIndexLo);

				int bufferXIndex = windowPixel;
				int bufferYIndex = CurrentScanline;

				screen.UpdateZBuffer(bufferXIndex, bufferYIndex, paletteIndex == 0);
				screen.UpdatePixelBuffer(bufferXIndex, bufferYIndex, GetColor(TilePalette, paletteIndex));
			}
		}

		private void RenderSprites()
		{
			List<Sprite> sprites = new List<Sprite>();

			for (ushort i = 0; i < 40 && sprites.Count < 10; i++)
			{
				ushort oamSpriteAddress = (ushort)(0xFE00 + i * 4);
				short  yPosition        = (short)(memory.Read(oamSpriteAddress) - 16);

				//Check if Sprite is visible and on current Scanline
				if (CurrentScanline < yPosition || CurrentScanline >= yPosition + SpriteSize) continue;

				ushort spriteAddress = oamSpriteAddress++;
				short  xPosition     = (short)(memory.Read(oamSpriteAddress++) - 8);
				byte   tileNumber    = memory.Read(oamSpriteAddress++);
				byte   attributes    = memory.Read(oamSpriteAddress);

				if (SpriteSize == 16) tileNumber &= 0xFE;

				sprites.Add(new Sprite(spriteAddress, xPosition, yPosition, tileNumber, attributes));
			}

			//Sort Sprites by x-coordinate in descending order
			//If Sprites have the same x-coordinate, they are sorted by the Address in OAM in descending order
			sprites = sprites.OrderByDescending(s => s.x).ThenByDescending(s => s.oamAddress).ToList();

			//Draw Sprites
			foreach (Sprite sprite in sprites)
			{
				short xPosition  = sprite.x;
				short yPosition  = sprite.y;
				byte  tileNumber = sprite.tileNumber;
				byte  attributes = sprite.attributes;

				bool usingPalette0          = !Cpu.GetBit(attributes, 4);
				bool xFlip                  = Cpu.GetBit(attributes, 5);
				bool yFlip                  = Cpu.GetBit(attributes, 6);
				bool spriteBehindBackground = Cpu.GetBit(attributes, 7);

				int spriteLine = CurrentScanline - yPosition;

				if (yFlip)
				{
					spriteLine -= SpriteSize - 1;
					spriteLine *= -1;
				}

				spriteLine *= 2;

				ushort spriteDataAddress = (ushort)(0x8000 + spriteLine + tileNumber * 16);
				byte   spriteDataLo      = memory.Read(spriteDataAddress++);
				byte   spriteDataHi      = memory.Read(spriteDataAddress);

				for (int spritePixelIndex = 7; spritePixelIndex >= 0; spritePixelIndex--)
				{
					int spriteDataIndex = spritePixelIndex;

					ushort paletteAddress =
						usingPalette0 ? SPRITE_PALETTE_0_ADDRESS : SPRITE_PALETTE_1_ADDRESS;

					int  paletteIndexLo = Cpu.GetBit(spriteDataLo, spriteDataIndex) ? 1 : 0;
					int  paletteIndexHi = Cpu.GetBit(spriteDataHi, spriteDataIndex) ? 1 : 0;
					byte paletteIndex   = (byte)((paletteIndexHi << 1) | paletteIndexLo);

					//Transparent Pixel
					if (paletteIndex == 0) continue;

					byte                palette = memory.Read(paletteAddress);
					SFML.Graphics.Color color   = GetColor(palette, paletteIndex);

					int spriteDataIndexReverse = spriteDataIndex;

					if (!xFlip)
					{
						spriteDataIndexReverse -= 7;
						spriteDataIndexReverse *= -1;
					}

					int bufferXIndex = xPosition + spriteDataIndexReverse;
					int bufferYIndex = CurrentScanline;

					//Don't display Pixel if it's off the Screen
					if (bufferXIndex >= 160 || bufferXIndex < 0 || bufferYIndex >= 144) continue;

					if (spriteBehindBackground)
					{
						//Sprite only shows if BG Color is 0
						if (!screen.GetZBufferAt(bufferXIndex, bufferYIndex)) continue;
					}

					screen.UpdatePixelBuffer(bufferXIndex, bufferYIndex, color);
				}
			}
		}

		private static SFML.Graphics.Color GetColor(byte palette, byte paletteIndex)
		{
			int colorIdLo = paletteIndex * 2;
			int colorIdHi = colorIdLo + 1;

			int colorId = ((Cpu.GetBit(palette, colorIdHi) ? 1 : 0) << 1) |
						  (Cpu.GetBit(palette, colorIdLo) ? 1 : 0);

			return colorId switch
			{
				0 => Color.white,
				1 => Color.lightGray,
				2 => Color.darkGray,
				_ => Color.black
			};
		}
	}
}