using SFML.Graphics;

namespace GameboyEmulator
{
	class Graphics
	{
		//Modules
		private readonly Lcd    lcd;
		private readonly Screen screen;
		private readonly Memory memory;

		public Graphics(Memory memory, Cpu cpu)
		{
			this.memory = memory;

			screen = new Screen();
			lcd    = new Lcd(memory, cpu, screen);
		}

		public bool IsScreenOpen => screen.IsOpen;

		public void Update(int cycles)
		{
			if (!lcd.IsEnabled) return;

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
			int tileMapY = (lcd.CurrentScanline + lcd.ScrollY) / 8;

			for (int tileMapX = lcd.ScrollX; tileMapX < lcd.ScrollX + 20; tileMapX++)
			{
				ushort tileMapIndex  = (ushort)(lcd.BackgroundTileMapBaseAddress + tileMapY * 32 + tileMapX);
				ushort tileDataIndex = lcd.TileDataBaseAddress;

				if (lcd.TileDataIsSigned)
					tileDataIndex += (byte)((sbyte)memory.Read(tileMapIndex) + 128);
				else
					tileDataIndex += memory.Read(tileMapIndex);

				byte currentTileLine = (byte)(lcd.CurrentScanline % 8);
				byte tileDataLo      = memory.Read((ushort)(tileDataIndex + currentTileLine));
				byte tileDataHi      = memory.Read((ushort)(tileDataIndex + currentTileLine + 1));

				for (int tilePixelIndex = 0; tilePixelIndex < 8; tilePixelIndex++)
				{
					int paletteIndexHi = Cpu.GetBit(tileDataHi, tilePixelIndex) ? 1 : 0;
					int paletteIndexLo = Cpu.GetBit(tileDataLo, tilePixelIndex) ? 1 : 0;
					byte paletteIndex   = (byte)((paletteIndexHi << 1) | paletteIndexLo);

					screen.Buffer[(tileMapX - lcd.ScrollX) * 8 + tilePixelIndex, lcd.CurrentScanline].FillColor =
						GetColor(lcd.TilePalette, paletteIndex);
				}
			}

			//Window
		}

		private void RenderSprites()
		{
		}

		private Color GetColor(byte palette, byte paletteIndex)
		{
			int colorIdLo = paletteIndex * 2;
			int colorIdHi = colorIdLo + 1;

			int colorId = (colorIdHi << 1) | colorIdLo;

			return colorId switch
			{
				0 => Colors.white,
				1 => Colors.lightGray,
				2 => Colors.darkGray,
				_ => Color.Black
			};
		}
	}
}