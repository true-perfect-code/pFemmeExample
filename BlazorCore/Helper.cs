using BlazorCore.Models;
using BlazorCore.Services.Apis;
using BlazorCore.Services.AppState;
using BlazorCore.Services.Dam;
using BlazorCore.Services.GlobalState;
using BlazorCore.Services.SqlClient;
using p11.UI.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;


namespace BlazorCore
{
    internal class Helper
    {
    }

    public static class ErrorHelper
    {
        public static string GetErrorContext(
            string message,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string filePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            var fileName = System.IO.Path.GetFileName(filePath); // nur Dateiname, nicht ganzer Pfad
            return $"Error: {message}\nMethod: {memberName}\nFile: {fileName}\nLine: {lineNumber}";
        }
    }


    public static class Utilities
    {
        public static string DetectMimeFromBase64(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64)) return "image/jpeg";

            // JPEG: Startet mit /9j/
            if (base64.StartsWith("/9j/")) return "image/jpeg";

            // PNG: Startet mit iVBOR
            if (base64.StartsWith("iVBOR")) return "image/png";

            // GIF: Startet mit R0lG
            if (base64.StartsWith("R0lG")) return "image/gif";

            // WebP: Startet mit UklG
            if (base64.StartsWith("UklG")) return "image/webp";

            return "image/jpeg"; // Sicherer Fallback
        }

        public static string GenerateBackupCode()
        {
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            byte[] bytes = new byte[8];
            rng.GetBytes(bytes);
            return BitConverter.ToString(bytes).Replace("-", "");
        }

        /// <summary>
        /// Generiert ein kryptografisch sicheres Secret (z.B. Pepper)
        /// </summary>
        public static string GenerateSecureSecret(int byteLength = 64)
        {
            byte[] bytes = new byte[byteLength];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
        }
    }

    public static class UnixTsGeneratorWebApi
    {
        //private static readonly DateTime Epoch =
        //    new DateTime(Appl.EpochYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Generiert eine konsistente, 35-stellige UnixTS-ID mit "T"-Präfix für die WebAPI.
        /// </summary>
        public static string Generate(ConfigurationGeneral configurationGeneral)
        {
            DateTime Epoch = new DateTime(configurationGeneral.EpochYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // 1. Zeitstempel (ms seit Epoch)
            long timestamp = (long)(DateTime.UtcNow - Epoch).TotalMilliseconds;

            // 2. Kryptografische Zufallssegmente
            string r9 = GenerateRandomSegment(9);
            string rDevice = GenerateDeviceSegment();
            string rUser = GenerateUserSegment();

            // 3. Zusammensetzen (Länge 34)
            string combined =
                (timestamp.ToString() + r9 + rDevice + rUser).PadLeft(34, '0');

            // 4. Finales Format mit "T" (Länge 35)
            return "T" + combined;
        }

        // ---------- Hilfsmethoden ----------

        /// <summary>
        /// Generiert ein numerisches Zufallssegment mit fixer Stellenanzahl.
        /// </summary>
        private static string GenerateRandomSegment(int digits)
        {
            int max = (int)Math.Pow(10, digits);
            return System.Security.Cryptography.RandomNumberGenerator
                .GetInt32(0, max)
                .ToString($"D{digits}");
        }

        /// <summary>
        /// Web-Device-Ersatz (6-stellig, numerisch).
        /// </summary>
        public static string GenerateDeviceSegment()
        {
            return GenerateRandomSegment(6);
        }

        /// <summary>
        /// Web-User-Ersatz (6-stellig, numerisch).
        /// </summary>
        public static string GenerateUserSegment()
        {
            return GenerateRandomSegment(6);
        }
    }


    public static class ServerConfiguration
    {
        private static bool debugLogServer = false;
        private static string debugLogPathServer = @"C:\inetpub\vhosts\true-perfect-code.ch\logs\debug.log";

        public static Services.SqlClient.ScalarModel GetSecurityConfigurationFile(ConfigurationGeneral configurationGeneral)
        {
            Services.SqlClient.ScalarModel result = new();

            try
            {
                result.out_value_str = $@"C:\inetpub\vhosts\true-perfect-code.ch\_Connections\{configurationGeneral.ApplicationName}.security.config.json";

                if (debugLogServer)
                    File.AppendAllText(debugLogPathServer, "GetSecurityConfigurationFile() called\n");

                string? dir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                if (debugLogServer)
                    File.AppendAllText(debugLogPathServer, $"Assembly Directory: {dir}\n");

                dir = Path.GetDirectoryName(dir);
                if (debugLogServer)
                    File.AppendAllText(debugLogPathServer, $"Parent Directory: {dir}\n");

                if (dir != null)
                {
                    dir = Path.Combine(dir, configurationGeneral.ConnectionsServerFolder);
                    if (debugLogServer)
                        File.AppendAllText(debugLogPathServer, $"Connections Folder: {dir}\n");

                    dir = Path.Combine(dir, configurationGeneral.SecurityConfigJsonFilename);
                    if (debugLogServer)
                    {
                        File.AppendAllText(debugLogPathServer, $"Full Path: {dir}\n");
                        File.AppendAllText(debugLogPathServer, $"File exists: {File.Exists(dir)}\n");
                    }

                    if (File.Exists(dir))
                    {
                        result.out_value_str = dir;
                        if (debugLogServer)
                            File.AppendAllText(debugLogPathServer, "Security config file found\n");
                    }
                    else
                    {
                        if (debugLogServer)
                            File.AppendAllText(debugLogPathServer, "Security config file NOT found\n");
                        result.out_err = "File not found";
                    }
                }
            }
            catch (Exception ex)
            {
                if (debugLogServer)
                    File.AppendAllText(debugLogPathServer, $"Exception: {ex.Message}\n");
                result.out_err = ex.Message;
            }

            if (debugLogServer)
                File.AppendAllText(debugLogPathServer, $"Result: Success={result.out_value_str}, Error={result.out_err}\n");
            return result;
        }
    }


    public static class HostBridge
    {
        public static event Action<bool>? InspectionChanged;

        public static void RaiseInspectionChanged(bool enabled)
        {
            InspectionChanged?.Invoke(enabled);
        }
    }

    public enum MSG_CODES
    {
        no_userotp = 0, // Wenn 2FA aktiviert ist, dann müssen entweder 6-stelliger otp-Usercode oder Backupcode vorhanden sein
        no_user = 1, // Siehe 'SelectOtp>>AuthUsers'
        locked = 2, // Siehe 'SelectOtp>>AuthUsers'
        error_no_otp_empty = 3, // Siehe 'SelectOtp>>AuthUsers'
        error_resetloginattempts = 4, // Siehe 'ResetLoginAttempts>>AuthUsers'
        verifytotp_failed = 5, // Siehe 'bool verifyTotp = VerifyTotp(...)'
        error_selectotp = 6, // Siehe 'SelectOtp>>AuthUsers'
        deleteotp_failed = 7, // Siehe 'DeleteOtp>>AuthUsers'
        empty_email_passwordhash = 8, // Wenn Account und/oder Passwort fehlen
        empty_secret = 9, // Siehe 'case "SaveOtp>>AuthUsers":'

        mssql_result_wrong_format = 10,
        empty_mssql_result = 11,
        empty_json = 12,
        record_exists_no_adding = 13,

        no_feedback_value = 14,
        no_feedback_result = 15,

        no_storeurl_value = 16,
        no_storeurl_result = 17,

        no_case = 18,
        no_connection = 19,
        no_result = 20,
        empty_pollingid = 21,
        no_userid = 22,

        Unknown = 23
    }
}
