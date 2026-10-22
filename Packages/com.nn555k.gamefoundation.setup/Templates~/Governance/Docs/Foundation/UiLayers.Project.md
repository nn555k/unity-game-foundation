# {{PROJECT_NAME}} UI layer adoption

Adopt the contract in [UiLayers.md](UiLayers.md) when creating production UI. This file is project-owned and preserved by default during Setup repair. Setup installs guidance; it does not create or validate production UI hosts.

Record the following before the first UI implementation:

- UI domain, root asset/scene and lifetime owner.
- Actual hosts for Pages / Panels / Popups / Guides / Effects / Toasts / Blockers; default orders 0 / 100 / 200 / 300 / 400 / 500 / 600, or the centrally configured ordered equivalent.
- Layer mounting and Canvas/sorting owner; camera/rendering-domain choices, Safe Area and page scalers.
- Popup loader/cache owner, Replace/Stack interfaces and unsupported modes; top-window close/back behavior.
- Owner-scoped uGUI gates, release paths and routing for keyboard/gamepad/back/world input.
- Project Commands/Systems that decide gameplay pause/resume and navigation cleanup.
- Exact layer/asset/runtime verification entry points and recorded evidence.

Do not claim these bindings or runtime checks are complete until the project implements and verifies them. Keep product-specific buttons, prices, page geometry and business state out of the portable contract.
