# Game Foundation Project Setup

Open `Game Foundation > Project Setup` to generate a project-owned QFramework architecture, Controller base, centralized role folders, asmdef, UI convention profile, AI governance, Feature Spec workflow, validation script and CI gate.

Existing generated paths are preserved unless `Overwrite existing generated files` is explicitly enabled. The Foundation-managed block inside an existing `AGENTS.md` is updated without replacing project-owned instructions. Keep this Editor-only package installed when the project uses the Feature Spec window or Unity convention validator.

Use `Game Foundation > Feature Workflow > New Feature Specification` before behavior-changing implementation. Use `Game Foundation > Validate Project Conventions` before completion; the generated Python script and GitHub workflow apply the same project contract outside Unity.

Use `Game Foundation > Install Optional Tools > Recommended All` to install the pinned YooAsset backend and the two project-owned Figma import/sync tools. The install queue survives Unity domain reloads and skips packages already present in the project.

For AI or CI BatchMode, set `GAME_FOUNDATION_PROJECT_NAME`, `GAME_FOUNDATION_ROOT_NAMESPACE`, and optionally the other `GAME_FOUNDATION_*` flags, then execute `GameFoundation.Setup.Editor.FoundationProjectSetupBatch.GenerateFromEnvironment`. Governance installation is enabled by default and can be controlled with `GAME_FOUNDATION_INSTALL_GOVERNANCE`.

Feature Specs can be generated with `GameFoundation.Setup.Editor.FoundationFeatureSpecBatch.GenerateFromEnvironment` and `GAME_FOUNDATION_FEATURE_ID`, `GAME_FOUNDATION_FEATURE_TITLE`, and `GAME_FOUNDATION_FEATURE_SUMMARY`.
