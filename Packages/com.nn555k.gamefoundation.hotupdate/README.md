# Game Foundation Hot Update

Vendor-neutral content-update orchestration covering check, download, verification, commit, rollback, cancellation and offline fallback.

Implement `IContentUpdateBackend` in the consuming project for YooAsset, Addressables or another provider. This package does not implement managed-code hotfixing.

Import `Content Backend Template` from Package Manager to get a provider bridge that preserves the Foundation check/download/verify/commit/rollback contract.
