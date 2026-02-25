# dm-api-dotnet

.NET SDK for DistroMate `dm_api.dll`.

## Install

```bash
dotnet add package DistroMate.DmApi
```

## Integration Flow

This SDK follows the same LexActivator-style flow as Python SDK:

1. `SetProductData()` and `SetProductId()`.
2. `SetLicenseKey()` and `ActivateLicense()`.
3. `IsLicenseGenuine()` or `IsLicenseValid()` on every startup.
4. Optional version/update APIs: `GetVersion()`, `GetLibraryVersion()`, `CheckForUpdates()`.

## Quick Start

```csharp
using DistroMate;

using var api = new DmApi();
api.SetProductData("<product_data>");
api.SetProductId("your-product-id", 0);
api.SetLicenseKey("XXXX-XXXX-XXXX");

if (!api.ActivateLicense())
{
    throw new Exception(api.GetLastError() ?? "activation failed");
}

if (!api.IsLicenseGenuine())
{
    throw new Exception(api.GetLastError() ?? "license not genuine");
}
```

## Dev License Skip Check

Use `DmApi.ShouldSkipCheck(appId, publicKey)` for local dev-license validation when needed.

## Build

```bash
dotnet build -c Release
dotnet pack -c Release
```

## Release

- CI validates build and NuGet package generation.
- Tag `v*` triggers publish to NuGet.
- Required secret: `NUGET_API_KEY`.

## Note

No `{{PUBKEY}}` placeholder replacement is required.
