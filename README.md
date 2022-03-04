# GameboyEmulator

This is a Work-In-Progress Gameboy(https://en.wikipedia.org/wiki/Game_Boy) Emulator written in C# with SFML.net.  
For test progress see [TESTING.md](TESTING.md).

# Dependencies

If you decide to build this repository yourself, all dependencies will be downloaded by NuGet at build time.

# Libraries used

- [SFML.Net](https://github.com/SFML/SFML.Net)
- [INI File Parser](https://github.com/rickyah/ini-parser)
- [Mono.Options](https://github.com/xamarin/XamarinComponents/tree/main/XPlat/Mono.Options)

# Build

***.NET version 6.0 is required to build this repository***

You can build this repository yourself by

1. Downloading the code
2. Navigating to the folder in which the .csproj file lies (normally '/GameboyEmulator/GameboyEmulator/')
3. Running 'dotnet build' inside the console

# Controls

The default controls are as follows

| Gameboy  | Keyboard     |
|----------|--------------|
| Up       | Up arrow     |
| Down     | Down arrow   |
| Left     | Left arrow   |
| Right    | Right arrow  |
| Start    | Enter        |
| Select   | Space        |
| A        | S key        |
| B        | A key        |
| Speed    | Shift        |
| Pause    | Left control |

These controls can be changed inside the **settings.ini** file, which gets created in the same directory as the
executable after the first launch.