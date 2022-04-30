# GameboyEmulator

This is a Work-In-Progress [Gameboy](https://en.wikipedia.org/wiki/Game_Boy) Emulator written in C#
with [SFML.Net](https://www.sfml-dev.org/download/sfml.net/).  
For test progress see [TESTING.md](TESTING.md).

# Dependencies

To build this project [.NET 6.0](https://docs.microsoft.com/en-us/dotnet/core/install/) is required.  
Libraries used will be downloaded automatically while building.

# Libraries used

- [SFML.Net](https://github.com/SFML/SFML.Net)
- [INI File Parser](https://github.com/rickyah/ini-parser)
- [Mono.Options](https://github.com/xamarin/XamarinComponents/tree/main/XPlat/Mono.Options)

# Build

***.NET version 6.0 is required to build this repository***

You can build this repository yourself by

1. Downloading the code
2. Navigating to the folder in which the .csproj file lies  
   (normally `<repository_root>/GameboyEmulator/`)
3. Running `dotnet build -c release` inside the console

# Run

After building the repository you can run the program in 2 ways.  
First you navigate to `<repository_root>/GameboyEmulator/bin/release/netcoreapp6.0`,  
then you either execute `GameboyEmulator.exe` or you run `dotnet GameboyEmulator.dll` in the console

# Linux

There is a compatibility issue with SFML.Net and .NET 6.0 that prevents the repository from being built on Linux. I am
sorry for the inconvenience.

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

These controls can be changed inside the ***settings.ini*** file, which gets created in the same directory as the
executable after the first launch.