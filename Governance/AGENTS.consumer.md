<!-- GAME FOUNDATION MANAGED START -->
# Game Foundation Project Rules

This project uses Game Foundation and one project-owned QFramework architecture.

Project configuration:

- Project: `{{PROJECT_NAME}}`
- Root namespace: `{{ROOT_NAMESPACE}}`
- Code root: `{{CODE_ROOT}}`
- Architecture: `{{ROOT_NAMESPACE}}.{{PROJECT_NAME}}App`
- Controller base: `{{ROOT_NAMESPACE}}.{{PROJECT_NAME}}Controller`

Read before behavior-changing work:

- `.gamefoundation/project.json`
- `Docs/Foundation/Architecture.md`
- `Docs/Features/README.md`
- The related `Docs/Features/<feature-id>.md`

## Onboarding and repair

- For AI-driven initial setup, follow `Docs/Foundation/NewProjectSetup.md` and the framework repository's `Scripts/onboard_project.py` entry point. Installing UPM references alone does not complete onboarding.
- Reuse `.gamefoundation/project.json` on reruns. Never substitute `NewGame`, rename the project identity, or generate a second Architecture during setup/repair.
- Read `.gamefoundation/onboarding-report.json` after compilation and provider validation. Distinguish local readiness from Figma credentials, CDN, SDK, and device tasks.
- The configured content root is for explicitly selected hot-update content only; preserve existing YooAsset packages, groups, and collection rules. Default UI assets follow the Resources convention below.
- The two Figma tools are developer-managed. Do not install, replace, or upgrade them during default onboarding. Their absence is not a base-readiness failure; UI and prefab conventions still apply.

## Required feature workflow

- Every behavior-changing requirement must create or update one Feature Spec under `Docs/Features` before implementation.
- Use `.agents/skills/specify-foundation-feature` to classify scope, ownership, acceptance criteria, failure paths, and verification.
- Use `.agents/skills/develop-foundation-feature` for project code and `.agents/skills/normalize-figma-unity-ui` for imported Figma UI.
- Keep the Feature Spec status current and mark it `Verified` only after its acceptance criteria and Definition of Done have evidence.
- Run `python3 Scripts/validate_game_foundation_project.py --project .` before completion. CI applies the same check to pull-request diffs.

## Architecture

- Keep centralized role folders directly under `{{CODE_ROOT}}`: `Commands`, `Models`, `Systems`, `Events`, `ViewControllers`, and `Utilities`.
- Add responsibility subfolders inside those role folders. Do not create feature folders containing another role tree.
- Project state mutation belongs in QFramework Commands. ViewControllers and UI callbacks send Commands and render state; they do not mutate Models or Systems directly.
- Shared project rules belong in Systems, persistent or observable state in Models, one-shot notifications in Events, Unity presentation in ViewControllers, and stateless bridges in Utilities.
- Register dependencies only in `{{PROJECT_NAME}}App.Init`.
- Cache QFramework dependencies during initialization. Runtime callbacks, refresh methods, animations, and analytics helpers must not repeatedly call `GetModel`, `GetSystem`, or `GetUtility`.
- Gameplay-facing MonoBehaviours should inherit `{{PROJECT_NAME}}Controller`.

## Foundation boundary

- Foundation contains reusable Core, Save, content-update, SDK lifecycle, UI, setup, and optional provider infrastructure.
- Gameplay, levels, progression, economy, rank, store, IAP, rewards, concrete navigation, player save schemas, analytics event schemas, and vendor policy remain in the project.
- Do not edit `Packages/com.nn555k.gamefoundation.*` for a project-only requirement.
- Do not place vendor credentials, product URLs, production prefabs, or project state inside Foundation packages.

## Unity assets and UI

- Read `Docs/Foundation/ResourceLoading.md` before creating UI or prefabs. Default path-based loading uses `Resources.Load<T>` / `Resources.LoadAsync<T>`, not YooAsset.
- Put path-loaded UI prefabs under `Assets/Resources/Prefabs/UI` and path-loaded sprites under `Assets/Resources/Sprites/UI`. Put directly referenced UI art under `Assets/Sprites/UI`, directly referenced prefabs under `Assets/Sprites/Prefabs/UI`, and raw imports under `Assets/Sprites/DesignImports`.
- Record the final asset path, Resources key or direct reference owner, and lifecycle in the Feature Spec. A Resources prefab may reference art outside Resources; do not duplicate or move its entire dependency tree.
- Preserve existing resource paths unless migration is explicitly requested. Never automatically replace existing loaders or move Resources into YooAsset collectors.
- Do not edit `.prefab`, `.unity`, or `.asset` files as text. Use Unity Editor APIs, Prefab Mode, or Unity MCP and read back exact values.
- Treat Figma output as raw design data. Normalize naming and hierarchy, preserve stable Figma binding IDs and references, then run the project prefab validator.
- Preserve existing Inspector references and prefab overrides unless the Feature Spec explicitly authorizes structural changes.

## Completion gate

- Convention validation passes.
- Unity compiles without new errors.
- Focused EditMode/PlayMode tests and exact asset/runtime checks cover the changed boundary.
- Failure, offline, cancellation, lifecycle, persistence, and performance checks are included when relevant.
- The completion report links the Feature Spec and lists changed scripts/assets, affected scenes/prefabs/components, evidence, performance impact, and remaining Inspector or device checks.
<!-- GAME FOUNDATION MANAGED END -->
