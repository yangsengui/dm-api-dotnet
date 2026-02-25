# dm-api-dotnet

.NET SDK for DistroMate `dm_api.dll`.

## Install

```bash
dotnet add package DistroMate.DmApi
```

## Integration Flow (Launcher Profile)

1. Call `DmApi.RestartAppIfNecessary()` at process start.
2. Connect to launcher pipe with `Connect` (or use `VerifyAndActivate`).
3. Validate using `Verify` and `Activate` signed-response flow.
4. Drive updates via `CheckForUpdates`, `DownloadUpdate`, and state APIs.

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
