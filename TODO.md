# TODO

- APU
  - **_Generate samples asynchronously_**
  - Implement obscure behaviour/hardware bugs for all channels
  - Pass all audio test roms
  - Implement VIn (Audio from cartridge)
  - Maybe use Blip-Buf to emulate audio
  - Apply smoothing/drop off to square waves and add high pass filter
- Core
  - **_Add the ability to resize the window (change scale)_**
  - **_Scale window according to screen size (or have screen scale in config)_**
  - **_Implement GUI_**
  - **_Implement save states/rewind_**
  - Implement all checks at startup (checksums, logo, etc.)
  - Add support for the Gameboy Color (CGB)
  - Somehow dont block while events get handled
  - Add ability to set emulator speed to any value
- Improve resetting the emulator (dont create all modules every time)
- Memory
  - Maybe change cartridge ram to be a memory mapped file
- Logger
  - Change log file to be a memory mapped file
  - Implement full logging of all opcodes, modules, etc. with configurable log levels
- MBC
  - **_Implement other memory bank controllers (especially mbc3)_**
- Config
  - Add audio master enable to config file
- Controls
  - **_Implement controller support_**
- CPU
  - Implement STOP opcode
  - Refactor opcode decoding to use blocks of similar opcodes instead of a big switch statement
  - Maybe implement different opcode structure from branch "opcodeStructureExperiment"
- Fix game specific bugs
  - Buggy games
    - mario.gb (coin collect sound doesnt stop when the next one starts)
    - donkeykong.gb (weird lines in select screen and at the bottom of the screen)
    - kirby.gb (white noise when kirby is sucking is too loud)
    - pokemonrot.gb (save file gets corrupted, probably because file gets saved at the beginning of ram writes and not the end)
  - Broken games
    - frogger.gb (pretty much unplayable, some sprites missing/corrupted, annoying sound)
