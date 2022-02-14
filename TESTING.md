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
|--------------|-------|
| cpu_instrs   | ❌     |
| instr_timing | ❌     |
| halt_bug     | ❌     |
| mem_timing-2 | ❌     |
| dmg_sound    | ❌     |
| cgb_sound    | ❌     |
| oam_bug      | ❌     |

## [Scribble tests][scribbltests]

| Test      | State |
|-----------|-------|
| fairylake | ❌     |
| lycscx    | ❌     |
| lycscy    | ❌     |
| palettely | ❌     |
| scxly     | ❌     |
| statcount | ❌     |
| winpos    | ❌     |

## [Mooneye tests][mooneye_tests]

### Acceptance

| Test                    | State |
|-------------------------|-------|
| add_sp_e_timing         | ❌     |
| boot_div-dmgABCmgb      | ❌     |
| boot_hwio-dmgABCmgb     | ❌     |
| boot_regs-dmgABC        | ❌     |
| call_timing             | ❌     |
| call_timing2            | ❌     |
| call_cc_timing          | ❌     |
| call_cc_timing2         | ❌     |
| di_timing GS            | ❌     |
| div_timing              | ❌     |
| ei_sequence             | ❌     |
| ei_timing               | ❌     |
| halt_ime0_ei            | ❌     |
| halt_ime0_nointr_timing | ❌     |
| halt_ime1_timing        | ❌     |
| halt_ime1_timing2-GS    | ❌     |
| if_ie_registers         | ❌     |
| intr_timing             | ❌     |
| jp_timing               | ❌     |
| jp_cc_timing            | ❌     |
| ld_hl_sp_e_timing       | ❌     |
| oam_dma_restart         | ❌     |
| oam_dma_start           | ❌     |
| oam_dma_timing          | ❌     |
| pop_timing              | ❌     |
| push_timing             | ❌     |
| rapid_di_ei             | ❌     |
| ret_timing              | ❌     |
| ret_cc_timing           | ❌     |
| reti_timing             | ❌     |
| reti_intr_timing        | ❌     |
| rst_timing              | ❌     |

#### Bits (unusable bits in memory and registers)

| Test           | State |
|----------------|-------|
| mem_oam        | ❌     |
| reg_f          | ❌     |
| unused_hwio-GS | ❌     |

#### Instructions

| Test | State |
|------|-------|
| daa  | ❌     |

#### Interrupt handling

| Test    | State |
|---------|-------|
| ie_push | ❌     |

#### OAM DMA

| Test       | State |
|------------|-------|
| basic      | ❌     |
| reg_read   | ❌     |
| sources-GS | ❌     |

#### PPU

| Test                        | State |
|-----------------------------|-------|
| hblank_ly_scx_timing-GS     | ❌     |
| intr_1_2_timing-GS          | ❌     |
| intr_2_0_timing             | ❌     |
| intr_2_mode0_timing         | ❌     |
| intr_2_mode3_timing         | ❌     |
| intr_2_oam_ok_timing        | ❌     |
| intr_2_mode0_timing_sprites | ❌     |
| lcdon_timing-GS             | ❌     |
| lcdon_write_timing-GS       | ❌     |
| stat_irq_blocking           | ❌     |
| stat_lyc_onoff              | ❌     |
| vblank_stat_intr-GS         | ❌     |

#### Serial

| Test                      | State |
|---------------------------|-------|
| boot_sclk_align-dmgABCmgb | ❌     |

#### Timer

| Test                 | State |
|----------------------|-------|
| div_write            | ❌     |
| rapid_toggle         | ❌     |
| tim00_div_trigger    | ❌     |
| tim00                | ❌     |
| tim01_div_trigger    | ❌     |
| tim01                | ❌     |
| tim10_div_trigger    | ❌     |
| tim10                | ❌     |
| tim11_div_trigger    | ❌     |
| tim11                | ❌     |
| tima_reload          | ❌     |
| tima_write_reloading | ❌     |
| tma_write_reloading  | ❌     |

### emulator-only

#### MBC1

