# TODO

- Implement toggling APU channels on and off (F5-F8) and add the keys to the settings.ini
- Implement APU channels 3&4
- Implement obscure behaviour/hardware bugs for all APU channels
- Implement all checks at startup (checksums, logo, etc.)
- Implement better waiting at the end of a frame (Thread.Sleep() + correcting)
- Add the ability to resize the window (change scale)
- Implement other memory bank controllers
- Implement STOP opcode
- Implement GUI
- Implement save states/rewind
- Controller support
- Buggy games
  - mario.gb (coin collect sound doesnt stop when the next one starts)
- Broken games
  - donkeykong.gb (glitchy screen at the beginning and locks up after 2 levels)
  - frogger.gb (pretty much unplayable, some sprites missing/corrupted)
  - pokemonred.gb (no mbc3 support yet)