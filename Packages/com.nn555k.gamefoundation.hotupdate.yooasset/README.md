# Game Foundation YooAsset Backend

Concrete YooAsset `2.3.18` integration for `IContentUpdateBackend`.

Install this package together with the root-manifest Git dependency for YooAsset. The repository installer writes both references automatically when the `yooasset` alias is selected.

The backend uses `PreDownloadContentAsync`, so the remote manifest does not become active until download and verification succeed. Project CDN URLs, encryption services, package names and content tags remain project configuration.

```csharp
var options = YooAssetContentUpdateOptions.Host(
    packageName: "DefaultPackage",
    remoteMainUrl: "https://cdn.example.com/iOS/DefaultPackage",
    remoteFallbackUrl: "https://origin.example.com/iOS/DefaultPackage");

FoundationHotUpdateModule.Register(
    this,
    new YooAssetContentUpdateBackend(options));
```

For offline packages use `YooAssetContentUpdateOptions.Offline`. Editor simulation accepts the simulated package root produced by the YooAsset build pipeline.

This package updates resource content only. It does not install or configure a managed-code hotfix solution such as HybridCLR.
