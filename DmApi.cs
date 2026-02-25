/**
 * DM API - DistroMate License Verification SDK (C# Binding)
 *
 * Provides an integration interface for launched programs to communicate
 * with the launcher via Windows Named Pipe.
 *
 * Example (轻度混淆，基础用法 - 使用 VerifyAndActivate 一键完成验证和激活):
 *   using DistroMate;
 *
 *   using var api = new DmApi();
 *
 *   // 是否由启动器启动，否则退出进程
 *   if (DmApi.RestartAppIfNecessary()) {
 *       return;
 *   }
 *
 *   var result = api.VerifyAndActivate();
 *   if (result.Success) {
 *       Console.WriteLine("许可证验证成功");
 *       // 继续执行应用逻辑...
 *   } else {
 *       Console.WriteLine($"许可证验证失败: {result.Error}");
 *       return;
 *   }
 *
 *   // 通知启动器我已初始化成功准备显示窗口
 *   api.Initiated();
 *
 *   app.Show();
 *
 * Example (中度/重度混淆，手动控制流程):
 *   using DistroMate;
 *
 *   using var api = new DmApi();
 *   var pipe = Environment.GetEnvironmentVariable("DM_PIPE");
 *
 *   // 是否由启动器启动，否则退出进程
 *   if (DmApi.RestartAppIfNecessary()) {
 *       return;
 *   }
 *
 *   // 连接到许可证服务
 *   api.Connect(pipe);
 *
 *   // 验证许可证
 *   var data = api.Verify();
 *   if (data != null && data["verification"]?["valid"]?.GetValue<bool>() == true) {
 *       Console.WriteLine("许可证有效");
 *   } else {
 *       // 需要激活
 *       var activation = api.Activate();
 *       if (activation != null) {
 *           Console.WriteLine("激活成功");
 *       }
 *   }
 *
 *   // 通知启动器我已初始化成功准备显示窗口
 *   api.Initiated();
 *
 *   app.Show();
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DistroMate
{
    /// <summary>
    /// Hardcoded public key for signature verification
    /// </summary>
    internal static class DmApiKeys
    {
        public const string PublicKey = @"{{PUBKEY}}";
    }

    /// <summary>
    /// DM API native methods
    /// </summary>
    internal static class DmApiNative
    {
        private const string DllName = "dm_api.dll";

        static DmApiNative()
        {
            var envPath = Environment.GetEnvironmentVariable("DM_API_PATH");
            if (!string.IsNullOrEmpty(envPath) && System.IO.File.Exists(envPath))
            {
                try
                {
                    NativeLibrary.Load(envPath);
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
        public static extern int DM_IsConnected();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr DM_Verify(
            [MarshalAs(UnmanagedType.LPStr)] string jsonData);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr DM_Activate(
            [MarshalAs(UnmanagedType.LPStr)] string jsonData);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int DM_Initiated();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr DM_GetVersion();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int DM_RestartAppIfNecessary();

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
        public static extern IntPtr DM_GetLastError();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DM_FreeString(IntPtr ptr);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr DM_JsonToCanonical(
            [MarshalAs(UnmanagedType.LPStr)] string jsonStr);
    }

    /// <summary>
    /// Result of VerifyAndActivate operation
    /// </summary>
    public class VerifyAndActivateResult
    {
        /// <summary>
        /// Whether the operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if the operation failed
        /// </summary>
        public string? Error { get; set; }
    }

    /// <summary>
    /// Exception thrown when DM API operation fails
    /// </summary>
    public class DmApiException : Exception
    {
        public int ErrorCode { get; }

        public DmApiException(string message, int errorCode = 0)
            : base(message)
        {
            ErrorCode = errorCode;
        }
    }

    /// <summary>
    /// DM API wrapper class
    /// </summary>
    public class DmApi : IDisposable
    {
        private bool _disposed = false;
        private readonly RSA _rsa;

        /// <summary>
        /// Default connection timeout in milliseconds
        /// </summary>
        public const uint DefaultTimeoutMs = 5000;

        /// <summary>
        /// Create a new DM API instance with hardcoded RSA public key for signature verification
        /// </summary>
        /// <exception cref="InvalidOperationException">If public key is invalid</exception>
        public DmApi()
        {
            _rsa = RSA.Create();
            try
            {
                _rsa.ImportFromPem(DmApiKeys.PublicKey.AsSpan());
            }
            catch (Exception ex)
            {
                _rsa.Dispose();
                throw new InvalidOperationException("Invalid public key: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Check if the program was launched by the launcher, if not, restart via launcher.
        /// This should be called at the entry point of your program.
        /// </summary>
        /// <returns>true if launcher was started and current process should exit</returns>
        /// <exception cref="DmApiException">If failed to find or start launcher</exception>
        public static bool RestartAppIfNecessary()
        {
            int result = DmApiNative.DM_RestartAppIfNecessary();
            switch (result)
            {
                case 0:
                    return false;  // Launched by launcher
                case 1:
                    return true;   // Launcher started, should exit
                default:
                    throw new DmApiException(GetLastError() ?? "Failed to restart via launcher", result);
            }
        }

        /// <summary>
        /// Get the DLL version
        /// </summary>
        public static string Version
        {
            get
            {
                IntPtr ptr = DmApiNative.DM_GetVersion();
                return Marshal.PtrToStringAnsi(ptr) ?? "";
            }
        }

        /// <summary>
        /// Get the last error message
        /// </summary>
        public static string? GetLastError()
        {
            IntPtr ptr = DmApiNative.DM_GetLastError();
            if (ptr == IntPtr.Zero)
                return null;

            string? result = Marshal.PtrToStringAnsi(ptr);
            DmApiNative.DM_FreeString(ptr);
            return result;
        }

        /// <summary>
        /// Connect to the launcher
        /// </summary>
        /// <param name="pipeName">Pipe name, format: \\.\pipe\{name}</param>
        /// <param name="timeoutMs">Timeout in milliseconds, 0 for default</param>
        /// <exception cref="DmApiException">If connection fails</exception>
        public void Connect(string pipeName, uint timeoutMs = DefaultTimeoutMs)
        {
            int result = DmApiNative.DM_Connect(pipeName, timeoutMs);
            if (result != 0)
            {
                throw new DmApiException(GetLastError() ?? "Connection failed", result);
            }
        }

        /// <summary>
        /// Close the connection
        /// </summary>
        public void Close()
        {
            DmApiNative.DM_Close();
        }

        /// <summary>
        /// Check if connected to the launcher
        /// </summary>
        public bool IsConnected => DmApiNative.DM_IsConnected() == 1;

        /// <summary>
        /// Verify license
        /// Automatically generates nonce and verifies response signature
        /// </summary>
        /// <returns>Verified response data, or null on failure</returns>
        public JsonObject? Verify()
        {
            string nonce = GenerateNonce();
            string request = JsonSerializer.Serialize(new { nonce_str = nonce });

            IntPtr ptr = DmApiNative.DM_Verify(request);
            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            string? respStr = Marshal.PtrToStringAnsi(ptr);
            DmApiNative.DM_FreeString(ptr);

            if (string.IsNullOrEmpty(respStr))
            {
                return null;
            }

            try
            {
                var resp = JsonNode.Parse(respStr);
                if (resp == null)
                {
                    return null;
                }

                var data = resp["data"]?.AsObject();
                if (data == null)
                {
                    return null;
                }

                // Check success field
                var success = data["success"]?.GetValue<bool>();
                if (success != true)
                {
                    return null;
                }

                // New format: data.verification contains signed data from server
                var verification = data["verification"]?.AsObject();
                if (verification == null)
                {
                    return null;
                }

                // Verify signature (required)
                if (!CheckSignature(verification, nonce))
                {
                    return null;
                }

                // Build result object (nested format)
                var result = new JsonObject();
                result["success"] = true;
                if (data.ContainsKey("is_online"))
                {
                    result["is_online"] = data["is_online"]?.DeepClone();
                }
                result["verification"] = verification.DeepClone();

                return result;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Activate license
        /// Automatically generates nonce and verifies response signature
        /// </summary>
        /// <returns>Verified response data, or null on failure</returns>
        public JsonObject? Activate()
        {
            string nonce = GenerateNonce();
            string request = JsonSerializer.Serialize(new { nonce_str = nonce });

            IntPtr ptr = DmApiNative.DM_Activate(request);
            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            string? respStr = Marshal.PtrToStringAnsi(ptr);
            DmApiNative.DM_FreeString(ptr);

            if (string.IsNullOrEmpty(respStr))
            {
                return null;
            }

            try
            {
                var resp = JsonNode.Parse(respStr);
                if (resp == null)
                {
                    return null;
                }

                var data = resp["data"]?.AsObject();
                if (data == null)
                {
                    return null;
                }

                // Check success field
                var success = data["success"]?.GetValue<bool>();
                if (success != true)
                {
                    return null;
                }

                // New format: data.activation contains signed data from server
                var activation = data["activation"]?.AsObject();
                if (activation == null || !CheckSignature(activation, nonce))
                {
                    return null;
                }

                // Build result object (nested format)
                var result = new JsonObject();
                result["success"] = true;
                result["activation"] = activation.DeepClone();

                return result;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Notify launcher that initialization is complete
        /// </summary>
        /// <exception cref="DmApiException">If notification fails</exception>
        public void Initiated()
        {
            int result = DmApiNative.DM_Initiated();
            if (result != 0)
            {
                throw new DmApiException(GetLastError() ?? "Initiated notification failed", result);
            }
        }

        /// <summary>
        /// Electron-compatible: checkForUpdates
        /// </summary>
        public JsonObject? CheckForUpdates(JsonObject? options = null)
        {
            string req = JsonSerializer.Serialize(options ?? new JsonObject());
            IntPtr ptr = DmApiNative.DM_CheckForUpdates(req);
            return ExtractDataObject(ParseJsonResponsePointer(ptr));
        }

        /// <summary>
        /// Alias of CheckForUpdates
        /// </summary>
        public JsonObject? CheckForUpdate(JsonObject? options = null)
        {
            return CheckForUpdates(options);
        }

        /// <summary>
        /// Electron-compatible: downloadUpdate
        /// </summary>
        public JsonObject? DownloadUpdate(JsonObject? options = null)
        {
            string req = JsonSerializer.Serialize(options ?? new JsonObject());
            IntPtr ptr = DmApiNative.DM_DownloadUpdate(req);
            return ExtractDataObject(ParseJsonResponsePointer(ptr));
        }

        /// <summary>
        /// Get current update state snapshot
        /// </summary>
        public JsonObject? GetUpdateState()
        {
            IntPtr ptr = DmApiNative.DM_GetUpdateState();
            return ExtractDataObject(ParseJsonResponsePointer(ptr));
        }

        /// <summary>
        /// Wait for update state changes without native callbacks
        /// </summary>
        public JsonObject? WaitForUpdateStateChange(ulong lastSequence, uint timeoutMs = 30000)
        {
            IntPtr ptr = DmApiNative.DM_WaitForUpdateStateChange(lastSequence, timeoutMs);
            return ExtractDataObject(ParseJsonResponsePointer(ptr));
        }

        /// <summary>
        /// Electron-compatible: quitAndInstall
        /// Returns true when launcher accepts the request.
        /// </summary>
        public bool QuitAndInstall(JsonObject? options = null)
        {
            string req = JsonSerializer.Serialize(options ?? new JsonObject());
            int ret = DmApiNative.DM_QuitAndInstall(req);
            return ret == 1;
        }

        /// <summary>
        /// Connect to pipe, verify license, and activate in a loop until successful
        /// </summary>
        /// <param name="timeout">Connection timeout in milliseconds (default: 5000)</param>
        /// <returns>Result object containing success status and optional error message</returns>
        public VerifyAndActivateResult VerifyAndActivate(uint timeout = DefaultTimeoutMs)
        {
            var pipe = Environment.GetEnvironmentVariable("DM_PIPE");
            if (string.IsNullOrEmpty(pipe))
            {
                return new VerifyAndActivateResult
                {
                    Success = false,
                    Error = "DM_PIPE environment variable not set"
                };
            }

            try
            {
                Connect(pipe, timeout);
            }
            catch
            {
                return new VerifyAndActivateResult
                {
                    Success = false,
                    Error = "Failed to connect to license service"
                };
            }

            var verifyResult = Verify();
            if (verifyResult != null)
            {
                var verification = verifyResult["verification"]?.AsObject();
                if (verification != null && verification["valid"]?.GetValue<bool>() == true)
                {
                    return new VerifyAndActivateResult { Success = true };
                }
            }

            while (true)
            {
                var activateResult = Activate();
                if (activateResult != null && activateResult["activation"] != null)
                {
                    return new VerifyAndActivateResult { Success = true };
                }
            }
        }

        /// <summary>
        /// Dispose the API instance
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _rsa.Dispose();
                }
                Close();
                _disposed = true;
            }
        }

        ~DmApi()
        {
            Dispose(false);
        }

        /// <summary>
        /// Generate random hex nonce (32 characters = 16 bytes)
        /// </summary>
        private static string GenerateNonce()
        {
            byte[] bytes = new byte[16];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static JsonObject? ParseJsonResponsePointer(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            string? respStr = Marshal.PtrToStringAnsi(ptr);
            DmApiNative.DM_FreeString(ptr);

            if (string.IsNullOrEmpty(respStr))
            {
                return null;
            }

            try
            {
                return JsonNode.Parse(respStr)?.AsObject();
            }
            catch
            {
                return null;
            }
        }

        private static JsonObject? ExtractDataObject(JsonObject? envelope)
        {
            if (envelope == null)
            {
                return null;
            }

            if (envelope["data"] is JsonObject data)
            {
                return data;
            }

            return null;
        }



        /// <summary>
        /// Verify RSA signature
        /// </summary>
        private bool CheckSignature(JsonObject data, string nonce)
        {
            try
            {
                var signatureNode = data["signature"];
                if (signatureNode == null)
                {
                    return false;
                }

                string? signatureB64 = signatureNode.GetValue<string>();
                if (string.IsNullOrEmpty(signatureB64))
                {
                    return false;
                }

                byte[] signature = Convert.FromBase64String(signatureB64);

                // Build payload (exclude signature, add nonce_str)
                var payload = new JsonObject();
                foreach (var kvp in data)
                {
                    if (kvp.Key != "signature")
                    {
                        payload[kvp.Key] = kvp.Value?.DeepClone();
                    }
                }
                payload["nonce_str"] = JsonValue.Create(nonce);

                // Serialize to JSON first
                string jsonStr = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    WriteIndented = false
                });

                // Use DM_JsonToCanonical to ensure consistency with Go version
                string? canonical = JsonToCanonical(jsonStr);
                if (canonical == null)
                {
                    return false;
                }

                byte[] dataBytes = Encoding.UTF8.GetBytes(canonical);

                // Verify signature
                return _rsa.VerifyData(dataBytes, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Serialize to canonical JSON (sorted keys, compact, no whitespace)
        /// </summary>
        private static string SerializeCanonical(SortedDictionary<string, JsonNode?> dict)
        {
            var jsonObject = new JsonObject();
            foreach (var kvp in dict)
            {
                jsonObject[kvp.Key] = kvp.Value?.DeepClone();
            }

            return JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }

        /// <summary>
        /// Convert JSON string to canonical format (sorted keys)
        /// This ensures consistency without performing any hashing or verification.
        /// </summary>
        /// <param name="jsonStr">JSON formatted string</param>
        /// <returns>Canonical JSON string, or null on failure</returns>
        public static string? JsonToCanonical(string jsonStr)
        {
            IntPtr ptr = DmApiNative.DM_JsonToCanonical(jsonStr);
            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            string? result = Marshal.PtrToStringAnsi(ptr);
            DmApiNative.DM_FreeString(ptr);
            return result;
        }
    }
}
