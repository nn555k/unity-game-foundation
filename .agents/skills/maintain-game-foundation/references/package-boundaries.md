# Package Boundaries

```text
com.nn555k.gamefoundation.core
  ^       ^          ^        ^
  |       |          |        |
 save  hotupdate    sdk       ui
          ^
          |
       yooasset

setup (Editor only) -> core + save + hotupdate + sdk + ui
```

## Core

Owns only shared composition and minimal platform-neutral contracts such as time and logging. It must not know concrete UI, save, hot-update, SDK, vendor, or project types.

## Save

Owns storage and serialization abstractions, version envelopes, checksum, migration sequencing and recovery. Consuming projects own save schemas and cloud/vendor integration.

## HotUpdate

Owns the deterministic check/download/verify/commit/rollback lifecycle. Backends translate YooAsset, Addressables or another system into `IContentUpdateBackend`.

## YooAsset Backend

Optionally depends on HotUpdate and the pinned YooAsset package. It owns vendor translation only; projects still own CDN URLs, package names, tags, encryption and rollout policy.

## SDK

Owns adapter lifecycle, ordering, consent gating, timeout and isolated diagnostics. It explicitly excludes purchasing flows and business event definitions.

## UI

Owns generic screen/popup routing, Safe Area handling, prefab convention profiles, validation and editor normalization. Projects own visual assets, controllers, transitions and business Commands.

## External design import

Download and synchronization tools are not distributed in this repository. Developers own their installation, licensing, credentials and verification. UI normalization preserves existing stable node bindings without depending on a particular tool or component type.

## Setup

Owns Editor-only project scaffolding and may depend on every runtime package. It generates project-owned code, settings, AI instructions, Skills, Feature Spec templates, validation, and CI. It never ships runtime behavior, replaces project-owned AGENTS content outside its managed block, or silently overwrites existing files.

## Public API test

A type belongs in Foundation only if its name and contract remain meaningful in a new game without renaming product concepts. If it needs a project model, level rule, product event, SKU, currency or rank season, it belongs in the consumer.
