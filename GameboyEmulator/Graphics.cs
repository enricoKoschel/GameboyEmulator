using System;
using SFML.Graphics;

namespace GameboyEmulator
{
	class Graphics
	{
		//Modules
		private readonly Lcd    lcd;
		private readonly Screen screen;
		private readonly Memory memory;

		public Graphics(Memory memory, Interrupts interrupts)
		{
			this.memory = memory;

			screen = new Screen();
			lcd    = new Lcd(memory, screen, interrupts);
		}

		public bool IsScreenOpen => screen.IsOpen;

		public Screen GetScreen()
		{
			return screen;
		}

		public void Update(int cycles)
		{
			lcd.Update(cycles);

			if (lcd.shouldDrawScanline) DrawScanline();

			if (lcd.shouldIncreaseScanline) lcd.CurrentScanline++;
		}

		private void DrawScanline()
		{
			if (lcd.TilesEnabled) RenderTiles();

			if (lcd.SpritesEnabled) RenderSprites();
		}

		private void RenderTiles()
		{
			//Background
			int tileMapY = (lcd.CurrentScanline + lcd.ScrollY) / 8 * 32;

			//If the Screen Overflows at the bottom of the Screen, start at the top again
			tileMapY %= 0x400;

			for (int screenPixel = 0; screenPixel < 160; screenPixel++)
			{
				int tileMapX = (screenPixel + lcd.ScrollX) / 8;

				//If the Screen Overflows at the right of the Screen, start at the left again
				tileMapX %= 32;

				ushort tileMapIndex  = (ushort)(lcd.BackgroundTileMapBaseAddress + tileMapY + tileMapX);
				ushort tileDataIndex = lcd.TileDataBaseAddress;

				if (lcd.TileDataIsSigned)
					tileDataIndex += (ushort)(((sbyte)memory.Read(tileMapIndex) + 128) * 16);
				else
					tileDataIndex += (ushort)(memory.Read(tileMapIndex) * 16);

				int currentTileLine          = ((lcd.CurrentScanline + lcd.ScrollY) % 8) * 2;
				int currentTileColumn        = (screenPixel + lcd.ScrollX) % 8;
				int currentTileColumnReverse = (currentTileColumn - 7) * -1;

				byte tileDataLo = memory.Read((ushort)(tileDataIndex + currentTileLine));
				byte tileDataHi = memory.Read((ushort)(tileDataIndex + currentTileLine + 1));

				int  paletteIndexLo = Cpu.GetBit(tileDataLo, currentTileColumnReverse) ? 1 : 0;
				int  paletteIndexHi = Cpu.GetBit(tileDataHi, currentTileColumnReverse) ? 1 : 0;
				byte paletteIndex   = (byte)((paletteIndexHi << 1) | paletteIndexLo);

				int bufferXIndex = screenPixel;
				int bufferYIndex = lcd.CurrentScanline;

				screen.Buffer[bufferXIndex, bufferYIndex].FillColor =
					GetColor(lcd.TilePalette, paletteIndex);
			}

			//Window
		}

		private void RenderSprites()
		{
			for (ushort oamSpriteAddress = 0xFE00; oamSpriteAddress < 0xFEA0;)
			{
				//Using short to allow for Sprites to touch the Sides of the Screen
				short yPosition = memory.Read(oamSpriteAddress++);
				short xPosition = memory.Read(oamSpriteAddress++);

				//Check if Sprite is Off-Screen
				if (xPosition <= 0 || xPosition >= 168 || yPosition <= 0 || yPosition >= 160)
				{
					oamSpriteAddress += 2;
					continue;
				}

				xPosition -= 8;
				yPosition -= 16;

				byte tileNumber = memory.Read(oamSpriteAddress++);
				byte attributes = memory.Read(oamSpriteAddress++);

				bool usingPalette0          = !Cpu.GetBit(attributes, 4);
				bool xFlip                  = Cpu.GetBit(attributes, 5);
				bool yFlip                  = Cpu.GetBit(attributes, 6);
				bool spriteBehindBackground = Cpu.GetBit(attributes, 7);

				//Check if Sprite is visible
				if (lcd.CurrentScanline < yPosition || lcd.CurrentScanline >= yPosition + lcd.SpriteSize) continue;

				int spriteLine = lcd.CurrentScanline - yPosition;

				if (yFlip)
				{
					spriteLine -= 7;
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
						usingPalette0 ? Lcd.SPRITE_PALETTE_0_ADDRESS : Lcd.SPRITE_PALETTE_1_ADDRESS;

					int  paletteIndexLo = Cpu.GetBit(spriteDataLo, spriteDataIndex) ? 1 : 0;
					int  paletteIndexHi = Cpu.GetBit(spriteDataHi, spriteDataIndex) ? 1 : 0;
					byte paletteIndex   = (byte)((paletteIndexHi << 1) | paletteIndexLo);

					//Transparent Pixel
					if (paletteIndex == 0) continue;

					byte  palette = memory.Read(paletteAddress);
					Color color   = GetColor(palette, paletteIndex);

					int spriteDataIndexReverse = spriteDataIndex;

					if (!xFlip)
					{
						spriteDataIndexReverse -= 7;
						spriteDataIndexReverse *= -1;
					}

					int bufferXIndex = xPosition + spriteDataIndexReverse;
					int bufferYIndex = lcd.CurrentScanline;

					//Don't display Pixel if it's off the Screen
					if (bufferXIndex >= 160 || bufferXIndex < 0 || bufferYIndex >= 144 || bufferYIndex < 0) continue;

					screen.Buffer[bufferXIndex, bufferYIndex].FillColor = color;
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
				0 => Colors.white,
				1 => Colors.lightGray,
				2 => Colors.darkGray,
				_ => Colors.black
			};
		}
	}
}