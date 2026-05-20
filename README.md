# ReCnC

ReCnC is a custom Command & Conquer workspace built around a vendored `OpenRA-source/` tree, local gameplay and packaging changes, and reference material pulled in from classic C&C source drops and remastered-era files.

This is not a clean fork of a single upstream project. It is a working integration repository used to compare behavior, track parity issues, patch the engine, and produce Windows builds.

## What This Repo Contains

- `OpenRA-source/`
  Main engine and mod codebase. This is the source tree that actually builds the game and launcher binaries.
- `CnC_Tiberian_Dawn/`, `CnC_Red_Alert/`, `CnC_Remastered_Collection/`
  Imported reference trees used for behavior comparisons, data lookups, and parity checks.
- `RulesFiles/`
  Raw rules/config references used for balance and behavior comparisons.
- `patches/`
  Saved diffs and backups for targeted changes.
- `Todo.md`, `OpenBugs.md`, `changelog.md`, `differences.md`
  Project tracking, bug notes, research, and change history.
- `make.ps1`
  Wrapper build script for compiling the modified OpenRA workspace.
- `build-launchers.ps1`
  Windows packaging script that builds launchers, portable bundles, and optionally an NSIS installer.

## Repository Layout

The repo is organized as a source workspace, not as a minimal distributable package:

- source code lives primarily under `OpenRA-source/`
- reference material is checked in alongside it for comparison work
- generated build output is intentionally not committed
- release artifacts should be attached to GitHub Releases instead of stored in git

## Requirements

For the current workspace state, Windows packaging expects:

- Windows PowerShell
- .NET 10 SDK
- NSIS, if you want the installer `.exe`
- network access during packaging if `rcedit-x64.exe` or the GeoIP database are not already cached locally

The effective target framework in this workspace is `net10.0` via `OpenRA-source/Directory.Build.props`.

## Building

Basic compile:

```powershell
.\make.ps1
```

Release-style launcher/package build for Win64:

```powershell
.\build-launchers.ps1 -Platform x64
```

That script builds the launcher executables and produces a portable zip at the repo root named like:

```text
ReCnC-release-YYYYMMDD-x64-winportable.zip
```

## Building The Win64 Installer

To produce the Windows installer executable as well:

```powershell
.\build-launchers.ps1 -Platform x64 -BuildInstaller
```

If NSIS is installed and the packaging step succeeds, the script writes an installer like:

```text
ReCnC-release-YYYYMMDD-x64.exe
```

The packaging script uses `OpenRA-source/packaging/windows/OpenRA.nsi` and also:

- downloads `rcedit-x64.exe` if not already cached at the repo root
- downloads or reuses the GeoIP database used by the packaged server bits
- stamps version/tag information into the packaged output

## Publishing Releases

The recommended split is:

- Git repository: source, docs, scripts, reference files
- GitHub Release assets: Win64 installer `.exe` and optionally the portable `.zip`

Do not commit generated installer binaries, zips, logs, `build/`, or `test-release/` back into the repository. Those are already ignored by the root `.gitignore`.

## Provenance And Licensing

This workspace includes imported upstream code and reference material from multiple sources. Before making the repository public, review the provenance and licensing of everything you intend to redistribute, especially any non-source assets or imported third-party material.

At minimum:

- preserve upstream license files already present in imported trees
- avoid claiming this is a clean original codebase
- document what is vendored, what is modified, and what is reference-only

## Current Status

This repo is being prepared as a clean publishable source repository. Generated release artifacts, local logs, cached tools, and nested upstream git metadata are excluded so the first commit represents the actual project rather than a dump of local build output.
