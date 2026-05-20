# Provenance And Licensing Notes

This document records the current publication posture of the `ReCnC` repository as of the initial cleanup pass. It is not legal advice. Its purpose is to make the repository's composition explicit before any decision to make the GitHub repo public.

## Summary

The repository is an aggregate workspace, not a single-origin codebase. It combines:

- a vendored OpenRA engine tree
- imported Electronic Arts C&C source releases
- locally authored bug notes, diffs, packaging scripts, and workflow documents
- reference configuration files whose redistribution basis should be reviewed before broad publication

Because of that mix, the repo should not currently advertise one simple top-level license for everything in the tree.

## Current Component Review

| Path | Role in repo | License evidence in tree | Current publication note |
| --- | --- | --- | --- |
| `OpenRA-source/` | Main engine and mod codebase | `OpenRA-source/COPYING`, `OpenRA-source/README.md` | Treat as `GPL-3.0-or-later` based on the vendored OpenRA licensing files. |
| `CnC_Tiberian_Dawn/` | Reference source tree | `CnC_Tiberian_Dawn/LICENSE.md`, `CnC_Tiberian_Dawn/README.md` | EA states the source was released under GPLv3 with additional Section 7 terms. Preserve those notices. |
| `CnC_Red_Alert/` | Reference source tree | `CnC_Red_Alert/LICENSE.md`, `CnC_Red_Alert/README.md` | EA states the source was released under GPLv3 with additional Section 7 terms. Preserve those notices. |
| `CnC_Remastered_Collection/` | Reference source tree | `CnC_Remastered_Collection/LICENSE.md`, `CnC_Remastered_Collection/README.md` | README is narrower than the folder name: it says the release covers `TiberianDawn.dll`, `RedAlert.dll`, the Map Editor, and corresponding source. Do not describe this subtree as the full remastered game source. |
| `RulesFiles/` | Reference balance/config data | No top-level license summary seen in this pass | Redistribution basis needs manual review before public promotion. Current guidance: keep private until origin is documented. |
| `patches/` | Local patch history and backups | Local project material unless a file itself says otherwise | Safe to publish as project change history, but keep upstream notices in any copied files. |
| `Todo.md`, `OpenBugs.md`, `changelog.md`, `differences.md`, `Outline.txt`, `UnitComparison.md`, `UnitStatsComparisonGuide.md` | Local project docs and research | Project-authored workspace docs | Safe to publish as repository documentation. |
| `make.ps1`, `build-launchers.ps1`, `tools/` | Local build and helper tooling | Project-authored unless a contained file says otherwise | Safe to publish, subject to ordinary code review. |

## Important Caveats

### 1. No top-level license is declared yet

That is intentional. The repo contains multiple upstream trees with their own license files and extra terms. Until the remaining uncertain material is reviewed, adding a single blanket `LICENSE` file at the root would overstate certainty.

### 2. EA trees include additional terms

Both `CnC_Tiberian_Dawn/LICENSE.md` and `CnC_Red_Alert/LICENSE.md` state GPLv3 applies with additional terms under GPL Section 7. Those notices need to remain intact if the repo is shared.

### 3. Remastered scope should be described carefully

`CnC_Remastered_Collection/README.md` says the published source covers `TiberianDawn.dll`, `RedAlert.dll`, and the Map Editor. Any repo description or README text should avoid implying that EA released the entire remastered collection source wholesale.

### 4. `RulesFiles/` still needs a paper trail

Inference from file names: `RulesFiles/` appears to contain game rules and balance references for RA and TS-era content. Before making the repo public, document where those files came from and whether redistribution is permitted in the way this repo uses them.

## Recommended Public-Readiness Checklist

- Keep the repository private until the origin of `RulesFiles/` is documented.
- Preserve all upstream `LICENSE*`, `COPYING`, and `README` files in imported trees.
- Do not add a top-level license until the mixed-license story is intentionally resolved.
- If the repo is made public, add a short disclaimer in the top-level README that this is an aggregate workspace combining vendored upstream code and reference material.
- If release binaries are distributed publicly, ensure the corresponding source remains available in a way consistent with the applicable GPL obligations.

## Short Conclusion

The repo is publishable as a private working archive now. It is not yet clean enough, from a provenance-story standpoint, to present as a fully polished public open-source project without additional review of the reference-material subtrees, especially `RulesFiles/`.
