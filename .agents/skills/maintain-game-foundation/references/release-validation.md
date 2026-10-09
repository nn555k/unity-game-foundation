# Release Validation

## Required checks

1. Run `scripts/validate-foundation-boundaries.sh`.
2. Confirm every runtime package has a valid `package.json` and runtime asmdef; Editor-only tooling packages require an Editor asmdef.
3. Run `Scripts/sync_governance_templates.py --check` and the project-validator unit tests when Setup governance changes.
4. Compile in the minimum supported Unity version.
5. Run EditMode tests for all changed packages.
6. Import the Core architecture sample into a clean test project when changing composition APIs.
7. Validate the generated governance contract in an isolated project when changing Setup.
8. Validate at least one project-specific `UiPrefabConventionProfile` when changing UI rules.
9. Test old save fixtures when changing envelopes, serializers or migrations.
10. Test cancel, failure and offline paths when changing hot-update orchestration.
11. Test slow and throwing adapters when changing SDK lifecycle.

## Versioning

- Patch: compatible bug fix or internal test/tool improvement.
- Minor: additive public API or new optional behavior.
- Major: removed/renamed API, changed serialized contract, changed default behavior requiring consumer work.

During `0.x`, still document breaking changes explicitly and update all in-repository consumers atomically.

## Diff review

No project namespace, vendor credentials, IAP, gameplay, rank or economy dependency may enter the shared packages. Approved provider dependencies belong only in separately installable adapter packages. Generated `Library`, `Temp`, `Logs`, build artifacts and unrelated Unity assets are never part of the release.
