using System;
using System.Collections.Generic;
using System.Linq;

namespace GameboyEmulator
{
	public class Ppu
	{
		public enum Color
		{
			Black,
			DarkGray,
			LightGray,
			White
		}

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

		//Registers
		private byte Mode
		{
			get => (byte)(LcdStatusRegister & 0b00000011);
			set
			{
				if (value > 0x3)
				{
					Logger.LogMessage("LCD Mode cannot be larger than 3!", Logger.LogLevel.Error);
					throw new ArgumentOutOfRangeException(nameof(value), "LCD Mode cannot be larger than 3!");
				}

				LcdStatusRegister &= 0b11111100;
				LcdStatusRegister |= value;
			}
		}

		public byte LcdControlRegister { get; set; }

		public byte LcdStatusRegister { get; set; }

		public byte CurrentScanline { get; private set; }

		public byte CurrentScanlineCompare { get; set; }

		public byte ScrollX { get; set; }

		public byte ScrollY { get; set; }

		public byte WindowX { get; set; }

		private short WindowXCorrected
		{
			get => (short)(WindowX - 7);
			set => WindowX = (byte)(value + 7);
		}

		public byte WindowY { get; set; }

		public byte TilePalette { get; set; }

		public byte SpritePalette0 { get; set; }

		public byte SpritePalette1 { get; set; }

		private bool CoincidenceFlag
		{
			get => Cpu.GetBit(LcdStatusRegister, 2);
			set => LcdStatusRegister = Cpu.SetBit(LcdStatusRegister, 2, value);
		}

		private bool CoincidenceInterruptEnabled
		{
			get => Cpu.GetBit(LcdStatusRegister, 6);
			set => LcdStatusRegister = Cpu.SetBit(LcdStatusRegister, 6, value);
		}

		private bool Mode2InterruptEnabled
		{
			get => Cpu.GetBit(LcdStatusRegister, 5);
			set => LcdStatusRegister = Cpu.SetBit(LcdStatusRegister, 5, value);
		}

		private bool Mode1InterruptEnabled
		{
			get => Cpu.GetBit(LcdStatusRegister, 4);
			set => LcdStatusRegister = Cpu.SetBit(LcdStatusRegister, 4, value);
		}

		private bool Mode0InterruptEnabled
		{
			get => Cpu.GetBit(LcdStatusRegister, 3);
			set => LcdStatusRegister = Cpu.SetBit(LcdStatusRegister, 3, value);
		}

		private ushort WindowTileMapBaseAddress => Cpu.GetBit(LcdControlRegister, 6) ? (ushort)0x9C00 : (ushort)0x9800;

		private ushort BackgroundTileMapBaseAddress =>
			Cpu.GetBit(LcdControlRegister, 3) ? (ushort)0x9C00 : (ushort)0x9800;

		private ushort TileDataBaseAddress => Cpu.GetBit(LcdControlRegister, 4) ? (ushort)0x8000 : (ushort)0x8800;

		private int SpriteSize => Cpu.GetBit(LcdControlRegister, 2) ? 16 : 8;

		private bool IsEnabled => Cpu.GetBit(LcdControlRegister, 7);

		private bool TilesEnabled => Cpu.GetBit(LcdControlRegister, 0);

		private bool SpritesEnabled => Cpu.GetBit(LcdControlRegister, 1);

		private bool WindowEnabled => Cpu.GetBit(LcdControlRegister, 5);

		private bool TileDataIsSigned => !Cpu.GetBit(LcdControlRegister, 4);

		//Constants
		private const int MODE_2_TIME = 80;
		private const int MODE_3_TIME = 390; //Maybe 390? Was 252 before

		private int drawScanlineCounter;

		private bool vBlankRequested;
		private bool coincidenceRequested;
		private bool mode2Requested;
		private bool mode1Requested;
		private bool mode0Requested;

		private int internalWindowCounter;

		private bool wasEnabledLastFrame;

		private readonly Emulator emulator;

