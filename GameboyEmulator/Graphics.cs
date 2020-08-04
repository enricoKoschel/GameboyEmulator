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

			for (int tileMapX = lcd.ScrollX; tileMapX < lcd.ScrollX + 20; tileMapX++)
			{
				//If Overflow at the bottom of Screen, start at the Top again
				tileMapY %= 0x400;

				ushort tileMapIndex  = (ushort)(lcd.BackgroundTileMapBaseAddress + tileMapY + tileMapX);
				ushort tileDataIndex = lcd.TileDataBaseAddress;

				if (lcd.TileDataIsSigned)
					tileDataIndex += (ushort)(((sbyte)memory.Read(tileMapIndex) + 128) * 16);
				else
					tileDataIndex += (ushort)(memory.Read(tileMapIndex) * 16);

				byte currentTileLine = (byte)(((lcd.CurrentScanline + lcd.ScrollY) % 8) * 2);
				byte tileDataLo      = memory.Read((ushort)(tileDataIndex + currentTileLine));
				byte tileDataHi      = memory.Read((ushort)(tileDataIndex + currentTileLine + 1));

				for (int tilePixelIndex = 0; tilePixelIndex < 8; tilePixelIndex++)
				{
					int  paletteIndexLo = Cpu.GetBit(tileDataLo, tilePixelIndex) ? 1 : 0;
					int  paletteIndexHi = Cpu.GetBit(tileDataHi, tilePixelIndex) ? 1 : 0;
					byte paletteIndex   = (byte)((paletteIndexHi << 1) | paletteIndexLo);

					int tilePixelIndexReverse = tilePixelIndex - 7;
					tilePixelIndexReverse *= -1;
					int bufferXIndex = (tileMapX - lcd.ScrollX) * 8 + tilePixelIndexReverse;
					int bufferYIndex = lcd.CurrentScanline;

					screen.Buffer[bufferXIndex, bufferYIndex].FillColor =
						GetColor(lcd.TilePalette, paletteIndex);
				}
			}

			//Window
		}

		private void RenderSprites()
		{
			for (ushort oamSpriteAddress = 0xFE00; oamSpriteAddress < 0xFEA0;)
			{
				byte yPosition  = (byte)(memory.Read(oamSpriteAddress++) - 16);
				byte xPosition  = (byte)(memory.Read(oamSpriteAddress++) - 8);
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
					if (xFlip)
					{
						spriteDataIndex -= 7;
						spriteDataIndex *= -1;
					}

					ushort paletteAddress =
						usingPalette0 ? Lcd.SPRITE_PALETTE_0_ADDRESS : Lcd.SPRITE_PALETTE_1_ADDRESS;

					int  paletteIndexLo = Cpu.GetBit(spriteDataLo, spriteDataIndex) ? 1 : 0;
					int  paletteIndexHi = Cpu.GetBit(spriteDataHi, spriteDataIndex) ? 1 : 0;
					byte paletteIndex   = (byte)((paletteIndexHi << 1) | paletteIndexLo);

					//Transparent Pixel
					if(paletteIndex == 0) continue;

					byte  palette = memory.Read(paletteAddress);
					Color color   = GetColor(palette, paletteIndex);

					int spriteDataIndexReverse = spriteDataIndex - 7;
					spriteDataIndexReverse *= -1;

					int bufferXIndex = xPosition + spriteDataIndexReverse;
					int bufferYIndex = lcd.CurrentScanline;
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