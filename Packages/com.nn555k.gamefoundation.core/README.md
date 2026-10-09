# Game Foundation Core

QFramework composition helpers plus replaceable clock and logging utilities.

## Requirement

The consuming Unity project must provide a QFramework assembly named `QFramework`.

## Registration

```csharp
protected override void Init()
{
    FoundationCoreModule.Register(this);
}
```

The project remains the owner of its `Architecture<T>` and Controller base.
