using System;
using System.Globalization;
using System.IO;
using IniParser.Exceptions;
using IniParser.Model;
using IniParser.Model.Configuration;
using IniParser.Parser;

namespace GameboyEmulator;

public static class Config
{
	private const string CONFIG_FILE_PATH = "settings.ini";

	private static readonly IniData CONFIG_DATA;

	static Config()
	{
		if (!File.Exists(CONFIG_FILE_PATH)) CreateDefaultConfigFile();

		try
		{
			IniParserConfiguration config = new()
			{
				SkipInvalidLines = true,
				CaseInsensitive  = true
			};

			CONFIG_DATA = new IniDataParser(config).Parse(File.ReadAllText(CONFIG_FILE_PATH));
		}
		catch (ParsingException e)
		{
			Logger.ControlledCrash(e);
		}
	}

	public static string? GetControlConfig(string key)
	{
		KeyDataCollection? controls      = CONFIG_DATA["Controls"];
		string?            configuredKey = controls?[key];

		return configuredKey;
	}

	public static int? GetColorConfig(string color)
	{
		KeyDataCollection? colors = CONFIG_DATA["Colors"];
		string?            value  = colors?[color];

		if (value is null) return null;

		bool validInt = Int32.TryParse(value, NumberStyles.HexNumber, null, out int output);

		if (validInt && output <= 0xFFFFFF) return output;

		return -1;
	}

	public static string? GetSaveLocationConfig()
	{
		KeyDataCollection? saving = CONFIG_DATA["Saving"];
		return saving?["LOCATION"];
	}

	public static bool GetSaveEnabledConfig()
	{
		KeyDataCollection? saving = CONFIG_DATA["Saving"];
		string?            value  = saving?["ENABLE"];

		bool validBool = Boolean.TryParse(value, out bool output);

		if (validBool) return output;

		Logger.LogInvalidConfigValue("[Saving].ENABLE", value, true);
		return true;
	}

	public static string? GetLogLocationConfig()
	{
		KeyDataCollection? logging = CONFIG_DATA["Logging"];
		return logging?["LOCATION"];
	}

	public static string GetRomConfig(string rom)
	{
		KeyDataCollection? roms  = CONFIG_DATA["Roms"];
		string?            value = roms?[rom];

		return value ?? "";
	}

	private static void CreateDefaultConfigFile()
	{
		const string defaultConfig = @"[Controls]
;Supported Keys can be found at: https://www.sfml-dev.org/documentation/2.5.1/classsf_1_1Keyboard.php#acb4cacd7cc5802dec45724cf3314a142

;Default:
;UP = Up
;DOWN = Down
;LEFT = Left
;RIGHT = Right
;START = Enter
;SELECT = Space
;A = S
;B = A
;SPEED = LShift
;PAUSE = LControl
;RESET = Escape
;AUDIO_CHANNEL_1 = F5
;AUDIO_CHANNEL_2 = F6
;AUDIO_CHANNEL_3 = F7
;AUDIO_CHANNEL_4 = F8

UP = Up
DOWN = Down
LEFT = Left
RIGHT = Right
START = Enter
SELECT = Space
A = S
B = A
SPEED = LShift
PAUSE = LControl
RESET = Escape
AUDIO_CHANNEL_1 = F5
AUDIO_CHANNEL_2 = F6
AUDIO_CHANNEL_3 = F7
AUDIO_CHANNEL_4 = F8

[Colors]
;Uses HEX color values WITHOUT the leading # (123456)

;Default:
;BLACK = 081820
;DARK_GRAY = 346856
;LIGHT_GRAY = 88C070
;WHITE = E0F8D0

BLACK = 081820
DARK_GRAY = 346856
LIGHT_GRAY = 88C070
WHITE = E0F8D0

[Saving]
;LOCATION can be an absolute or a relative path
;The path is relative to the emulator executable
;Disabling saving also disables loading

;Default:
;ENABLE = true
;LOCATION = ./saves

ENABLE = true
LOCATION = ./saves

[Logging]
;LOCATION can be an absolute or a relative path
;The path is relative to the emulator executable

;Default:
;LOCATION = ./logs

LOCATION = ./logs

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