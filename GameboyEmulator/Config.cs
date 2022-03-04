﻿using System;
using System.Globalization;
using System.IO;
using IniParser;

namespace GameboyEmulator
{
	public static class Config
	{
		private const string CONFIG_FILE_PATH = "settings.ini";

		private static readonly IniData CONFIG_DATA;

		static Config()
		{
			if (!File.Exists(CONFIG_FILE_PATH)) CreateDefaultConfigFile();

			CONFIG_DATA = new IniDataParser().Parse(File.ReadAllText(CONFIG_FILE_PATH));
		}

		public static int GetControlConfig(string key)
		{
			string value = CONFIG_DATA["Controls"][key];

			bool validInt = Int32.TryParse(value, out int output);

			//Ensures a valid key was provided by the ini file
			if (validInt && (Memory.IsInRange(output, 65, 90)   //A-Z
						  || Memory.IsInRange(output, 48, 57)   //0-9
						  || output == 27                       //Esc
						  || output == 17                       //Ctrl
						  || output == 16                       //Shift
						  || output == 18                       //Alt
						  || output == 32                       //Space
						  || output == 13                       //Enter
						  || output == 8                        //Backspace
						  || output == 9                        //Tab
						  || output == 33                       //PageUp
						  || output == 34                       //PageDown
						  || output == 35                       //End
						  || output == 36                       //Home
						  || output == 45                       //Insert
						  || output == 46                       //Delete
						  || output == 107                      //NumpadAdd
						  || output == 109                      //NumpadSubtract
						  || output == 106                      //NumpadMultiply
						  || output == 111                      //NumpadDivide
						  || Memory.IsInRange(output, 37, 40)   //Arrow keys
						  || Memory.IsInRange(output, 96, 105)  //Numpad0-Numpad9
						  || Memory.IsInRange(output, 112, 123) //F1-F12
						  || output == 19                       //Pause
							)) return output;

			return -1;
		}

		public static int GetColorConfig(string color)
		{
			string value = CONFIG_DATA["Colors"][color];

			bool validInt = Int32.TryParse(value, NumberStyles.HexNumber, null, out int output);

			if (validInt && output <= 0xFFFFFF) return output;

			return -1;
		}

		public static string GetRomConfig(string rom)
		{
			return CONFIG_DATA["Roms"][rom];
		}

		private static void CreateDefaultConfigFile()
		{
			const string defaultConfig = @";Uses JavaScript Keycodes (https://keycode.info)
;Supported Keys are:
;A-Z, 0-9, Esc, LCtrl, LShift, LAlt, Space, Enter, Backspace,
;Tab, PageUp, PageDown, End, Home, Insert, Delete,  NumpadAdd, NumpadSubtract, NumpadMultiply,
;NumDivide, Arrow keys, Numpad0-Numpad9, F1-F12, Pause
[Controls]
;Default:
;UP = 38 (Up arrow)
;DOWN = 40 (Down arrow)
;LEFT = 37 (Left arrow)
;RIGHT = 39 (Right arrow)
;START = 13 (Enter)
;SELECT = 32 (Space)
;A = 83 (S)
;B = 65 (A)
;SPEED = 16 (Shift)
;PAUSE = 17 (LCtrl)

UP = 38
DOWN = 40
LEFT = 37
RIGHT = 39
START = 13
SELECT = 32
A = 83
B = 65
SPEED = 16
PAUSE = 17

;Uses HEX color values WITHOUT the leading # (123456)
[Colors]
;Default:
;BLACK = 081820
;DARK_GRAY = 346856
;LIGHT_GRAY = 88c070
;WHITE = e0f8d0

BLACK = 081820
DARK_GRAY = 346856
LIGHT_GRAY = 88c070
WHITE = e0f8d0

;The roms provided by the console parameters have priority over these settings
[Roms]
;Default:
;GAME =
;BOOT =
GAME =
BOOT =";

			File.WriteAllText(CONFIG_FILE_PATH, defaultConfig);
		}
	}
}