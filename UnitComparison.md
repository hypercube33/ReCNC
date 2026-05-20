# Unit and weapon comparison — OpenRA vs original C&C sources

This document compares **OpenRA** YAML stats (`OpenRA-source/mods/{cnc,ra}`) with **original Tiberian Dawn** data from `CnC_Tiberian_Dawn` (`UDATA.CPP`, `CONST.CPP`, `BBDATA.CPP`, `DEFINES.H`). **Original Red Alert** combat numbers are not in the C++ tree (INI-loaded at runtime); OpenRA RA values are still listed for side-by-side review against OpenRA TD.

## Legend

| Topic | Rule |
|-------|------|
| OpenRA HP / damage | Divide by **100** for a rough “classic scale” comparison (e.g. `45000` → `450`). |
| OpenRA range | `NcM` = N cells + M/1024 sub-cells (e.g. `4c768` ≈ 4.75 cells). |
| TD weapon range | Hex leptons; **1 cell = `0x100` (256)** leptons. |
| TD warhead armor table | Five bytes: None, Wood, Aluminum, Steel, Concrete on **0–255**; ÷ **2.55** ≈ percent. |
| TD `MPH_*` | Enum stored in unit data; numeric values are from `DEFINES.H` (e.g. `MPH_MEDIUM` = 18, `MPH_MEDIUM_FAST` = 30, `MPH_MEDIUM_SLOW` = 12, `MPH_MEDIUM_FASTER` = 35, `MPH_FAST` = 40, `MPH_ROCKET` = 60, `MPH_VERY_FAST` = 100). |

## TD naming quirk (MSAM / MLRS)

In **original TD** `UDATA.CPP`, the internal `"NAME"` string is swapped relative to common labels: `UNIT_MSAM` uses the literal `"MLRS"` while `UNIT_MLRS` uses `"MSAM"`. OpenRA uses **`MSAM`** for the Honest-John-style launcher (`227mm`) and **`MLRS`** for the Patriot SAM (`Patriot`). Rows below follow **OpenRA actor IDs** and map to the matching **TD struct** by behavior.

---

## Part 1 — Tiberian Dawn vehicles (OpenRA `mods/cnc` vs `CnC_Tiberian_Dawn/UDATA.CPP`)

| OpenRA unit | OpenRA HP (÷100) | OpenRA cost | OpenRA speed | OpenRA sight | OpenRA armor | TD struct | TD HP | TD cost | TD `MPH_*` (value) | TD sight | TD armor | Primary / secondary (TD enums) |
|-------------|------------------|-------------|----------------|----------------|--------------|-----------|-------|---------|---------------------|----------|----------|----------------------------------|
| MCV | 120000 (1200) | 3000 | 60 | 8c0 | Heavy | `UnitMCV` | 600 | 5000 | `MPH_MEDIUM_SLOW` (12) | 2 | Aluminum | none |
| HARV | 62500 (625) | 1100 | 72 | 4c0 | Heavy | `UnitHarvester` | 600 | 1400 | `MPH_MEDIUM_SLOW` (12) | 2 | Aluminum | none |
| APC | 19000 (190) | 600 | 128 | 7c0 | Heavy | `UnitAPC` | 200 | 700 | `MPH_MEDIUM_FASTER` (35) | 4 | Steel | `WEAPON_M60MG` |
| ARTY | 7500 (75) | 600 | 72 | 5c0 | Light | `UnitArty` | 75 | 450 | `MPH_MEDIUM_SLOW` (12) | 4 | Aluminum | `WEAPON_155MM` |
| FTNK | 27000 (270) | 600 | 92 | 6c0 | Heavy | `UnitFTank` | 300 | 800 | `MPH_MEDIUM` (18) | 4 | Steel | `WEAPON_FLAME_TONGUE` |
| LTNK | 32000 (320) | 750 | 102 | 6c0 | Heavy | `UnitLTank` | 300 | 600 | `MPH_MEDIUM` (18) | 3 | Steel | `WEAPON_75MM` |
| MTNK | 45000 (450) | 900 | 72 | 6c0 | Heavy | `UnitMTank` | 400 | 800 | `MPH_MEDIUM` (18) | 3 | Steel | **`WEAPON_105MM`** (not 120mm in TD source) |
| HTNK | 87000 (870) | 1800 | 46 | 6c0 | Heavy | `UnitHTank` | 600 | 1500 | `MPH_MEDIUM_SLOW` (12) | 4 | Steel | `WEAPON_120MM`, `WEAPON_MAMMOTH_TUSK` |
| MSAM | 12000 (120) | 900 | 72 | 6c0 | Light | `UnitSAM` (`UNIT_MSAM`, name string `"MLRS"`) | 120 | 750 | `MPH_MEDIUM` (18) | 4 | Aluminum | `WEAPON_HONEST_JOHN` |
| MLRS | 18000 (180) | 600 | 92 | 8c0 | Light | `UnitMLRS` (`UNIT_MLRS`, name string `"MSAM"`) | 100 | 800 | `MPH_MEDIUM` (18) | 4 | Aluminum | `WEAPON_MLRS` |
| STNK | 15000 (150) | 900 | 127 | 7c0 | Light | `UnitSTank` | 110 | 900 | `MPH_MEDIUM_FAST` (30) | 4 | Aluminum | `WEAPON_DRAGON` |

