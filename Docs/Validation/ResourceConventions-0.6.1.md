# Resource conventions release validation

Date: 2026-10-19. Release: v0.6.1.

## Scope

- Canonical AGENTS, consumer Skills, Feature Spec template, ProjectLayout and ResourceLoading documents are synchronized into the Setup package.
- Setup creates typed Prefabs / Art / Sprite-only Sprites roots, plus Resources/Prefabs/UI/Popups; it does not migrate existing assets.
- Python and Unity validators reject non-Sprite content under Assets/Sprites while retaining duplicate/nested Resources checks.
- Popup first-use loading, reuse, failure handling and model dependency audits are documented as consumer responsibilities; no product controller or prefab is shipped.
- Default install/onboarding and optional-tool references target v0.6.1. All seven package versions are aligned; runtime source, public runtime APIs and save formats are unchanged.

## Evidence

| Check | Result |
| --- | --- |
| Scripts/validate-foundation.sh | Passed: package boundaries, JSON/asmdef syntax, template synchronization and 22 Python tests |
| Updated consumer Skill quick validation | All three passed |
| Unity repository EditMode | 48 passed, 0 failed/skipped |
| Unity repository PlayMode | 1 passed, 0 failed/skipped |
| Isolated external consumer Setup | Passed; expected resource directories and both new convention documents present; obsolete Sprites/Prefabs and Sprites/DesignImports absent |
| Generated consumer Python convention validation | Passed |
| Isolated external consumer EditMode | 49 passed, 0 failed/skipped |
| Change review | No product source/assets, scene, prefab, meta or runtime implementation changes |

Unity tests ran on the installed 2022.3.62f3c1 Editor. The repository's pinned 2022.3.62f2c1 Editor was not installed and was not tested; its project version pin remains unchanged. No platform build or device performance claim is made for this Editor/governance change.

The initial clean installation was interrupted by packages.unity.cn ECONNRESET errors before Setup. The successful isolated consumer used external local Foundation packages and the same resolved dependency versions from the validation project's package cache: YooAsset 2.3.18, Test Framework 1.1.33, NUnit 1.0.6, Scriptable Build Pipeline 1.21.25 and uGUI 1.0.0. This verifies package consumption, compilation, generation and conventions; it does not claim a successful fresh registry download.

## Upgrade contract

Existing package pins do not upgrade automatically. Fresh onboarding defaults to v0.6.1. Existing non-Sprite assets under Assets/Sprites now fail the convention gate and require an explicitly scoped migration that preserves GUIDs, references, Resources keys and behavior. Existing custom architectures must be adopted without creating a second Architecture. Setup preserves existing project-owned files by default; merge updated documentation and Skills deliberately.

Local test artifacts are under the ignored TestResults directory: editmode.xml, playmode.xml, clean-editmode.xml and cached-consumer-summary.json. Inspector rebinding is not required by this release because no consuming scene or prefab is modified.
