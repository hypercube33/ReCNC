# OpenRA (ReCnC) vs Original C&C Source - Unit Stats Comparison Guide

## Purpose
This guide provides instructions for comparing unit range, damage, and other combat stats between **OpenRA/ReCnC** and the **original Command & Conquer source code** (Tiberian Dawn and Red Alert).

---

## Source Code Structure Overview

### OpenRA/ReCnC (YAML-based)
| Category | File Path Pattern | Description |
|----------|------------------|-------------|
| **Vehicles** | `mods/{game}/rules/vehicles.yaml` | Tank, APC, Harvester stats |
| **Infantry** | `mods/{game}/rules/infantry.yaml` | Soldier unit stats |
| **Aircraft** | `mods/{game}/rules/aircraft.yaml` | Helicopter, plane stats |
| **Ships** | `mods/ra/rules/ships.yaml` | Naval unit stats (RA only) |
| **Structures** | `mods/{game}/rules/structures.yaml` | Building stats |
| **Weapons** | `mods/{game}/weapons/*.yaml` | Damage, range, ROF definitions |
| **Defaults** | `mods/{game}/rules/defaults.yaml` | Base/inherited values |

Where `{game}` = `cnc` (Tiberian Dawn) or `ra` (Red Alert)

### Original C&C Tiberian Dawn (C++ source)
| Category | File Path | Description |
|----------|-----------|-------------|
| **Vehicles** | `CnC_Tiberian_Dawn/UDATA.CPP` | UnitTypeClass definitions |
| **Infantry** | `CnC_Tiberian_Dawn/IDATA.CPP` | InfantryTypeClass definitions |
| **Aircraft** | `CnC_Tiberian_Dawn/AADATA.CPP` | AircraftTypeClass definitions |
| **Buildings** | `CnC_Tiberian_Dawn/BDATA.CPP` | BuildingTypeClass definitions |
| **Weapons** | `CnC_Tiberian_Dawn/CONST.CPP` | WeaponTypeClass array |
| **Warheads** | `CnC_Tiberian_Dawn/CONST.CPP` | WarheadTypeClass array |

### Original C&C Red Alert (C++ source)
| Category | File Path | Description |
|----------|-----------|-------------|
| **Vehicles** | `CnC_Red_Alert/CODE/UDATA.CPP` | UnitTypeClass definitions |
| **Infantry** | `CnC_Red_Alert/CODE/IDATA.CPP` | InfantryTypeClass definitions |
| **Aircraft** | `CnC_Red_Alert/CODE/ADATA.CPP` | AircraftTypeClass definitions |
| **Buildings** | `CnC_Red_Alert/CODE/BDATA.CPP` | BuildingTypeClass definitions |
| **Weapons** | `CnC_Red_Alert/CODE/WEAPON.CPP` | WeaponTypeClass (INI-driven) |
| **Warheads** | `CnC_Red_Alert/CODE/WARHEAD.CPP` | WarheadTypeClass definitions |

---

## Key Stats to Compare

### Unit Stats
| Stat | OpenRA YAML Key | Original C++ Parameter | Notes |
|------|-----------------|----------------------|-------|
| Health/HP | `Health: HP:` | `STRENGTH` parameter | OpenRA uses larger values (multiply by 100) |
| Cost | `Valued: Cost:` | `COST` parameter | Credits to build |
| Speed | `Mobile: Speed:` | `MPH_*` / `SPEED` | Movement speed |
| Sight Range | `RevealsShroud: Range:` | `SIGHTRANGE` | Vision radius |
| Armor Type | `Armor: Type:` | `ARMOR_*` enum | None, Wood, Light, Heavy, Concrete |
| Turn Speed | `Turreted: TurnSpeed:` or `Mobile: TurnSpeed:` | `ROT` (rate of turn) | Rotation speed |
| Primary Weapon | `Armament: Weapon:` | `WEAPON_*` enum | Weapon reference |

### Weapon Stats
| Stat | OpenRA YAML Key | Original C++ Parameter | Notes |
|------|-----------------|----------------------|-------|
| Damage | `Warhead: Damage:` | `dmg` (2nd param in Weapons array) | Base damage value |
| Range | `Range:` | `range` (4th param, hex lepton) | Attack range |
| Reload/ROF | `ReloadDelay:` | `rof` (3rd param) | Rate of fire in ticks |
| Burst | `Burst:` | Varies | Shots per attack cycle |

