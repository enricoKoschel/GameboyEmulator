﻿using System;
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
			int backgroundTileMapY = (lcd.CurrentScanline + lcd.ScrollY) / 8 * 32;

			//If the Screen Overflows at the bottom of the Screen, start at the top again
			backgroundTileMapY %= 0x400;

			for (int backgroundPixel = 0; backgroundPixel < 160; backgroundPixel++)
			{
				int backgroundTileMapX = (backgroundPixel + lcd.ScrollX) / 8;

				//If the Screen Overflows at the right of the Screen, start at the left again
				backgroundTileMapX %= 32;

				ushort backgroundTileMapIndex =
					(ushort)(lcd.BackgroundTileMapBaseAddress + backgroundTileMapY + backgroundTileMapX);

				ushort backgroundTileDataIndex = lcd.TileDataBaseAddress;

				if (lcd.TileDataIsSigned)
					backgroundTileDataIndex += (ushort)(((sbyte)memory.Read(backgroundTileMapIndex) + 128) * 16);
				else
					backgroundTileDataIndex += (ushort)(memory.Read(backgroundTileMapIndex) * 16);

				int currentTileLine          = ((lcd.CurrentScanline + lcd.ScrollY) % 8) * 2;
				int currentTileColumn        = (backgroundPixel + lcd.ScrollX) % 8;
				int currentTileColumnReverse = (currentTileColumn - 7) * -1;

				byte tileDataLo = memory.Read((ushort)(backgroundTileDataIndex + currentTileLine));
				byte tileDataHi = memory.Read((ushort)(backgroundTileDataIndex + currentTileLine + 1));

				int  paletteIndexLo = Cpu.GetBit(tileDataLo, currentTileColumnReverse) ? 1 : 0;
				int  paletteIndexHi = Cpu.GetBit(tileDataHi, currentTileColumnReverse) ? 1 : 0;
				byte paletteIndex   = (byte)((paletteIndexHi << 1) | paletteIndexLo);

				int bufferXIndex = backgroundPixel;
				int bufferYIndex = lcd.CurrentScanline;

				screen.UpdateZBuffer(bufferXIndex, bufferYIndex, paletteIndex == 0);
				screen.UpdatePixelBuffer(bufferXIndex, bufferYIndex, GetColor(lcd.TilePalette, paletteIndex));
			}

			//Window
			if (!lcd.WindowEnabled || lcd.WindowY > lcd.CurrentScanline) return;

			int windowTileMapY = (lcd.CurrentScanline - lcd.WindowY) / 8 * 32;

			for (int windowPixel = lcd.WindowX; windowPixel < 160; windowPixel++)
			{
				int windowTileMapX = (windowPixel - lcd.WindowX) / 8;

				ushort windowTileMapIndex = (ushort)(lcd.WindowTileMapBaseAddress + windowTileMapX + windowTileMapY);

				ushort windowTileDataIndex = lcd.TileDataBaseAddress;

				if (lcd.TileDataIsSigned)
					windowTileDataIndex += (ushort)(((sbyte)memory.Read(windowTileMapIndex) + 128) * 16);
				else
					windowTileDataIndex += (ushort)(memory.Read(windowTileMapIndex) * 16);

				int currentTileLine          = ((lcd.CurrentScanline - lcd.WindowY) % 8) * 2;
				int currentTileColumn        = (windowPixel - lcd.WindowX) % 8;
				int currentTileColumnReverse = (currentTileColumn - 7) * -1;

				byte tileDataLo = memory.Read((ushort)(windowTileDataIndex + currentTileLine));
				byte tileDataHi = memory.Read((ushort)(windowTileDataIndex + currentTileLine + 1));

				int  paletteIndexLo = Cpu.GetBit(tileDataLo, currentTileColumnReverse) ? 1 : 0;
				int  paletteIndexHi = Cpu.GetBit(tileDataHi, currentTileColumnReverse) ? 1 : 0;
				byte paletteIndex   = (byte)((paletteIndexHi << 1) | paletteIndexLo);

				int bufferXIndex = windowPixel;
				int bufferYIndex = lcd.CurrentScanline;

				screen.UpdateZBuffer(bufferXIndex, bufferYIndex, paletteIndex == 0);
				screen.UpdatePixelBuffer(bufferXIndex, bufferYIndex, GetColor(lcd.TilePalette, paletteIndex));
			}
		}

		private void RenderSprites()
		{
			int numberOfSpritesThisLine = 0;

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

				//Only 10 Sprites per Scanline
				if (++numberOfSpritesThisLine > 10) return;

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

					byte                palette = memory.Read(paletteAddress);
					SFML.Graphics.Color color   = GetColor(palette, paletteIndex);

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