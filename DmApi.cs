using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DistroMate
{
    internal static class DmApiEnv
    {
        public const string DmApiPath = "DM_API_PATH";
        public const string DmAppId = "DM_APP_ID";
        public const string DmPublicKey = "DM_PUBLIC_KEY";
        public const string DmLauncherEndpoint = "DM_LAUNCHER_ENDPOINT";
        public const string DmLauncherToken = "DM_LAUNCHER_TOKEN";
    }

    internal static class DmApiErrors
    {
        public static readonly IReadOnlyDictionary<uint, string> ActivationErrorNames =
            new Dictionary<uint, string>
            {
                [0] = "DM_ERR_OK",
                [1] = "DM_ERR_FAIL",
                [2] = "DM_ERR_INVALID_PARAMETER",
                [3] = "DM_ERR_APPID_NOT_SET",
                [4] = "DM_ERR_LICENSE_KEY_NOT_SET",
                [5] = "DM_ERR_NOT_ACTIVATED",
                [6] = "DM_ERR_LICENSE_EXPIRED",
                [7] = "DM_ERR_NETWORK",
                [8] = "DM_ERR_FILE_IO",
                [9] = "DM_ERR_SIGNATURE",
                [10] = "DM_ERR_BUFFER_TOO_SMALL",
            };
    }

    internal static class DmApiNative
    {
        private const string DllName = "dm_api.dll";

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void LicenseCallback();

        public static void EnsureLoaded(string? explicitPath = null)
        {
            string? path = explicitPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                path = Environment.GetEnvironmentVariable(DmApiEnv.DmApiPath);
            }

            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                _ = NativeLibrary.Load(path);
            }
        }

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr DM_GetVersion();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int DM_RestartAppIfNecessary();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr DM_GetLastError();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DM_FreeString(IntPtr ptr);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr DM_JsonToCanonical([MarshalAs(UnmanagedType.LPStr)] string jsonStr);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr DM_CheckForUpdates([MarshalAs(UnmanagedType.LPStr)] string? optionsJson);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr DM_DownloadUpdate([MarshalAs(UnmanagedType.LPStr)] string? optionsJson);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr DM_CancelUpdateDownload([MarshalAs(UnmanagedType.LPStr)] string? optionsJson);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr DM_GetUpdateState();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr DM_GetPostUpdateInfo();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr DM_AckPostUpdateInfo([MarshalAs(UnmanagedType.LPStr)] string? optionsJson);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr DM_WaitForUpdateStateChange(ulong lastSequence, uint timeoutMs);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int DM_QuitAndInstall([MarshalAs(UnmanagedType.LPStr)] string? optionsJson);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetProductData([MarshalAs(UnmanagedType.LPStr)] string productData);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetProductId([MarshalAs(UnmanagedType.LPStr)] string productId);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetDataDirectory([MarshalAs(UnmanagedType.LPStr)] string directoryPath);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetDebugMode(uint enable);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetCustomDeviceFingerprint([MarshalAs(UnmanagedType.LPStr)] string fingerprint);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetLicenseKey([MarshalAs(UnmanagedType.LPStr)] string licenseKey);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetLicenseCallback(LicenseCallback callback);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ActivateLicense();

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
        public static extern int GetLicenseKey([Out] StringBuilder licenseKey, uint length);

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
        public static extern int GetActivationId([Out] StringBuilder id, uint length);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetLibraryVersion();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Reset();
    }

    public sealed class DmApi : IDisposable
    {
        private const uint DefaultBufferSize = 256;
        private const uint DefaultModeBufferSize = 64;
        private const string DevLicenseErrorText =
            "Development license is missing or corrupted. Run `distromate sdk renew` to regenerate the dev certificate.";

        private DmApiNative.LicenseCallback? _licenseCallbackRef;

        private delegate int StringOutCall(StringBuilder buffer, uint size);

        public DmApi(string? dllPath = null)
        {
            DmApiNative.EnsureLoaded(dllPath);
        }

        public static bool ShouldSkipCheck(string? appId = null, string? publicKey = null)
        {
            string? endpoint = Environment.GetEnvironmentVariable(DmApiEnv.DmLauncherEndpoint);
            string? token = Environment.GetEnvironmentVariable(DmApiEnv.DmLauncherToken);
            if (!string.IsNullOrWhiteSpace(endpoint) && !string.IsNullOrWhiteSpace(token))
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
            catch (Exception ex)
            {
                throw new InvalidOperationException(DevLicenseErrorText, ex);
            }

            if (string.IsNullOrWhiteSpace(devPublicKey) ||
                !string.Equals(devPublicKey, resolvedPublicKey, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(DevLicenseErrorText);
            }

            return true;
        }

        public string GetVersion()
        {
            return PtrToStaticString(DmApiNative.DM_GetVersion());
        }

        public bool RestartAppIfNecessary()
        {
            return DmApiNative.DM_RestartAppIfNecessary() != 0;
        }

        public string? GetLastError()
        {
            return PtrToOwnedString(DmApiNative.DM_GetLastError());
        }

        public string? GetActivationErrorName(uint? code)
        {
            if (code is null)
            {
                return null;
            }

            if (DmApiErrors.ActivationErrorNames.TryGetValue(code.Value, out string? name))
            {
                return name;
            }

            return $"UNKNOWN({code.Value})";
        }

        public bool SetProductData(string productData)
        {
            return DmApiNative.SetProductData(productData) == 0;
        }

        public bool SetProductId(string productId)
        {
            return DmApiNative.SetProductId(productId) == 0;
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

        public bool SetLicenseCallback(Action callback)
        {
            if (callback is null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            DmApiNative.LicenseCallback native = () => callback();
            if (DmApiNative.SetLicenseCallback(native) != 0)
            {
                return false;
            }

            _licenseCallbackRef = native;
            return true;
        }

        public bool ActivateLicense()
        {
            return DmApiNative.ActivateLicense() == 0;
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

        public string GetLibraryVersion()
        {
            return PtrToStaticString(DmApiNative.GetLibraryVersion());
        }

        public bool Reset()
        {
            return DmApiNative.Reset() == 0;
        }

        public JsonObject? CheckForUpdates(JsonObject? options = null)
        {
            return CallEnvelope(() => DmApiNative.DM_CheckForUpdates(EncodeOptions(options)));
        }

        public JsonObject? DownloadUpdate(JsonObject? options = null)
        {
            return CallEnvelope(() => DmApiNative.DM_DownloadUpdate(EncodeOptions(options)));
        }

        public JsonObject? CancelUpdateDownload(JsonObject? options = null)
        {
            return CallEnvelope(() => DmApiNative.DM_CancelUpdateDownload(EncodeOptions(options)));
        }

        public JsonObject? GetUpdateState()
        {
            return CallEnvelope(DmApiNative.DM_GetUpdateState);
        }

        public JsonObject? GetPostUpdateInfo()
        {
            return CallEnvelope(DmApiNative.DM_GetPostUpdateInfo);
        }

        public JsonObject? AckPostUpdateInfo(JsonObject? options = null)
        {
            return CallEnvelope(() => DmApiNative.DM_AckPostUpdateInfo(EncodeOptions(options)));
        }

        public JsonObject? WaitForUpdateStateChange(ulong lastSequence, uint timeoutMs = 30000)
        {
            return CallEnvelope(() => DmApiNative.DM_WaitForUpdateStateChange(lastSequence, timeoutMs));
        }

        public int QuitAndInstall(JsonObject? options = null)
        {
            return DmApiNative.DM_QuitAndInstall(EncodeOptions(options));
        }

        public string? JsonToCanonical(string jsonStr)
        {
            return PtrToOwnedString(DmApiNative.DM_JsonToCanonical(jsonStr));
        }

        public static string? JsonToCanonical(string jsonStr, string? dllPath)
        {
            DmApiNative.EnsureLoaded(dllPath);
            return PtrToOwnedString(DmApiNative.DM_JsonToCanonical(jsonStr));
        }

        public void Dispose()
        {
            GC.KeepAlive(_licenseCallbackRef);
        }

        private static JsonObject? CallEnvelope(Func<IntPtr> nativeCall)
        {
            string? raw = PtrToOwnedString(nativeCall());
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            try
            {
                return JsonNode.Parse(raw) as JsonObject;
            }
            catch
            {
                return null;
            }
        }

        private static string? EncodeOptions(JsonObject? options)
        {
            if (options is null)
            {
                return null;
            }

            return options.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = false,
            });
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
