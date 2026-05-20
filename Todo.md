# ReCnC — Todo

Work from this list first; update statuses as you go.

## Backlog

### Bug Investigation — IN PROGRESS (identified 2026-04-25 from OpenBugs.md; notes rolled in 2026-04-25)

These are open bugs from `OpenBugs.md` plus BUG-007 (build/version). Investigation notes from
codebase review are under each item. **Active execution plan (BUG-009, 010, 011):**
`C:/Users/bthorp/.cursor/plans/openbugs_six-pack_9e8fd989.plan.md` (includes **classic EA source**
research for 009/010). **BUG-012–025** added 2026-04-27 from user triage (see changelog). Older
combined plan `bugs_and_ai_rename_b2b25372.plan.md` is historical.

**Overall Engine:**
- [ ] **BUG-001:** MRLS still has issues targeting things into the fog of war
  - **Investigation (2026-04-25):** `AutoTarget.ChooseTarget` merges live actors in a scan
    circle with **frozen actors** from `FrozenActorLayer` when `allowMove ||
    Armament.TargetFrozenActors` — artillery can legally pick shrouded targets from intel.
    Live actors under shroud are skipped with `!CanBeViewedByPlayer`. For **bots** only,
    frozen actors with `FrozenActor.Actor == null` are skipped (see
    `OpenRA.Mods.Common/Traits/AutoTarget.cs` ~387–396; comment mentions AI endlessly shelling
    fog). **Human** MLRS/MSAM oddities likely involve frozen vs actor targeting, weapon LOS, or
    stance — not yet reproduced in-game.
  - Partial YAML mitigation already: COMBAT-001 / COMBAT-001b (`ScanRadius`, stances). Still
    needs a **repro case** (replay / steps) and trace through `AttackBase` for frozen targets.
- [ ] **BUG-002:** Circle drawn around the Construction Yard — what is it?
  - **Investigation:** Not weapon `RenderRangeCircle` on FACT. It is **`BaseProvider`** on FACT:
    “Limits the zone where buildings can be constructed” — TD uses **`Range: 14c0`** in
    `mods/cnc/rules/structures.yaml`. Circle color/ready state comes from
    `OpenRA.Mods.Common/Traits/Buildings/BaseProvider.cs` (`RangeCircleAnnotationRenderable`).
  - **UX direction (user):** Paint **transparent green** on cells where building is allowed;
    **orange** inside the BaseProvider circle where placement is **not** valid; **red** where
    placement is blocked by **actors/terrain rules** even when the cursor is not hovering (full
    disk preview, not hover-only). Implementation touches place-building preview + annotation
    render path (likely `PlaceBuilding`, `BaseProvider`, world interaction layer).
- [ ] **BUG-003:** Airstrike isn't available when "no superweapons" is set
  - **Investigation:** Lobby tier **`nopowers`** (`ProvidesTechPrerequisite@nosuper` in
    `mods/cnc/rules/player.yaml`) grants `techlevel.low, medium, high` only — **no**
    `techlevel.superweapons`. HQ **`AirstrikePower`** uses **`Prerequisites: ~techlevel.superweapons`**
    (`mods/cnc/rules/structures.yaml`), same bucket as Ion/Nuke-style powers — so airstrike is
    intentionally off when "no powers" is picked.
  - **Direction (user):** Add **another tech tier** — e.g. **airstrike allowed, superweapons
    off** — **below** current "No Superweapons" strictness: grant a new prerequisite (e.g.
    `techlevel.airstrike` or similar) on that tier, move **`AirstrikePower`** to require that
    tier **instead of** full `techlevel.superweapons`; keep Ion/Nuke on `superweapons` only.
    Still document **OG TD** behavior for historians (verify in classic / docs).
- [ ] **BUG-004:** Cannot repair walls
  - **Investigation (OpenRA):** `^Wall` in `mods/cnc/rules/defaults.yaml` has **no**
    `RepairableBuilding` / engineer-repair path (unlike `^TechBuilding`). Rules never enabled
    wall repair.
  - **Direction (user, backlog):** Walls **repairable in TD**; **TS** = **build-over == repair**
    when that mod path is worked. **Not** in the current three-bug execution plan.
  - **Classic source (2026-04-25):** EA `CnC_Tiberian_Dawn/CELL.CPP` `CellClass::Reduce_Wall`
    only applies **damage** to overlay walls (no heal path). `HouseClass::Sell_Wall` sells walls.
    **Implication:** DOS TD in this tree is **damage / sell**, not engineer-repair — any OpenRA
    wall repair is **QoL / design**, not strict OG parity unless remaster differs.
- [ ] **BUG-005:** Destroying churches doesn't leave a crate
  - **Investigation:** CNC churches are civ actors like **V01** (Fluent: “Church”); YAML only
    `SpawnActorOnDeath: V01.Husk` — **no** crate spawn on death. No `SpawnCrateOnDeath`-style
    trait found on those actors in `mods/cnc/rules/civilian.yaml`. OG behavior: high chance of
    **high-quality crate** — needs trait + balance table matching classic odds.
- [ ] **BUG-006:** Pathfinding — units in a group can "give up" and not move
  - **Investigation:** No single "give up" string in `Mobile.cs` from a quick pass; behavior
    usually ties to **null path**, **blocked destination**, formation / group locomotion, or
    activity cancellation. Overlaps possible with PATH-v2 / classic pathfinder work in this repo.
  - **Next:** Reproduce with replay; grep `Move` activity + group move for early exit when path
    fails; compare `OpenRAPathfinder` vs `ClassicPathfinder` / `ImprovedPathfinder` when enabled.
- [ ] **BUG-024:** Lobby / **multiplayer** settings **reset every new MP game** (reporter had to reconfigure each session). **Research (2026-04-28):** [SkirmishLogic.cs](OpenRA-source/OpenRA.Mods.Common/ServerTraits/SkirmishLogic.cs) only runs for **`ServerType.Skirmish`**, persisting `skirmish.<modId>.yaml` to support dir; **`ServerType.Multiplayer`** has **no** stock equivalent. **Next:** product decision — extend persistence to MP / host defaults / `LastServer` adjacent options, or document limitation.
- [ ] **BUG-025:** Vehicle **husks block movement** — parity vs classic: EA `CnC_Tiberian_Dawn/UNIT.CPP`
    and `CnC_Red_Alert/CODE/UNIT.CPP` destroy path uses **`delete this`** after explosion (no
    persistent wreck **unit**). Trace OpenRA/ReCnC husk spawn / `SpawnActorOnDeath` / locomotor
    blocking; decide design (fade, crush-only, no husk, etc.).

**Build/Version:**
- [ ] **BUG-007:** Internal build number doesn't match build date
  - **Investigation / fix landed (2026-04-25):** Stale default **`release-20250330`** in
    `build-launchers.ps1` and all `mods/*/mod.yaml` `Metadata: Version:` caused mismatch vs real
    build dates. **Changed:** `$Tag` default to `release-$(Get-Date -Format 'yyyyMMdd')`;
    baseline `Version:` bumped in source `mod.yaml` files. `Directory.Build.props` had no
    assembly file version tied to game display — primary sources were script + mod metadata.
  - **Todo:** Mark complete after one **full launcher build** confirms in-game version + packaged
    `VERSION` file match expected date; keep `-Tag` override documented for release trains.

**Tiberian Dawn:**
- [ ] **BUG-008:** Harvester docking — units not clearing docking / refinery approach
  - **Investigation:** Not deep-dived in code this round; repo has **SmartDockingService** /
    harvester–refinery flow. **Next:** Trace **HARV** + refinery orders vs docking selector
    setting (`ClassicCnC` / `Improved` / OpenRA); repro steps / replay.
- [ ] **BUG-019:** TD nuclear / superweapon balance — review vs classic (feels overpowered).
  - **Research:** `Atomic` warhead stack in [mods/cnc/weapons/superweapons.yaml](OpenRA-source/mods/cnc/weapons/superweapons.yaml); compare to EA `CnC_Tiberian_Dawn` / remaster when available.
  - **Classic:** [RulesFiles/RA/rules.ini](RulesFiles/RA/rules.ini) `[General]` **AtomDamage=1000** (nuke baseline note — TD may differ; TS rules in `RulesFiles/TS/` / `TS-FS/` for FS-era).
- [ ] **BUG-020:** TD artillery range — review vs classic (felt excessive).
  - **Research:** `ArtilleryShell` **Range: 11c0** in [mods/cnc/weapons/ballistics.yaml](OpenRA-source/mods/cnc/weapons/ballistics.yaml); compare classic tables.
  - **Classic (DOS TD):** [CnC_Tiberian_Dawn/CONST.CPP](CnC_Tiberian_Dawn/CONST.CPP) **`WEAPON_155MM`** range **`0x0600`** (~**6 cells** at 256 leptons/cell); [UDATA.CPP](CnC_Tiberian_Dawn/UDATA.CPP) **ARTY** Strength **75**. OpenRA **11c0** is materially longer than **6** classic cells.
- [ ] **BUG-021:** TD capture enemy buildings — no captured-faction tech (e.g. GDI captures Nod refinery, radar, temple).
- [ ] **BUG-022:** TD lobby — verify option to turn off **multi-engineer** in skirmish / MP start settings.
- [ ] **BUG-023:** TD **Orca** — helipad cap, land/stay landed/spawn landed parity vs classic.

**Red Alert:**
- [ ] **BUG-009:** Can build more Hinds/Blackhawks than helipads — **in execution plan**
  - **Investigation (OpenRA):** `~hpad` = **faction owns at least one helipad**, not 1:1
    capacity. `HIND` in `mods/ra/rules/aircraft.yaml` — `Buildable` + `BuildAtProductionType:
    Helicopter` but **no** hard cap vs pad/field slots. Reservation limits parking, not queue
    count.
  - **Direction (user):** **Hard 1:1** — **planes** + **non–Chinook** helis ≤ **helipads +
    airfields**; **exclude Chinook** from heli cap; engine vs YAML TBD.
  - **Classic source (2026-04-25):** EA `CnC_Red_Alert/CODE/HOUSE.CPP` `AI_Aircraft`: AI only
    queues Longbow/Hind when `BQuantity[STRUCT_HELIPAD] > AQuantity[LONGBOW]+AQuantity[HIND]`;
    Mig/Yak when `BQuantity[STRUCT_AIRSTRIP] > AQuantity[MIG]+AQuantity[YAK]`. `OBJECT.CPP`:
    helipad ↔ non–fixed-wing, airfield ↔ fixed-wing. **Strong parity argument** for human 1:1
    in OpenRA. Details: `openbugs_six-pack_9e8fd989.plan.md`.
- [ ] **BUG-010:** Mines cause friendly fire — **in execution plan**
  - **Investigation (OpenRA):** `^Mine` in `mods/ra/rules/defaults.yaml`: **`AvoidFriendly: false`**,
    **`BlockFriendly: false`** — allies trigger mines. `ATMine` / `APMine` in
    `mods/ra/weapons/explosions.yaml` (e.g. `AffectsParent`) need review after trait alignment.
  - **Direction (user):** Remove **stupid** friendly mine hits; match **OG RA** feel.
  - **Classic source (2026-04-25) — answered:** EA `CnC_Red_Alert/CODE/UNIT.CPP` (per-cell move):
    mine detonation requires `!bldng->House->Is_Ally(this)` — **allies do not pop allied mines**;
    minelayer has same-house exception for just-laid mine. **OpenRA diverges; fix = restore RA
    diplomacy.** Plan: `openbugs_six-pack_9e8fd989.plan.md`.
- [ ] **BUG-016:** RA artillery + Allied ship weapons — balance review vs core RA.
  - **Research:** `155mm` (^Artillery **12c0**), naval `8Inch` (**20c0**) in [mods/ra/weapons/ballistics.yaml](OpenRA-source/mods/ra/weapons/ballistics.yaml); ship actors in `mods/ra/rules/ships.yaml`.
  - **Classic:** [RulesFiles/RA/rules.ini](RulesFiles/RA/rules.ini) — `[ARTY]` + `[155mm]` (**Range=6** cells, **Damage=150**, **ROF=65**); `[8Inch]` (**Range=22**, **Damage=500**, **ROF=160**); `[Stinger]`, `[TorpTube]`, `[DepthCharge]`, `[2Inch]`; hulls `[DD]`, `[CA]`.
