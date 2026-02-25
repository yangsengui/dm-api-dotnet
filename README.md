# dm-api-dotnet

.NET SDK for DistroMate `dm_api.dll`.

## Install

```bash
dotnet add package DistroMate.DmApi
```

## Integration Flow

1. Initialization: `SetProductData`, `SetProductId`.
2. Activation: `SetLicenseKey`, `ActivateLicense`.
3. Validation on startup: `IsLicenseGenuine` or `IsLicenseValid`.
4. Version/update: `Version`, `GetLibraryVersion`, update IPC methods.

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

`DmApi.cs` currently contains `{{PUBKEY}}` placeholder for signature verification.
Replace it with your real PEM public key before publishing production package.
