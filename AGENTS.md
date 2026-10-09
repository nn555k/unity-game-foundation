# AI Coding Instructions For Unity Game Foundation

Read first:

- `Docs/Architecture.md`
- `Docs/NewProjectSetup.md`
- `Docs/FeatureGovernance.md`
- `Docs/FigmaUiWorkflow.md`
- `Docs/IntegrationAdapters.md`
- `.agents/skills/maintain-game-foundation/SKILL.md` when changing shared packages

## Scope

- Shared package roots are `Packages/com.nn555k.gamefoundation.*`.
- Runtime dependency direction is `Core <- Save/HotUpdate/SDK/UI`; optional vendor packages may depend only on their owning capability (`YooAsset -> HotUpdate`). Design import tools are developer-owned external integrations, not distributed packages.
- The Editor-only Setup package may reference all runtime packages.
- Do not add gameplay, level rules, rank, economy, store, purchasing, IAP, rewards, project save DTOs, product analytics schemas, concrete navigation or vendor credentials.
- Do not reference a consuming project's namespace or files from a shared package.
- Keep vendor integrations behind `IContentUpdateBackend`, `ISdkAdapter`, `ISaveStore`, or other explicit adapter boundaries. Cross-project provider packages must remain optional and contain no project URLs or credentials.

## QFramework

- The consuming project owns one `Architecture<T>` and its Controller base.
- Foundation modules only register replaceable utilities into that architecture.
- Projects register modules only in their architecture `Init` method.
- UI presentation forwards state changes through project Commands; Foundation routers do not mutate game state.

## Unity assets

- Consumer UI/prefab creation follows `Docs/ResourceLoading.md`: Resources for path-loaded assets, `Assets/Sprites` for directly referenced UI/prefab/art assets. Preserve existing paths; do not migrate consumer assets automatically.
- Never edit `.prefab`, `.unity`, or `.asset` as text. Use Unity Editor APIs, Prefab Mode or Unity MCP.
- Figma raw imports are not production prefabs. Preview normalization, preserve stable node IDs and references, then validate the production prefab.
- Do not commit real product scenes or Figma pages to this repository.
- Keep Setup governance templates synchronized with their canonical Docs, Scripts, and consumer Skills by running `Scripts/sync_governance_templates.py --check`.
- Figma access tokens and OAuth secrets are machine-local only; never serialize them into package assets or source.

## Code and performance

- Keep public APIs vendor-neutral and additive where possible.
- Add a concise purpose or boundary comment to every new or changed method.
- Avoid repeated lookups, LINQ allocation and reflection on hot paths.
- Cache Unity component and QFramework dependencies during initialization.
- Avoid hidden managers, global singletons and implicit scene objects.

## Validation

Before completion:

1. Run `Scripts/validate-foundation.sh`.
2. Run Unity EditMode and PlayMode tests relevant to the change.
3. Validate package JSON and asmdef files.
4. Review for accidental project, vendor, serialized asset or generated-cache content.
5. Report package/API compatibility and required manual platform verification.