- [ ] **BUG-017:** RA **V2** — range and damage review.
  - **Research:** `SCUD` in [mods/ra/weapons/missiles.yaml](OpenRA-source/mods/ra/weapons/missiles.yaml) (**Range 10c0**, **Damage 4500** main WH); **V2RL** + ReCnC COMBAT-001b in [mods/ra/rules/vehicles.yaml](OpenRA-source/mods/ra/rules/vehicles.yaml).
  - **Classic:** [RulesFiles/RA/rules.ini](RulesFiles/RA/rules.ini) — `[V2RL]` Strength **150**, Armor **light**; `[SCUD]` **Damage=600**, **ROF=400**, **Range=10** cells (per file header).
- [ ] **BUG-018:** RA **APC** — durability / matchup feels overpowered vs core RA.
  - **Research:** **HP 35000**, **Armor Heavy**, **Speed 128** in [mods/ra/rules/vehicles.yaml](OpenRA-source/mods/ra/rules/vehicles.yaml); compare classic APC stats.
  - **Classic:** [RulesFiles/RA/rules.ini](RulesFiles/RA/rules.ini) — `[APC]` Strength **200**, Armor **heavy**, **M60mg**, **Passengers=5**, Cost **800** (OpenRA cost **850**, **HP 35000** — different scaling).

**Tiberian Sun:**
- [ ] **BUG-011:** TS Windows entry / launcher + shortcuts + URI — **in execution plan** (see OpenBugs; mitigation in `build-launchers.ps1` / NSIS).
  - **Investigation:** Aligns with OpenBugs **BUG-011** text (not an unspecified hotkey).
  - **Next:** Grep `mods/ts` hotkeys; confirm `TiberianSun.exe` / shortcuts / `openra-ts-${TAG}` in shipped builds. Plan: `openbugs_six-pack_9e8fd989.plan.md`.
- [ ] **BUG-012:** TS **Titan (MMCH)** — **voxel barrel** wrong during **vehicle factory** roll-out; **same** bad angle when **moving normally** at identical facing (not exit-only). Rules: `WithSpriteTurret` + `WithVoxelBarrel` in [mods/ts/rules/gdi-vehicles.yaml](OpenRA-source/mods/ts/rules/gdi-vehicles.yaml). **Next:** trace render vs `Turreted`/`BodyOrientation`/`LeaveProductionActivity`.
- [ ] **BUG-013:** **Merged with OpenBugs TS harvester–refinery–docking block** (same as **BUG-008 TS** there): pile-up + queue + **Improved** docking selector. **Design:** optional split — **harvester** vs **heli/plane** vs **service depot** (depots OK per user).
- [ ] **BUG-014:** TS **client crash** when host **aborts** game (not on normal win). **Logs:** repo `Crash Logs 3rd Party/` + `%UserProfile%\Documents\OpenRA\Logs`. **Next:** map stack from `exception-*.log` to `World.OnClientDisconnected` / order disconnect path.
- [ ] **BUG-015:** TS **Hunter seeker** — off-map, spin; should dash to random enemy. **Tracing:** Debug menu **Show actor tags** (`DebugVisualizations.ActorTags` → `RenderDebugState` activity labels); **Developer** `EnableSimulationPerfLogging` → `perf.log`; `debug.log` / `client.log`; replay. No engine-specific HunterSeeker log channel found.

### Setup
- [x] Initialize git in `ReCnC` root *(2026-03-28)*
- [x] Clone EA CnC_Remastered_Collection source *(2026-03-28)*
- [x] Clone EA CnC_Tiberian_Dawn standalone source *(2026-03-28)*
- [x] Clone EA CnC_Red_Alert standalone source (includes engine libs) *(2026-03-28)*
- [x] Verify OpenRA build — `make.cmd all` *(2026-03-28)* ✓ BUILD SUCCEEDED
- [ ] Verify OpenRA launch smoke test — `launch-game.cmd` (skirmish boots, main menu reachable)
- [x] Obtain classic **`rules.ini`** reference copies for balance work *(2026-04-28)* — see repo
  **[RulesFiles](RulesFiles/)**: `RulesFiles/RA/rules.ini`, `RulesFiles/TS/rules.ini`,
  `RulesFiles/TS-FS/rules.ini`. **Note:** DOS **Tiberian Dawn** unit/weapon tables remain in
  EA source [CnC_Tiberian_Dawn/CONST.CPP](CnC_Tiberian_Dawn/CONST.CPP) / [UDATA.CPP](CnC_Tiberian_Dawn/UDATA.CPP)
  (no separate `RulesFiles` TD file yet).

### Workflow
- [x] Create `differences.md` with checkboxes for review *(2026-03-28)*
- [x] Complete comparison of TD pathfinding vs OpenRA *(2026-03-28)*
- [x] Complete comparison of TD aircraft vs OpenRA *(2026-03-28)*
- [ ] Complete comparison of `RulesFiles/*/rules.ini` vs OpenRA YAML *(RA/TS in repo; TD still CONST/UDATA or add TD rules when available)*
- [x] **USER REVIEW:** Approve items in `differences.md` *(2026-03-28)*
- [x] Move approved items to implementation backlog *(2026-03-28)*

### Pathfinding Selector (C# Engine) — ✓ IMPLEMENTED
- [x] **PATH-001:** Create `IPathfindingStrategy` interface *(2026-03-28)*
- [x] **PATH-002:** Wrap OpenRA's HPA* as `OpenRAPathfinder` *(2026-03-28)*
- [x] **PATH-003:** Port classic LOS + edge-following as `ClassicPathfinder` *(2026-03-28)*
- [x] **PATH-004:** Port `MoveType` cost system (MOVE_MOVING_BLOCK = cost 3) *(2026-03-28)*
- [x] **PATH-005:** Add settings: "PathfindingAlgorithm: [OpenRA | ClassicCnC | Improved]" *(2026-03-28)*
- [x] **PATH-006:** Create `ImprovedPathfinder` — HPA* + cost-based blocking + prediction *(2026-03-28)*

### Aircraft Selector (C# Engine) — ✓ IMPLEMENTED
- [x] **AIR-001:** Create `IAircraftLanding` interface *(2026-03-28)*
- [x] **AIR-002:** Wrap OpenRA's Reservable as `OpenRAAircraftLanding` *(2026-03-28)*
- [x] **AIR-003:** Port classic `Is_LZ_Clear` + `New_LZ` as `ClassicAircraftLanding` *(2026-03-28)*
- [x] **AIR-004:** Port radio contact model for helipad coordination *(2026-03-28)*
- [x] **AIR-005:** Add settings: "AircraftLandingAlgorithm: [OpenRA | ClassicCnC | Improved]" *(2026-03-28)*
- [x] **AIR-006:** Create `ImprovedAircraftLanding` — queue-based + priority + load balancing *(2026-03-28)*

### Docking Selector (C# Engine) — ✓ IMPLEMENTED
- [x] **DOCK-001:** Create `IDockingStrategy` interface *(2026-03-28)*
- [x] **DOCK-002:** Wrap OpenRA's ClosestDock as `OpenRADockingStrategy` *(2026-03-28)*
- [x] **DOCK-003:** Port classic `Find_Docking_Bay` as `ClassicDockingStrategy` *(2026-03-28)*
- [x] **DOCK-004:** Create `SmartDockingService` — wait-vs-travel calculation *(2026-03-28)*
- [x] **DOCK-005:** Create `ImprovedDockingStrategy` — uses SmartDockingService *(2026-03-28)*
- [x] **DOCK-006:** Create `ImprovedMoveToDock` — continuous re-evaluation activity *(2026-03-28)*
- [x] **DOCK-007:** Create `DockingStrategyManager` for strategy selection *(2026-03-28)*
- [x] **DOCK-008:** Add settings: "DockingAlgorithm: [OpenRA | ClassicCnC | Improved]" *(2026-03-28)*

### Performance Optimizations — ✓ IMPLEMENTED
- [x] **PERF-001:** Replace `SortedSet` with `PriorityQueue` in ImprovedPathfinder A* *(2026-04-21)*
- [x] **PERF-002:** Add collection pooling to ImprovedPathfinder *(2026-04-21)*
- [x] **PERF-003:** Add collection pooling to ClassicPathfinder *(2026-04-21)*
- [x] **PERF-004:** Replace LINQ `TakeWhile().ToList()` with in-place `RemoveRange()` in Move.cs *(2026-04-21)*
- [x] **PERF-005:** Pre-allocate neighbor offset array to avoid iterator allocations *(2026-04-21)*

### Performance Optimizations v2 — PLANNED (identified 2026-04-21, not yet discussed)

Findings from code review of ReCnC-added files. These are *planned* and need approval
before editing source. All pertain to `OpenRA-source/OpenRA.Mods.Common/` code added by ReCnC.

#### Pathfinding hot paths (per-cell cost while HPA*/A* expands)

- [ ] **PERF-006:** `ImprovedPathfinder.CreateImprovedCostFunction` — closure invoked per expanded cell
  - File: `Pathfinder/ImprovedPathfinder.cs` lines 164-226
  - `GetActorBlockingCost` does `actorMap.GetActorsAt(cell)` + `TraitOrDefault<Mobile>()` + relationship
    check **every cell**. When Improved mode is selected, this runs on top of the HPA* cost function,
    multiplying per-node work several-fold compared to stock OpenRA.
  - Options: cache per-tick blocking lookups, fast-path `check == BlockedByActor.None`, cache
    `self.Owner.RelationshipWith(...)` per owner, skip for frozen/disabled actors earlier.

