# Release Template

Use this as the starting point for GitHub Releases.

## Title

```text
ReCnC release-YYYYMMDD
```

## Tag

```text
release-YYYYMMDD
```

## Body

```md
Private release build for Win64.

Highlights:
- Updated OpenRA-based ReCnC workspace snapshot
- Windows installer build
- Portable Win64 bundle

Assets:
- `ReCnC-release-YYYYMMDD-x64.exe`
- `ReCnC-release-YYYYMMDD-x64-winportable.zip`

Notes:
- Source for this build is tracked in the repository on `main`
- This repository is an aggregate workspace containing vendored upstream code and reference material
- Public redistribution posture is still under review; keep the repository private until provenance review is complete
```

## Pre-Publish Checklist

- Verify the target commit on `main`.
- Confirm the README still reflects the current build/runtime requirements.
- Confirm the release artifacts exist locally:
  - `ReCnC-release-YYYYMMDD-x64.exe`
  - `ReCnC-release-YYYYMMDD-x64-winportable.zip`
- Confirm the repo is still private if provenance review is not complete.
- Upload only release artifacts to GitHub Releases, not to git history.

## Optional Additions

If the release contains meaningful gameplay or packaging changes, add a short section:

```md
Build notes:
- Added or updated gameplay parity work
- Updated launcher packaging
- Refreshed Win64 installer
```
