# Tests

- [Tests](#tests)
  - [Blargg tests](#blargg-tests)
  - [Scribble tests](#scribble-tests)
  - [Mooneye tests](#mooneye-tests)
    - [Acceptance](#acceptance)
      - [Bits (unusable bits in memory and registers)](#bits-unusable-bits-in-memory-and-registers)
      - [Instructions](#instructions)
      - [Interrupt handling](#interrupt-handling)
      - [OAM DMA](#oam-dma)
      - [PPU](#ppu)
      - [Serial](#serial)
      - [Timer](#timer)
    - [emulator-only](#emulator-only)
      - [MBC1](#mbc1)
      - [MBC2](#mbc2)
      - [MBC5](#mbc5)
    - [manual](#manual)
    - [misc (CGB)](#misc-cgb)
      - [Bits](#bits)
      - [PPU](#ppu-1)
  - [Tearoom tests](#tearoom-tests)
  - [Extra](#extra)

## [Blargg tests][blargg_tests]

| Test         | State |
| ------------ | ----- |
| cpu_instrs   | :x:  |
| instr_timing | :x:  |
| halt_bug     | :x:  |
| mem_timing-2 | :x:  |
| dmg_sound    | :x:  |
| cgb_sound    | :x:  |
| oam_bug      | :x:  |

## [Scribble tests][scribbltests]

| Test         | State |
| ------------ | ----- |
| fairylake    | :x:  |
| lycscx       | :x:  |
| lycscy       | :x:  |
| palettely    | :x:  |
| scxly        | :x:  |
| statcount    | :x:  |
| winpos       | :x:  |

## [Mooneye tests][mooneye_tests]

### Acceptance

| Test                    | State |
| ----------------------- | ----- |
| add_sp_e_timing         | :x:  |
| boot_div-dmgABCmgb      | :x:  |
| boot_hwio-dmgABCmgb     | :x:  |
| boot_regs-dmgABC        | :x:  |
| call_timing             | :x:  |
| call_timing2            | :x:  |
| call_cc_timing          | :x:  |
| call_cc_timing2         | :x:  |
| di_timing GS            | :x:  |
| div_timing              | :x:  |
| ei_sequence             | :x:  |
| ei_timing               | :x:  |
| halt_ime0_ei            | :x:  |
| halt_ime0_nointr_timing | :x:  |
| halt_ime1_timing        | :x:  |
| halt_ime1_timing2-GS    | :x:  |
| if_ie_registers         | :x:  |
| intr_timing             | :x:  |
| jp_timing               | :x:  |
| jp_cc_timing            | :x:  |
| ld_hl_sp_e_timing       | :x:  |
| oam_dma_restart         | :x:  |
| oam_dma_start           | :x:  |
| oam_dma_timing          | :x:  |
| pop_timing              | :x:  |
| push_timing             | :x:  |
| rapid_di_ei             | :x:  |
| ret_timing              | :x:  |
| ret_cc_timing           | :x:  |
| reti_timing             | :x:  |
| reti_intr_timing        | :x:  |
| rst_timing              | :x:  |

#### Bits (unusable bits in memory and registers)

| Test           | State |
| -------------- | ----- |
| mem_oam        | :x:  |
| reg_f          | :x:  |
| unused_hwio-GS | :x:  |

#### Instructions

| Test | State |
| ---- | ----- |
| daa  | :x:  |

#### Interrupt handling

| Test                        | State |
| --------------------------- | ----- |
| ie_push                     | :x:  |

#### OAM DMA

| Test       | State     |
| ---------- | --------- |
| basic      | :x:      |
| reg_read   | :x:      |
| sources-GS | :x:/:x:* |

#### PPU

| Test                        | State |
| --------------------------- | ----- |
| hblank_ly_scx_timing-GS     | :x:   |
| intr_1_2_timing-GS          | :x:  |
| intr_2_0_timing             | :x:  |
| intr_2_mode0_timing         | :x:   |
| intr_2_mode3_timing         | :x:   |
| intr_2_oam_ok_timing        | :x:  |
| intr_2_mode0_timing_sprites | :x:   |
| lcdon_timing-GS             | :x:   |
| lcdon_write_timing-GS       | :x:   |
| stat_irq_blocking           | :x:  |
| stat_lyc_onoff              | :x:  |
| vblank_stat_intr-GS         | :x:  |

#### Serial 

| Test                       | State |
| -------------------------- | ----- |
| boot_sclk_align-dmgABCmgb  | :x:  |


#### Timer

| Test                 | State |
| -------------------- | ----- |
| div_write            | :x:  |
| rapid_toggle         | :x:  |
| tim00_div_trigger    | :x:  |
| tim00                | :x:  |
| tim01_div_trigger    | :x:  |
| tim01                | :x:  |
| tim10_div_trigger    | :x:  |
| tim10                | :x:  |
| tim11_div_trigger    | :x:  |
| tim11                | :x:  |
| tima_reload          | :x:  |
| tima_write_reloading | :x:  |
| tma_write_reloading  | :x:  |

### emulator-only

#### MBC1

| Test              | State |
| ----------------- | ----- |
| bits_bank1        | :x:  |
| bits_bank2        | :x:  |
| bits_mode         | :x:  |
| bits_ramg         | :x:  |
| rom_512kb         | :x:  |
| rom_1Mb           | :x:  |
| rom_2Mb           | :x:  |
| rom_4Mb           | :x:  |
| rom_8Mb           | :x:  |
| rom_16Mb          | :x:  |
| ram_64kb          | :x:  |
| ram_256kb         | :x:  |
| multicart_rom_8Mb | :x:  |

#### MBC2

| Test              | State |
| ----------------- | ----- |
| bits_ramg         | :x:  |
| bits_romb         | :x:  |
| bits_unused       | :x:  |
| rom_512kb         | :x:  |
| rom_1Mb           | :x:  |
| rom_2Mb           | :x:  |
| ram               | :x:  |

#### MBC5

| Test              | State |
| ----------------- | ----- |
| rom_512kb         | :x:  |
| rom_1Mb           | :x:  |
| rom_2Mb           | :x:  |
| rom_4Mb           | :x:  |
| rom_8Mb           | :x:  |
| rom_16Mb          | :x:  |
| rom_32Mb          | :x:  |
| rom_64Mb          | :x:  |

### manual

| Test            | State |
| --------------- | ----- |
| sprite_priority | :x:  |

### misc (CGB)

| Test              | State |
| ---------------   | ----- |
| boot_div-cgbABCDE | :x:  |
| boot_hwio-C       | :x:  |
| boot_regs-cgb     | :x:  |

#### Bits

| Test          | State |
| ------------- | ----- |
| unused_hwio-C | :x:  |

#### PPU

| Test               | State |
| ------------------ | ----- |
| vblank_stat_intr-C | :x:  |

## [Tearoom tests][tearoom_tests]

| Test                              | State |
| --------------------------------- | ----- | 
| m2_win_en_toggle                  | :x:  |
| m3_bgp_change                     | :x:  |
| m3_bgp_change_sprites             | :x:  |
| m3_lcdc_bg_en_change              | :x:  |
| m3_lcdc_bg_en_change2             | :x:  |
| m3_lcdc_bg_map_change             | :x:  |
| m3_lcdc_bg_map_change2            | :x:  |
| m3_lcdc_obj_en_change             | :x:  |
| m3_lcdc_obj_en_change_variant     | :x:  |
| m3_lcdc_obj_size_change           | :x:  |
| m3_lcdc_obj_size_change_scx       | :x:  |
| m3_lcdc_tile_sel_change           | :x:  |
| m3_lcdc_tile_sel_change2          | :x:  |
| m3_lcdc_tile_sel_win_change       | :x:  |
| m3_lcdc_tile_sel_win_change2      | :x:  |
| m3_lcdc_win_en_change_multiple    | :x:  |
| m3_lcdc_win_en_change_multiple_wx | :x:  |
| m3_lcdc_win_map_change            | :x:  |
| m3_lcdc_win_map_change2           | :x:  |
| m3_obp0_change                    | :x:  |
| m3_scx_high_5_bits                | :x:  |
| m3_scx_high_5_bits_change2        | :x:  |
| m3_scx_low_3_bits                 | :x:  |
| m3_scy_change                     | :x:  |
| m3_scy_change2                    | :x:  |
| m3_window_timing                  | :x:  |
| m3_window_timing_wx_0             | :x:  |
| m3_wx_4_change                    | :x:  |
| m3_wx_4_change_sprites            | :x:  |
| m3_wx_5_change                    | :x:  |
| m3_wx_6_change                    | :x:  |

## Extra
These are valuable tests, they come in a single rom, so they were grouped into
a single table

| Test             | State |
| ---------------- | ----- |
| [rtc3test]       | :x:  |
| [bullyGB] in DMG | :x:  |
| [dmg_acid2]      | :x:  |


[blargg_tests]: https://gbdev.gg8.se/wiki/articles/Test_ROMs
[scribbltests]: https://github.com/Hacktix/scribbltests
[mooneye_tests]: https://github.com/Gekkio/mooneye-gb/tree/master/tests
[rtc3test]: https://github.com/aaaaaa123456789/rtc3test
[bullyGB]: https://github.com/Hacktix/BullyGB
[tearoom_tests]: https://github.com/mattcurrie/mealybug-tearoom-tests
[dmg_acid2]: https://github.com/mattcurrie/dmg-acid2
