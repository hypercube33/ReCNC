# ReCnC — Changelog

All notable changes to this repository (outside vendored `OpenRA-source\` unless noted) are listed here. Use for history and rollback context.

## release-20260425

### Added

- **2026-04-28** — **Classic `rules.ini` reference tree [RulesFiles](RulesFiles/).** Contains
  `RulesFiles/RA/rules.ini`, `RulesFiles/TS/rules.ini`, `RulesFiles/TS-FS/rules.ini` for balance
  parity vs OpenRA YAML. [Todo.md](Todo.md) Setup item **Obtain … Rules.ini** marked done; workflow
  “Rules.ini vs YAML” unblocked for RA/TS; BUG-016–018 / BUG-019–020 bullets now cite **`[ARTY]`**,
  **`[155mm]`**, **`[SCUD]`**, **`[APC]`**, **`[8Inch]`**, etc.

- **2026-04-28** — **Open bugs / Todo — elaboration & research (BUG-012–024).** [OpenBugs.md](OpenBugs.md)
  and [Todo.md](Todo.md): **BUG-012** Titan voxel barrel wrong on **vehicle factory** exit **and** at same
  facing when moving normally (reporter typo: refinery → **vehicle factory**); **BUG-013** rolled into TS harvester/refinery/docking with
  **Improved** docking note and design split (harvester / heli-plane / depot); **BUG-014** log
  path `Crash Logs 3rd Party/`; **BUG-015** tracing via actor-tags overlay, `EnableSimulationPerfLogging`
  → `perf.log`, standard logs, replay; **BUG-016–020** rebalance pointers to YAML + EA trees;
  **BUG-024** MP vs **SkirmishLogic** (`ServerType.Skirmish` only — not `Multiplayer`).

- **2026-04-27** — **Open bugs backlog BUG-012 through BUG-025.** [OpenBugs.md](OpenBugs.md) and
  [Todo.md](Todo.md) now track: TS Titan barrel at war-factory exit, TS harvester queues, TS
  client crash on host abort, TS Hunter seeker spin; RA artillery/ships, V2, APC reviews; TD
  nukes, artillery range, capture-without-tech, multi-engineer lobby option, Orca helipad
  landing; overall lobby settings persistence (**BUG-024**) and vehicle wreck / husk behavior
  vs **classic EA source** (**BUG-025**): `UnitClass::Take_Damage` → `delete this` → destructor
  `Limbo()` in [CnC_Tiberian_Dawn/UNIT.CPP](CnC_Tiberian_Dawn/UNIT.CPP) and
  [CnC_Red_Alert/CODE/UNIT.CPP](CnC_Red_Alert/CODE/UNIT.CPP) (no persistent wreck unit). **BUG-008**
  OpenBugs line completed; **BUG-011** Todo text aligned with OpenBugs (TS launcher / packaging,
  not an unspecified hotkey).

### Changed
 
- **2026-05-19** - **Lobby persistence widened beyond stock skirmish-only behavior.** Gameplay code in
  [OpenRA-source/OpenRA.Mods.Common/ServerTraits/SkirmishLogic.cs](OpenRA-source/OpenRA.Mods.Common/ServerTraits/SkirmishLogic.cs)
  now treats `ServerType.Local` as skirmish-style persistence, writes a separate
  `multiplayer.<modId>.yaml` for hosted multiplayer lobbies, restores that state only when the
  first admin host joins a fresh lobby, and persists `AllowSpectators` alongside map / options /
  host slot / bots. This should reduce the repeated "set everything again" churn for local and
  host-created sessions without stomping later joining multiplayer clients.

- **2026-05-08** — **CnC helipad parking, RTB stickiness, resupply repair, Chinook idle.** Gameplay code ([OpenRA-source/OpenRA.Mods.Common](OpenRA-source/OpenRA.Mods.Common)): **`ProvidesAircraftParkingPrerequisite`** now counts **queued** ORCA/HELI across **`Aircraft.GDI` / `Aircraft.Nod`** production queues toward slot use (same **`cnc-air-parking-available`** prereq), so total **alive + queued** cannot exceed helipad count — avoids mass-queue then wholesale cancel when cap hits. **`Aircraft`**: **`PreferredResupplier`** set whenever the unit **reserves** a helipad (persists after takeoff); **`ReturnToBase`** / **`ChooseResupplier`** prefer that pad, return **null** when it is busy so RTB **waits near that pad** instead of stealing another. **`GroundResupplyProximity`** (~3 cells): **`Resupply`** no longer uses **`WDist.Zero`**, so landed craft count as “close enough” for **repair + rearm** together. Rules: **`TRAN`** (**Chinook**) **`IdleBehavior: Land`** so idle combat transports settle instead of **`ReturnToBase`** to a pad. Files: `Traits/Player/ProvidesAircraftParkingPrerequisite.cs`, `Traits/Air/Aircraft.cs`, `Activities/Air/ReturnToBase.cs`; YAML `mods/cnc/rules/aircraft.yaml` (+ **build** mirror).

- **2026-05-08** — **CnC artillery & MLRS: 8-cell range + OG-era per-shot damage parity.** **`ArtilleryShell`** ([OpenRA-source/mods/cnc/weapons/ballistics.yaml](OpenRA-source/mods/cnc/weapons/ballistics.yaml)): **`Range: 8c0`**, **`ReloadDelay: 65`**, **`Damage: 10000`** — matches **`CONST.CPP`** **`WEAPON_155MM`** **Attack 150**, **ROF 65**, relative to Mammoth tusks **75 → 5000** in this mod (**150 = 2× 75 → 10000**). New **`MLRSRocket`** (**same missiles.yaml path**): **`Range: 8c0`**, **`ReloadDelay: 80`**, Mammoth-aligned **`SpreadDamage` / Damage 5000** — **`CONST.CPP`** **`WEAPON_MLRS`** **Attack 75**, **ROF 80**, **`BULLET_SSM2` / HE** (`SSM` / Mammoth tusks share that scale **→ 5000** here). **`MLRS`** ([OpenRA-source/mods/cnc/rules/vehicles.yaml](OpenRA-source/mods/cnc/rules/vehicles.yaml)): armament **`Patriot`** → **`MLRSRocket`** (ground/air/water rockets per OG **`IsAntiAircraft`** SSM2); **`^AutoTargetAir`** → **`^AutoTargetAllAssaultMove`**; COMBAT-001b **AutoTarget** scan/stance; **`ReloadAmmoPool.Delay: 80`**.

- **2026-05-08** — **CnC Atomic (nuke): mirror OG multiplayer damage and blast reach.** File: [OpenRA-source/mods/cnc/weapons/superweapons.yaml](OpenRA-source/mods/cnc/weapons/superweapons.yaml). Removed the prior **×2.5** overshoot. **Damage:** EA TD `ANIM.CPP` network-game branch (`rawdamage=200`, `radius=3`) → **100** classic per-cell `Explosion_Damage`; × MTNK HP scale **45000/400**; × **~9** epicenter overlaps vs the **7×7** grid → OpenRA total **101250**, same **15 : 11 : 5 : 2** split → **46024**, **33750**, **15340**, **6136**. Delays unchanged. **Radius (OG parity):** multiplayer `radius=3` is a **7×7** cell square; on OpenRA’s rectangular TD grid, corner cell centers are **`3 * sqrt(2)` cells** from impact, so all four `SpreadDamage` warheads use the same explicit **`Range`** ladder ending at **`4c248` WDist (~4344)** instead of `Spread`-derived rings. **DestroyResource** / **LeaveSmudge** outer cell radii aligned to that constant: **3** for each delayed pass (fits `radius=3`; OpenRA still uses circular tile rings, not an exact square footprint). OG **WARHEAD_FIRE** armor table is still not replicated (`Versus` unchanged).

### Fixed

- **2026-05-08** — **MSAM (GDI rocket launcher / classic “MLRS”) — halved inflated burst DPS.** Original TD `UnitSAM` arms **`WEAPON_HONEST_JOHN` only** (`WEAPON_NONE` secondary) in [CnC_Tiberian_Dawn/UDATA.CPP](CnC_Tiberian_Dawn/UDATA.CPP). OpenRA **`AttackFrontal`** defaults to **`primary` + `secondary`** armaments; a duplicate **`Armament@SECONDARY`** both firing **`227mm`** caused **two full 4-rocket bursts per attack cycle** (~2× sustained damage vs OG). Removed the secondary armament; kept one **`Armament`** with **two `LocalOffset` muzzles** (barrel alternation unchanged). Files: [OpenRA-source/mods/cnc/rules/vehicles.yaml](OpenRA-source/mods/cnc/rules/vehicles.yaml), [build/mods/cnc/rules/vehicles.yaml](build/mods/cnc/rules/vehicles.yaml).

- **2026-05-08** — **BUG-025 — CnC vehicle husks/carcasses removed (classic parity).** Removed
  `SpawnActorOnDeath` from all 15 CnC vehicles (MCV, HARV, APC, ARTY, FTNK, BGGY, BIKE, JEEP,
  LTNK, MTNK, HTNK, MSAM, MLRS, STNK, TRUCK). Original CnC behavior per `UNIT.CPP`: destroyed
  vehicles call `delete this` → destructor `Limbo()` → no persistent wreck unit blocking cells.
  OpenRA's husks cluttered the map and blocked movement ("maze of wrecks"). Now vehicles simply
  explode and disappear, matching classic TD behavior.
  File: [mods/cnc/rules/vehicles.yaml](OpenRA-source/mods/cnc/rules/vehicles.yaml).

- **2026-05-08** — **GAME-009 — CnC Orca/Heli spawn, rearm, and return-to-base behavior.** Three issues
  fixed to match original Command & Conquer behavior:
  1. **Spawn landed** — Added `TakeOffOnCreation: false` to CnC `^Helicopter` defaults so aircraft
     spawn landed on the helipad instead of immediately taking off.
  2. **No auto-regen ammo** — Removed `ReloadAmmoPool` trait from ORCA and HELI (which auto-regenerated
     ammo in the air). Original CnC only reloaded ammo at the helipad via `RADIO_RELOAD` messages.
  3. **Return to base for rearm** — Added `Rearmable: RearmActors: hpad` to ORCA and HELI so they
     land at helipads to reload. Combined with `IdleBehavior: ReturnToBase`, aircraft now automatically
     return to helipad when out of ammo (matching original CnC behavior per `Enter_Idle_Mode()` in
     `AIRCRAFT.CPP` lines 1834-1841: `Find_Docking_Bay(STRUCT_HELIPAD)` → `MISSION_ENTER`).
  Files: [mods/cnc/rules/defaults.yaml](OpenRA-source/mods/cnc/rules/defaults.yaml),
  [mods/cnc/rules/aircraft.yaml](OpenRA-source/mods/cnc/rules/aircraft.yaml).

- **2026-05-08** — **GAME-009 — RA helicopters greyed out / build immediately canceled.** Two bugs
  in `ProvidesAircraftParkingPrerequisite`: (1) **Case-sensitive actor name comparison** — OpenRA
  lowercases actor names during ruleset loading (`k.Key.ToLowerInvariant()` in `Ruleset.cs`), but
  the `ParkingActors` defaults were uppercase ("HPAD"), causing `HashSet.Contains()` to never match.
  Fixed by using `StringComparer.OrdinalIgnoreCase` for both HashSets. (2) **Queued aircraft counted
  as used slots** — clicking to build immediately added the aircraft to the queue, which was counted
  as a used slot, making `capacity > used` false and revoking the prerequisite (canceling the build).
  Removed queue counting to match original RA behavior per `HOUSE.CPP` line 6237:
  `BQuantity[STRUCT_HELIPAD] > AQuantity[AIRCRAFT_LONGBOW] + AQuantity[AIRCRAFT_HIND]` — original
  game only counts **existing/alive** aircraft, not queued ones.

- **2026-04-26** — **TS crash: DivideByZeroException in `BodyOrientationInfo.QuantizeFacing`.** Log
  (`exception-2026-04-26T020823Z.log`) showed a render-path crash on map *Tiers of Sorrow* while
  drawing a `WithIdleOverlay` on an actor whose `BodyOrientation` uses **`QuantizedFacings: 0`**
  (voxel / aircraft defaults in `mods/ts/rules/defaults.yaml`). `QuantizeOrientation` already
  treated `0` as “no quantization,” but `QuantizeFacing` still called `Util.QuantizeFacing` and
  divided by zero. [BodyOrientation.cs](OpenRA-source/OpenRA.Mods.Common/Traits/BodyOrientation.cs)
  now returns the raw facing when `facings == 0`.

### Changed

- **2026-04-25** — **BUG-009 / GAME-009 — RA aircraft aggregate parking cap.** Player-owned
  combat aircraft (**MIG**, **YAK**, **HELI**, **HIND**, **MH60**) now require prerequisite
  `ra-air-parking-available`, granted only while **(helipads + airfields) is greater than (those aircraft
  alive)**. **TRAN** (Chinook-style transport) is uncapped.
  Engine: `ProvidesAircraftParkingPrerequisite` in
  [OpenRA.Mods.Common/Traits/Player/ProvidesAircraftParkingPrerequisite.cs](OpenRA-source/OpenRA.Mods.Common/Traits/Player/ProvidesAircraftParkingPrerequisite.cs);
  rules: [mods/ra/rules/player.yaml](OpenRA-source/mods/ra/rules/player.yaml),
  [mods/ra/rules/aircraft.yaml](OpenRA-source/mods/ra/rules/aircraft.yaml). Aligns with classic
  RA AI/factory split rationale (helipad vs fixed-wing) while enforcing a single shared pool
  per prior design note.
- **2026-04-25** — **BUG-010 / GAME-010 — RA mines ally-safe (classic parity).** Template
  `^Mine` now sets `AvoidFriendly: true` and `BlockFriendly: true` so allied units do not
  detonate or path-crush allied mines, matching original RA’s `Is_Ally` mine gate. See
  [mods/ra/rules/defaults.yaml](OpenRA-source/mods/ra/rules/defaults.yaml).
- **2026-04-25** — **BUG-011 — Tiberian Sun Windows entry point.** `build-launchers.ps1` now
  builds **TiberianSun.exe** (`ModID: ts`), copies **mods/ts** and **mods/ts-content** into the
  output tree, versions `mods/ts/mod.yaml` and `mods/ts-content/mod.yaml`, and generates
  **ts.ico** (placeholder copy from **cnc.ico** until dedicated TS artwork exists). NSIS
  ([packaging/windows/OpenRA.nsi](OpenRA-source/packaging/windows/OpenRA.nsi)) installs TS mod
  folders, Start Menu / Desktop shortcuts, `openra-ts-${TAG}` URI registration, and
  `OpenRA.Utility.exe ts --register-mod` / unregister on uninstall. [OpenBugs.md](OpenBugs.md)
  updated with the concrete gap and mitigation.

- **2026-04-25** — **OpenBugs — Tiberian Sun triage notes.** [OpenBugs.md](OpenBugs.md) § Tiberian Sun
  now tracks crash log location (`%AppData%\OpenRA\Logs`), shadow angle mismatch (infantry vs
  buildings), harvester home-refinery stickiness vs single-refinery pile-up, and Titan gun
  rendering at certain angles.

- **2026-04-25** — **TD AI bot rename: difficulty-based names (UI-001).** Renamed Tiberian
Dawn's three skirmish AI bots from sci-fi pop-culture references to standardized difficulty
names, and added a fourth turtle/defensive bot. Modular bot identifiers in
[mods/cnc/rules/ai.yaml](OpenRA-source/mods/cnc/rules/ai.yaml) and Fluent display strings in
[mods/cnc/fluent/rules.ftl](OpenRA-source/mods/cnc/fluent/rules.ftl). Sentinel-wrapped with
`# BEGIN ReCnC UI-001` / `# END ReCnC UI-001`.

  | Old name | Old ID    | New display name    | New ID       | Mapping rationale                                                                          |
  | -------- | --------- | ------------------- | ------------ | ------------------------------------------------------------------------------------------ |
  | Watson   | `watson`  | Easy                | `easy`       | Balanced production, predictable squad sizes (15)                                          |
  | Cabal    | `cabal`   | Medium              | `medium`     | Larger squads (15), artillery-heavy, no helipads                                           |
  | HAL 9001 | `hal9001` | Hard                | `hard`       | Smaller focused squads (8), highest building limits                                        |
  | *(new)*  | —         | Specialist (Turtle) | `specialist` | Cloned Hard base; defense fractions ~2x, building limits +50%, squad size 6, harv limit 10 |

  All `enable-{name}-ai` conditions, `BaseBuilderBotModule@{name}`, `SquadManagerBotModule@{name}`,
  and `UnitBuilderBotModule@{name}` keys were renamed accordingly. Existing bot configurations for
  the three renamed bots were preserved as-is. Specialist's tuning bumps `gtwr` 5→12, `gun` 5→12,
  `atwr` 9→18, `obli` 7→14, `sam` 7→14, raises `proc` limit 4→5 / `pyle` 2→3 / `weap` 2→3, and
  reduces `arty` build weight to compensate (turtles don't need an artillery rush).
- **2026-04-25** — **Build version mismatch fix (BUG-007).** Internal build version no longer
reports the wrong date. Two fixes:
  1. [build-launchers.ps1](build-launchers.ps1) `$Tag` parameter now defaults to
    `release-$(Get-Date -Format 'yyyyMMdd')` instead of the hardcoded stale value
     `release-20250330`. Every invocation without `-Tag` now stamps today's date into both the
     rcedit-set EXE ProductVersion (via `Set-ExeMetadata`) and the per-mod `Metadata: Version:`
     field (via `Set-ModVersion`).
  2. Source `Metadata: Version:` baseline in all nine `mods/*/mod.yaml` files (cnc, ra, d2k, ts,
    all, cnc-content, ra-content, d2k-content, ts-content) bumped from `release-20250330` to
     `release-20260425`. This matters when running from source via `make.cmd` / `dotnet run`
     (no Set-ModVersion pass), since the in-game version display reads directly from these files.
  **Before:** Running `.\build-launchers.ps1` with no `-Tag` flag stamped a 13-month-old version
  string (`release-20250330`) onto every EXE and mod.yaml even though the build was happening in
  April 2026. Running from source showed the same stale string.
  **After:** No `-Tag` argument → today's date is used. Source-tree mod.yaml shows
  `release-20260425` (the date this fix landed) until a packaged build re-stamps it. Override
  with `-Tag "release-YYYYMMDD"` or any custom label remains supported.
- **2026-04-25** — **Artillery Auto-Targeting Fix (COMBAT-001b).** Completes the partial
COMBAT-001 fix by ensuring artillery / rocket-launcher units auto-engage at full weapon range,
matching classic C&C and Red Alert behavior. Original 1995/1996 code had no stance system —
units automatically engaged anything in weapon range (capped at `0x0A00` ≈ 10 cells per
`TechnoClass::Threat_Range`). OpenRA's stance/ScanRadius system defaults `ScanRadius` to ~4-5
cells when not specified, leaving long-range units severely under-utilizing their weapon range
unless force-fired (Ctrl+click).
  Sentinel-wrapped with `# BEGIN ReCnC COMBAT-001b` / `# END ReCnC COMBAT-001b`. No `patches/`
  unified diff (repo has no git baseline yet); sentinels in YAML are the tracking mechanism.

  | Unit                                   | File                           | Before                                                                                          | After                                                                                                  | Classic Range                                |
  | -------------------------------------- | ------------------------------ | ----------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------ | -------------------------------------------- |
  | TD ARTY                                | `mods/cnc/rules/vehicles.yaml` | `InitialStanceAI: Defend`, no ScanRadius (~4-5 default), no player stance                       | `ScanRadius: 11`, `InitialStance: AttackAnything`, `InitialStanceAI: AttackAnything`                   | 6 cells (0x0600) — OpenRA weapon range 11c0  |
  | TD MSAM (GDI Rocket Launcher / "MRLS") | `mods/cnc/rules/vehicles.yaml` | `ScanRadius: 11`, `InitialStance: AttackAnything` (from COMBAT-001), no AI stance, no sentinels | `ScanRadius: 11`, `InitialStance: AttackAnything`, `InitialStanceAI: AttackAnything`, sentinel-wrapped | 10 cells (0x0A00) — OpenRA weapon range 11c0 |
  | RA ARTY                                | `mods/ra/rules/vehicles.yaml`  | `ScanRadius: 12`, `InitialStance: AttackAnything` (from COMBAT-001), no AI stance, no sentinels | `ScanRadius: 12`, `InitialStance: AttackAnything`, `InitialStanceAI: AttackAnything`, sentinel-wrapped | OpenRA weapon range 12c0                     |
  | RA V2RL                                | `mods/ra/rules/vehicles.yaml`  | `ScanRadius: 10`, `InitialStance: AttackAnything` (from COMBAT-001), no AI stance, no sentinels | `ScanRadius: 10`, `InitialStance: AttackAnything`, `InitialStanceAI: AttackAnything`, sentinel-wrapped | OpenRA weapon range 10c0                     |

  TD MLRS (the Nod *Mobile SAM*, anti-air only — separate from the GDI Rocket Launcher despite
  the confusingly-similar internal name) was **not** changed — it inherits `^AutoTargetAir` and
  is air-target-only with weapon range 9c0; classic Honest John was 10 cells but the
  range/stance/scan trio is already consistent for the Patriot weapon.
  **Reproduction note:** User reported that even after COMBAT-001 landed, units "still seem to
  nerf the shit out of us range wise." Root cause was twofold: (1) TD ARTY was simply missed in
  the original COMBAT-001 fix, and (2) the existing fixes had no `InitialStanceAI` override, so
  AI-controlled artillery still defaulted to a different stance than player-controlled artillery.

### Added

- **2026-04-25** — **UnitStatsComparisonGuide.md** — Documentation guide for comparing unit
range, damage, and combat stats between OpenRA/ReCnC and original C&C source code (Tiberian
Dawn and Red Alert). Includes file path references, data format conversion notes (HP ÷100,
hex leptons to cell notation, armor % scaling), unit name cross-reference tables, and a
structured workflow for stat extraction. Intended for delegation to other AI agents.

### Changed

- **2026-04-21** — **Targeted post-Batch-A perf pass (PERF-028, PERF-024, PERF-017).** Three
independent high-multiplier fixes landed in one session to keep Batch A momentum before the
Batch B ActorIndex scaffolding work. Full-solution `make.cmd all` on SDK 10.0.203:
**0 errors, NU1901-only warnings** (incremental rebuild emits 6, a from-scratch rebuild
emits the same 12 as Batch A — no new advisory classes, no `SYSLIB*`, no `CS0672`).
Sentinel-wrapped with `BEGIN/END ReCnC PERF-NNN`; backups in `patches\PERF-NNN-backup\`;
unified-diff patches saved alongside.
  - **PERF-028** *(Armament.Tick)* — hoisted the per-tick `RemoveAll` predicate to a
  `static readonly Predicate<...> ExpiredDelayedAction` field, and wrapped the
  delayed-action loop + `RemoveAll` in a `delayedActions.Count > 0` guard. An Armament
  with no pending delayed actions (the overwhelmingly common case — delayed actions are
  only queued by burst / reload-pause weapons that have actively fired) now takes zero
  allocations through this path per tick. Patch:
  `patches\PERF-028_Armament.cs.patch`.
  - **PERF-024** *(Mobile.MovementSpeedForCell)* — replaced
  `readonly Lazy<IEnumerable<int>> speedModifiers` with
  `readonly Lazy<ISpeedModifier[]> speedModifierTraits`. The previous implementation
  built a lazy `Select(x => x.GetSpeedModifier())` inside the `Lazy`, and
  `MovementSpeedForCell` then did `.Append(terrainSpeed)` and passed the result to
  `Util.ApplyPercentageModifiers` — three allocating iterator wrappers per call
  (Select, Append, the foreach enumerator), firing on every pathfinding cell-cost
  eval and every movement step. New code snapshots the trait array once (same lazy
  ordering as before, so `Created`-time trait discovery is unchanged) and inlines the
  exact decimal arithmetic from `Util.ApplyPercentageModifiers`
  (`a = (decimal)Info.Speed; foreach (modifier) a *= m / 100m; a *= terrainSpeed / 100m; return (int)a;`) directly, preserving bit-identical math and the dynamic
  `GetSpeedModifier()` call on every evaluation (so rank / upgrade speed modifiers
  remain live). Patch: `patches\PERF-024_Mobile.cs.patch`.
  - **PERF-017** *(ImprovedMoveToDock.GetAlternativeDocks + Tick)* — added a per-activity
  `List<TraitPair<IDockHost>> cachedAlternates` with cache key `(WorldTick, dockHost)`.
  `GetAlternativeDocks` now returns a `List<>` and only rebuilds it when the current
  tick or the current dockHost changed (so mid-tick dockHost reassignment after a
  successful re-evaluation still produces a fresh scan on the fallback path). Both
  hot-path call sites migrated from `alternates.Any()` to `alternates.Count > 0`.
  The previous code rebuilt a `Where`-filtered `IEnumerable<>` on every call and then
  double-enumerated it with `.Any()` + the service call — fired on every tick during
  a `ReserveHost` hold-pattern (harvester waiting for an occupied dock). Landed
  ahead of the Batch B ActorIndex scaffolding because the per-activity scope did not
  need a per-player index. Patch:
  `patches\PERF-017_ImprovedMoveToDock.cs.patch`.
  **Rebaseline captured 2026-04-21** on the same *Forest* / 1 Cabal / ~60-unit scenario:
  - `**ms/tick`: 0.5 ms → 0.1–0.2 ms** (60–80 % reduction vs Batch A post-fix baseline;
  ~5–10× faster than the pre-Batch-A 1.0 ms/tick).
  - `**ms/frame`: 6 ms steady** (~166 FPS), unchanged from Batch A — render path is not a
  bottleneck at this workload.
  - `dotnet-counters` session snapshot (longer session than Batch A's 30 s window, so raw
  alloc and JIT totals are not directly comparable to Batch A's 3.87 MB / 0.223 s):
  `dotnet.assembly.count` 70, `dotnet.jit.compilation.time` 6.911 s (cumulative session
  figure, full mod + map load including shader / audio / replay), GC collections gen0
  502 / gen1 132 / gen2 20, `dotnet.gc.pause.time` 0.644 s total. Exceptions observed
  (IOException×2, SocketException×2, TaskCanceledException×1, TimeoutException×20) are
  all network / lobby-chatter; none originate in the hot tick path and none appear after
  the perf changes that did not appear before them.
  - **Interpretation:** the tick-time collapse confirms the hypothesis — `MovementSpeedForCell`
  was the dominant per-cell allocator during pathfinding (PERF-024 removed three iterator
  wrappers per call firing in the thousands-per-tick), with PERF-028's per-armament
  delegate elimination compounding at high unit counts and PERF-017's per-(tick, dockHost)
  cache removing redundant world scans on harvester hold-patterns.
- **2026-04-21** — **Batch A post-fix baseline captured; Batch A testing row closed in
`Todo.md`.** Same scenario as the PERF-046 baseline (map *Forest* CnC, 1 Cabal AI, ~60
units on screen, SDK 10.0.203) re-measured after PERF-009/010/011/014/015/016/020/055
landed:
  - **ms/tick 1.0 → 0.5 ms (~50% reduction)** via in-game `Debug.PerfText`; ms/frame
  steady at ~~6.0 ms (~~166 FPS, display-capped).
  - `dotnet-counters` 30 s sample: `dotnet.gc.heap.total_allocated` ≈ 3.87 MB;
  `dotnet.gc.collections` = 0 across Gen-0/1/2 in-window;
  `dotnet.monitor.lock_contentions` = 0; `dotnet.gc.pause.time` = 0 s.
  - `dotnet.jit.compilation.time` = 0.223 s, 936 methods, 82,340 bytes IL — identical
  to the PERF-046 baseline, confirming Batch A added no steady-state JIT surface
  (as expected: all edits are existing methods, no new types or virtuals).
  - Interactive smoke pass in the same session: pathfinding selector cycled OpenRA /
  Classic / Improved without exceptions; harvester docking under Improved
  "seemed better" (user feedback, no stalls); no mid-game crashes or desync
  warnings.
  Batch A is now the new baseline for Batch B measurements.
- **2026-04-21** — **Aircraft rearm-at-pad investigation (non-regression).** User skirmish
test flagged "Orcas / Chinooks never land," "Apaches seem OP," "Orcas regen ammo in
air, return to pad blocked." Investigation against `OpenRA-source\mods\cnc\rules\ aircraft.yaml` and `OpenRA-source\mods\ra\rules\aircraft.yaml` confirmed this is stock
OpenRA mod-rules design, **not** a regression from PERF-014 / PERF-020:
  - CnC `ORCA` / `HELI` — have `AmmoPool` + `ReloadAmmoPool` (Delay 100 / 70, Count 2)
  but **no** `Rearmable` trait. `ReloadAmmoPool` regenerates ammo in-air; with no
  `Rearmable`, aircraft never issue a landing request, so `AircraftLandingService`
  (PERF-020) and `ImprovedAircraftLanding` (PERF-014) are never invoked for them.
  - RA `HELI` (Longbow) — has **no `AmmoPool` at all** ⇒ literally infinite ammo. RA
  `HIND` — has `AmmoPool: 24, ReloadDelay: 8` (2.4 s in-air cycle) and no
  `Rearmable`.
  - Chinook (`^Helicopter` with `Carryall` / cargo load) lands briefly only because
  the cargo activity forces a ground touch; that is correct.
  Net effect: Batch A aircraft-landing code paths are currently covered in skirmish only
  by the Chinook load cycle. Restoring classic rearm-at-pad is queued as **GAME-001**
  (CnC TD Orca + HELI) and **GAME-002** (RA Longbow + HIND) in `Todo.md`; a debug-panel
  readout of the currently-selected algorithm is queued as **UI-002**. All three are
  YAML / small-UI changes; no C# code changes required.
- **2026-04-21** — **Batch A completion pass (PERF-009, 010, 011, 014, 015, 016, 020, 055).**
Seven ReCnC-owned perf items plus the PERF-055 net10 warning cleanup landed in a single
session. Full-solution `make.cmd all` on SDK 10.0.203: **0 errors, 12 warnings — 100%
`NU1901` (NuGet.CommandLine advisory), 0 `SYSLIB*`, 0 `CS0672`**, matching the PERF-055
acceptance criterion. All edits are bracketed with `BEGIN/END ReCnC PERF-NNN` sentinels for
upstream-merge resilience; per-ticket backups in `patches\PERF-NNN-backup\`, unified-diff
patches saved as `patches\PERF-NNN_<file>.patch`. Each ticket below remains independently
revertible via its patch.
  - **PERF-009** *(ClassicPathfinder.FindPathClassic)* — pooled `path` list is now handed
  back to the caller instead of being deep-copied; the `finally` block guards with
  `if (path != null)` so only paths we retained ownership of get recycled. Removes an
  `O(N)` `new List<CPos>(path)` + copy on every successful Classic pathfind.
  - **PERF-010** *(ImprovedPathfinder.FindPathAStar)* — added a pooled `HashSet<CPos>`
  closed set (`RentClosed` / `ReturnClosed`, max pool depth 4 to match the existing
  `cameFrom` / `gScore` pools) so stale PriorityQueue entries and already-settled
  neighbors short-circuit on dequeue; `ReconstructPath` pre-sizes its result to
  `cameFrom.Count + 1`.
  - **PERF-011** *(PathfindingStrategyManager.CurrentAlgorithm)* — resolved enum is cached
  in a `PathfindingAlgorithm?` field, so the two `string.Equals(StringComparison...)`
  comparisons fire once per cache invalidation instead of per `GetStrategy` call.
  `ClearCache()` / `SetAlgorithmFromLobby` reset the cache so lobby-driven changes are
  still honored.
  - **PERF-014** *(ImprovedAircraftLanding)* — landing queues migrated from
  `Dictionary<Actor, Queue<LandingRequest>>` to `Dictionary<Actor, List<LandingRequest>>`;
  priority-asc / arrival-asc insertion-sort replaces the `ToList().OrderByDescending() .ThenBy().ToList()` rebuild on every reserve/release; three `.Any(lambda)`
  contains-checks rewritten as manual loops. Eliminates the per-enqueue closure + sort
  allocations even at `MaxQueuePerPad = 3`.
  - **PERF-015** *(SmartDockingService.FindBestDock)* — `List<DockCandidate>` allocation
    - `OrderBy(Score).ToList()` deleted; replaced by a single-pass best + runner-up scan.
    `lastUsedDock.TryGetValue` hoisted out of the candidate loop so it fires once per
    `FindBestDock` call instead of once per candidate.
  - **PERF-016** *(SmartDockingService.RegisterQueueEntry)* — the
  `OrderByDescending(Priority).ThenBy(ArrivalTick).ToList()` rebuild is gone; new entries
  are inserted at the correct priority-desc / arrival-asc position in the existing
  `List<DockQueueEntry>`. `.Any(predicate)` contains-check rewritten as a manual loop.
  - **PERF-020** *(AircraftLandingService.GetStrategy)* — active `IAircraftLanding` +
  `AircraftLandingAlgorithm` are cached on the service; the strategy is only re-fetched
  from `AircraftLandingManager` when the algorithm enum actually changes. Combines with
  the manager's existing per-strategy cache to keep the hot path (IsLandingZoneClear /
  IsLandingZoneAvailableFor / FindAlternateLandingZone) to one field read + one
  interface call.
  - **PERF-055** *(net10 SYSLIB0050/51/CS0672 cleanup)* —
  `FormatterServices.GetUninitializedObject` → `RuntimeHelpers.GetUninitializedObject`
  in `OpenRA.Game\Map\ActorReference.cs` and `OpenRA.Mods.Common\Scripting\Global\ ActorGlobal.cs`. The two `Exception.GetObjectData` overrides in `OpenRA.Game\ FieldLoader.cs` and `OpenRA.Utility\Program.cs` are preserved for binary-formatter
  compat but wrapped in `#pragma warning disable SYSLIB0051` + `#pragma warning disable CS0672` / `restore`. Warning count on `make.cmd all` (SDK 10.0.203): 14 →
  12 (NU1510 removed by PERF-046, SYSLIB0050 ×2 + SYSLIB0051 ×2 + CS0672 ×2 removed
  by PERF-055, remaining 12 are NU1901 NuGet.CommandLine advisories unrelated to
  this work).
- **2026-04-21** — **PERF-046 spike (Eluant / .NET 10): PASSED.** Sentinel-wrapped edits in
`OpenRA-source\Directory.Build.props` (dropped Mono / netstandard2.1 conditionals; TFM set
to `net10.0`; removed Mono-only `AD0001` conditions on Roslynator analyzers) and
`OpenRA-source\OpenRA.Game\OpenRA.Game.csproj` (dropped `System.Runtime.Loader 4.3.0`,
`System.Collections.Immutable 6.0.0` Mono branch, `System.Threading.Channels 6.0.0` — all
in-box on net10; bumped `Microsoft.Extensions.DependencyModel` 6.0.2 → 10.0.0). Every edit
is bracketed with `BEGIN/END ReCnC PERF-046` sentinels. Full-solution `make.cmd all` on SDK
10.0.203: **0 errors, 14 warnings** (net10 flagged `SYSLIB0050/51` on `FieldLoader.cs`,
`ActorReference.cs`, `ActorGlobal.cs`, `OpenRA.Utility\Program.cs`; one `NU1510`
Microsoft.Win32.Registry-is-in-box notice; `NU1901` NuGet.CommandLine pre-existing). Eluant
runtime probe (`tools\perf\EluantProbe\Program.cs`) instantiated `LuaRuntime` and executed
`return 6*7` → `42` under net10 without touching Eluant's managed or native assemblies.
Full patch backups in `patches\PERF-046-backup\`; unified-diff patches saved as
`patches\PERF-046_Directory.Build.props.patch` and `patches\PERF-046_OpenRA.Game.csproj.patch`
for replay after upstream merges.
- **2026-04-21** — **PERF-046 Lua mission smoke: PASSED (interactive).** D2K main-menu shellmap
(`mods\d2k\maps\shellmap\d2k-shellmap.lua`) animates normally on net10; CnC GDI Mission 01
(`mods\cnc\maps\gdi01\gdi01.lua`) runs end-to-end — reinforcement drops, Nod patrol triggers,
and `Media.PlaySpeechNotification` all fire as expected. Confirmed by user.
- **2026-04-21** — **PERF-046 net10 baseline captured; ticket CLOSED.** Skirmish measurement on
map *Forest* (CnC), 1 Cabal AI opponent, ~60 units on screen, SDK 10.0.203:
  - Frame time ~~6.0 ms (~~166 FPS), tick time ~1.0 ms via in-game `Debug.PerfText` overlay.
  - `dotnet.gc.heap.total_allocated` = 3.87 MB across a ~30 s `dotnet-counters` sample;
  `dotnet.gc.collections` = 0 for all generations in-window (GC pressure very low on the
  sampled workload).
  - `dotnet.jit.compilation.time` = 0.223 s, 936 methods, 82,340 bytes IL; 32 logical CPUs.
  This is now the canonical baseline that every subsequent PERF item (PERF-011, PERF-015,
  PERF-016, PERF-009, PERF-010, PERF-014, PERF-020, PERF-055, …) will be measured against.
- **2026-04-21** — **PERF-046 NU1510 cleanup.** Dropped `Microsoft.Win32.Registry 5.0.0` from
`OpenRA-source\OpenRA.Mods.Common\OpenRA.Mods.Common.csproj` (package is in-box on net10;
the reference triggered NU1510). Sentinel-wrapped and patched at
`patches\PERF-046_OpenRA.Mods.Common.csproj.patch`. Build-warning count 14 → 12. Remaining
12 warnings are 6× pre-existing `NU1901` NuGet.CommandLine low-severity advisory + 6×
`SYSLIB0050/51`/`CS0672` obsolete-serialization notices captured as **PERF-055** in
`Todo.md` for a follow-up session.
- **2026-04-21** — **PERF-046 publish pipeline: PASSED.** `build-launchers.ps1` runs end-to-end on
net10 with no script edits required (TFM is driven entirely by `Directory.Build.props`, so the
launcher script is TFM-agnostic). All three launcher EXEs (`RedAlert.exe`, `TiberianDawn.exe`,
`Dune2000.exe`) publish self-contained for `win-x64`; portable ZIP generates at **72.27 MB**
vs **69.89 MB** baseline on net6 (+2.38 MB for the larger net10 self-contained runtime);
wall-clock build time **105 s** vs ~125 s baseline (caching-affected, not a real comparison).
`rcedit` "Unable to parse version string for ProductVersion" warning reproduces on any
non-numeric tag (pre-existing behavior, not a net10 regression); cosmetic only — EXEs still
launch and run.

### Decisions

- **2026-04-21** — Batch A start: **PERF-046 (.NET 10 port) goes first.** Gated by a 2-hour
time-boxed OpenRA-Eluant / Lua spike; only proceed if Eluant loads and a mission Lua smoke
test runs clean on net10. Publish mode remains **self-contained** to match today's
`build-launchers.ps1` (`--self-contained true`); installer / portable ZIP size is not a
blocker. Recorded next to the PERF-046 ticket in Todo.md.

### Documentation

- **2026-04-21** — Todo.md review and reconciliation:
  - Split the duplicated "Verify OpenRA build" item: `make.cmd all` marked complete (mirrors the
  existing Completed Setup entry from 2026-03-28); `launch-game.cmd` smoke test kept as an
  open backlog item so the two sources of truth no longer contradict.
  - Fixed the Performance Fix Plan intro paragraph to reflect the current ID range (52 findings
  across PERF-001…PERF-054, with PERF-044 / PERF-045 intentionally reserved).
  - Added a numbering note next to the Runtime Upgrade section explaining the 043 → 046 gap.
  - Re-ran `OpenRA-source\make.cmd all` to confirm the tree still compiles cleanly:
  0 errors, 8 warnings (EOL `net6.0` + NuGet.CommandLine `NU1901` — both tracked under
  PERF-046, not regressions). Logged the re-verification under "Ready for Testing" in
  Todo.md. In-game skirmish tests for pathfinding / aircraft / artillery / docking remain
  open for interactive user verification.

### Added

- **2026-04-05** — **ENHANCED: `build-launchers.ps1`** - Ported remaining functionality from Linux `buildpackage.sh` to Windows PowerShell. Script now fully replaces the shell-based build process.
  **New features:**
  - **Icon Generation:** Creates `.ico` files from PNG artwork using .NET `System.Drawing`. No ImageMagick required.
    - Generates `ra.ico`, `cnc.ico`, `d2k.ico` from artwork PNGs (16x16 through 256x256)
  - **PE Header Patching:** Sets `/LARGEADDRESSAWARE` flag and GUI subsystem via direct binary manipulation (ports `fixlauncher.py`)
  - **EXE Metadata:** Downloads `rcedit-x64.exe` and uses it to set:
    - Product version, ProductName, CompanyName
    - FileDescription, LegalCopyright
    - Application icon (embedded in EXE)
  - **GeoIP Database:** Downloads `IP2LOCATION-LITE-DB1.IPV6.BIN.ZIP` for server hosting (cached 30 days)
  - **Mod Versioning:** Updates `Version:` line in all `mod.yaml` files
  - **OpenRA.Utility.exe:** Now built as part of core assemblies (required for NSIS mod registration)
  - **OpenRA.Server.exe:** Now built as part of core assemblies (for dedicated server hosting)
  - **Portable ZIP:** Creates `ReCnC-{tag}-{platform}-winportable.zip` alongside installer
  - `**-PortableOnly` switch:** Skip NSIS installer, create only portable ZIP
  **Example commands:**
  ```powershell
  .\build-launchers.ps1                           # Basic build, portable ZIP only
  .\build-launchers.ps1 -BuildInstaller           # Full build with NSIS installer
  .\build-launchers.ps1 -BuildInstaller -PortableOnly  # ZIP only, no installer
  .\build-launchers.ps1 -Platform x86 -Tag "release-20260405"  # 32-bit with custom tag
  ```
  **Build output (tested 2026-04-05):**
  - Portable ZIP: 69.89 MB
  - NSIS Installer: 58.62 MB
  - Build time: ~125 seconds (x64, Release)
- **2026-04-05** — Created `build-launchers.ps1` - Windows-native PowerShell script to build game launcher executables (RedAlert.exe, TiberianDawn.exe, Dune2000.exe) and optionally compile the NSIS installer. This replaces the need for the Linux-based `packaging/windows/buildpackage.sh` which requires bash, wine64, makensis, and ImageMagick.
  - Use `-BuildInstaller` flag to compile the NSIS installer after building
  - Use `-Tag "release-YYYYMMDD"` to set the version tag
  - Example: `.\build-launchers.ps1 -BuildInstaller -Tag "release-20250405"`

### Fixed

- **2026-04-05** — Changed default installer path from `C:\Program Files\OpenRA` to `C:\Games\ReCnC\` in `OpenRA-source/packaging/windows/OpenRA.nsi`. Installer now defaults to the ReCnC-specific location.

### Added (Historical)

- **2026-03-27** — Project scaffolding: `Outline.txt` (workflow), `Todo.md` (planning), `changelog.md` (this file). Documents alignment between ReCnC house conventions (PowerShell 5.1, WinForms for new tools, CMTrace/CSV/XML where applicable) and the OpenRA tree under `OpenRA-source\`.

### Research

- **2026-03-27** — Code review of OpenRA pathfinding and aircraft systems. Documented findings in `Todo.md` under "Known OpenRA Issues":
  **Pathfinding issues identified:**
  - `Move.cs` lines 307-314: Bridge/chokepoint re-pathing bug — units re-path with `BlockedByActor.All` when blocked, treating moving units as permanent obstacles. Acknowledged TODO on line 133.
  - `GroundStates.cs`: AI squads get stuck, use 2.5s timeout hack to recover.
  - `Mobile.cs`: `NearestMoveableCell()` described as "entire method is a hack."
  **Aircraft issues identified:**
  - `Aircraft.cs` line 471: Moving twice per tick visibility hack.
  - `Resupply.cs`: 4 HACKs around repair + rearm flow; reservation system can't handle repairs.
  - `Land.cs`: Multiple TODOs for approach trajectory, altitude handling, CCW/CW turns.
  - `Fly.cs` line 205: Blocked detection uses crude "moved < 64 WDist in 5 ticks" heuristic.
  - `Reservable.cs`: Single-pad reservation blocks multiple aircraft.
  **Other issues cataloged:** AmmoPool rearm hacks, capture-while-moving hack, tunnel fog hack, isometric minelayer bug, resource density lerp bug, missile TODOs.
- **2026-03-28** — Cloned all three EA C&C source repositories:
  - `CnC_Remastered_Collection` — Remastered DLL sources (TD + RA, 2020)
  - `CnC_Tiberian_Dawn` — Original TD source (1995, game code only)
  - `CnC_Red_Alert` — Original RA source (1996, full engine + libraries)
  Key files acquired:
  - `TIBERIANDAWN/FINDPATH.CPP` — Classic pathfinding (LOS + edge-following algorithm)
  - `TIBERIANDAWN/FOOT.CPP` — Ground unit movement
  - `TIBERIANDAWN/AIRCRAFT.CPP` — Aircraft logic
  - Same files for `REDALERT/`
  **Algorithm discovery:** Classic C&C uses fundamentally different pathfinding than OpenRA:
  - Classic: Line-of-sight path following + edge-following around obstacles
  - OpenRA: Hierarchical A* with abstract graph and heuristics
  Classic approach is simpler (~1500 lines) and may avoid some of the "treating moving units as blockers" issues.
- **2026-03-28** — Completed pathfinding algorithm comparison:
  **Critical finding:** Classic C&C uses COST-BASED blocking (`MoveType` enum):
  - `MOVE_MOVING_BLOCK` (moving unit) = cost 3, PASSABLE with penalty
  - OpenRA's `BlockedByActor.All` = COMPLETE blocker, triggers re-path
  This explains the bridge problem: Classic paths THROUGH moving units, OpenRA paths AROUND.
- **2026-03-28** — Completed aircraft landing comparison:
  Classic C&C uses simple `Is_LZ_Clear()` check + `New_LZ()` fallback.
  OpenRA uses complex `Reservable` system with multiple HACKs.

### Milestones

- **2026-03-28** — **User approved selector approach** for both pathfinding and aircraft.
  Strategy: Keep OpenRA implementations as-is (bugs and all) as one option, port classic C&C implementations as second option, add in-game settings to switch between them.
- **2026-03-28** — **IMPLEMENTED: Pathfinding Selector**
  Created swappable pathfinding system allowing users to choose between OpenRA's HPA* and classic C&C LOS+edge-following.
  Files created in `OpenRA.Mods.Common/Pathfinder/`:
  - `IPathfindingStrategy.cs` — Strategy interface defining `FindPath()` and `PathExists()` methods
  - `OpenRAPathfinder.cs` — Wrapper around existing `HierarchicalPathFinder`, preserves all current behavior
  - `ClassicPathfinder.cs` — Port of FINDPATH.CPP algorithm:
    - LOS path following toward target
    - Edge-following around obstacles (both clockwise and counter-clockwise)
    - Takes shorter of two detour paths
  - `MoveType.cs` — Port of cost-based blocking enum from DEFINES.H:
    - `MOVE_OK` = 1 (clear)
    - `MOVE_MOVING_BLOCK` = 3 (moving unit — PASSABLE with penalty!)
    - `MOVE_DESTROYABLE` = 8 (enemy)
    - `MOVE_TEMP` = 10 (friendly)
    - `MOVE_NO` = 0 (impassable)
  - `PathfindingStrategyManager.cs` — Selects strategy based on settings
  **Key difference:** Classic treats moving units as passable (cost 3), OpenRA treats as complete blockers. This should fix the bridge/chokepoint re-pathing problem when using Classic mode.
- **2026-03-28** — **IMPLEMENTED: Aircraft Landing Selector**
  Created swappable aircraft landing system allowing users to choose between OpenRA's Reservable system and classic C&C Is_LZ_Clear logic.
  Files created in `OpenRA.Mods.Common/Traits/Air/`:
  - `IAircraftLanding.cs` — Strategy interface defining landing zone management methods
  - `OpenRAAircraftLanding.cs` — Wrapper around existing `Reservable` system, preserves all current behavior/HACKs
  - `ClassicAircraftLanding.cs` — Port of AIRCRAFT.CPP logic:
    - `Is_LZ_Clear()` — Simple check: self on pad OR in radio contact
    - `New_LZ()` — Concentric ring scan for alternate pads
    - Radio contact model instead of complex reservation
  - `AircraftLandingManager.cs` — Selects strategy based on settings
- **2026-03-28** — **IMPLEMENTED: Game Settings**
  Added to `OpenRA.Game/Settings.cs`:
  - `PathfindingAlgorithm` — "OpenRA" (default) or "ClassicCnC"
  - `AircraftLandingAlgorithm` — "OpenRA" (default) or "ClassicCnC"
  Settings can be changed in settings.yaml or via future UI dropdown.
- **2026-03-28** — **INTEGRATED: Wired up strategy pattern to PathFinder.cs**
  Modified `OpenRA.Mods.Common/Traits/World/PathFinder.cs`:
  - Added `PathfindingStrategyManager` instance
  - `FindPathToTarget()` now uses selected strategy
  - `PathExistsForLocomotor()` uses selected strategy
  - `PathMightExistForLocomotorBlockedByImmovable()` uses selected strategy
  - Added `CurrentAlgorithm` property for debugging
  Created `OpenRA.Mods.Common/Traits/World/AircraftLandingService.cs`:
  - World trait providing aircraft landing services
  - Uses `AircraftLandingManager` to select strategy
  - Exposes `IsLandingZoneClear()`, `FindAlternateLandingZone()`, `IsLandingZoneAvailableFor()`
  **To enable:** Add `AircraftLandingService:` to world.yaml
- **2026-03-28** — **IMPLEMENTED: Improved (Third Option) Algorithms**
  Created `OpenRA.Mods.Common/Pathfinder/ImprovedPathfinder.cs`:
  - Combines HPA* with cost-based blocking from classic C&C
  - Moving units have cost penalty (3) instead of being complete blockers
  - Movement prediction to determine if waiting is better than re-routing
  - Smart chokepoint handling
  Created `OpenRA.Mods.Common/Traits/Air/ImprovedAircraftLanding.cs`:
  - Queue-based landing system (up to 3 aircraft can queue per pad)
  - ETA prediction for pad availability
  - Priority queuing for damaged aircraft
  - Load balancing across multiple pads
  - No reservation conflicts with repair logic
  Updated enums and managers to support three options:
  - `PathfindingAlgorithm`: OpenRA | ClassicCnC | Improved
  - `AircraftLandingAlgorithm`: OpenRA | ClassicCnC | Improved
- **2026-03-28** — **IMPLEMENTED: Smart Docking Selector**
  Created intelligent docking system for harvesters, aircraft, and repair-seeking units.
  **Core question answered:** "Is waiting here faster than going somewhere else?"
  Files created in `OpenRA.Mods.Common/Traits/`:
  - `IDockingStrategy.cs` — Strategy interface for dock selection
  - `OpenRADockingStrategy.cs` — Wrapper for existing `ClosestDock()` (distance + occupancy count)
  - `ClassicDockingStrategy.cs` — Port of `Find_Docking_Bay()` from TECHNO.CPP:
    - Simple distance-based selection
    - Prefers "leader" (primary) buildings
    - No queue consideration (matches original behavior where harvesters pile up)
  - `ImprovedDockingStrategy.cs` — Uses SmartDockingService for intelligent decisions
  - `SmartDockingService.cs` — World trait providing wait-vs-travel analysis:
    - Calculates actual travel time to each dock
    - Estimates wait time based on queue length
    - Compares: travel_to_alt + wait_at_alt VS wait_here
    - Remembers preferred docks (familiarity bonus)
    - Priority queuing for damaged units
  - `DockingStrategyManager.cs` — Selects strategy based on settings
  Files created in `OpenRA.Mods.Common/Activities/`:
  - `ImprovedMoveToDock.cs` — Enhanced MoveToDock activity:
    - Continuously re-evaluates best dock while traveling/waiting
    - Can redirect mid-journey if alternate becomes better
    - Registers with SmartDockingService queue
  Added to `OpenRA.Game/Settings.cs`:
  - `DockingAlgorithm` — "OpenRA" (default), "ClassicCnC", or "Improved"
  **Example scenario (harvesters at refineries):**
  - Refinery A: nearby, 3 harvesters waiting = ~300 tick wait
  - Refinery B: 10 cells away (~200 tick travel), 0 waiting = 0 tick wait
  - Improved decision: 200 < 300, redirect to Refinery B!
- **2026-03-28** — **BUILD VERIFIED**
  Successfully compiled OpenRA with all ReCnC modifications.
  Build command: `.\make.ps1 -Command all -Configuration Debug`
  Result: 0 errors, 241 warnings (code style, not functional)
  All 19 new C# files compile successfully:
  - Pathfinding: 6 files (IPathfindingStrategy, OpenRA/Classic/Improved pathfinders, MoveType, Manager)
  - Aircraft Landing: 6 files (IAircraftLanding, OpenRA/Classic/Improved landing, Manager, Service)
  - Docking: 7 files (IDockingStrategy, OpenRA/Classic/Improved docking, SmartDockingService, Manager, ImprovedMoveToDock)
- **2026-03-28** — **YAML Configuration Added**
  Registered new world traits in all four mod world.yaml files:
  - `mods\cnc\rules\world.yaml`
  - `mods\ra\rules\world.yaml`
  - `mods\ts\rules\world.yaml`
  - `mods\d2k\rules\world.yaml`
  Traits added (after PathFinder:):
  - `SmartDockingService:` — Wait-vs-travel calculation for harvesters/aircraft/repair
  - `DockingStrategyManager:` — Selects docking strategy based on settings
  - `AircraftLandingService:` — Provides aircraft landing management
  Build 004 verified: 0 errors, 6 warnings (framework only)

---

## Legacy Change-Tracking Archive

Merged from the retired `changes.md` tracker on **2026-05-08** so `changelog.md` remains the single change-history source.

### 2026-03-28 Session 1 — Original Selector Implementation

#### Build Logs

- `build-logs/2026-03-28_001_initial-build.log` — First attempt, 7 errors.
- `build-logs/2026-03-28_002_fixes-applied.log` — Second attempt, 7 new errors.
- `build-logs/2026-03-28_003_build-success.log` — Success, 0 errors.

#### Files Created

| Area | Files |
|------|-------|
| Pathfinding system | `OpenRA.Mods.Common\Pathfinder\IPathfindingStrategy.cs`, `OpenRAPathfinder.cs`, `ClassicPathfinder.cs`, `ImprovedPathfinder.cs`, `MoveType.cs`, `PathfindingStrategyManager.cs` |
| Aircraft landing system | `OpenRA.Mods.Common\Traits\Air\IAircraftLanding.cs`, `OpenRAAircraftLanding.cs`, `ClassicAircraftLanding.cs`, `ImprovedAircraftLanding.cs`, `AircraftLandingManager.cs`, `OpenRA.Mods.Common\Traits\World\AircraftLandingService.cs` |
| Docking system | `OpenRA.Mods.Common\Traits\IDockingStrategy.cs`, `OpenRADockingStrategy.cs`, `ClassicDockingStrategy.cs`, `ImprovedDockingStrategy.cs`, `SmartDockingService.cs`, `DockingStrategyManager.cs`, `OpenRA.Mods.Common\Activities\ImprovedMoveToDock.cs` |

#### Build-Fix Notes

| File | Fix |
|------|-----|
| `ClassicPathfinder.cs`, `ImprovedPathfinder.cs`, `ImprovedAircraftLanding.cs` | Added missing `using OpenRA.Traits;`. |
| `ClassicDockingStrategy.cs`, `IDockingStrategy.cs` | Escaped `C&C` as `C&amp;C` in XML comments. |
| `ClassicDockingStrategy.cs` | Changed `int bestDistance` to `long bestDistance`. |
| `ImprovedAircraftLanding.cs`, `ClassicAircraftLanding.cs` | Replaced explicit `string[]` declarations with `var` for HashSet compatibility. |

### 2026-04-21 Session Archive

- Performance pass replaced `SortedSet` with `PriorityQueue` in `ImprovedPathfinder`, added collection pooling to `ImprovedPathfinder` and `ClassicPathfinder`, and changed `Move.cs` path truncation from LINQ allocation to in-place `RemoveRange`.
- Artillery auto-targeting fix changed RA `V2RL` / `ARTY` and TD `MSAM` toward weapon-range scans and `AttackAnything` stance, matching original C&C hunt-style behavior.
- Aircraft landing fix `AIR-v2-001` set RA helicopters to stay on helipads after resupply and removed the `Resupply.cs` `wasRepaired` forced-takeoff hack.
- Targeted perf pass `PERF-028` / `PERF-024` / `PERF-017` saved backups under `patches\PERF-NNN-backup\`, unified diffs under `patches\PERF-NNN_*.patch`, and built with `OpenRA-source\make.cmd all` on SDK 10.0.203: **0 errors**, NU1901-only warnings.
- World traits added to all four mod `world.yaml` files after `PathFinder:`: `SmartDockingService:`, `DockingStrategyManager:`, and `AircraftLandingService:`.

---

## Format (for future entries)

- Use reverse-chronological sections under `[Unreleased]` or version/date headings.
- Each bullet: what changed, where (paths), and rollback hint if non-obvious.
