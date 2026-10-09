---
name: maintain-game-foundation
description: Safely create, refactor, test, version, or release the shared Unity packages under Packages/com.nn555k.gamefoundation.*. Use when changing Foundation Core, Save, HotUpdate, SDK, UI runtime APIs, prefab convention tooling, package manifests, adapter contracts, migrations, or cross-project infrastructure; not for ordinary game-specific feature work.
---

# Maintain Game Foundation

Evolve reusable infrastructure while keeping package boundaries, QFramework composition, and consuming projects stable.

## Required context

Read repository `AGENTS.md`, `Docs/Architecture.md`, and `references/package-boundaries.md`. Before verification or release, read `references/release-validation.md`.

## Workflow

1. Establish the cross-project use case and reject project-only behavior from the package scope.
2. Identify the owning package and inspect its public API, assembly definition, package manifest, tests, and consumers.
3. Preserve dependency direction: Core has no sibling package dependency; Save, HotUpdate, SDK, and UI may depend on Core but not one another. Optional provider packages may depend only on their owning capability, and the Editor-only Setup package may depend on the base runtime packages and owns new-project governance Bootstrap assets.
4. Define vendor-neutral interfaces at actual substitution boundaries. A concrete provider may live in a separately installable package only after it is reusable across projects; vendor SDK policy and all game policy remain in the consuming project.
5. Prefer additive API changes. For a breaking change, update all consumers, docs, tests, and package version in the same task.
6. Add or update focused EditMode tests. For persistence, preserve backward migration and recovery behavior.
7. When consumer governance sources change, synchronize Setup templates with `Scripts/sync_governance_templates.py`, then run `scripts/validate-foundation-boundaries.sh` from this skill directory.
8. Compile with the repository's pinned Unity version and run Foundation EditMode tests.
9. Review the diff for accidental `.meta`, scene, prefab, package-lock, or business-code changes.

## Hard exclusions

Foundation does not own gameplay rules, levels, rank, economy, store, purchasing, IAP, rewards, concrete app navigation, concrete player data, or vendor event schemas. It does not create a second `Architecture<T>`, a global Manager, or hidden scene objects.

## UI asset rule

The UI package may define schema, validation and editor transformation APIs. It must not contain project-specific production prefabs. Project rules belong in a `UiPrefabConventionProfile` asset or project-side factory.

## Completion report

State the package and API changed, compatibility impact, consumer changes, tests run, boundary-check result, and whether a semantic version bump is required.
