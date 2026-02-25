using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DistroMate
{
    internal static class DmApiEnv
    {
        public const string DmPipe = "DM_PIPE";
        public const string DmApiPath = "DM_API_PATH";
        public const string DmAppId = "DM_APP_ID";
        public const string DmPublicKey = "DM_PUBLIC_KEY";
    }

    internal static class DmApiNative
    {
        private const string DllName = "dm_api.dll";

        public static void EnsureLoaded(string? explicitPath = null)
        {
            string? path = explicitPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                path = Environment.GetEnvironmentVariable(DmApiEnv.DmApiPath);
            }

            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                try
                {
                    NativeLibrary.Load(path);
                }
                catch
                {
                }
            }
        }

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int DM_Connect(
            [MarshalAs(UnmanagedType.LPStr)] string pipeName,
            uint timeoutMs);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int DM_Close();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr DM_GetVersion();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int DM_RestartAppIfNecessary();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr DM_GetLastError();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DM_FreeString(IntPtr ptr);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr DM_JsonToCanonical(
            [MarshalAs(UnmanagedType.LPStr)] string jsonStr);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr DM_CheckForUpdates(
            [MarshalAs(UnmanagedType.LPStr)] string optionsJson);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr DM_DownloadUpdate(
            [MarshalAs(UnmanagedType.LPStr)] string optionsJson);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr DM_GetUpdateState();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr DM_WaitForUpdateStateChange(
            ulong lastSequence,
            uint timeoutMs);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int DM_QuitAndInstall(
            [MarshalAs(UnmanagedType.LPStr)] string optionsJson);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetProductData(
            [MarshalAs(UnmanagedType.LPStr)] string productData);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetProductId(
            [MarshalAs(UnmanagedType.LPStr)] string productId,
            uint flags);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetDataDirectory(
            [MarshalAs(UnmanagedType.LPStr)] string directoryPath);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetDebugMode(uint enable);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetCustomDeviceFingerprint(
            [MarshalAs(UnmanagedType.LPStr)] string fingerprint);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetLicenseKey(
            [MarshalAs(UnmanagedType.LPStr)] string licenseKey);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetActivationMetadata(
            [MarshalAs(UnmanagedType.LPStr)] string key,
            [MarshalAs(UnmanagedType.LPStr)] string value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ActivateLicense();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ActivateLicenseOffline(
            [MarshalAs(UnmanagedType.LPStr)] string filePath);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GenerateOfflineDeactivationRequest(
            [MarshalAs(UnmanagedType.LPStr)] string filePath);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetLastActivationError(out uint errorCode);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int IsLicenseGenuine();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int IsLicenseValid();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetServerSyncGracePeriodExpiryDate(out uint expiryDate);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int GetActivationMode(
            [Out] StringBuilder initialMode,
            uint initialModeLength,
            [Out] StringBuilder currentMode,
            uint currentModeLength);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int GetLicenseKey(
            [Out] StringBuilder licenseKey,
            uint length);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetLicenseExpiryDate(out uint expiryDate);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetLicenseCreationDate(out uint creationDate);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetLicenseActivationDate(out uint activationDate);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetActivationCreationDate(out uint activationCreationDate);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetActivationLastSyncedDate(out uint activationLastSyncedDate);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int GetActivationId(
            [Out] StringBuilder id,
            uint length);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int GetLibraryVersion(
            [Out] StringBuilder libraryVersion,
            uint length);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Reset();
    }

    public sealed class DmApi : IDisposable
    {
        public const uint DefaultTimeoutMs = 5000;
        private const uint DefaultBufferSize = 256;
        private const uint DefaultModeBufferSize = 64;
        private const uint DefaultVersionBufferSize = 32;

        private const string DevLicenseErrorText =
            "Development license is missing or corrupted. Run `distromate sdk renew` to regenerate the dev certificate.";

        private readonly uint _pipeTimeoutMs;

        private delegate int StringOutCall(StringBuilder buffer, uint size);

        public DmApi(string? dllPath = null, uint pipeTimeoutMs = DefaultTimeoutMs)
        {
            DmApiNative.EnsureLoaded(dllPath);
            _pipeTimeoutMs = pipeTimeoutMs;
        }

        public static bool ShouldSkipCheck(string? appId = null, string? publicKey = null)
        {
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(DmApiEnv.DmPipe)) &&
                !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(DmApiEnv.DmApiPath)))
            {
                return false;
            }

            string resolvedAppId = (appId ?? Environment.GetEnvironmentVariable(DmApiEnv.DmAppId) ?? string.Empty).Trim();
            string resolvedPublicKey = (publicKey ?? Environment.GetEnvironmentVariable(DmApiEnv.DmPublicKey) ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(resolvedAppId) || string.IsNullOrWhiteSpace(resolvedPublicKey))
            {
                throw new InvalidOperationException(
                    "App identity is required for dev-license checks. Provide appId/publicKey or set DM_APP_ID and DM_PUBLIC_KEY.");
            }

            string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrWhiteSpace(homePath))
            {
                throw new InvalidOperationException(DevLicenseErrorText);
            }

            string pubkeyPath = Path.Combine(
                homePath,
                ".distromate-cli",
                "dev_licenses",
                resolvedAppId,
                "pubkey");

            string devPublicKey;
            try
            {
                devPublicKey = File.ReadAllText(pubkeyPath).Trim();
            }
            catch
            {
                throw new InvalidOperationException(DevLicenseErrorText);
            }

            if (string.IsNullOrWhiteSpace(devPublicKey) || !string.Equals(devPublicKey, resolvedPublicKey, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(DevLicenseErrorText);
            }

            return true;
        }

        public bool RestartAppIfNecessary()
        {
            return DmApiNative.DM_RestartAppIfNecessary() != 0;
        }

        public string GetVersion()
        {
            return PtrToStaticString(DmApiNative.DM_GetVersion());
        }

        public string? GetLastError()
        {
            return PtrToOwnedString(DmApiNative.DM_GetLastError());
        }

        public bool SetProductData(string productData)
        {
            return DmApiNative.SetProductData(productData) == 0;
        }

        public bool SetProductId(string productId, uint flags = 0)
        {
            return DmApiNative.SetProductId(productId, flags) == 0;
        }

        public bool SetDataDirectory(string directoryPath)
        {
            return DmApiNative.SetDataDirectory(directoryPath) == 0;
        }

        public bool SetDebugMode(bool enable)
        {
            return DmApiNative.SetDebugMode(enable ? 1u : 0u) == 0;
        }

        public bool SetCustomDeviceFingerprint(string fingerprint)
        {
            return DmApiNative.SetCustomDeviceFingerprint(fingerprint) == 0;
        }

        public bool SetLicenseKey(string licenseKey)
        {
            return DmApiNative.SetLicenseKey(licenseKey) == 0;
        }

        public bool SetActivationMetadata(string key, string value)
        {
            return DmApiNative.SetActivationMetadata(key, value) == 0;
        }

        public bool ActivateLicense()
        {
            return DmApiNative.ActivateLicense() == 0;
        }

        public bool ActivateLicenseOffline(string filePath)
        {
            return DmApiNative.ActivateLicenseOffline(filePath) == 0;
        }

        public bool GenerateOfflineDeactivationRequest(string filePath)
        {
            return DmApiNative.GenerateOfflineDeactivationRequest(filePath) == 0;
        }

        public uint? GetLastActivationError()
        {
            return DmApiNative.GetLastActivationError(out uint value) == 0 ? value : null;
        }

        public bool IsLicenseGenuine()
        {
            return DmApiNative.IsLicenseGenuine() == 0;
        }

        public bool IsLicenseValid()
        {
            return DmApiNative.IsLicenseValid() == 0;
        }

        public uint? GetServerSyncGracePeriodExpiryDate()
        {
            return DmApiNative.GetServerSyncGracePeriodExpiryDate(out uint value) == 0 ? value : null;
        }

        public JsonObject? GetActivationMode(uint bufferSize = DefaultModeBufferSize)
        {
            uint size = bufferSize == 0 ? DefaultModeBufferSize : bufferSize;
            var initial = new StringBuilder((int)size);
            var current = new StringBuilder((int)size);

            if (DmApiNative.GetActivationMode(initial, size, current, size) != 0)
            {
                return null;
            }

            return new JsonObject
            {
                ["initial_mode"] = initial.ToString(),
                ["current_mode"] = current.ToString(),
            };
        }

        public string? GetLicenseKey(uint bufferSize = DefaultBufferSize)
        {
            return CallStringOut(DmApiNative.GetLicenseKey, bufferSize, DefaultBufferSize);
        }

        public uint? GetLicenseExpiryDate()
        {
            return DmApiNative.GetLicenseExpiryDate(out uint value) == 0 ? value : null;
        }

        public uint? GetLicenseCreationDate()
        {
            return DmApiNative.GetLicenseCreationDate(out uint value) == 0 ? value : null;
        }

        public uint? GetLicenseActivationDate()
        {
            return DmApiNative.GetLicenseActivationDate(out uint value) == 0 ? value : null;
        }

        public uint? GetActivationCreationDate()
        {
            return DmApiNative.GetActivationCreationDate(out uint value) == 0 ? value : null;
        }

        public uint? GetActivationLastSyncedDate()
        {
            return DmApiNative.GetActivationLastSyncedDate(out uint value) == 0 ? value : null;
        }

        public string? GetActivationId(uint bufferSize = DefaultBufferSize)
        {
            return CallStringOut(DmApiNative.GetActivationId, bufferSize, DefaultBufferSize);
        }

        public string? GetLibraryVersion(uint bufferSize = DefaultVersionBufferSize)
        {
            return CallStringOut(DmApiNative.GetLibraryVersion, bufferSize, DefaultVersionBufferSize);
        }

        public bool Reset()
        {
            return DmApiNative.Reset() == 0;
        }

        public JsonObject? CheckForUpdates(JsonObject? options = null)
        {
            string req = JsonSerializer.Serialize(options ?? new JsonObject());
            return CallPipeJson(() => DmApiNative.DM_CheckForUpdates(req));
        }

        public JsonObject? DownloadUpdate(JsonObject? options = null)
        {
            string req = JsonSerializer.Serialize(options ?? new JsonObject());
            return CallPipeJson(() => DmApiNative.DM_DownloadUpdate(req));
        }

        public JsonObject? GetUpdateState()
        {
            return CallPipeJson(() => DmApiNative.DM_GetUpdateState());
        }

        public JsonObject? WaitForUpdateStateChange(ulong lastSequence, uint timeoutMs = 30000)
        {
            return CallPipeJson(() => DmApiNative.DM_WaitForUpdateStateChange(lastSequence, timeoutMs));
        }

        public bool QuitAndInstall(JsonObject? options = null)
        {
            string req = JsonSerializer.Serialize(options ?? new JsonObject());
            return CallPipeAccepted(() => DmApiNative.DM_QuitAndInstall(req));
        }

        public string JsonToCanonical(string jsonStr)
        {
            return PtrToOwnedString(DmApiNative.DM_JsonToCanonical(jsonStr)) ?? string.Empty;
        }

        public void Dispose()
        {
            DmApiNative.DM_Close();
        }

        private string? ResolvePipe()
        {
            string? pipe = Environment.GetEnvironmentVariable(DmApiEnv.DmPipe);
            return string.IsNullOrWhiteSpace(pipe) ? null : pipe;
        }

        private JsonObject? CallPipeJson(Func<IntPtr> nativeCall)
        {
            string? pipe = ResolvePipe();
            if (pipe == null)
            {
                return null;
            }

            if (DmApiNative.DM_Connect(pipe, _pipeTimeoutMs) != 0)
            {
                return null;
            }

            try
            {
                return ParseResponseData(nativeCall());
            }
            finally
            {
                DmApiNative.DM_Close();
            }
        }

        private bool CallPipeAccepted(Func<int> nativeCall)
        {
            string? pipe = ResolvePipe();
            if (pipe == null)
            {
                return false;
            }

            if (DmApiNative.DM_Connect(pipe, _pipeTimeoutMs) != 0)
            {
                return false;
            }

            try
            {
                return nativeCall() == 1;
            }
            finally
            {
                DmApiNative.DM_Close();
            }
        }

        private static JsonObject? ParseResponseData(IntPtr ptr)
        {
            string? raw = PtrToOwnedString(ptr);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            try
            {
                var envelope = JsonNode.Parse(raw) as JsonObject;
                return envelope?["data"] as JsonObject;
            }
            catch
            {
                return null;
            }
        }

        private static string? CallStringOut(StringOutCall call, uint bufferSize, uint defaultSize)
        {
            uint size = bufferSize == 0 ? defaultSize : bufferSize;
            var buffer = new StringBuilder((int)size);
            return call(buffer, size) == 0 ? buffer.ToString() : null;
        }

        private static string PtrToStaticString(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return string.Empty;
            }

            return Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
        }

        private static string? PtrToOwnedString(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                return Marshal.PtrToStringAnsi(ptr);
            }
            finally
            {
                DmApiNative.DM_FreeString(ptr);
            }
        }
    }
}
