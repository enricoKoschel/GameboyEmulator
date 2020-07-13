namespace GameboyEmulator
{
	class Graphics
	{
		//Modules
		private readonly Lcd    lcd;
		private readonly Screen screen;

		public Graphics(Memory memory, Cpu cpu)
		{
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
		}

		private void RenderSprites()
		{
		}
	}
}