| Test              | State |
|-------------------|-------|
| bits_bank1        | ✅     |
| bits_bank2        | ✅     |
| bits_mode         | ✅     |
| bits_ramg         | ❌     |
| rom_512kb         | ❌     |
| rom_1Mb           | ❌     |
| rom_2Mb           | ❌     |
| rom_4Mb           | ❌     |
| rom_8Mb           | ❌     |
| rom_16Mb          | ❌     |
| ram_64kb          | ❌     |
| ram_256kb         | ❌     |
| multicart_rom_8Mb | ❌     |

#### MBC2

| Test        | State |
|-------------|-------|
| bits_ramg   | ❌     |
| bits_romb   | ❌     |
| bits_unused | ❌     |
| rom_512kb   | ❌     |
| rom_1Mb     | ❌     |
| rom_2Mb     | ❌     |
| ram         | ❌     |

#### MBC5

| Test      | State |
|-----------|-------|
| rom_512kb | ❌     |
| rom_1Mb   | ❌     |
| rom_2Mb   | ❌     |
| rom_4Mb   | ❌     |
| rom_8Mb   | ❌     |
| rom_16Mb  | ❌     |
| rom_32Mb  | ❌     |
| rom_64Mb  | ❌     |

### manual

| Test            | State |
|-----------------|-------|
| sprite_priority | ❌     |

### misc (CGB)

| Test              | State |
|-------------------|-------|
| boot_div-cgbABCDE | ❌     |
| boot_hwio-C       | ❌     |
| boot_regs-cgb     | ❌     |

#### Bits

| Test          | State |
|---------------|-------|
| unused_hwio-C | ❌     |

#### PPU

| Test               | State |
|--------------------|-------|
| vblank_stat_intr-C | ❌     |

## [Tearoom tests][tearoom_tests]

| Test                              | State |
|-----------------------------------|-------|
| m2_win_en_toggle                  | ❌     |
| m3_bgp_change                     | ❌     |
| m3_bgp_change_sprites             | ❌     |
| m3_lcdc_bg_en_change              | ❌     |
| m3_lcdc_bg_en_change2             | ❌     |
| m3_lcdc_bg_map_change             | ❌     |
| m3_lcdc_bg_map_change2            | ❌     |
| m3_lcdc_obj_en_change             | ❌     |
| m3_lcdc_obj_en_change_variant     | ❌     |
| m3_lcdc_obj_size_change           | ❌     |
| m3_lcdc_obj_size_change_scx       | ❌     |
| m3_lcdc_tile_sel_change           | ❌     |
| m3_lcdc_tile_sel_change2          | ❌     |
| m3_lcdc_tile_sel_win_change       | ❌     |
| m3_lcdc_tile_sel_win_change2      | ❌     |
| m3_lcdc_win_en_change_multiple    | ❌     |
| m3_lcdc_win_en_change_multiple_wx | ❌     |
| m3_lcdc_win_map_change            | ❌     |
| m3_lcdc_win_map_change2           | ❌     |
| m3_obp0_change                    | ❌     |
| m3_scx_high_5_bits                | ❌     |
| m3_scx_high_5_bits_change2        | ❌     |
| m3_scx_low_3_bits                 | ❌     |
| m3_scy_change                     | ❌     |
| m3_scy_change2                    | ❌     |
| m3_window_timing                  | ❌     |
| m3_window_timing_wx_0             | ❌     |
| m3_wx_4_change                    | ❌     |
| m3_wx_4_change_sprites            | ❌     |
| m3_wx_5_change                    | ❌     |
| m3_wx_6_change                    | ❌     |

## Extra

These are valuable tests, they come in a single rom, so they were grouped into a single table

| Test             | State |
|------------------|-------|
| [rtc3test]       | ❌     |
| [bullyGB] in DMG | ❌     |
| [dmg_acid2]      | ❌     |

[blargg_tests]: https://github.com/retrio/gb-test-roms

[scribbltests]: https://github.com/Hacktix/scribbltests

[mooneye_tests]: https://github.com/Gekkio/mooneye-test-suite/

[rtc3test]: https://github.com/aaaaaa123456789/rtc3test

[bullyGB]: https://github.com/Hacktix/BullyGB

[tearoom_tests]: https://github.com/mattcurrie/mealybug-tearoom-tests

[dmg_acid2]: https://github.com/mattcurrie/dmg-acid2
