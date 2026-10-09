# Changelog

## 0.6.0 - 2026-10-09

- Remove bundled design-tool installation, readiness inspection and provider dependencies; retain vendor-neutral UI normalization and developer-managed external integrations.

## 0.5.0 - 2026-10-09

- Create Resources and direct-reference Sprites directories, install resource-loading governance into AGENTS/Skills/Feature Specs, and validate duplicate Resources keys and invalid folder nesting. Existing assets and loaders are not migrated.

## 0.4.1 - 2026-10-09

- Excluded developer-managed Figma tools from default onboarding and the recommended installer; retain explicit manual installation.
- Allow missing optional Figma tools during base readiness validation, while preserving existing installation references.

## 0.4.0 - 2026-10-09

- Added AI onboarding and post-compilation readiness reporting for generated architecture, UI conventions, YooAsset and Figma tools.
- Reuse the saved project identity on reruns; reject identity changes and duplicate architectures before generation.
- Complete installed provider defaults automatically and disable redundant tool installation menu actions.
- First batch initialization now requires an explicit project name; migration is separate from repair.

## 0.3.1 - 2026-10-08

- Expanded Feature Spec diff coverage to all non-meta Unity assets, project settings, package dependencies, project tooling, and embedded Foundation changes.
- Added blocking validation for project C# files placed outside the configured centralized code root.
- Limited the governance Bootstrap diff exemption to projects whose base revision has no project contract.

## 0.3.0 - 2026-10-08

- Added a new-project governance Bootstrap with managed `AGENTS.md`, consumer Skills, Foundation docs, Feature Spec templates, project configuration, validation script, pull-request template, and GitHub CI.
- Added Unity menu and BatchMode project convention validation.
- Added Unity menu and BatchMode Feature Spec generation.
- Added idempotent managed-block updates that preserve project-owned AI instructions.

## 0.2.0 - 2026-09-04

- Added a reload-safe optional tool installer for YooAsset and Figma import/sync packages.

## 0.1.0 - 2026-09-03

- Added safe project scaffolding window.
- Added selective Foundation module registration generation.
- Added input validation and no-overwrite defaults.