- [ ] **PERF-007:** `ClassicPathfinder.GetCellMoveType` — same per-cell actor-map + trait lookup
  - File: `Pathfinder/ClassicPathfinder.cs` lines 418-477
  - Called for every neighbor in LOS walk, every scan in `FindNextPassable`, every ring check in
    `FollowEdge`. Each call does `TraitOrDefault<Mobile>()` twice (mobile branch + aircraft branch is
    in the Classic landing file; here it's Mobile + owner relationship).
  - Options: cache Mobile/Owner per actor in a scratch map for the duration of the search.

- [ ] **PERF-008:** `ClassicPathfinder.FollowEdge` allocates `new HashSet<CPos>(globalVisited)` per call
  - File: `Pathfinder/ClassicPathfinder.cs` line 325
  - Detour runs this twice (CW + CCW). Both copies are GC garbage after the detour. No pooling.
  - Options: reuse a pooled `HashSet<CPos>` and diff-apply/restore it, or switch to a small
    bit/flag array keyed by cell index.

- [x] **PERF-009:** `ClassicPathfinder.FindPathClassic` returns `new List<CPos>(path)` just to free the pool — **COMPLETE (2026-04-21)**
  - File: `Pathfinder/ClassicPathfinder.cs` lines 229-231
  - The pooled list is `.Reverse()`d and then fully copied into a new list before `ReturnPath`.
    Defeats most of the pool benefit when `path.Count` is large.
  - Option: hand the pooled list back to the caller and pool a fresh list on next rent.
  - **Fix shipped:** the pooled `path` is now returned to the caller directly; the `finally` block
    guards on `if (path != null)` so only paths we kept ownership of go back to the pool.
    Sentinel-wrapped (`BEGIN/END ReCnC PERF-009`). Patch: `patches/PERF-009_ClassicPathfinder.cs.patch`.

- [x] **PERF-010:** `ImprovedPathfinder.FindPathAStar` — minor but useful — **COMPLETE (2026-04-21)**
  - File: `Pathfinder/ImprovedPathfinder.cs` lines 256-306
  - `ReconstructPath` walks `cameFrom` and never pre-sizes. `PriorityQueue.Enqueue` already updates
    even when `tentativeG >= neighborG` check passes — we re-enqueue duplicates instead of using
    a decrease-key pattern (OK with PQ but leaves stale entries). Consider a visited/closed set
    to skip stale dequeues.
  - **Fix shipped:** `ReconstructPath` now pre-sizes to `cameFrom.Count + 1` to avoid list resize
    copies; added a pooled `HashSet<CPos>` closed set so stale priority-queue entries and settled
    neighbors are short-circuited on dequeue. Sentinel-wrapped (`BEGIN/END ReCnC PERF-010`). Patch:
    `patches/PERF-010_ImprovedPathfinder.cs.patch`.

- [x] **PERF-011:** `PathfindingStrategyManager.CurrentAlgorithm` string-compares on every pathfind — **COMPLETE (2026-04-21)**
  - File: `Pathfinder/PathfindingStrategyManager.cs` lines 52-63
  - Called by `PathFinder.FindPathToTarget`, `PathExistsForLocomotor`, etc. Two `Equals(StringComparison...)`
    calls per request.
  - Options: cache the enum value and invalidate only via `SetAlgorithmFromLobby` / a settings change event.
  - **Fix shipped:** `cachedAlgorithm` field caches the resolved `PathfindingAlgorithm` enum; the
    string parse only runs on first access or after invalidation. `ClearCache()` (called by
    `SetAlgorithmFromLobby`) resets the cache so lobby changes remain honored.
    Sentinel-wrapped (`BEGIN/END ReCnC PERF-011`). Patch: `patches/PERF-011_PathfindingStrategyManager.cs.patch`.

#### Aircraft landing

- [ ] **PERF-012:** `ClassicAircraftLanding.FindAlternateLandingZone` — O(radii × dirs × landing zones × actors on cell)
  - File: `Traits/Air/ClassicAircraftLanding.cs` lines 102-148
  - 16 radii × 8 directions × N landing zones, and each inner call does `IsLandingZoneClear` which
    hits `actorMap.GetActorsAt`. Hot when helipads are contested.
  - Options: build a shortlist of candidate pads once, sort by distance, early-out on first clear pad.

- [ ] **PERF-013:** `ImprovedAircraftLanding.FindAlternateLandingZone` — full-world scan per query
  - File: `Traits/Air/ImprovedAircraftLanding.cs` lines 101-140
  - `world.ActorsHavingTrait<Reservable>().Where(...).ToList()` on every query; then calls
    `CalculatePadScore` which itself does `queue.Any(r => r.Aircraft == aircraft)` per candidate.
  - Options: cache the per-player Reservable list and invalidate on actor add/remove; replace
    `Queue<LandingRequest>` with a small array/list to avoid LINQ over the queue.

- [x] **PERF-014:** `ImprovedAircraftLanding.ReserveLandingZone` / `ReleaseLandingZoneInternal` — LINQ re-sort of queue — **COMPLETE (2026-04-21)**
  - File: `Traits/Air/ImprovedAircraftLanding.cs` lines 173-234
  - Each call does `ToList().OrderByDescending().ThenBy().ToList()` then `queue.Clear()` + re-enqueue.
    With `MaxQueuePerPad = 3` this is small but fires on every reserve/release.
  - Options: replace `Queue<T>` with `List<T>` and use in-place insertion at the priority position.
  - **Fix shipped:** `landingQueues` is now `Dictionary<Actor, List<LandingRequest>>`; reserve uses
    an insertion-sort pass (priority desc, arrival asc), release uses `RemoveAll`, peek uses `queue[0]`,
    and the contains-aircraft check in `CalculatePadScore` / `IsLandingZoneAvailableFor` was converted
    from LINQ `.Any(lambda)` to manual loops. Sentinel-wrapped (`BEGIN/END ReCnC PERF-014`). Patch:
    `patches/PERF-014_ImprovedAircraftLanding.cs.patch`.

#### Docking

- [x] **PERF-015:** `SmartDockingService.FindBestDock` allocates + sorts to find a minimum — **COMPLETE (2026-04-21)**
  - File: `Traits/SmartDockingService.cs` lines 86-146
  - `candidates = new List<DockCandidate>()` per call; `candidates.OrderBy(c => c.Score).ToList()`
    just to select `[0]` and `[1]`.
  - Options: track best + second-best in a single pass without allocating the list, or rent/return
    a pooled list.
  - **Fix shipped:** single-pass scan tracks `best` + `second` by score; the `candidates` list and
    the `OrderBy(...).ToList()` are gone. `lastUsedDock` lookup is also hoisted out of the inner
    loop so it fires once per call instead of once per candidate. Sentinel-wrapped
    (`BEGIN/END ReCnC PERF-015`). Patch: `patches/PERF-015_SmartDockingService.cs.patch`.

- [x] **PERF-016:** `SmartDockingService.RegisterQueueEntry` — LINQ sort on every enqueue — **COMPLETE (2026-04-21)**
  - File: `Traits/SmartDockingService.cs` lines 249-272
  - `queue.OrderByDescending(e => e.Priority).ThenBy(e => e.ArrivalTick).ToList()` and
    `dockQueues[dock] = queue` on every entry.
  - Options: insertion-sort into the existing list; queues stay tiny so this is free.
  - **Fix shipped:** `.Any(...)` contains-check is a manual loop; the new entry is inserted at the
    correct priority-desc / arrival-asc position in the existing list; the `ToList()` rebuild and
    the dictionary rewrite are gone. Sentinel-wrapped (`BEGIN/END ReCnC PERF-016`). Patch:
    `patches/PERF-016_SmartDockingService.cs.patch`.

- [ ] **PERF-017:** `ImprovedMoveToDock.Tick` re-scans the world multiple times per tick
  - File: `Activities/ImprovedMoveToDock.cs` lines 86-206
  - Hot path: every tick calls `GetAlternativeDocks` (full `ActorsWithTrait<IDockHost>()` scan) and
    every `reevaluateInterval = 50` ticks does it up to three times (`Any()` → `ShouldWaitAtCurrentDock`
    → `FindBestDock`), each iteration re-enumerating.
  - Options: materialize `alternates` into a pooled list once per tick; raise default
    `reevaluateInterval`; early-out `Any()` before building the enumerable.

- [ ] **PERF-018:** `ImprovedMoveToDock.FindBestDock` full-world scan
  - File: `Activities/ImprovedMoveToDock.cs` lines 183-197
  - `self.World.ActorsWithTrait<IDockHost>().Where(...)` — scans every dock-hosting actor in the world
    on every re-evaluation, including `dockClient.CanDockAt` per actor.
  - Options: pre-filter by owner; cache per-player dock host list, invalidated on add/remove.

- [ ] **PERF-019:** `SmartDockingService.EstimateTravelTime` uses straight-line distance
  - File: `Traits/SmartDockingService.cs` lines 178-191
  - Not strictly a perf issue but causes wrong decisions that generate extra pathfinds/re-routes
    (which *is* a perf issue). At minimum document the trade-off; better, sample the HPA* abstract
    graph for a cheap obstacle-aware estimate.

#### Landing service indirection

- [x] **PERF-020:** `AircraftLandingService` double-dispatch on every call — **COMPLETE (2026-04-21)**
  - File: `Traits/World/AircraftLandingService.cs` lines 60-81
  - Every public method calls `GetStrategy()` then the interface method. `landingManager.GetStrategy()`
    already caches, but the extra virtual call + property getter per check is unnecessary in hot callers.
  - Options: cache the current `IAircraftLanding` on the service and refresh only when the algorithm
    setting changes.
  - **Fix shipped:** `AircraftLandingService.GetStrategy()` caches the active `IAircraftLanding` in
    `cachedStrategy` and refreshes only when `landingManager.CurrentAlgorithm` changes. Once the
    pathfinder-side PERF-011 cache and this service cache are both warm, the hot path
    (`IsLandingZoneClear` / `IsLandingZoneAvailableFor` / `FindAlternateLandingZone`) is one field
    read + interface call. Sentinel-wrapped (`BEGIN/END ReCnC PERF-020`). Patch:
    `patches/PERF-020_AircraftLandingService.cs.patch`.

### Upstream OpenRA Performance Opportunities — PLANNED (identified 2026-04-21)

These are in vendored `OpenRA-source\` code (not added by ReCnC). Higher-risk to change because
upstream already optimized these paths, but each has a clear allocation/iteration penalty that
matters in large battles. Needs approval before editing.

#### Combat scanning (fires on every auto-targeting unit every few ticks)

- [ ] **PERF-021:** `AutoTarget.ChooseTarget` allocates and filters per target in range
  - File: `OpenRA.Mods.Common/Traits/AutoTarget.cs` lines 354, 404-419
  - `activePriorities.ToList()` at scan start, then inside the per-target loop:
    `activePriorities.Where(ati => ...).ToList()` — one `List<T>` + closure per target.
    With many units + dense battles this is hot.
  - Options: manual loop with a reusable scratch buffer (`[ThreadStatic]` or field). Track
    "best priority found so far" to short-circuit without materializing the list. `activePriorities`
    itself is a lazy `Select` over a `ToArray`; could be fully materialized in `Created`.

- [ ] **PERF-022:** `AttackBase.ChooseArmamentsForTarget` callers sort via LINQ
  - File: `OpenRA.Mods.Common/Traits/Attack/AttackBase.cs` lines 455-458, 493-496
  - `.OrderBy(x => x.IsTraitPaused).ThenByDescending(x => x.MaxRange()).FirstOrDefault()` —
    allocates `OrderedEnumerable` twice per call plus enumerator. Called from attack-decision
    paths that fire per target.
  - Options: manual two-pass selection (best unpaused max-range armament; fall back to best paused).

- [ ] **PERF-023:** `World.FindActorsInCircle` / `ActorMap.ActorsInBox` iterator allocation
  - Files: `OpenRA.Game/WorldUtils.cs` line 69, `OpenRA.Mods.Common/Traits/World/ActorMap.cs` line 644
  - `ActorsInBox` uses `yield return` → compiler-generated state machine allocated per call;
    `FindActorsInCircle` wraps it in `.Where(...)` which allocates an iterator closure.
    Called from AutoTarget, SpreadDamageWarhead, Aircraft.GetRepulsionForce, many activities.
  - Options: provide an allocation-free struct enumerator (OpenRA already does this for
    `GetActorsAt`), or a callback variant `ForEachActorInCircle(origin, r, Action<Actor>)` for hot callers.

#### Movement / speed recomputation

- [ ] **PERF-024:** `Mobile.MovementSpeedForCell` allocates an `Append` iterator per call
  - File: `OpenRA.Mods.Common/Traits/Mobile.cs` lines 747-753
  - `speedModifiers.Value.Append(terrainSpeed)` → `AppendIterator` heap allocation, and
    `speedModifiers.Value` itself is a lazy `Select` over `ISpeedModifier` that re-invokes
    `GetSpeedModifier()` on every enumeration. Called per move step per unit.
  - Options: cache `ISpeedModifier[]` at `Created`, compute aggregate modifier each call with
    a `for` loop — no Select, no Append, no enumerator.

#### Shroud / vision

- [ ] **PERF-025:** `AffectsShroud.ProjectedCells` allocates per update
  - File: `OpenRA.Mods.Common/Traits/AffectsShroud.cs` lines 64-88
  - `SelectMany(...)` + `footprint.ToArray()` / `.ToArray()` on each update. Fires on
    `CenterPositionChanged`, `MovementTypeChanged`, and gated `ITick`. With lots of vision-
    providing units moving, this is regular pressure.
  - Options: pool the `PPos[]` result or pass the footprint `HashSet<PPos>` directly to the
    `AddCellsToPlayerShroud` path (avoids the final `.ToArray()` copy).

- [ ] **PERF-026:** `Cloak.IsVisible` scans all `DetectCloaked` actors per viewer-check
  - File: `OpenRA.Mods.Common/Traits/Cloak.cs` lines 296-302
  - `self.World.ActorsWithTrait<DetectCloaked>().Any(a => ... range check)` — linear scan
    of every detector on the map, called per cloaked-actor visibility query.
  - Options: maintain a `SpatiallyPartitioned<DetectCloaked>` (similar to `ScreenMap`) so range
    queries are O(local). Invalidate on add/remove/owner-change.

#### Missile / projectile hot loops

- [ ] **PERF-027:** `Missile.HomingTick` scans all jammers every tick per missile
  - File: `OpenRA.Mods.Common/Projectiles/Missile.cs` line 816
  - `world.ActorsWithTrait<JamsMissiles>().Any(JammedBy)` — N missiles × M jammers each
    tick. `JammedBy` closure captures per instance.
  - Options: maintain a world-level list of active `JamsMissiles` actors and cache the set of
    currently-jamming sources each tick, so each missile does a single membership/range test.

#### Armament tick allocations

- [ ] **PERF-028:** `Armament.Tick` uses `delayedActions.RemoveAll(lambda)` every tick
  - File: `OpenRA.Mods.Common/Traits/Armament.cs` line 234
  - `RemoveAll(a => a.Ticks <= 0)` allocates the predicate delegate once (cached by the
    compiler as a static cache), but still walks the whole list every tick on every armament
    on every actor — even when the list is empty in 99% of cases.
  - Options: early-out if `delayedActions.Count == 0`; or do the removal in the same forward
    loop that decrements `x.Ticks` using a swap-with-last pattern.

#### Rendering allocations per frame

- [ ] **PERF-029:** `WorldRenderer.GenerateRenderables` sorts via `OrderBy` every frame
  - File: `OpenRA.Game/Graphics/WorldRenderer.cs` lines 160-161
  - `renderablesBuffer.OrderBy(RenderableZPositionComparisonKey)` — allocates an
    `OrderedEnumerable` + internal buffers every frame. Stable sort is required.
  - Options: keep a pooled `List<IRenderable>` + a pooled key array and use a stable-sort
    helper (merge-sort on indices), or mark renderables with insertion order and sort in place.

- [ ] **PERF-030:** `Contrail` / `Trail` traits allocate a `new IRenderable[1]` per Render
  - File: `OpenRA.Mods.Common/Traits/Render/Contrail.cs` line 103 (and similar patterns)
  - `return new IRenderable[] { trail };` — one heap alloc per trail per frame.
  - Options: return a cached single-element array or a small struct enumerator.

#### Announcement / bot scans (lower priority)

- [ ] **PERF-031:** `EnemyWatcher.Tick` re-allocates two `HashSet<>`s every scan interval
  - File: `OpenRA.Mods.Common/Traits/Player/EnemyWatcher.cs` lines 70-71
  - `visibleActorIds = new HashSet<uint>(); playedNotifications = new HashSet<string>();`
    on each rescan — easy to convert to `.Clear()` on retained instances.

- [ ] **PERF-032:** Bot modules re-enumerate `World.ActorsHavingTrait<T>()` frequently
  - File: `OpenRA.Mods.Common/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManager.cs`
    lines 64, 65, 89, 102, 162, 191, 486 — multiple full scans per AI tick.
  - Options: consolidate into one pass per tick and stash counts in fields; trait dictionary
    queries are fast but add up for heavy-AI skirmishes.

### Additional Upstream OpenRA Performance Opportunities — PLANNED (identified 2026-04-21, second pass: graphics / platform / spatial index)

Second review focused on the render pipeline, OpenGL back-end, and spatial data structures.

#### Rendering hot paths

- [ ] **PERF-033:** `WorldRenderer.Draw` — `GroupBy(prs => prs.GetType())` every frame
  - File: `OpenRA.Game/Graphics/WorldRenderer.cs` line 310 (marked `// HACK: Keep old grouping behaviour`)
  - `preparedOverlayRenderables.GroupBy(prs => prs.GetType())` allocates a LINQ grouping +
    internal buckets + closure, every single frame.
  - Options: pre-sort `preparedOverlayRenderables` by a cached integer type-key at insert time,
    then iterate in order. Or, because the grouping is only for draw-call grouping, split the
    list into a small `Dictionary<Type, List<IFinalizedRenderable>>` that we reuse and clear.

- [ ] **PERF-034:** `ScreenMap.*AtMouse` / `*InMouseBox` / `RenderableActorsInBox` LINQ chains
  - File: `OpenRA.Game/Traits/World/ScreenMap.cs` lines 148-211
  - Every mouse query and every render query chains `.Where(...).Select(...).Where(...)` — each
    call allocates 2-3 iterator objects even when there are zero results.
  - Options: convert the four public getters into methods that take an `Action<Actor>` / a
    `ref` struct enumerator, or expose the `SpatiallyPartitioned.InBox` raw enumeration and let
    callers filter with a plain `foreach`. Callers are few and well-known.

- [ ] **PERF-035:** `FrozenActorLayer.Render` uses `.Where(...).SelectMany(...)` per render
  - File: `OpenRA.Game/Traits/Player/FrozenActorLayer.cs` lines 342-347
  - `RenderableFrozenActorsInBox(...).Where(f => f.Visible).SelectMany(ff => ff.Render())` —
    three iterators allocated per frame per render player. `FrozenActor.Render` itself already
    does `yield return`.
  - Options: materialize into a pooled `List<IRenderable>` that `WorldRenderer.GenerateRenderables`
    already owns; or expose a `RenderInto(List<IRenderable>)` method on `FrozenActor`.

- [ ] **PERF-036:** `SpatiallyPartitioned.InBox` allocates `new HashSet<T>()` per call
  - File: `OpenRA.Game/Primitives/SpatiallyPartitioned.cs` lines 109-140
  - For any query that spans multiple bins (which is every viewport-sized rendering query),
    it allocates a fresh de-dup `HashSet<T>`. Called by `ScreenMap.InBox`, `ActorMap.ActorsInBox`,
    `FindActorsInCircle`, selection rectangles, etc.
  - Options: keep a `[ThreadStatic] HashSet<T>` pool per generic type (must be reset between
    reentrant calls — guarded by a "borrowed" flag to fall back to allocation on reentry), or
    replace the set with a small array + linear de-dup when expected count is low.

- [ ] **PERF-037:** `SpatiallyPartitioned.At` uses `yield return`
  - File: `OpenRA.Game/Primitives/SpatiallyPartitioned.cs` lines 100-107
  - Every mouse-hover per frame hits this via `ScreenMap.ActorsAtMouse`. A compiler-generated
    state machine is allocated each time.
  - Options: provide a `struct AtEnumerator` that reads directly from the bin dictionary without
    allocation. Pair with PERF-036's struct enumerator.

#### Platform / OpenGL back-end

- [ ] **PERF-038:** `ThreadedGraphicsContext` boxes value-tuples per draw call
  - File: `OpenRA.Platforms.Default/ThreadedGraphicsContext.cs` multiple sites, examples:
    - line 111: `var t = ((PrimitiveType, int, int))tuple;` — tuple was boxed in `DrawPrimitives`
      at line 456 `Post(doDrawPrimitives, (type, firstVertex, numVertices));`.
    - line 117: same for `DrawElements`.
    - line 123: same for `EnableScissor`.
  - Every `DrawPrimitives`/`DrawElements`/`EnableScissor`/`SetData` posts a boxed value-tuple
    through the threaded queue. There can be hundreds of these per frame.
  - Options: extend the `Message` class with typed fields (e.g. `int IntParam1/2/3`,
    `object RefParam`) so primitives don't need to be boxed. This is a notable refactor but
    touches a single file and removes per-draw-call GC pressure on the render thread.

- [ ] **PERF-039:** `Post`/`QueueMessage` lock contention per draw call
  - File: `OpenRA.Platforms.Default/ThreadedGraphicsContext.cs` lines 303-323
  - Every `Post`/`Send` takes `lock (messages)` + `Monitor.Pulse`, and every `RunMessage` also
    takes `lock (messagePool)` at least twice. Hundreds of locks per frame.
  - Options: replace the producer-consumer queue with a lock-free `ConcurrentQueue<Message>` +
    a `SemaphoreSlim` signal; or batch posts (defer `Pulse` to once per `Flush`). Low-risk
    variant: only keep the Pulse on the transition 0→1 (already done), but move the pool to a
    `ConcurrentStack` to eliminate one of the two locks.

#### Viewport / mouse cell picking

- [ ] **PERF-040:** `Viewport.CandidateMouseoverCells` uses `yield return` + `ToList` + per-cell LINQ
  - File: `OpenRA.Game/Graphics/Viewport.cs` lines 246-281
  - `CandidateMouseoverCells` yields; the caller calls `.ToList()` to iterate twice. Inside the
    inner loop `ramp.Corners.Select(c => worldRenderer.ScreenPxPosition(pos + c)).ToArray()`
    allocates a 4-element array on every candidate cell. Runs on every mouse-move.
  - Options: switch the outer method to return a small `MPos[]` from a pooled buffer or a
    struct enumerator; reuse a pre-allocated `float2[4]` for ramp corners since ramps always
    have 4 corners.

#### Minor / load-time

- [ ] **PERF-041:** `MarkerTileRenderable.Render` LINQ per frame when order generator is active
  - File: `OpenRA.Game/Graphics/MarkerTileRenderable.cs` line 48
  - `r.Corners.Select(...).ToList()` for every marker tile each frame the order generator draws.
  - Options: use a pooled `List<int2>` cleared each frame, or stack-allocated via `stackalloc`
    since count is small and fixed.

- [ ] **PERF-042:** `TargetLineRenderable` LINQ on waypoints each render
  - File: `OpenRA.Game/Graphics/TargetLineRenderable.cs` lines 42-56
  - `waypoints.Select(...)`, `.Skip(1).Select(...)`, `.Any()` — all on every selected actor when
    move-order lines are visible.
  - Options: accept `IReadOnlyList<WPos>` / `WPos[]` and loop by index.

- [ ] **PERF-043:** Debug-overlay `ScreenMap` draw allocates `new float2[b.Vertices.Length]`
  - File: `OpenRA.Game/Graphics/WorldRenderer.cs` lines 361-368
  - Only when the screen-map debug overlay is toggled on, but still worth a pooled buffer.

#### UI and Production Queues

- [ ] **PERF-047:** `ClassicProductionQueue.Tick` / `ClassicParallelProductionQueue.Tick` full-world scans
  - File: `OpenRA.Mods.Common/Traits/Player/ClassicProductionQueue.cs` and `ClassicParallelProductionQueue.cs`
  - Runs a full `foreach` over `self.World.ActorsWithTrait<Production>()` every game tick per queue.
  - Options: Cache the list of `Production` actors per player and update via add/remove hooks.

- [ ] **PERF-048:** `RadarWidget.Tick` full-world scan and per-frame allocation
  - File: `OpenRA.Mods.Common/Widgets/RadarWidget.cs`
  - Walks `world.ActorsWithTrait<IRadarSignature>()` and allocates `new List<(CPos Cell, Color Color)>()` every UI tick when radar is enabled.
  - Options: Maintain a spatial or cached list of radar signatures, and reuse a pooled list for the cells.

- [ ] **PERF-049:** `PlaceBuilding.GetNumBuildables` LINQ allocations
  - File: `OpenRA.Mods.Common/Traits/Player/PlaceBuilding.cs`
  - Uses `.Where`, `.SelectMany`, `.Distinct`, `.Count` on building placement.
  - Options: Replace with a manual `HashSet` loop to avoid multiple iterator allocations.

#### Triggers and Pathfinding Entry Points

- [ ] **PERF-050:** `ActorMap` triggers (`CellTrigger.Tick`, `ProximityTrigger.Tick`) LINQ in tick
  - File: `OpenRA.Mods.Common/Traits/World/ActorMap.cs`
  - Uses `Footprint.SelectMany` and `ActorsInBox(...).Where(...)` when triggers are dirty.
  - Options: Use allocation-free struct enumerators or manual loops.

- [ ] **PERF-051:** `PathFinder` entry points `.ToList()` materialization
  - File: `OpenRA.Mods.Common/Traits/World/PathFinder.cs`
  - `FindPathToTargetCell` and `FindPathToTargetCells` allocate `.ToList()` for sources and targets on every path request.
  - Options: Accept `IReadOnlyList` or use array pooling instead of forcing LINQ materialization.

#### Economy and Traits

- [ ] **PERF-052:** `Refinery.AcceptResources` full-world scan on delivery
  - File: `OpenRA.Mods.Common/Traits/Buildings/Refinery.cs`
  - `foreach (var notify in self.World.ActorsWithTrait<INotifyResourceAccepted>())` on every resource delivery.
  - Options: Cache `INotifyResourceAccepted` actors globally or per-player.

- [ ] **PERF-053:** `PowerManager.UpdateActor` LINQ aggregation
  - File: `OpenRA.Mods.Common/Traits/Power/Player/PowerManager.cs`
  - `a.TraitsImplementing<Power>().Where(...).Sum(...)` allocates iterators on power updates.
  - Options: Manual loop over traits.

- [ ] **PERF-054:** `Cloak.ModifyRender` and `Cloak.Tick` overhead
  - File: `OpenRA.Mods.Common/Traits/Cloak.cs`
  - `ModifyRender` uses `.Select(...)` iterator per cloaked actor render. `Tick` uses `Enum.HasFlag` which may box on older runtimes.
  - Options: Manual loop for render modification; bitwise math for enum checks.

#### Runtime upgrade

> Numbering note: PERF-044 and PERF-045 are intentionally reserved and unused; PERF-046 is the
> runtime port and is deliberately elevated to the top of Batch A.
>
> **Decision (2026-04-21):** Start Batch A with PERF-046. First action is a 2-hour time-boxed
> Eluant / Lua spike on net10; PERF-046 proceeds only if that spike passes. Publish mode stays
> **self-contained** to match today's `build-launchers.ps1` (`--self-contained true`); ZIP size
> is not a blocker.

- [x] **PERF-046:** Port from .NET 6 / netstandard2.1 (Mono) to .NET 10 — **COMPLETE *(2026-04-21)***
  - Net10 skirmish baseline (canonical, measure future PERF items against this):
    map = *Forest* (CnC), 1 Cabal AI opponent, ~60 units on screen, SDK 10.0.203.
    Observed via `Debug.PerfText` +
    `dotnet-counters monitor --process-id <TiberianDawn> System.Runtime`:
    - **Frame time**: ~6.0 ms (~166 FPS)
    - **Tick time**: ~1.0 ms
    - `dotnet.gc.heap.total_allocated`: **3.87 MB** over ~30 s sample
    - `dotnet.gc.collections`: **0** across gen0/1/2 during sample (GC pressure very low)
    - `dotnet.jit.compilation.time`: 0.223 s / 936 methods / 82,340 bytes IL
    - `dotnet.process.cpu.count`: 32
  - Lua mission smoke: D2K main-menu shellmap (`d2k-shellmap.lua`) runs clean on net10; CnC GDI
    Mission 01 (`gdi01.lua`) runs clean on net10 — `Reinforcements.Reinforce`,
    `Media.PlaySpeechNotification`, `Player.GetPlayer`, and trigger callbacks all fire normally.
    Confirmed interactively by user.
  - Spike verdict: `Directory.Build.props` + `OpenRA.Game\OpenRA.Game.csproj` edits (sentinel-wrapped
    `BEGIN/END ReCnC PERF-046`) build clean on net10.0.203: **0 errors, 14 warnings**
    (`NU1510` Microsoft.Win32.Registry unnecessary, `NU1901` NuGet.CommandLine pre-existing,
    + `SYSLIB0050/51` around `FormatterServices` / `Exception.GetObjectData` in
    `FieldLoader.cs`, `ActorReference.cs`, `ActorGlobal.cs`, `Utility/Program.cs` — all new to
    net10 and flagged for a follow-up cleanup pass).
  - Eluant runtime probe (`tools\perf\EluantProbe\`) executed `return 6*7` on net10 → `42`.
    `Eluant.dll` + `lua51.dll` both deploy to `bin\` on `make.cmd all`.
  - Unified-diff patches captured: `patches\PERF-046_Directory.Build.props.patch`,
    `patches\PERF-046_OpenRA.Game.csproj.patch`; file backups in `patches\PERF-046-backup\`.
  - Publish pipeline verified *(2026-04-21)*: `build-launchers.ps1` runs end-to-end on net10 with
    no script edits needed (TFM is driven entirely by `Directory.Build.props`). All three launcher
    EXEs build self-contained for `win-x64`; portable ZIP = **72.27 MB** (vs 69.89 MB on net6,
    +2.38 MB for the net10 self-contained runtime); wall-clock 105 s.
  - NU1510 cleanup: dropped `Microsoft.Win32.Registry 5.0.0` from `OpenRA.Mods.Common.csproj`
    (in-box on net10). Warning count 14 → 12. Patch: `patches\PERF-046_OpenRA.Mods.Common.csproj.patch`.
  - **Remaining work before closing the ticket:** re-baseline frametime / Gen-0 allocation
    measurements (`Debug.ShowPerfText = true` for 60 s in a dense skirmish, plus
    `dotnet-counters monitor`). Optional follow-up: see PERF-055 below.

- [x] **PERF-055:** Clean up net10 `SYSLIB0050` / `SYSLIB0051` / `CS0672` obsolete-serialization
      warnings (6 sites, all new on net10) — **COMPLETE (2026-04-21)**
  - Files: `OpenRA.Game\FieldLoader.cs` (lines 50, 52); `OpenRA.Game\Map\ActorReference.cs`
    (line 73); `OpenRA.Utility\Program.cs` (lines 32, 34); `OpenRA.Mods.Common\Scripting\Global\ActorGlobal.cs`
    (line 39).
  - `FormatterServices.GetUninitializedObject(t)` → `System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(t)`
    (drop-in replacement, available since .NET Core 3.0).
  - `Exception.GetObjectData` overrides → either add `[Obsolete]` attribute or delete the override
    (formatter-based serialization is dead).
  - Deferred from PERF-046 to keep that ticket's diff minimal; low risk, pure source edits in
    upstream files — wrap each with `BEGIN/END ReCnC PERF-055` sentinels.
  - **Fix shipped:** `FormatterServices.GetUninitializedObject` replaced with
    `RuntimeHelpers.GetUninitializedObject` in `ActorReference.cs` and `ActorGlobal.cs`
    (SYSLIB0050 gone). The two `Exception.GetObjectData` overrides in `FieldLoader.cs` and
    `Utility/Program.cs` are kept for binary-formatter round-trip compat but locally pragma-disable
    `SYSLIB0051` + `CS0672`. Post-build warning breakdown: **12 total, all NU1901** (NuGet.CommandLine
    advisory, pre-existing and orthogonal). SYSLIB / CS0672 count = 0. Sentinel-wrapped
    (`BEGIN/END ReCnC PERF-055`). Patches: `patches/PERF-055_FieldLoader.cs.patch`,
    `patches/PERF-055_ActorReference.cs.patch`, `patches/PERF-055_ActorGlobal.cs.patch`,
    `patches/PERF-055_Program.cs.patch`.
  - Files: `OpenRA-source\Directory.Build.props`, every `OpenRA.*.csproj`, `make.ps1`,
    `build-launchers.ps1`, publish scripts.
  - Current target: `<TargetFramework>net6.0</TargetFramework>` (non-Mono) /
    `netstandard2.1` (Mono), `<LangVersion>9</LangVersion>`.
  - New target: `<TargetFramework>net10.0</TargetFramework>`, drop Mono conditionals,
    keep `<LangVersion>9</LangVersion>` for minimum diff vs upstream.
  - Why it belongs at the top of Batch A: it is the single largest perf win available and
    touches only build files, so it cannot conflict with any subsequent code-level PERF fix
    and gives us a clean re-baseline to measure them against.
  - **Expected performance gains (no code changes)**
    - Dynamic PGO (on by default in .NET 8+): 10–20% on hot CPU paths — devirtualizes
      `ITick.Tick`, `IRenderable.Render`, `IPathfindingStrategy.*`, trait dispatches.
    - GC improvements (.NET 8/9/10): 5–15% tick time, lower Gen-0 cost, smoother
      concurrent behaviour — compounds with every other PERF ticket.
    - `Array.Sort` / `List<T>.Sort`: up to 20–30% faster — directly helps PERF-029.
    - `Dictionary`/`HashSet` throughput: ~5–10% — helps `ActorMap`, `ScreenMap`, `TraitDictionary`.
    - `System.Threading.Lock` (.NET 9+): lower uncontended lock overhead — helps
      PERF-038 / PERF-039 in `ThreadedGraphicsContext`.
    - Exception perf (.NET 9): 2–4× cheaper.
    - SIMD/AVX-512: modest, OpenRA has limited SIMD surface.
    - **Ballpark combined**: 15–25% average tick-time reduction, 20–35% reduction in
      Gen-0 allocation rate in dense skirmishes, without touching a line of game code.
  - **Dependency work**
    - Drop `System.Runtime.Loader 4.3.0` (built-in).
    - Drop `System.Collections.Immutable 6.0.0` Mono-branch (built-in).
    - Drop `System.Threading.Channels 6.0.0` (built-in).
    - Bump `Microsoft.Extensions.DependencyModel 6.0.2` → `10.x`.
    - Verify `OpenRA-Eluant 1.0.22` (Lua bindings fork) runs on .NET 10. Single real risk —
      if it bakes in net6 ABI assumptions, rebuild the fork or retarget.
    - Bump `StyleCop.Analyzers`, `Roslynator.Analyzers`, `Roslynator.Formatting.Analyzers`
      to current versions. Analyzer-only, no runtime impact.
    - `Linguini.Bundle`, `Mono.NAT`, `SharpZipLib` — netstandard, fine as-is.
  - **Code risk audit** (read-only pass, fix warnings if present)
    - `WebRequest` / `WebClient` — obsoleted; replace with `HttpClient` where found
      (likely in master-server comms / GeoIP / downloads).
    - `Thread.Abort` — removed; unlikely to be present but grep anyway.
    - `BinaryFormatter` — not used (OpenRA has its own MiniYaml serialiser).
    - P/Invoke to SDL2 / OpenGL / OpenAL / FreeType — unchanged, works identically.
    - `ILGenerator` / `DynamicMethod` in `Sync.cs` — fully supported, no change.
  - **Effort: 1–2 focused days**
    | Step | Time |
    |------|------|
    | Update `Directory.Build.props`: TFM, drop Mono conditionals, sentinel-wrap | 30 min |
    | Clean Mono `ItemGroup Condition` blocks across `.csproj` files | 30 min |
    | Bump/drop NuGet deps per list above | 1 h |
    | Verify Eluant on net10 (build + run Lua mission smoke test) | 1–3 h |
    | Fix `WebRequest`/obsolete-API warnings | 1–3 h |
    | Update `make.ps1`, `build-launchers.ps1`, publish for `-f net10.0 -r win-x64` self-contained | 30 min |
    | Full clean build; resolve stragglers | 1–2 h |
    | Smoke test + replay-hash regression | 1–2 h |
  - **Upstream-resilient strategy** (same rules as every other PERF item)
    - Every edit in `Directory.Build.props` and each `.csproj` wrapped with
      `<!-- BEGIN ReCnC PERF-046 --> … <!-- END ReCnC PERF-046 -->`.
    - Save unified diffs to `patches\PERF-046_Directory.Build.props.patch` +
      `patches\PERF-046_<project>.csproj.patch` for each modified csproj.
    - On upstream merge: if upstream has bumped to net8/net10 themselves, delete our patch
      (their move supersedes ours). Otherwise `git apply -3` re-applies in one shot since we
      only touch TFM + deps.
    - If Eluant needed a rebuild, document the `ReCnC-Eluant` package origin + rebuild
      procedure in `changes.md` so we can reproduce on a fresh clone.
  - **Verification playbook for PERF-046 specifically**
    1. Clean `make.cmd all` builds without warnings.
    2. Launch skirmish; confirm no obsolete-API runtime exceptions.
    3. Record `tests\replays\baseline.orarep` against the *new* net10 baseline —
       this replaces the earlier pre-port baseline. Every subsequent PERF batch measures
       against this new hash.
    4. 60 s pan/zoom around dense base with `Debug.ShowPerfText = true`; capture frametime.
    5. `dotnet-counters monitor --process-id <OpenRA.Game>` for 60 s; record Gen-0 rate.
    6. Log before/after numbers in `changes.md` session entry.
  - **Blockers / open questions before starting**
    - Is the `OpenRA-Eluant` source available to us, in case we need to rebuild? If not,
      time-box a 2 h spike on just loading Eluant under net10 before committing to the
      rest of the work.
    - Do we want self-contained publish (larger zip, no user install of .NET runtime) or
      framework-dependent publish? Self-contained is the default assumption here.
    - Are there CI/build machines to update, or is build local-only today?

#### Static review addendum (2026-04-21)

Read-only review of `Todo.md` against `OpenRA-source` confirmed the existing PERF backlog is
tracking the right problems. The highest-risk items are those that sit in per-tick, per-frame, or
per-expanded-cell loops and allocate/enumerate repeatedly.

- **Confirmed highest user-visible risks**
  - `PERF-006` / `PERF-007`: ReCnC pathfinding modes call actor-map lookups, `TraitOrDefault<Mobile>()`,
    and relationship checks from per-cell expansion paths. This is the largest ReCnC-owned tick-time
    multiplier when Classic/Improved pathfinding is enabled.
  - `PERF-017` / `PERF-018`: `ImprovedMoveToDock.Tick` repeatedly scans `ActorsWithTrait<IDockHost>()`
    through deferred enumerables. This scales with active dock clients and world dock count.
  - `PERF-021` / `PERF-023`: `AutoTarget.ChooseTarget` allocates priority lists inside target scans,
    while `FindActorsInCircle` / `ActorsInBox` add iterator allocations used by combat, aircraft
    repulsion, warheads, crates, and bot modules.
  - `PERF-029` / `PERF-033`: `WorldRenderer` uses LINQ sort/grouping every frame. These are likely
    frame-pacing bugs, not just CPU cleanup.
- **Confirmed medium risks**
  - `PERF-013` / `PERF-014`: improved aircraft landing scans all reservable actors per alternate-pad
    query and rebuilds tiny queues via LINQ. Most visible when helipads are contested.
  - `PERF-024`: `Mobile.MovementSpeedForCell` allocates an `Append` iterator and re-enumerates speed
    modifiers on movement-speed lookups.
  - `PERF-026` / `PERF-027`: cloak detection and missile jamming are full-world trait scans in hot
    visibility/projectile paths.
  - `PERF-009` / `PERF-010`: earlier pooling/PriorityQueue work helps, but large Classic paths still
    copy pooled lists before return and Improved A* can still accumulate stale priority-queue entries.
- **Priority adjustment note**
  - Keep `PERF-046` first only if the project is ready to take the runtime-port risk and re-baseline
    all perf measurements afterward.
  - If the next session should be low-risk source work instead, pull `PERF-006` / `PERF-007` and
    `PERF-017` / `PERF-018` forward before the smaller Batch A cleanups; these are more likely to
    move real skirmish tick time than enum caching or tiny queue insertion fixes.
  - Treat `PERF-021` / `PERF-023` and `PERF-029` / `PERF-033` as the first upstream-owned items to
    benchmark once ReCnC-owned hot paths are under control.
- **Verification note**
  - This was static review only. Before closing any PERF ticket, capture a replay hash plus either
    `Debug.ShowPerfText` frametime or `dotnet-counters` Gen-0 allocation numbers and log them in
    `changes.md`.

---

## Performance Fix Plan - upstream-resilient patches

We vendor OpenRA source in `OpenRA-source\` and will periodically pull new upstream releases.
Every performance fix must therefore be *re-appliable* with minimal human judgement. The plan
below defines the patch strategy, then groups the 52 findings (PERF-001 … PERF-054, with
PERF-044 / PERF-045 reserved and unused) into batches A–G with ordering, risk, and verification.

### Patch strategy (applies to every change)

1. **Isolate ReCnC-owned code from upstream-owned code.**
   - Files added by ReCnC (e.g. `ImprovedPathfinder.cs`, `SmartDockingService.cs`,
     `AircraftLandingService.cs`) → fix freely; not affected by upstream pulls.
   - Files modified in-place (e.g. `PathFinder.cs`, `Move.cs`, `Resupply.cs`, `WorldRenderer.cs`,
     `ScreenMap.cs`, `SpatiallyPartitioned.cs`) → see rules 2–6.

2. **Prefer extension over modification.**
   - If a fix can be implemented by adding a new trait / helper / extension method in a new file
     under `OpenRA.Mods.Common\ReCnC\Perf\`, do that. Example: a new
     `SpatiallyPartitionedPoolingExtensions.cs` in our folder that exposes allocation-free
     enumerators the original type does not.
   - Upstream pulls then only require verifying the new helpers still compile against the new
     base types — not re-writing the fix.

3. **If in-place edits are unavoidable, keep them surgical and marked.**
   - Every edit block in upstream files must be wrapped with sentinel comments:
     ```
     // BEGIN ReCnC PERF-NNN  (short reason)
     ...changed code...
     // END ReCnC PERF-NNN
     ```
   - This lets a single `rg "ReCnC PERF-"` list every touch-point after an upstream merge.
   - Never mix a behaviour change with a perf change inside one sentinel block.

4. **Maintain a patch registry.**
   - `changes.md` already lists every modified upstream file. Extend the "Files Modified" tables
     in each session with a new column **PERF-ID** so each line of change is traceable to a
     ticket. On upstream merge, walk the registry and replay.

5. **Keep a `patches\` folder of unified diffs.**
   - For every in-place upstream edit, run `git diff <upstream-tag> -- <file>` and save the
     output as `patches\PERF-NNN_<file-basename>.patch`. When upstream changes land, attempt
     `git apply -3 patches\PERF-NNN_*.patch`; any rejects are a short, reviewable list.

6. **Do not mix perf + logic + refactor in the same commit.**
   - One commit per PERF-ID. Commit message: `perf(PERF-NNN): <file>: <one-line reason>`. This
     makes cherry-picking across upstream tags trivial.

7. **Benchmarking gate.**
   - Each PERF-ID must include, in `changes.md`, either:
     - a micro-benchmark result (BenchmarkDotNet or stopwatch harness in a tiny `.csproj` under
       `tools\perf\`), or
     - a repro skirmish scenario + replay + before/after tick-time measurement (OpenRA already
       ships `PerfSample` / `PerfTimer`; enable via `Debug.PerfText` in settings).
   - If no measurable delta, the PERF item is closed as "not worth the maintenance cost".

8. **Test coverage.**
   - Any change to pathfinding / movement / targeting must pass a deterministic replay
     smoke-test (OpenRA records replays — load a canned skirmish replay after the change and
     confirm final state hash matches pre-change hash). A replay that desyncs is an instant
     revert.
   - Rendering changes are verified by a 60-second pan/zoom around a dense base with
     `Debug.ShowPerfText` enabled; no average frametime regression vs baseline on the same box.

9. **Setting-gated fallbacks for risky items.**
   - Any in-place edit to a widely-used upstream path (ScreenMap, SpatiallyPartitioned,
     ThreadedGraphicsContext) gets a hidden setting
     `Debug.UseReCnCPerfPatch<AreaName> = true` that flips between original and patched code.
     If a release breaks, users set it false and we investigate.

### Batches and ordering

**Batch A — Low-risk, ReCnC-owned, high multiplier** — **COMPLETE (2026-04-21)**
No upstream *source* files are touched. Only build files + our own code. If it breaks, only ReCnC code is affected.

| ID | Status | File | Fix approach | Risk | Test |
|----|--------|------|--------------|------|------|
| PERF-046 | COMPLETE | `OpenRA-source\Directory.Build.props` + all `*.csproj` | **Port runtime to .NET 10** (see detailed ticket below). Single largest perf win; do this FIRST so later batches are measured against the new baseline. | Low-Medium | Full-build + replay hash + frametime re-baseline |
| PERF-011 | COMPLETE | `PathfindingStrategyManager.cs` | Cache enum; invalidate on `SetAlgorithmFromLobby`. | Trivial | Algorithm-switch smoke test |
| PERF-015 | COMPLETE | `SmartDockingService.cs` | Single-pass best+second scan, no list. | Low | Harvester replay hash |
| PERF-016 | COMPLETE | `SmartDockingService.cs` | Insertion-sort in place on enqueue. | Low | Harvester replay hash |
| PERF-009 | COMPLETE | `ClassicPathfinder.cs` | Return pooled list; caller responsible for `ReturnPath`. | Low | Classic pathfind replay hash |
| PERF-010 | COMPLETE | `ImprovedPathfinder.cs` | Pre-size `ReconstructPath`; add closed set for stale PQ entries. | Low | Improved pathfind replay hash |
| PERF-014 | COMPLETE | `ImprovedAircraftLanding.cs` | Replace `Queue<T>` with `List<T>` + insertion-sort. | Low | Helipad replay hash |
| PERF-020 | COMPLETE | `AircraftLandingService.cs` | Cache active `IAircraftLanding` in field; invalidate on setting change. | Trivial | Helipad replay hash |
| PERF-055 | COMPLETE | `FieldLoader.cs` / `Program.cs` / `ActorReference.cs` / `ActorGlobal.cs` | SYSLIB0050/0051/CS0672 cleanup (`FormatterServices` → `RuntimeHelpers`; pragma-wrap `GetObjectData`). | Trivial | Clean build, 0 SYSLIB warnings |

**Batch B — ReCnC-owned, requires caching infrastructure** (*target: session 2*)
Introduces `OpenRA.Mods.Common\ReCnC\Perf\ActorIndex.cs`: per-player / per-trait caches with
add/remove hooks. Once this exists, several PERF-IDs use it. **PERF-017 landed ahead of schedule
2026-04-21 via a per-activity per-tick cache that does not need ActorIndex; the remaining 7 items
still wait on the scaffolding session.**

| ID | Status | File | Fix approach | Risk | Test |
|----|--------|------|--------------|------|------|
| PERF-013 | pending | `ImprovedAircraftLanding.cs` | Use `ActorIndex<Reservable>` cached per player. | Medium | Helipad replay hash |
| PERF-018 | pending | `ImprovedMoveToDock.cs` | Use `ActorIndex<IDockHost>` cached per player. | Medium | Harvester replay hash |
| PERF-017 | COMPLETE (2026-04-21) | `ImprovedMoveToDock.cs` | Per-tick `List<TraitPair<IDockHost>>` cache keyed on (WorldTick, dockHost); `.Any()` → `.Count > 0`; scan only once per tick per dockHost instead of every call site. Landed without ActorIndex. | Medium | Harvester replay hash |
| PERF-019 | pending | `SmartDockingService.cs` | Optional: sample HPA* abstract graph for travel time. | Medium (pathing-adjacent) | Harvester replay hash |
| PERF-006 | pending | `ImprovedPathfinder.cs` | Per-search `ActorCellCache` reused across cost-function calls. | Medium | Improved pathfind replay hash |
| PERF-007 | pending | `ClassicPathfinder.cs` | Share same `ActorCellCache` as PERF-006 when Classic is active. | Medium | Classic pathfind replay hash |
| PERF-008 | pending | `ClassicPathfinder.cs` | Pool `HashSet<CPos>` for detours; diff-apply/restore. | Medium | Classic pathfind replay hash |
| PERF-012 | pending | `ClassicAircraftLanding.cs` | Pre-filter pad shortlist + early-out. | Low | Helipad replay hash |

**Batch C — Upstream in-place, extension-preferred** (*target: session 3*)
Every edit below is bracketed with `// BEGIN ReCnC PERF-NNN` sentinels and mirrored in `patches\`.
**PERF-028 and PERF-024 landed 2026-04-21** in the same targeted session as PERF-017; remaining
items still queued.

| ID | Status | File | Fix approach | Risk | Test |
|----|--------|------|--------------|------|------|
| PERF-028 | COMPLETE (2026-04-21) | `Armament.cs` | Static `ExpiredDelayedAction` predicate + `delayedActions.Count > 0` guard around the loop + `RemoveAll`. Fires once per Armament per tick; empty-list case now allocation-free. | Trivial | Attack replay hash |
| PERF-031 | pending | `EnemyWatcher.cs` | `.Clear()` existing `HashSet<>`s instead of realloc. | Trivial | Skirmish replay hash |
| PERF-030 | pending | `Contrail.cs` / `Trail` | Return cached single-element array (`readonly IRenderable[] renderableBuffer = new IRenderable[1];`). | Trivial | Visual smoke test |
| PERF-024 | COMPLETE (2026-04-21) | `Mobile.cs` | Swap `Lazy<IEnumerable<int>>` (Select closure) for `Lazy<ISpeedModifier[]>`; inline `Util.ApplyPercentageModifiers` decimal math in `MovementSpeedForCell`. Drops Select/Append/foreach iterator allocations on every cell cost and movement eval. | Low | Movement replay hash |
| PERF-025 | `AffectsShroud.cs` | Pool `PPos[]` or consume `HashSet<PPos>` directly. | Low | Shroud smoke test (FOW on) |
| PERF-029 | `WorldRenderer.cs` | Replace `OrderBy` with merge-sort on indices into a pooled buffer. | Low-Medium | Visual smoke + frametime |
| PERF-033 | `WorldRenderer.cs` | Pre-sort `preparedOverlayRenderables` into pooled per-type buckets. | Low-Medium | Visual smoke + frametime |
| PERF-035 | `FrozenActorLayer.cs` | Direct `foreach` + pooled buffer; no LINQ. | Low | FOW visual smoke test |
| PERF-041 / PERF-042 / PERF-043 | Various renderables | Drop LINQ in favour of indexed loops + stackalloc/pool. | Trivial | Visual smoke test |

**Batch D — Upstream in-place, larger surface** (*target: session 4*)
These touch `SpatiallyPartitioned`, `ScreenMap`, and the auto-target / projectile hot loops. Every
one is gated by `Debug.UseReCnCPerfPatch*` so we can disable if an upstream merge misbehaves.

| ID | File | Fix approach | Risk | Test |
|----|------|--------------|------|------|
| PERF-036 | `SpatiallyPartitioned.cs` | Add struct enumerator + thread-static pool for multi-bin dedup. Keep old method for compat. | Medium | Full replay hash |
| PERF-037 | `SpatiallyPartitioned.cs` | Struct enumerator for `At`. | Low | Mouse hover smoke test |
| PERF-023 | `WorldUtils.cs` + `ActorMap.cs` | Add `ForEachActorInCircle(origin, r, Action<Actor>)` + a struct `ActorsInBox` enumerator. Keep old API for existing callers. | Medium | Replay hash |
| PERF-021 | `AutoTarget.cs` | Scratch-buffer best-priority scan; materialize `activePriorities` once in `Created`. | Medium | Combat replay hash |
| PERF-022 | `AttackBase.cs` | Manual two-pass selection of armaments. | Low | Combat replay hash |
| PERF-027 | `Missile.cs` | World-level `JamsMissiles` cache; one per-tick jammer-list sweep. | Medium | Missile replay hash |
| PERF-026 | `Cloak.cs` | Spatial index of `DetectCloaked`. Requires add/remove hooks; substantial. | Medium | Cloak/detector replay hash |

**Batch E — Platform layer** (*target: session 5, hold until A-D land*)

| ID | File | Fix approach | Risk | Test |
|----|------|--------------|------|------|
| PERF-038 | `ThreadedGraphicsContext.cs` | Typed primitive fields on `Message` — no tuple boxing. | Medium | Frametime harness |
| PERF-039 | `ThreadedGraphicsContext.cs` | Swap `messagePool` to `ConcurrentStack`; keep `messages` lock (it already serialises Pulse) or move to `ConcurrentQueue + SemaphoreSlim`. | Medium-High | Frametime harness + 10-min soak |
| PERF-040 | `Viewport.cs` | Struct enumerator for candidate cells; pooled 4-element `float2` buffer for ramp corners. | Low-Medium | Mouse hover smoke test |

**Batch F — AI / long-tail** (*no schedule yet*)
| ID | File | Fix approach | Risk | Test |
|----|------|--------------|------|------|
| PERF-032 | `BotModules/…` | Consolidate trait scans into one pass per tick; stash counts. | Low | AI skirmish replay hash |

**Batch G — UI, Economy, and Triggers (Newly Identified)** (*target: session 6*)
These address full-world scans and LINQ allocations in UI ticks, triggers, and resource delivery.

| ID | File | Fix approach | Risk | Test |
|----|------|--------------|------|------|
| PERF-047 | `ClassicProductionQueue.cs` | Cache `Production` actors per player. | Medium | Production replay hash |
| PERF-048 | `RadarWidget.cs` | Cache `IRadarSignature` globally; pool cell list. | Low | UI smoke test |
| PERF-049 | `PlaceBuilding.cs` | Replace LINQ with manual `HashSet` loop. | Low | Building placement test |
| PERF-050 | `ActorMap.cs` | Struct enumerators for triggers. | Medium | Trigger/proximity replay hash |
| PERF-051 | `PathFinder.cs` | Remove `.ToList()` materialization. | Low | Pathfinding replay hash |
| PERF-052 | `Refinery.cs` | Cache `INotifyResourceAccepted` actors. | Medium | Harvester delivery replay hash |
| PERF-053 | `PowerManager.cs` | Manual loop for power trait aggregation. | Low | Power update replay hash |
| PERF-054 | `Cloak.cs` | Manual render loop; bitwise enum checks. | Low | Cloak visual/tick test |

### Verification playbook for every batch

1. `make.cmd all` clean build.
2. Replay hash test — load `tests\replays\baseline.orarep` (we will record this once, against
   the pre-batch commit) and confirm final world state hash matches.
3. Visual smoke test — 60 s pan/zoom around dense base with `Debug.ShowPerfText = true`.
4. `dotnet-counters monitor --process-id <OpenRA.Game>` during step 3; record Gen-0 allocation
   rate. Expect the batched fixes to reduce it monotonically.
5. Append results to `changes.md` under the session for that batch.

### Upstream merge playbook

When pulling a new OpenRA release into `OpenRA-source\`:

1. `rg "ReCnC PERF-" OpenRA-source\` → list every sentinel block.
2. For each hit, open the corresponding `patches\PERF-NNN_*.patch` and run `git apply -3`.
3. For any reject, open the ticket's file + current upstream version side-by-side and re-apply
   the fix inside new sentinel comments. Update the `.patch` file.
4. Re-run the full verification playbook.
5. Update `changes.md` with a new session entry listing which PERF-IDs re-applied cleanly and
   which required adjustment.

### Improved Pathfinding v2 — PLANNED
- [ ] **PATH-v2-001:** Idle unit nudging — stationary units move slightly to let others pass
  - Detect when a moving unit is blocked by an idle unit
  - Calculate perpendicular "nudge" direction to clear the path
  - Only nudge if the idle unit can return to approximately the same position
  - Should feel natural, not disruptive to formations

### Improved Aircraft Landing v2 — PARTIALLY COMPLETE
- [x] **AIR-v2-001:** Fix RA aircraft not staying landed on helipads *(2026-04-21)*
  - YAML: Set `TakeOffOnResupply: false` + `IdleBehavior: ReturnToBase` for `^Helicopter`
  - Code: Removed `wasRepaired` HACK from `Resupply.cs` that forced takeoff after repairs
- [ ] **AIR-v2-002:** Helipad affinity — aircraft prefer their "home" helipad
  - Track which helipad an aircraft first docked at
  - Prefer that helipad if available, fall back to others only if occupied

### Improved Docking v2 — PLANNED (Low Priority)
- [ ] **DOCK-v2-001:** Building affinity for harvesters/docking units
  - Harvesters should prefer the refinery they first docked at
  - Reduces "queue chaos" where all harvesters go to nearest refinery
  - Track `homeBuilding` on first successful dock
  - Re-evaluate home if building is destroyed
- [ ] **DOCK-v2-002:** Queue balancing — distribute units across buildings
  - If home building has long queue, consider switching to shorter queue
  - Threshold-based: only switch if queue difference > N units

### Combat/Targeting Selector (C# Engine) — PLANNED
- [x] **COMBAT-001:** Artillery auto-targeting mismatch — FIXED *(2026-04-21)*
  - Applied YAML fix: `InitialStance: AttackAnything` + `ScanRadius` for V2RL, ARTY, MSAM
  - Root cause: OpenRA `Defend` stance vs original C&C `MISSION_HUNT`
- [ ] **COMBAT-002:** Create `IAutoTargetStrategy` interface
  - `OpenRAAutoTarget` — Current behavior (stance-based)
  - `ClassicAutoTarget` — Original C&C MISSION_HUNT behavior
  - `ImprovedAutoTarget` — Hybrid: weapon-range scan + smart stance switching
- [ ] **COMBAT-003:** Add setting: "AutoTargetAlgorithm: [OpenRA | ClassicCnC | Improved]"

### UI/UX Improvements — PLANNED
- [ ] **UI-001:** Rename AI difficulty levels to standard names
  - Current: (unknown naming scheme)
  - Target: "Easy", "Medium", "Hard", etc.
  - Investigate current AI difficulty implementation in YAML/code
- [ ] **UI-002:** Show active ReCnC selector algorithms in the in-game Debug panel
  - Surface currently-selected `PathfindingAlgorithm`, `AircraftLandingAlgorithm`, and
    `DockingAlgorithm` (per the PathfindingStrategyManager / AircraftLandingService /
    DockingStrategyManager traits) as read-only labels in the ingame Debug menu so a tester
    can tell at a glance which strategy is running without checking game settings.
  - Files:
    - `mods\cnc\chrome\ingame-debug.yaml` (+ RA / TS / D2K equivalents) — add three labels
    - `OpenRA.Mods.Common\Widgets\Logic\Ingame\DebugMenuLogic.cs` — bind labels to the
      service's `CurrentAlgorithm` / `CurrentStrategy` accessors; refresh on open so lobby
      changes are reflected on the next session
  - No runtime overhead: labels read the service's cached enum (PERF-011 / PERF-020 already
    made this O(1)).
  - Sentinel: `// BEGIN ReCnC UI-002` / `// END ReCnC UI-002` in both files; patch saved to
    `patches\UI-002_*.patch`.
  - Est: 30-60 LOC across 2 files + 1 per-mod YAML block (~5 lines each).

### Game Behavior (YAML) — PLANNED (identified 2026-04-21 from skirmish smoke test)

Behavior gaps in vendored OpenRA's mod rules found while verifying Batch A aircraft-landing
fixes. These are **not** perf regressions — code paths in PERF-014 / PERF-020 are never
invoked for the affected aircraft because no `Rearmable` trait is wired in. Restoring
classic-C&C rearm-at-pad behavior is a pure YAML change that *starts* exercising the
aircraft-landing service for these units, which is also desirable as ongoing test coverage.

- [ ] **GAME-001:** Restore classic Orca / Nod-Apache (CnC TD) rearm-at-pad behavior
  - File: `OpenRA-source\mods\cnc\rules\aircraft.yaml`
  - Today — `ORCA` (lines 135-199) and `HELI` (lines 58-133) have `AmmoPool` +
    `ReloadAmmoPool` (Delay 100 / 70, Count 2) and **no** `Rearmable` trait. Result: ammo
    regenerates in-air forever, aircraft never request a landing, helipads are decorative.
    This conflicts with classic C&C where Orcas landed on helipads to rearm (and crashed
    without one).
  - Target:
    - Add `Rearmable: RearmActors: hpad` to both `ORCA` and `HELI`.
    - Replace `ReloadAmmoPool` with `RearmAmmoPool` so ammo only fills while docked.
    - Keep Delay / Count proportional (likely tune Delay down now that reload is gated on
      a pad visit).
    - Leave `IdleBehavior: ReturnToBase` (already set on `^Helicopter` by AIR-v2-001) —
      that combined with `Rearmable` gives auto-return-when-empty.
  - Risk: **medium, balance-affecting.** Nod HELI in CnC mod becomes materially weaker
    (limited time-on-target). Mirror-test an AI skirmish before / after.
  - Est: ~15 YAML lines per aircraft + playtest pass. Sentinel-wrap `# BEGIN ReCnC
    GAME-001` / `# END ReCnC GAME-001`; patch saved to
    `patches\GAME-001_mods_cnc_rules_aircraft.yaml.patch`.
  - Side benefit: actually exercises `AircraftLandingService` + `ImprovedAircraftLanding`
    for combat aircraft, giving the Batch A perf fixes real coverage in skirmish.

- [ ] **GAME-002:** Fix RA Longbow (HELI) infinite-ammo + Hind reload behavior
  - File: `OpenRA-source\mods\ra\rules\aircraft.yaml`
  - Today — RA `HELI` (Longbow, lines 273-353) has **no `AmmoPool` at all** → infinite
    ammo, never needs to rearm. User-observed "Apaches seem OP" is literally this. RA
    `HIND` (lines 355-430) has `AmmoPool: 24, ReloadDelay: 8` but no `Rearmable` — 24
    rounds that cycle in ~2.4 s in-air.
  - Target:
    - Add `AmmoPool` to `HELI` (Longbow), e.g. `Ammo: 10`, matching classic Longbow load.
    - Add `Rearmable: RearmActors: hpad` to both `HELI` and `HIND`.
    - Replace any `ReloadDelay` / reload-in-air with `RearmAmmoPool` so pad-docking is
      required to refill.
  - Risk: **higher — balance-affecting for RA skirmish.** Longbow power level drops
    noticeably; may need a weapon or cost tune after playtest.
  - Est: ~20 YAML lines + playtest pass. Sentinel-wrap `# BEGIN ReCnC GAME-002` / `# END
    ReCnC GAME-002`; patch saved to
    `patches\GAME-002_mods_ra_rules_aircraft.yaml.patch`.

### Balance (YAML) — *reference: [RulesFiles](RulesFiles/) + TD [CONST.CPP](CnC_Tiberian_Dawn/CONST.CPP)*
- [ ] Compare and adjust `mods\cnc\rules\infantry.yaml`
- [ ] Compare and adjust `mods\cnc\rules\vehicles.yaml`
- [ ] Compare and adjust `mods\cnc\rules\structures.yaml`
- [ ] Compare and adjust `mods\cnc\weapons\*.yaml`

---

## Known OpenRA Issues (from code review)

### Critical — Pathfinding

**Bridge/Chokepoint Re-pathing** (`Move.cs` lines 307-314)
- When blocked, units wait ~40 ticks then re-path with `BlockedByActor.All`
- Moving units on bridges/chokepoints are treated as permanent blockers
- `CellIsEvacuating()` only returns true when unit is actively leaving cell
- TODO on line 133: "Change this to BlockedByActor.Stationary after improving local avoidance"

**AI Squad Stuck** (`GroundStates.cs` lines 156-160, 233-237)
- HACK: "Drop back to idle state if we haven't moved in 2.5 seconds"
- Workaround for squads stuck trying to attack-move to unpathable locations
- Generates expensive pathfinding calls each tick

**NearestMoveableCell** (`Mobile.cs` lines 757-761)
- "HACK: This entire method is a hack, and needs to be replaced with proper path search"
- Can't properly handle movement layer transitions (bridges, tunnels)

### Critical — Aircraft

**Aircraft Moving Twice Per Tick** (`Aircraft.cs` line 471-473)
- "HACK: Prevent updating visibility twice per tick. We really shouldn't be moving twice in a tick"

**Resupply/Repair Logic** (`Resupply.cs` lines 80-81, 126-127, 143-144, 191-192)
- Multiple HACKs around repair + rearm flow
- "Reservable logic can't handle repairs, so force a take-off"
- "Repairable needs the actor to move to host center"

**Landing Issues** (`Land.cs`)
- Line 73: "TODO: For fixed-wing aircraft self.Location is not necessarily the most direct landing site"
- Line 196: "TODO: correctly handle CCW <-> CW turns"
- Line 210: Explicit "Fix a problem when airplane is sent to land near the landing cell"

**Fly Blocked Detection** (`Fly.cs` line 205-206)
- "HACK: Consider ourselves blocked if we have moved by less than 64 WDist in last five ticks"

### Medium — Combat

**Disguise Reveal** (`AttackBase.cs` lines 444-446)
- "HACK: works around limitations in the targeting code that force the targeting and attacking logic (which should be logically separate) to use the same code"

**Force Attack Persistence** (`AttackFollow.cs` line 165)
- "HACK: Manually set force attacking if we persisted an opportunity target"

**AmmoPool Rearm** (`AmmoPool.cs` lines 39, 55, 95)
- "HACK: Temporarily kept until Rearm activity is gone for good" (3 instances)

### Medium — Other

**Capture While Moving** (`CaptureManager.cs` lines 212-214)
- "HACK: Make sure the target is not moving and at its normal position with respect to the cell grid"

**Tunnel Interaction** (`EntersTunnels.cs` lines 137-139)
- "HACK: The engine does not support HiddenUnderFog combined with buildings that use the '_' footprint"

**Transit-Only Cell Idle** (`Mobile.cs` lines 852-853)
- "HACK: activities should be making sure that units aren't left in transit-only cells!"

**Isometric Minelayer** (`Minelayer.cs` line 201-202)
- "HACK: This will return the wrong results for isometric cells"

**Resource Density Lerp** (`ResourceLayer.cs`, `EditorResourceLayer.cs`)
- "HACK: we should not be lerping to 9, as maximum adjacent resources is 8. It's too disruptive to fix."

**Missiles** (`Missile.cs`)
- Multiple TODOs: "double check square roots", "Double check Launch parameter determination", "deceleration checks"

---

## In progress

### Ready for Testing
Build prerequisite re-verified *(2026-04-21, post Batch A)*: `OpenRA-source\make.cmd all` on SDK
10.0.203 → **0 errors, 12 warnings — 100% `NU1901` (NuGet.CommandLine advisory), 0 `SYSLIB*`, 0
`CS0672`**. PERF-055 closed the net10 obsolete-serialization warnings that appeared after the
PERF-046 runtime port. Batch A skirmish verification completed *(2026-04-21)* on map *Forest*
vs 1 Cabal AI (see post-Batch A baseline entry in `changelog.md`); residual items below are
either non-regressions (aircraft landing — see GAME-001 / GAME-002) or still awaiting a targeted
artillery pass.

- [x] Test pathfinding selector in skirmish (OpenRA/Classic/Improved modes) — exercises PERF-009,
      PERF-010, PERF-011 *(2026-04-21)* ✓ all three modes cycled with no errors; no exception
      spam.
- [x] Test aircraft landing — exercises PERF-014, PERF-020 *(2026-04-21)* ✓ **no regression
      from Batch A.** Observed Orca / CnC-HELI / RA-HELI never visiting pads is stock OpenRA
      rule-design (no `Rearmable` trait; `ReloadAmmoPool` / no `AmmoPool` ⇒ in-air regen or
      infinite ammo). PERF-014 / PERF-020 code paths are only hit for aircraft that actually
      *have* `Rearmable`. Proper restoration of classic rearm-at-pad queued as GAME-001 /
      GAME-002; re-test this line after those land.
- [ ] Test artillery targeting — verify V2RL/ARTY/MSAM engage at weapon range *(still pending
      a focused pass)*
- [x] Test docking selector with harvesters in skirmish — exercises PERF-015, PERF-016
      *(2026-04-21)* ✓ user reported "Harvester docking in improved seemed better"; no stalls
      or dock-thrash observed.
- [x] Re-capture net10 perf baseline after Batch A *(2026-04-21, same scenario: map *Forest*,
      1 Cabal AI, ~60 units)* ✓ **ms/tick halved (1.0 ms → 0.5 ms); ms/frame stable at 6.0 ms
      (~166 FPS).** `dotnet-counters` 30 s sample: `dotnet.gc.heap.total_allocated` 3.87 MB,
      0 collections across Gen-0/1/2 in-window, 0 lock contentions. JIT: 0.223 s compile,
      936 methods, 82.3 KB IL — unchanged, confirming Batch A added no steady-state JIT
      surface. Full numbers in `changelog.md`.
- [x] **Targeted perf pass (PERF-028 / PERF-024 / PERF-017) — re-baseline captured**
      *(2026-04-21)* ✓ **ms/tick dropped from 0.5 ms → 0.1–0.2 ms (60–80 % reduction); ms/frame
      stable at 6 ms (~166 FPS).** Build: `OpenRA-source\make.cmd all` on SDK 10.0.203 →
      **0 errors, NU1901-only warnings** (incremental rebuild emits 6; a clean rebuild emits
      12, same advisory class as Batch A; no new `SYSLIB*`, no `CS0672`). Patches:
      `patches/PERF-028_Armament.cs.patch`, `patches/PERF-024_Mobile.cs.patch`,
      `patches/PERF-017_ImprovedMoveToDock.cs.patch`. `dotnet-counters` snapshot (longer session
      than Batch A's 30 s window, so alloc / JIT totals are not directly comparable): gen0
      collections 502, gen1 132, gen2 20; `gc.pause.time` 0.644 s total; exceptions observed
      were IOException×2, SocketException×2, TaskCanceledException×1, TimeoutException×20 —
      all network / multiplayer-lobby chatter, none in the hot path. Headline result is the
      tick-time drop; full details in `changelog.md`.

### Completed Setup
- [x] Verify OpenRA build (`make.cmd all`) *(2026-03-28)* ✓ BUILD SUCCEEDED
- [x] Add `AircraftLandingService` to world.yaml *(2026-03-28)* — added to cnc, ra, ts, d2k
- [x] Add `SmartDockingService` to world.yaml *(2026-03-28)* — added to cnc, ra, ts, d2k
- [x] Add `DockingStrategyManager` to world.yaml *(2026-03-28)* — added to cnc, ra, ts, d2k

## Done

- [x] Add project planning files: `Outline.txt`, `Todo.md`, `changelog.md`
- [x] Review OpenRA pathfinding code — identified bridge/chokepoint re-path bug
- [x] Review OpenRA aircraft code — identified landing/reservation issues
- [x] Catalog HACKs/TODOs across OpenRA.Mods.Common
- [x] Initialize git repo at `ReCnC` root *(2026-03-28)*
- [x] Clone all three EA C&C source repos *(2026-03-28)*
  - `CnC_Remastered_Collection` (remastered DLLs)
  - `CnC_Tiberian_Dawn` (original 1995 code)
  - `CnC_Red_Alert` (original 1996 code + engine libs)
- [x] Create `differences.md` review file *(2026-03-28)*
- [x] Compare TD pathfinding vs OpenRA — key finding: cost-based vs binary blocking *(2026-03-28)*
- [x] Compare TD aircraft vs OpenRA — key finding: simple LZ check vs complex reservation *(2026-03-28)*
- [x] User approved selector approach *(2026-03-28)*
- [x] **Implement Pathfinding Selector** *(2026-03-28)*
  - `IPathfindingStrategy.cs` — Strategy interface
  - `OpenRAPathfinder.cs` — Wrapper for existing HPA*
  - `ClassicPathfinder.cs` — Port of FINDPATH.CPP (LOS + edge-following)
  - `MoveType.cs` — Port of cost-based blocking enum
  - `PathfindingStrategyManager.cs` — Strategy selection based on settings
- [x] **Implement Aircraft Landing Selector** *(2026-03-28)*
  - `IAircraftLanding.cs` — Strategy interface
  - `OpenRAAircraftLanding.cs` — Wrapper for existing Reservable system
  - `ClassicAircraftLanding.cs` — Port of Is_LZ_Clear + New_LZ
  - `AircraftLandingManager.cs` — Strategy selection based on settings
- [x] **Add Game Settings** *(2026-03-28)*
  - `PathfindingAlgorithm` setting in GameSettings
  - `AircraftLandingAlgorithm` setting in GameSettings
- [x] **Implement Smart Docking Selector** *(2026-03-28)*
  - `IDockingStrategy.cs` — Strategy interface
  - `OpenRADockingStrategy.cs` — Wrapper for existing ClosestDock
  - `ClassicDockingStrategy.cs` — Port of Find_Docking_Bay (distance-based)
  - `SmartDockingService.cs` — Wait-vs-travel calculation
  - `ImprovedDockingStrategy.cs` — Uses SmartDockingService
  - `ImprovedMoveToDock.cs` — Continuous re-evaluation activity
  - `DockingStrategyManager.cs` — Strategy selection based on settings
  - `DockingAlgorithm` setting in GameSettings
- [x] **Performance Optimizations** *(2026-04-21)*
  - `ImprovedPathfinder.cs` — PriorityQueue, collection pooling, static neighbor array
  - `ClassicPathfinder.cs` — Collection pooling for path/visited sets
  - `Move.cs` — In-place path truncation instead of LINQ allocation
- [x] **Artillery Targeting Fix** *(2026-04-21)*
  - V2RL, ARTY (RA) and MSAM (TD) now use `InitialStance: AttackAnything`
  - Added `ScanRadius` to match weapon range for proper target acquisition
- [x] **Aircraft Landing Fix (AIR-v2-001)** *(2026-04-21)*
  - `^Helicopter`: `TakeOffOnResupply: false`, `IdleBehavior: ReturnToBase`
  - Removed `wasRepaired` HACK from `Resupply.cs`

---

*Tip: Move items between sections as state changes; keep one clear "next" action at the top of Backlog when possible.*
