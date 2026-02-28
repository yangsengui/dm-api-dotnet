# dm-api-dotnet

.NET SDK for DistroMate `dm_api` native library.

## Install

```bash
dotnet add package DistroMate.DmApi
```

## Quick Start (License)

```csharp
using DistroMate;

using var api = new DmApi();

api.SetProductData("<product-data>");
api.SetProductId("your-product-id");
api.SetLicenseKey("XXXX-XXXX-XXXX");

if (!api.ActivateLicense())
{
    throw new Exception(api.GetLastError() ?? "activation failed");
}

if (!api.IsLicenseGenuine())
{
    uint? code = api.GetLastActivationError();
    string? name = api.GetActivationErrorName(code);
    throw new Exception($"license check failed: {name}, err={api.GetLastError()}");
}
```

## API Groups

- License setup: `SetProductData`, `SetProductId`, `SetDataDirectory`, `SetDebugMode`, `SetCustomDeviceFingerprint`
- License activation: `SetLicenseKey`, `SetLicenseCallback`, `ActivateLicense`, `GetLastActivationError`
- License state: `IsLicenseGenuine`, `IsLicenseValid`, `GetServerSyncGracePeriodExpiryDate`, `GetActivationMode`
- License details: `GetLicenseKey`, `GetLicenseExpiryDate`, `GetLicenseCreationDate`, `GetLicenseActivationDate`, `GetActivationCreationDate`, `GetActivationLastSyncedDate`, `GetActivationId`
- Update: `CheckForUpdates`, `DownloadUpdate`, `CancelUpdateDownload`, `GetUpdateState`, `GetPostUpdateInfo`, `AckPostUpdateInfo`, `WaitForUpdateStateChange`, `QuitAndInstall`
- General: `GetLibraryVersion`, `JsonToCanonical`, `GetLastError`, `Reset`

## Update API Notes

- Update APIs return parsed JSON envelope (`JsonObject`) when transport succeeds.
- If native API returns `NULL`, .NET SDK returns `null`; check `GetLastError()`.
- `QuitAndInstall()` returns native `int` status code directly:
  - `1`: accepted, process should exit soon
  - `-1`: business-level rejection (check `GetLastError()`)
  - `-2`: transport or parse error

## Environment Variables

- `DM_API_PATH`: optional path to native library
- `DM_APP_ID`, `DM_PUBLIC_KEY`: optional defaults for app identity
- `DM_LAUNCHER_ENDPOINT`, `DM_LAUNCHER_TOKEN`: launcher IPC variables used by update APIs

## Build

```bash
dotnet build -c Release
dotnet pack -c Release
```

## Release

- CI validates build and NuGet package generation.
- Tag `v*` triggers publish to NuGet.
- Required secret: `NUGET_API_KEY`.