		public Ppu(Emulator emulator)
		{
			this.emulator = emulator;
		}

		public void Update(int cycles)
		{
			//Reset ppu if disabled
			if (!IsEnabled)
			{
				CurrentScanline       = 0;
				drawScanlineCounter   = 0;
				internalWindowCounter = 0;

				mode0Requested       = false;
				mode1Requested       = false;
				mode2Requested       = false;
				coincidenceRequested = false;
				vBlankRequested      = false;
				Mode                 = 0;

				return;
			}

			drawScanlineCounter += cycles;

			if (drawScanlineCounter >= 456)
			{
				//Increase Scanline every 456 Clockcycles, only draw if not in VBlank
				if (CurrentScanline < 144) DrawScanline();

				coincidenceRequested = false;
				CoincidenceFlag      = false;

				CurrentScanline++;
				drawScanlineCounter -= 456; //Maybe reset to 0?
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

			emulator.interrupts.Request(Interrupts.InterruptType.LcdStat);
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
					emulator.interrupts.Request(Interrupts.InterruptType.LcdStat);
					mode1Requested = true;
				}

				if (!vBlankRequested)
				{
					emulator.interrupts.Request(Interrupts.InterruptType.VBlank);
					vBlankRequested = true;
				}

				if (CurrentScanline <= 153) return;

				vBlankRequested = false;

				//One Frame done
				CurrentScanline       = 0;
				drawScanlineCounter   = 0;
				internalWindowCounter = 0;

				wasEnabledLastFrame = IsEnabled;

				emulator.inputOutput.DrawFrame();
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

					emulator.interrupts.Request(Interrupts.InterruptType.LcdStat);
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

					emulator.interrupts.Request(Interrupts.InterruptType.LcdStat);
					mode0Requested = true;
				}
			}
		}

		private void DrawScanline()
		{
			//The ppu only updates the screen one frame after being re-enabled
			if (!wasEnabledLastFrame) return;

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
				{
					backgroundTileDataIndex +=
						(ushort)(((sbyte)emulator.memory.Read(backgroundTileMapIndex) + 128) * 16);
				}
				else
					backgroundTileDataIndex += (ushort)(emulator.memory.Read(backgroundTileMapIndex) * 16);

				int currentTileLine          = (CurrentScanline + ScrollY) % 8 * 2;
				int currentTileColumn        = (backgroundPixel + ScrollX) % 8;
				int currentTileColumnReverse = (currentTileColumn - 7) * -1;

				byte tileDataLo = emulator.memory.Read((ushort)(backgroundTileDataIndex + currentTileLine));
				byte tileDataHi = emulator.memory.Read((ushort)(backgroundTileDataIndex + currentTileLine + 1));

				int  paletteIndexLo = Cpu.GetBit(tileDataLo, currentTileColumnReverse) ? 1 : 0;
				int  paletteIndexHi = Cpu.GetBit(tileDataHi, currentTileColumnReverse) ? 1 : 0;
				byte paletteIndex   = (byte)((paletteIndexHi << 1) | paletteIndexLo);

				int bufferXIndex = backgroundPixel;
				int bufferYIndex = CurrentScanline;

				emulator.inputOutput.UpdateZBuffer(bufferXIndex, bufferYIndex, paletteIndex == 0);
				emulator.inputOutput.UpdatePixelBuffer(bufferXIndex, bufferYIndex, GetColor(TilePalette, paletteIndex));
			}

			//Window
			if (!WindowEnabled || WindowY > CurrentScanline || WindowXCorrected > 159) return;

			int windowTileMapY = internalWindowCounter++ / 8 * 32;

			for (int windowPixel = WindowXCorrected; windowPixel < 160; windowPixel++)
			{
				int windowTileMapX = (windowPixel - WindowXCorrected) / 8;

				ushort windowTileMapIndex = (ushort)(WindowTileMapBaseAddress + windowTileMapX + windowTileMapY);

				ushort windowTileDataIndex = TileDataBaseAddress;

				if (TileDataIsSigned)
					windowTileDataIndex += (ushort)(((sbyte)emulator.memory.Read(windowTileMapIndex) + 128) * 16);
				else
					windowTileDataIndex += (ushort)(emulator.memory.Read(windowTileMapIndex) * 16);

				int currentTileLine = (CurrentScanline - WindowY) % 8 * 2;

				int currentTileColumn        = (windowPixel - WindowXCorrected) % 8;
				int currentTileColumnReverse = (currentTileColumn - 7) * -1;

				byte tileDataLo = emulator.memory.Read((ushort)(windowTileDataIndex + currentTileLine));
				byte tileDataHi = emulator.memory.Read((ushort)(windowTileDataIndex + currentTileLine + 1));

				int  paletteIndexLo = Cpu.GetBit(tileDataLo, currentTileColumnReverse) ? 1 : 0;
				int  paletteIndexHi = Cpu.GetBit(tileDataHi, currentTileColumnReverse) ? 1 : 0;
				byte paletteIndex   = (byte)((paletteIndexHi << 1) | paletteIndexLo);

				int bufferXIndex = windowPixel < 0 ? 0 : windowPixel;
				int bufferYIndex = CurrentScanline;

				emulator.inputOutput.UpdateZBuffer(bufferXIndex, bufferYIndex, paletteIndex == 0);
				emulator.inputOutput.UpdatePixelBuffer(bufferXIndex, bufferYIndex, GetColor(TilePalette, paletteIndex));
			}
		}

		private void RenderSprites()
		{
			List<Sprite> sprites = new List<Sprite>();

			for (ushort i = 0; i < 40 && sprites.Count < 10; i++)
			{
				ushort oamSpriteAddress = (ushort)(0xFE00 + i * 4);
				short  yPosition        = (short)(emulator.memory.Read(oamSpriteAddress) - 16);

				//Check if Sprite is visible and on current Scanline
				if (CurrentScanline < yPosition || CurrentScanline >= yPosition + SpriteSize) continue;

				ushort spriteAddress = oamSpriteAddress++;
				short  xPosition     = (short)(emulator.memory.Read(oamSpriteAddress++) - 8);
				byte   tileNumber    = emulator.memory.Read(oamSpriteAddress++);
				byte   attributes    = emulator.memory.Read(oamSpriteAddress);

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
				byte   spriteDataLo      = emulator.memory.Read(spriteDataAddress++);
				byte   spriteDataHi      = emulator.memory.Read(spriteDataAddress);

				for (int spritePixelIndex = 7; spritePixelIndex >= 0; spritePixelIndex--)
				{
					int spriteDataIndex = spritePixelIndex;

					int  paletteIndexLo = Cpu.GetBit(spriteDataLo, spriteDataIndex) ? 1 : 0;
					int  paletteIndexHi = Cpu.GetBit(spriteDataHi, spriteDataIndex) ? 1 : 0;
					byte paletteIndex   = (byte)((paletteIndexHi << 1) | paletteIndexLo);

					//Transparent Pixel
					if (paletteIndex == 0) continue;

					Color color = GetColor(usingPalette0 ? SpritePalette0 : SpritePalette1, paletteIndex);

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
						if (!emulator.inputOutput.GetZBufferAt(bufferXIndex, bufferYIndex)) continue;
					}

					emulator.inputOutput.UpdatePixelBuffer(bufferXIndex, bufferYIndex, color);
				}
			}
		}

		private static Color GetColor(byte palette, byte paletteIndex)
		{
			int colorIdLo = paletteIndex * 2;
			int colorIdHi = colorIdLo + 1;

			int colorId = ((Cpu.GetBit(palette, colorIdHi) ? 1 : 0) << 1) |
						  (Cpu.GetBit(palette, colorIdLo) ? 1 : 0);

			return colorId switch
			{
				0 => Color.White,
				1 => Color.LightGray,
				2 => Color.DarkGray,
				_ => Color.Black
			};
		}
	}
}