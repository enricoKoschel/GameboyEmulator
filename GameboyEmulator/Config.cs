using System;
using System.Globalization;
using System.IO;
using IniParser;

namespace GameboyEmulator;

public static class Config
{
	private const string CONFIG_FILE_PATH = "settings.ini";

	private static readonly IniData CONFIG_DATA;

	static Config()
	{
		if (!File.Exists(CONFIG_FILE_PATH)) CreateDefaultConfigFile();

		CONFIG_DATA = new IniDataParser().Parse(File.ReadAllText(CONFIG_FILE_PATH));
	}

	public static string GetControlConfig(string key)
	{
		return CONFIG_DATA["Controls"][key].ToUpper();
	}

	public static int GetColorConfig(string color)
	{
		string value = CONFIG_DATA["Colors"][color];

		bool validInt = Int32.TryParse(value, NumberStyles.HexNumber, null, out int output);

		if (validInt && output <= 0xFFFFFF) return output;

		return -1;
	}

	public static string GetSaveLocationConfig()
	{
		return CONFIG_DATA["Saving"]["LOCATION"];
	}

	public static bool GetSaveEnabledConfig()
	{
		string value = CONFIG_DATA["Saving"]["ENABLE"];

		bool validBool = Boolean.TryParse(value, out bool output);

		return !validBool || output;
	}

	public static string GetLogLocationConfig()
	{
		return CONFIG_DATA["Logging"]["LOCATION"];
	}

	public static bool GetLogEnabledConfig()
	{
		string value = CONFIG_DATA["Logging"]["ENABLE"];

		bool validBool = Boolean.TryParse(value, out bool output);

		return !validBool || output;
	}

	public static string GetRomConfig(string rom)
	{
		return CONFIG_DATA["Roms"][rom];
	}

	private static void CreateDefaultConfigFile()
	{
		const string defaultConfig = @"[Controls]
;Supported Keys are:
;A-Z, 0-9, Esc, LCtrl, LShift, LAlt, Space, Enter, Backspace,
;Tab, PageUp, PageDown, End, Home, Insert, Delete,  NumpadAdd, NumpadSubtract, NumpadMultiply,
;NumpadDivide, Arrow keys, Numpad0-Numpad9, F1-F12, Pause

;Default:
;UP = UpArrow
;DOWN = DownArrow
;LEFT = LeftArrow
;RIGHT = RightArrow
;START = Enter
;SELECT = Space
;A = S
;B = A
;SPEED = Shift
;PAUSE = LCtrl
;RESET = Esc
;AUDIO_CHANNEL_1 = F5
;AUDIO_CHANNEL_2 = F6
;AUDIO_CHANNEL_3 = F7
;AUDIO_CHANNEL_4 = F8

UP = UpArrow
DOWN = DownArrow
LEFT = LeftArrow
RIGHT = RightArrow
START = Enter
SELECT = Space
A = S
B = A
SPEED = Shift
PAUSE = LCtrl
RESET = Esc
AUDIO_CHANNEL_1 = F5
AUDIO_CHANNEL_2 = F6
AUDIO_CHANNEL_3 = F7
AUDIO_CHANNEL_4 = F8

[Colors]
;Uses HEX color values WITHOUT the leading # (123456)

;Default:
;BLACK = 081820
;DARK_GRAY = 346856
;LIGHT_GRAY = 88c070
;WHITE = e0f8d0

BLACK = 081820
DARK_GRAY = 346856
LIGHT_GRAY = 88c070
WHITE = e0f8d0

[Saving]
;LOCATION can be an absolute or a relative path
;The path is relative to the emulator executable
;Disabling saving also disables loading

;Default:
;ENABLE=TRUE
;LOCATION=./saves/

ENABLE=true
LOCATION=./saves/

[Logging]
;LOCATION can be an absolute or a relative path
;The path is relative to the emulator executable

;Default:
;ENABLE=true
;LOCATION=./logs/

ENABLE=true
LOCATION=./logs/

[Roms]
;The roms provided by the console parameters have priority over these settings

;Default:
;GAME =
;BOOT =

GAME =
BOOT =";

		File.WriteAllText(CONFIG_FILE_PATH, defaultConfig);
	}
}