**Notes**

- OpenRA **HP and costs** are often intentionally different from TD (balance / ruleset), not a 1:1 port.
- **Sight**: OpenRA `RevealsShroud` is not identical to TD `SIGHTRANGE` semantics; treat as approximate radius.
- **MTNK**: OpenRA labels the weapon `120mm`; TD medium tank references **`WEAPON_105MM`** in `UDATA.CPP`. Compare OpenRA `120mm` to TD **`WEAPON_105MM`** row in the table below for apples-to-apples on original data.

---

## Part 2 — Tiberian Dawn weapons (OpenRA `mods/cnc/weapons` vs `CONST.CPP` + `BBDATA.CPP`)

TD columns: **damage, ROF (ticks), range (hex)**, then **bullet class → default warhead** from `BBDATA.CPP`. OpenRA **ReloadDelay** is the comparable tick cadence (lower = faster).

### Cannon / ballistic-style

| Role | OpenRA weapon | OpenRA damage | OpenRA reload | OpenRA range | Burst | TD weapon | TD dmg / ROF / range | TD bullet → warhead |
|-----|---------------|---------------|----------------|----------------|-------|-----------|----------------------|---------------------|
| LTNK gun | `70mm` | 2500 (25) | 30 | 4c0 | 1 | `WEAPON_75MM` | 25 / 60 / `0x0400` (4 cells) | `BULLET_APDS` → `WARHEAD_AP` |
| MTNK gun (TD data) | `120mm` (OpenRA name) | 4000 (40) | 40 | 4c768 | 1 | **`WEAPON_105MM`** | 30 / 50 / `0x04C0` (4.75 cells) | `BULLET_APDS` → `WARHEAD_AP` |
| HTNK primary | `120mmDual` | 4000 (40) | 40 | 4c768 | 2, delay 8 | `WEAPON_120MM` | 40 / 80 / `0x04C0` | `BULLET_APDS` → `WARHEAD_AP` |
| HTNK missiles | `MammothMissiles` | 5000 (50) | 45 | 4c768 | 2, delay 15 | `WEAPON_MAMMOTH_TUSK` | 75 / 80 / `0x0500` (5 cells) | `BULLET_SSM` → `WARHEAD_HE` |
| Arty | `ArtilleryShell` | 10000 (100) | 65 | 11c0 (min 3c0) | 1 | `WEAPON_155MM` | 150 / 65 / `0x0600` (6 cells) | `BULLET_HE` → `WARHEAD_HE` |
| APC | `APCGun` (+ `APCGun.AA`) | 1000 / 1250 | 9 | 5c0 / 7c0 | 1 | `WEAPON_M60MG` | 15 / 30 / `0x0400` | `BULLET_BULLET` → `WARHEAD_SA` |

### Flame / stealth / MLRS / Honest John

| Role | OpenRA weapon | OpenRA damage | OpenRA reload | OpenRA range | Burst | TD weapon | TD dmg / ROF / range | TD bullet → warhead |
|-----|---------------|---------------|----------------|----------------|-------|-----------|----------------------|---------------------|
| Flame tank | `BigFlamer` | 10000 (100) | 65 | 3c512 | 2, delay 10 | `WEAPON_FLAME_TONGUE` | 50 / 50 / `0x0200` (2 cells) | `BULLET_FLAME` → `WARHEAD_FIRE` |
| Stealth tank | `227mm.stnk` (+ AA variant) | 6000 (60) | 70 | 7c0 | 2, delay 10 | `WEAPON_DRAGON` | 30 / 60 / `0x0400` | `BULLET_TOW` → `WARHEAD_AP` |
| MSAM (launcher) | `227mm` | 2500 (25), inherits `^MissileWeapon` | 100 | 11c0 (min 3c0) | 4 | `WEAPON_HONEST_JOHN` | 100 / 200 / `0x0A00` (10 cells) | `BULLET_HONEST_JOHN` → **`WARHEAD_FIRE`** (`ClassHonestJohn` in `BBDATA.CPP`) |
| MLRS (SAM) | `Patriot` | 5000 (50) | 25 | 9c0 (min 1c0) | 1 | `WEAPON_MLRS` | 75 / 80 / `0x0600` | `BULLET_SSM2` → `WARHEAD_HE` |

OpenRA **`227mm`** (MSAM) uses a lower base **damage** than a naïve `×100` scaling of TD `WEAPON_HONEST_JOHN` (100 → would be `10000`); the ruleset is tuned independently—compare **ROF, range, burst**, and projectile behavior as well as raw damage.