### Warhead/Armor Modifiers
| Stat | OpenRA YAML Key | Original C++ | Notes |
|------|-----------------|--------------|-------|
| vs None | `Versus: None:` | 1st value in armor array | % damage vs unarmored |
| vs Wood | `Versus: Wood:` | 2nd value | % damage vs wood armor |
| vs Light | `Versus: Light:` | 3rd value | % damage vs light armor |
| vs Heavy | `Versus: Heavy:` | 4th value | % damage vs heavy armor |
| vs Concrete | `Versus: Concrete:` | 5th value | % damage vs concrete |

---

## Data Format Differences

### Range Values
- **Original Source**: Uses "leptons" in hexadecimal (e.g., `0x0400` = 4 cells)
- **OpenRA**: Uses cell notation with sub-cell precision (e.g., `4c768` = 4.75 cells)
- **Conversion**: 1 cell = 256 leptons (0x100). `4c768` = 4 cells + 768/1024 subcells

### Health Values
- **Original Source**: Direct HP values (e.g., `300` HP)
- **OpenRA**: Multiplied by 100 (e.g., `30000` HP for same unit)
- **Conversion**: Divide OpenRA HP by 100 for comparison

### Damage Percentages (Versus)
- **Original Source**: Uses 0-255 scale (0xFF = 100%, 0x80 = 50%)
- **OpenRA**: Uses percentage directly (100 = 100%, 50 = 50%)
- **Conversion**: Original value / 2.55 = OpenRA percentage (approximately)

---

## Example Comparisons

### Tiberian Dawn: Medium Tank (MTNK)

**Original Source** (`CnC_Tiberian_Dawn/CONST.CPP`):
```cpp
// WEAPON_120MM
{BULLET_APDS, 40, 80, 0x04C0, VOC_TANK4, ANIM_MUZZLE_FLASH}
// Params: bullet_type, damage, ROF, range, sound, animation
```

**OpenRA** (`mods/cnc/weapons/ballistics.yaml`):
```yaml
120mm:
  Inherits: ^BallisticWeapon
  Report: tnkfire4.aud
  # Base damage from ^BallisticWeapon: 4000 (= 40 * 100)
```

### Red Alert: Light Tank (1TNK)

**OpenRA** (`mods/ra/rules/vehicles.yaml`):
```yaml
1TNK:
  Health:
    HP: 23000           # = 230 original
  Mobile:
    Speed: 113
  Armament:
    Weapon: 25mm
```

**OpenRA** (`mods/ra/weapons/ballistics.yaml`):
```yaml
25mm:
  ReloadDelay: 21       # ROF
  Range: 4c768          # ~4.75 cells
  Warhead@1Dam:
    Damage: 2500        # = 25 original
```

---

## Comparison Workflow

### Step 1: Identify Unit
1. Find the unit's internal name (e.g., `1TNK`, `E1`, `MTNK`)
2. Locate in both OpenRA YAML and original *DATA.CPP files

### Step 2: Extract Base Stats
For each unit, record:
- HP/Strength
- Cost
- Speed
- Sight Range
- Armor Type
- Primary/Secondary Weapon reference

### Step 3: Extract Weapon Stats
For each weapon referenced:
- Damage
- Range
- ROF (reload delay)
- Warhead type

### Step 4: Extract Warhead Modifiers
For each warhead:
- Damage vs each armor type (None, Wood, Light, Heavy, Concrete)
- Spread factor

### Step 5: Normalize Values
Apply conversions:
- OpenRA HP ÷ 100 = Original HP
- OpenRA Damage ÷ 100 = Original Damage
- Original Armor% ÷ 2.55 = OpenRA Armor%
- Range: Convert hex leptons to cell notation

---

## Comparison Table Template

### Unit: [UNIT_NAME]

| Stat | Original TD | Original RA | OpenRA CnC | OpenRA RA | Notes |
|------|-------------|-------------|------------|-----------|-------|
| HP | | | | | Div by 100 |
| Cost | | | | | |
| Speed | | | | | |
| Sight | | | | | |
| Armor | | | | | |
| Weapon | | | | | |

### Weapon: [WEAPON_NAME]

