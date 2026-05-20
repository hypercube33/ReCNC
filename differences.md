# ReCnC — Differences Review

This file documents differences between classic C&C (TD/RA source) and OpenRA. Check the boxes for changes you want to pursue.

**Workflow:** Review items below → Check boxes to approve → Items move to `Todo.md` → Implementation begins

---

## Pathfinding Algorithm

### Classic C&C Approach

**Source files available:**
- `CnC_Tiberian_Dawn/FINDPATH.CPP` — Original 1995 TD pathfinding
- `CnC_Red_Alert/CODE/FINDPATH.CPP` — Original 1996 RA pathfinding
- `CnC_Remastered_Collection/TIBERIANDAWN/FINDPATH.CPP` — Remastered 2020 version

- **Algorithm:** LOS (Line-of-Sight) + Edge-following
- **How it works:**
  1. Follow straight line toward target
  2. If obstacle hit, follow edge clockwise or counter-clockwise
  3. Continue until reaching target or timeout
- **Pros:** Simpler, predictable behavior, ~1500 lines of code
- **Cons:** Can get stuck in complex terrain, no hierarchical optimization

**CRITICAL: Moving Unit Handling**

Classic C&C uses `MoveType` enum with COSTS, not binary blocking:
```
MOVE_OK             cost 1  - clear path
MOVE_CLOAK          cost 1  - cloaked enemy
MOVE_MOVING_BLOCK   cost 3  - MOVING UNIT (passable with penalty!)
MOVE_DESTROYABLE    cost 8  - enemy blocking
MOVE_TEMP           cost 10 - friendly blocking
MOVE_NO             cost 0  - impassable
```

**Key insight:** `MOVE_MOVING_BLOCK` has cost 3 — the path goes THROUGH moving units with a penalty, not around them. This is why classic C&C doesn't have the bridge re-pathing problem.

### OpenRA Approach (`OpenRA.Mods.Common/Pathfinder/`)

- **Algorithm:** Hierarchical A* with abstract graph
- **How it works:**
  1. Build low-resolution abstract graph of map
  2. Search abstract graph first for rough path
  3. Refine with detailed A* search
- **Pros:** More optimal paths, better performance on large maps
- **Cons:** Complex (~2000+ lines across multiple files), known HACK/TODO issues

**CRITICAL: Moving Unit Handling**

OpenRA uses `BlockedByActor` enum with BINARY blocking:
```
BlockedByActor.None       - ignore all actors
BlockedByActor.Immovable  - only static actors block
BlockedByActor.Stationary - ignore moving actors
BlockedByActor.All        - ALL actors block (including moving!)
```

**Key problem:** When re-pathing (`Move.cs:313`), OpenRA uses `BlockedByActor.All`, treating moving units as COMPLETE blockers. This forces units to path AROUND a tank on a bridge instead of waiting.

### Proposed Changes — SELECTOR APPROACH

Keep OpenRA's existing code, add classic as alternative, let users choose.

**Pathfinding Selector:** ✓ APPROVED
- [x] **PATH-001:** Create `IPathfindingStrategy` interface
- [x] **PATH-002:** Wrap OpenRA's HPA* as `OpenRAPathfinder` (keep as-is, bugs and all)
- [x] **PATH-003:** Port classic LOS + edge-following as `ClassicPathfinder`
- [x] **PATH-004:** Port `MoveType` cost system (MOVE_MOVING_BLOCK = cost 3)
- [x] **PATH-005:** Add settings dropdown: "Pathfinding: [OpenRA | Classic C&C]"

**Optional Extras:** (deferred)
- [ ] **PATH-006:** Add flow field option for large groups
- [ ] **PATH-007:** Add "Improved" hybrid option (best of both)

---

## Aircraft Logic

### Classic C&C (`TIBERIANDAWN/AIRCRAFT.CPP`)

**Landing zone check (`Is_LZ_Clear`):**
```cpp
bool AircraftClass::Is_LZ_Clear(TARGET target) const
{
    ObjectClass *object = Map[cell].Cell_Object();
    if (object) {
        if (object == this) return true;           // Already on pad
        if (Contact_With_Whom() == object) return true;  // In radio contact with pad
        return false;                               // Something else there
    }
    return Map[cell].Is_Generally_Clear();
}
```

**Key behavior:**
- Simple check: one aircraft per pad
- If LZ not clear, just find another one (`New_LZ`)
- No complex reservation system
- Uses "radio contact" for coordination with helipad

### OpenRA (`OpenRA.Mods.Common/Traits/Air/`)

**Landing check (`Reservable.cs`):**
- Complex reservation system with `MayYieldReservation` flag
- Only one aircraft can reserve a pad at a time
- Multiple HACKs in `Resupply.cs` for repair+rearm flow
- Issues with altitude handling (multiple TODOs)

**Key problems:**
- Single reservation blocks all other aircraft
- Repair logic conflicts with reservation system
- HACKs for cancellation handling

### Proposed Changes — SELECTOR APPROACH

Keep OpenRA's existing code, add classic as alternative, let users choose.

**Aircraft Selector:** ✓ APPROVED
- [x] **AIR-001:** Create `IAircraftLanding` interface
- [x] **AIR-002:** Wrap OpenRA's Reservable system as `OpenRAAircraftLanding` (keep as-is)
- [x] **AIR-003:** Port classic `Is_LZ_Clear` + `New_LZ` as `ClassicAircraftLanding`
- [x] **AIR-004:** Port radio contact model for helipad coordination
- [x] **AIR-005:** Add settings dropdown: "Aircraft Landing: [OpenRA | Classic C&C]"

---

## Unit Balance (YAML)

### Classic C&C (`RULES.INI` — need game files)

*(Waiting for RULES.INI from game installation)*

### OpenRA (`mods/cnc/rules/*.yaml`)

*(Values already documented in OpenRA YAML)*

### Proposed Changes

- [ ] **BAL-001:** Adjust infantry HP/Cost/Speed to match classic
- [ ] **BAL-002:** Adjust vehicle HP/Cost/Speed to match classic
- [ ] **BAL-003:** Adjust structure HP/Cost to match classic
- [ ] **BAL-004:** Adjust weapon damage/range to match classic

---

## Other Issues

### From Code Review (optional — only if NOT using selector approach)

These are OpenRA bugs we identified. With the selector approach, we keep OpenRA as-is and users can switch to classic if they prefer. Only fix these if you want to improve OpenRA mode specifically:

- [ ] **MISC-001:** Fix `Mobile.cs:757` — replace `NearestMoveableCell()` hack
- [ ] **MISC-002:** Fix AI squad stuck timeout hack in `GroundStates.cs`
- [ ] **MISC-003:** Fix `AmmoPool.cs` temporary rearm hacks
- [ ] **MISC-004:** Fix capture-while-moving hack in `CaptureManager.cs`
- [ ] **MISC-005:** Fix isometric minelayer bug in `Minelayer.cs`

---

## How to Approve

1. Review each item above
2. Check `[x]` for items you want to pursue
3. Save this file
4. Tell me which items are approved
5. I'll move them to `Todo.md` and begin implementation

---

*Last updated: 2026-03-28*