**`WARHEAD_AP`** armor table (TD): `{0x40, 0xC0, 0xC0, 0xFF, 0x80}` → ≈ **25%, 75%, 75%, 100%, 50%** vs None / Wood / Aluminum / Steel / Concrete.

**`WARHEAD_HE`**: `{0xE0, 0xC0, 0x90, 0x40, 0xFF}` → ≈ **88%, 47%, 35%, 16%, 100%**.

**`WARHEAD_SA`**: `{0xFF, 0x80, 0x90, 0x40, 0x40}`.

**`WARHEAD_FIRE`**: `{0xE0, 0xFF, 0xB0, 0x40, 0x80}`.

OpenRA **Versus** blocks use 0–100% directly and often differ from TD after balance passes.

---

## Part 3 — Red Alert (OpenRA `mods/ra` only; original RA INI not in repo)

Original **Red Alert** unit/weapon numbers are loaded via `Read_INI` in `CnC_Red_Alert/CODE` (not present as plain tables in the source snapshot). Below is **OpenRA RA** data for common vehicles so you still have one engine column in Markdown.

### Vehicles (`mods/ra/rules/vehicles.yaml`)

| Unit | HP (÷100) | Cost | Speed | Sight (main) | Armor | Primary weapon | Secondary |
|------|-----------|------|-------|--------------|-------|----------------|------------|
| V2RL | 20000 (200) | 900 | 72 | 5c0 (min 4c0) | Light | `SCUD` | — |
| 1TNK | 23000 (230) | 700 | 113 | 5c0 (min 4c0) | Heavy | `25mm` | — |
| 2TNK | 46000 (460) | 850 | 72 | 6c0 (min 4c0) | Heavy | `90mm` | — |
| 3TNK | 60000 (600) | 1150 | 64 | 6c0 (min 4c0) | Heavy | `105mm` | — |
| 4TNK | 90000 (900) | 2000 | 43 | 6c0 (min 4c0) | Heavy | `120mm` | `MammothTusk` |
| ARTY | 10000 (100) | 850 | 72 | 5c0 (min 4c0) | Light | `155mm` | — |
| HARV | 60000 (600) | 1100 | 72 | 4c0 | Heavy | — | — |
| MCV | 60000 (600) | 2000 | 60 | 4c0 | Light | — | — |
| JEEP | 15000 (150) | 500 | 164 | 7c0 (min 4c0) | Light | `M60mg` | — |
| APC | 35000 (350) | 850 | 128 | 5c0 (min 4c0) | Heavy | `M60mg` | — |

### Weapons used above (`mods/ra/weapons/*.yaml`)

| Weapon | Damage (÷100) | Reload | Range | Burst | Notes |
|--------|-----------------|--------|-------|-------|-------|
| `25mm` | 2500 (25) | 21 | 4c768 | 1 | Inherits `^Cannon` projectile baseline |
| `90mm` | 4000 (40) | 50 | 4c768 | 1 | `Versus.Heavy: 115` |
| `105mm` | 4000 (40) | 70 | 4c768 | 2 | Dual-barrel feel |
| `120mm` | 6000 (60) | 90 | 4c768 | 2 | `InvalidTargets: Air, Infantry` |
| `MammothTusk` | 5000 (50) | 60 | 6c512 | 2 | Missile; AA-capable targets |
| `155mm` | 23000 (230) | 85 | 12c0 | 1 | Inherits `^Artillery`; `MinRange` 4c0, projectile speed 170 |
| `SCUD` | 4500 (45) | 215 | 10c0 (min 4c0) | 1 | Ballistic missile profile |
| `M60mg` | 1000 (10) | 30 | 4c0 | 5 | Inherits `^LightMG` |

---

## Source file quick reference

| Data | Path |
|------|------|
| OpenRA TD vehicles | `OpenRA-source/mods/cnc/rules/vehicles.yaml` |
| OpenRA TD weapons | `OpenRA-source/mods/cnc/weapons/*.yaml` |
| OpenRA RA vehicles | `OpenRA-source/mods/ra/rules/vehicles.yaml` |
| OpenRA RA weapons | `OpenRA-source/mods/ra/weapons/*.yaml` |
| TD units | `CnC_Tiberian_Dawn/UDATA.CPP` |
| TD weapon table | `CnC_Tiberian_Dawn/CONST.CPP` (`Weapons[]`) |
| TD warhead table | `CnC_Tiberian_Dawn/CONST.CPP` (`Warheads[]`) |
| TD bullet → warhead | `CnC_Tiberian_Dawn/BBDATA.CPP` |
| TD speed enums | `CnC_Tiberian_Dawn/DEFINES.H` (`MPH_Type`) |

---

*Generated for ReCnC / OpenRA comparison workflows. Extend with infantry, aircraft, and structures using the same column pattern.*
