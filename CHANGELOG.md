# Changelog

## 0.6.0 - 2026-10-09

- Publish a clean framework-only repository without the two bundled design import/sync tools, their supporting assets or product links.
- Remove the design-tool package alias, installer menus, provider readiness reflection and clean-install assembly dependencies. External project-owned tools remain untouched.
- Preserve all seven Foundation packages, Resources conventions, prefab normalization, AI Skills and feature governance. Runtime APIs and save formats are unchanged.
- Breaking distribution change: old tool packages and historical tags are available only from the private archive. See Docs/PublicRelease.md before upgrading an existing project.

## 0.5.0 - 2026-10-09

- Default new UI/prefab resource policy to Resources path loading, with direct-reference UI/prefab/art assets under Assets/Sprites.
- Generate resource directories during Setup, keep YooAsset content separate, and synchronize the rule into consumer AGENTS, Skills and Feature Specs.
- Add Unity/Python guards for ambiguous Resources keys and Resources folders inside the direct-reference art tree. Existing assets, save data and runtime loaders are not migrated.
- Upgrade note: validators now reject ambiguous Resources keys and invalid folder nesting. Review reported conflicts instead of automatically moving existing assets. Merge preserved project-owned docs/Skills as needed; Setup updates its managed AGENTS block without overwriting other project instructions.
- Runtime APIs and save formats are unchanged. All package versions and default install references are aligned to v0.5.0.

## 0.1.0 - 2026-09-03

- Added Core, Save, HotUpdate, SDK and UI runtime packages.
- Added schema-driven UI prefab validation and Figma normalization tools.
- Added an Editor-only new-project Setup Wizard.
- Added project-local AI Skills and deterministic boundary validation.
- Added a minimal Unity validation project and GitHub Actions workflows.
