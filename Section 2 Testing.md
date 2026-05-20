Here's the end-to-end test plan. Five sections, roughly 15-25 minutes total.

## 0. Pre-flight (30 seconds)

```powershell
cd c:\dfworking\ReCnC\OpenRA-source
.\launch-game.cmd
```

If the main menu renders and the shellmap animates, the build is healthy. Quit back to desktop.

While the game is quit, open `%APPDATA%\OpenRA\settings.yaml` and confirm (or add) under the `Debug:` block:

```yaml
Debug:
	PerfText: True
	PerfGraph: True
```

That enables the in-game frametime + tick-time HUD. Save the file and close it.

## 1. Pathfinding smoke — exercises PERF-009, PERF-010, PERF-011

Goal: confirm all three pathfinding algorithms still produce usable paths and that swapping between them mid-lobby is still instant (the PERF-011 cache must invalidate correctly).

1. Launch the game. Main menu → **Skirmish**.
2. Map: **Forest** (CnC). Opponent: **1 Cabal AI**. This is the same scenario as the PERF-046 baseline so numbers stay apples-to-apples.
3. In the lobby, find the **Pathfinding Algorithm** dropdown. Cycle it `OpenRA` → `ClassicCnC` → `Improved` → back to `OpenRA`. There should be no stall and no exception dialog.
4. Leave it on **ClassicCnC** and start the match.
5. Once the game loads:
   - Build 5-10 infantry + a couple of vehicles.
   - Right-click somewhere far across the map (long path).
   - Right-click *through* a tree line / chokepoint (forces detour logic — hits PERF-009's pooled-list path).
   - Select units, right-click a moving target (forces re-pathing).
6. Quit to menu. Re-enter Skirmish on the same map, switch to **Improved**, start again, repeat step 5 (this exercises PERF-010's closed set).
7. Same again with **OpenRA** mode as control.

**What a pass looks like:** units arrive at the destination in every mode, no `NullReferenceException`, no `KeyNotFoundException`, no crash, and path quality looks the same as before the change (no units getting stuck on edges).

## 2. Aircraft landing smoke — exercises PERF-014, PERF-020

Goal: confirm the `Queue<T>` → `List<T>` swap and strategy caching don't break helipad reservations.

1. New skirmish, same map, faction = **GDI** or **Nod** (anything with helipads).
2. In the lobby, set **Aircraft Landing Algorithm = Improved**. Start the match.
3. Build: Helipad (or 2 if the mod allows), 2-3 **Orca** / **Apache** / similar aircraft.
4. Key sequence to hit the contested-pad code paths:
   - Order all aircraft to attack a single enemy unit far away → they return to rearm simultaneously → forces queue priority insertion (PERF-014 `ReserveLandingZone`).
   - While two aircraft are queueing, damage the lead one → priority-upgrade insertion path.
   - Sell one helipad while aircraft are queued → release path (PERF-014 `ReleaseLandingZoneInternal` + PERF-020 alternate-pad find).
5. Repeat with **AircraftLandingAlgorithm = ClassicCnC** as sanity check (PERF-020 cache swap path).

**What a pass looks like:** every aircraft eventually lands, none stall mid-air forever, queue order matches priority (damaged lands first), and switching algorithm in the lobby actually takes effect next match.

## 3. Harvester docking smoke — exercises PERF-015, PERF-016

Goal: confirm the single-pass best/second scan + in-place insertion-sort give the same decisions as the old LINQ sort.

1. New skirmish, Forest, 1 Cabal AI, your faction has access to multiple refineries.
2. Build a second **Refinery** so there are 2 dock hosts within reach.
3. Build 4-6 harvesters. Watch them auto-dock.
4. Key sequences:
   - Let all 6 queue up at the same refinery (forces `RegisterQueueEntry` → PERF-016 insertion-sort).
   - Manually right-click a couple of harvesters onto the far refinery mid-run (forces `FindBestDock` → PERF-015).
   - Sell a refinery while harvesters are queued (forces re-evaluation and alternate selection).
5. Let the economy run for ~60 seconds.

**What a pass looks like:** harvesters keep delivering, no harvester parks forever at a refinery that's no longer there, the credit counter keeps increasing at roughly the pre-change rate.

## 4. Re-capture the perf baseline

Goal: produce the "after Batch A" numbers to compare against the PERF-046 baseline (~6 ms frametime, ~1 ms tick, 3.87 MB allocated over 30 s).

1. If `dotnet-counters` isn't on PATH:
   ```powershell
   dotnet tool install -g dotnet-counters
   ```
2. Launch the game, start the same skirmish (**Forest, 1 Cabal AI**). Build ~60 units (roughly the same as last time — mix of infantry + vehicles, no need to be exact but keep it in the same order of magnitude).
3. **Leave the game running.** In a new PowerShell window:
   ```powershell
   Get-Process | Where-Object { $_.MainWindowTitle -match 'Tiberian|OpenRA' } | Select-Object Id, ProcessName, MainWindowTitle
   ```
   Grab the PID of the game process.
4. Start sampling for ~30 seconds:
   ```powershell
   dotnet-counters monitor --process-id <PID> System.Runtime
   ```
5. While it samples, in-game: move a group of ~20 units across the map and engage the AI (keeps the pathfinder / AutoTarget paths hot).
6. Press `q` in the `dotnet-counters` window to stop. Record:
   - Frame time (from the in-game HUD, top-left — should still read ~6 ms)
   - Tick time (same HUD, ~1 ms)
   - `dotnet.gc.heap.total_allocated` at the end of the window
   - `dotnet.gc.collections` for gen0 / gen1 / gen2
   - `dotnet.jit.compilation.time` (should be similar; only climbs on new code paths)
7. Paste the numbers back to me and I'll append them to `changelog.md` as the "after Batch A" baseline so future perf tickets compare against it.

## 5. If anything blows up

- Crash / exception during any of the above → grab the log at `%APPDATA%\OpenRA\Logs\exception.log` and tell me which PERF-ID section you were in.
- Any single ticket can be reverted independently via its backup:
  ```powershell
  Copy-Item -Force patches\PERF-<nnn>-backup\* OpenRA-source\ -Recurse
  .\OpenRA-source\make.cmd all
  ```
  where `<nnn>` is `009 / 010 / 011 / 014 / 015 / 016 / 020 / 055`. Each backup mirrors the `OpenRA-source\` layout so the `Copy-Item -Recurse` drops the original file back in the right place.

That's it. Call out anything that looks off and I'll jump on it.