| Stat | Original TD | Original RA | OpenRA CnC | OpenRA RA | Notes |
|------|-------------|-------------|------------|-----------|-------|
| Damage | | | | | Div by 100 |
| Range | | | | | Convert |
| ROF | | | | | |

---

## Key Files Quick Reference

### Tiberian Dawn Comparison
```
OpenRA Units:     OpenRA-source/mods/cnc/rules/vehicles.yaml
OpenRA Infantry:  OpenRA-source/mods/cnc/rules/infantry.yaml
OpenRA Weapons:   OpenRA-source/mods/cnc/weapons/ballistics.yaml
OpenRA Missiles:  OpenRA-source/mods/cnc/weapons/missiles.yaml

Original Units:   CnC_Tiberian_Dawn/UDATA.CPP
Original Inf:     CnC_Tiberian_Dawn/IDATA.CPP
Original Weapons: CnC_Tiberian_Dawn/CONST.CPP (line ~76)
Original Warhead: CnC_Tiberian_Dawn/CONST.CPP (line ~111)
```

### Red Alert Comparison
```
OpenRA Units:     OpenRA-source/mods/ra/rules/vehicles.yaml
OpenRA Infantry:  OpenRA-source/mods/ra/rules/infantry.yaml
OpenRA Ships:     OpenRA-source/mods/ra/rules/ships.yaml
OpenRA Weapons:   OpenRA-source/mods/ra/weapons/ballistics.yaml
OpenRA Missiles:  OpenRA-source/mods/ra/weapons/missiles.yaml

Original Units:   CnC_Red_Alert/CODE/UDATA.CPP
Original Inf:     CnC_Red_Alert/CODE/IDATA.CPP
Original Weapons: CnC_Red_Alert/CODE/WEAPON.CPP (INI-loaded)
```

---

## Unit Name Cross-Reference

### Tiberian Dawn Vehicles
| Unit | Original ID | OpenRA ID | Description |
|------|-------------|-----------|-------------|
| Medium Tank | UNIT_MTANK / MTNK | MTNK | GDI medium tank |
| Light Tank | UNIT_LTANK / LTNK | LTNK | Nod light tank |
| Flame Tank | UNIT_FTANK / FTNK | FTNK | Nod flame tank |
| Stealth Tank | UNIT_STANK / STNK | STNK | Nod stealth tank |
| Mammoth Tank | UNIT_HTANK / HTNK | HTNK | GDI mammoth tank |
| APC | UNIT_APC / APC | APC | Armored personnel carrier |
| Harvester | UNIT_HARVESTER / HARV | HARV | Tiberium harvester |
| MCV | UNIT_MCV / MCV | MCV | Mobile construction vehicle |
| MLRS | UNIT_MSAM / MSAM | MSAM | Mobile SAM launcher |
| SSM Launcher | UNIT_ARTY / ARTY | ARTY | Artillery/SSM launcher |

### Red Alert Vehicles
| Unit | Original ID | OpenRA ID | Description |
|------|-------------|-----------|-------------|
| Light Tank | UNIT_LTANK / 1TNK | 1TNK | Allied light tank |
| Medium Tank | UNIT_MTANK / 2TNK | 2TNK | Allied medium tank |
| Heavy Tank | UNIT_HTANK / 3TNK | 3TNK | Soviet heavy tank |
| Mammoth Tank | UNIT_MTANK2 / 4TNK | 4TNK | Soviet mammoth tank |
| V2 Launcher | UNIT_V2_LAUNCHER / V2RL | V2RL | Soviet V2 rocket |
| Tesla Tank | UNIT_TTNK / TTNK | TTNK | Soviet tesla tank |

---

## Notes for AI Agent

1. **Always check inheritance** - OpenRA uses `Inherits:` to pull base stats from parent definitions in `defaults.yaml`
2. **Watch for overrides** - Child definitions override parent values
3. **Weapon references** - Units reference weapons by name; look up actual damage in weapon YAML files
4. **Multiple warheads** - OpenRA weapons can have multiple `Warhead@*` entries for different effects
5. **Original source comments** - The C++ files have inline comments explaining each parameter
6. **Hex to decimal** - Convert original hex values (0x0400) to decimal for comparison
7. **Armor enum mapping** - ARMOR_NONE=0, ARMOR_WOOD=1, ARMOR_ALUMINUM=2, ARMOR_STEEL=3, ARMOR_CONCRETE=4
