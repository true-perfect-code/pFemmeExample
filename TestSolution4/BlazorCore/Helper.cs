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
        //public static readonly string PepperApp = "rmodeOWR3//dIEme2aVsxe5/hCw8S+gxELuIhKFzUZG0wsqDh2VPvrBYTdq+hK8c9uvwnEfNEZBMHmsf"; // nicht anpassen !!!
        //public static readonly string PepperAppWasm = "gsmYpeqv5zIsFjy3jzQkCMkKx+ygM5IoVcPLrWfRzOoB5NUcE2HzgBbaKrRQT4Mgywuxr85gZ1GK3ie1"; // nicht anpassen !!!

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




    public static class AotTypeRegistry
    {
        // Wir speichern die TypeInfo-Objekte in einem Dictionary.
        // Der Key ist der Typ (z.B. typeof(TodoModel)), der Value ist das generierte JsonTypeInfo-Objekt.
        private static readonly Dictionary<Type, object> _listMap = new();

        /// <summary>
        /// Registriert die TypeInfo für eine Liste eines Modells.
        /// Wird vom Projekt beim Start aufgerufen.
        /// </summary>
        public static void RegisterList<T>(JsonTypeInfo<List<T?>> typeInfo)
        {
            _listMap[typeof(T)] = typeInfo;
        }

        /// <summary>
        /// Holt die registrierte TypeInfo für den generischen Typ T.
        /// </summary>
        public static JsonTypeInfo<List<T?>> GetListTypeInfo<T>()
        {
            if (_listMap.TryGetValue(typeof(T), out var value))
            {
                return (JsonTypeInfo<List<T?>>)value;
            }

            throw new InvalidOperationException(
                $"AOT TypeInfo für List<{typeof(T).Name}> wurde nicht registriert. " +
                "Bitte rufen Sie AotTypeRegistry.RegisterList in Ihrer Start-Sequenz auf.");
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



    public static class AotUtility
    {
        //public static JsonTypeInfo<List<T?>> GetListTypeInfo<T>() where T : class
        public static JsonTypeInfo<List<T?>> GetListTypeInfo<T>()
        {
            // Verwenden wir object anstelle von IJsonTypeInfo, falls der Typ nicht erkannt wird.
            // Der Typ, den wir tatsächlich speichern, ist JsonTypeInfo<List<ConcreteModel>>.
            object? listTypeInfoObject = null;

            // --- Die zentrale if/else if Kette ---
            if (typeof(T) == typeof(AppParameterModel))
            {
                listTypeInfoObject = JsonContext.Default.ListAppParameterModel;
            }
            else if (typeof(T) == typeof(AuthUsersExtendModel))
            {
                listTypeInfoObject = JsonContext.Default.ListAuthUsersExtendModel;
            }
            else if (typeof(T) == typeof(AuthUsersAuthUsersExtendModel))
            {
                listTypeInfoObject = JsonContext.Default.ListAuthUsersAuthUsersExtendModel;
            }
            else if (typeof(T) == typeof(SharingUsersModel))
            {
                listTypeInfoObject = JsonContext.Default.ListSharingUsersModel;
            }
            //else if (typeof(T) == typeof(TodoModel))
            //{
            //    listTypeInfoObject = JsonContext.Default.ListTodoModel;
            //}
            //else if (typeof(T) == typeof(TasksModel))
            //{
            //    listTypeInfoObject = JsonContext.Default.ListTasksModel;
            //}
            //else if (typeof(T) == typeof(AuthUsersTodoModel))
            //{
            //    listTypeInfoObject = JsonContext.Default.ListAuthUsersTodoModel;
            //}
            else if (typeof(T) == typeof(BlazorCore.Services.GlobalState.TranslationEntryModel))
            {
                // Hier den generierten TypInfo Property Namen verwenden
                listTypeInfoObject = JsonContext.Default.ListTranslationEntryModel;
            }
            else if (typeof(T) == typeof(AuthUsersModel))
            {
                listTypeInfoObject = JsonContext.Default.ListAuthUsersModel;
            }
            // Fügen Sie hier alle weiteren Modelle ein, die über GetRows abgerufen werden!
            // ... 

            if (listTypeInfoObject == null)
            {
                throw new InvalidOperationException(
                    $"AOT TypeInfo for List<{typeof(T).Name}> not found. " +
                    $"Bitte prüfen Sie die Registrierung im JsonContext und in AotUtility."
                );
            }

            // Cast des Object-Platzhalters zum benötigten, spezifisch generischen Typ.
            return (JsonTypeInfo<List<T?>>)listTypeInfoObject;
        }
    }

    // Optionen für alle Modelle in diesem Kontext
    [JsonSourceGenerationOptions(
        //WriteIndented = true,
        WriteIndented = false, // Auf 'false' setzen für Cloud-Transfer (spart Platz)
        PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
        // HINZUGEFÜGT: Übernimmt die globale Option PropertyNameCaseInsensitive = true
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        // Wenn deine Umgebung es zulässt, hier den Encoder definieren:
        // (In manchen AOT-Versionen von .NET 8+ ist dies möglich)
        //JavaScriptEncoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, 
        Converters = new[]
        {
            typeof(SqliteBoolConverter),
            typeof(TolerantInt32Converter),
            typeof(TolerantNullableInt32Converter),
            typeof(TolerantInt64Converter),
            typeof(TolerantNullableInt64Converter),
            typeof(SqliteDateTimeOffsetConverter)
        }
    )]
    //[JsonSerializable(typeof(Services.GlobalState.ConnectionStringParametersModel))]
    [JsonSerializable(typeof(Services.SqlClient.ConnectionStringModel))]
    [JsonSerializable(typeof(AppParameterModel))]
    [JsonSerializable(typeof(List<AppParameterModel>))]
    [JsonSerializable(typeof(SharingInfoModel))]
    [JsonSerializable(typeof(List<SharingInfoModel>))]
    [JsonSerializable(typeof(SharingInfoJsonModel))]
    [JsonSerializable(typeof(List<SharingInfoJsonModel>))]
    //[JsonSerializable(typeof(SearchForCategoryColorModel))]
    [JsonSerializable(typeof(FontSizeModel))]
    [JsonSerializable(typeof(FontWeightModel))]

    // Modelle für IApiBase:
    [JsonSerializable(typeof(UserWebApi))]
    [JsonSerializable(typeof(ClientStorageModel))]
    [JsonSerializable(typeof(ScalarModel))]
    [JsonSerializable(typeof(ReaderDynamicModel))]
    [JsonSerializable(typeof(Dictionary<string, object>))]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    [JsonSerializable(typeof(List<Dictionary<string, string>>))]

    // WebApi => Deserialisierung von generischen Listen:
    //[JsonSerializable(typeof(TodoModel))]
    //[JsonSerializable(typeof(List<TodoModel>))]
    //[JsonSerializable(typeof(TasksModel))]
    //[JsonSerializable(typeof(List<TasksModel>))]
    //[JsonSerializable(typeof(AuthUsersTodoModel))]
    //[JsonSerializable(typeof(List<AuthUsersTodoModel>))]
    [JsonSerializable(typeof(SharingUsersModel))]
    [JsonSerializable(typeof(List<SharingUsersModel>))]
    [JsonSerializable(typeof(AuthUsersExtendModel))]
    [JsonSerializable(typeof(List<AuthUsersExtendModel>))]
    [JsonSerializable(typeof(AuthUsersAuthUsersExtendModel))]
    [JsonSerializable(typeof(List<AuthUsersAuthUsersExtendModel>))]
    [JsonSerializable(typeof(AuthUsersModel))]
    [JsonSerializable(typeof(List<AuthUsersModel>))]
    [JsonSerializable(typeof(AppParameterModel))]
    [JsonSerializable(typeof(List<AppParameterModel>))]

    [JsonSerializable(typeof(BlazorCore.Services.GlobalState.TranslationEntryModel))]
    [JsonSerializable(typeof(List<BlazorCore.Services.GlobalState.TranslationEntryModel>))]

    [JsonSerializable(typeof(PinsModel))]
    [JsonSerializable(typeof(List<PinsModel>))]

    [JsonSerializable(typeof(DownloadAppModel))]
    [JsonSerializable(typeof(List<DownloadAppModel>))]

    public partial class JsonContext : JsonSerializerContext
    {
    }

    /// <summary>
    /// Zentraler Helper für AOT-/Trimming-sichere Reflection.
    /// Dieser sorgt dafür, dass der Trimmer alle Public Properties
    /// und Konstruktoren der verwendeten Modelle erhält.
    /// </summary>
    public static class ReflectionHelper
    {
        /// <summary>
        /// Gibt alle öffentlichen Properties von T zurück, AOT/Trimming-sicher.
        /// </summary>
        // ACHTUNG: Das Attribut DynamicDependency(typeof(T)) wurde entfernt, da es CS0246 verursacht. 
        // Die Sicherheit wird durch das DynamicallyAccessedMembers-Attribut am Typparameter gewährleistet.
        public static PropertyInfo[] GetPropertiesSafe<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        T>()
        {
            return typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        }

        /// <summary>
        /// Gibt die ID-Property zurück, falls vorhanden (AOT-sicher).
        /// </summary>
        // ACHTUNG: Das Attribut DynamicDependency(typeof(T)) wurde entfernt, da es CS0246 verursacht.
        public static PropertyInfo? GetIdPropertySafe<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>()
        {
            // Der Aufruf GetPropertiesSafe<T>() ist hier sauberer, 
            // da er die gesicherte Methode aus der gleichen Klasse nutzt.
            return GetPropertiesSafe<T>()
                .FirstOrDefault(p => p.Name.Equals("ID", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gibt alle öffentlichen Properties für einen übergebenen Typ zurück. 
        /// Diese Überladung wird für Szenarien benötigt, in denen nur ein Type-Objekt verfügbar ist.
        /// </summary>
        [RequiresUnreferencedCode("Diese Methode verwendet Reflection auf einem Type-Objekt, das nicht garantiert getrimmt-sicher ist. Verwenden Sie GetPropertiesSafe<T>() wo immer möglich.")]
        public static PropertyInfo[] GetPropertiesSafe(Type type)
        {
            // Diese Überladung behebt den CS1501-Fehler in GetIdProperty(Type type)
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        }
    }

    /// <summary>
    /// Stellt Hilfsmethoden für JSON-Operationen bereit, die nicht AOT-sicher sind
    /// und Reflection auf generischen Typen erfordern.
    /// </summary>
    public static class JsonUtility
    {
        /// <summary>
        /// Führt eine Deserialisierung für generische Listen durch.
        /// Dies ist NICHT Trimming-sicher, weshalb das RequiresUnreferencedCode-Attribut
        /// verwendet wird, um den Trimmer darauf hinzuweisen.
        /// </summary>
        [RequiresUnreferencedCode("Reflection on generic type T is required for JSON deserialization, which might break trimming. Use source generation for non-generic types.")]
        public static List<T?>? DeserializeListSafe<T>(string json)
        {
            // WICHTIG: Die Options müssen hier beibehalten werden, da JsonContext.Default.* nicht 
            // für generische Typen (List<T>) verwendet werden kann.
            // Der Rest des Codes verwendet nun JsonContext.Default.
            return JsonSerializer.Deserialize<List<T?>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
        }
    }

    public sealed class SqliteBoolConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.True => true,
                JsonTokenType.False => false,

                JsonTokenType.Number =>
                    reader.TryGetInt32(out var i)
                        ? i != 0
                        : reader.GetInt64() != 0,

                JsonTokenType.String =>
                    reader.GetString() == "1"
                    || reader.GetString()?.Equals("true", StringComparison.OrdinalIgnoreCase) == true,

                _ => throw new JsonException($"Invalid boolean value: {reader.TokenType}")
            };
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
            => writer.WriteBooleanValue(value);
    }


    public sealed class TolerantInt32Converter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.Number =>
                    reader.TryGetInt32(out var i)
                        ? i
                        : checked((int)reader.GetInt64()),

                JsonTokenType.String =>
                    int.Parse(reader.GetString()!, System.Globalization.CultureInfo.InvariantCulture),

                _ => throw new JsonException($"Cannot convert {reader.TokenType} to int")
            };
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
            => writer.WriteNumberValue(value);
    }

    public sealed class TolerantNullableInt32Converter : JsonConverter<int?>
    {
        public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // 1. Standard NULL Handling
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            return reader.TokenType switch
            {
                // 2. Number Handling (fängt das Int64/long Problem der Bridge ab)
                JsonTokenType.Number =>
                    reader.TryGetInt32(out var i)
                        ? i
                        : checked((int)reader.GetInt64()),

                // 3. String Handling (fängt "1" oder "" ab)
                JsonTokenType.String =>
                    string.IsNullOrWhiteSpace(reader.GetString())
                        ? null
                        : int.Parse(reader.GetString()!.Trim(), System.Globalization.CultureInfo.InvariantCulture),

                _ => throw new JsonException($"Cannot convert {reader.TokenType} to int?")
            };
        }

        public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteNumberValue(value.Value);
            else
                writer.WriteNullValue();
        }
    }

    public sealed class TolerantInt64Converter : JsonConverter<long>
    {
        public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.Number => reader.GetInt64(),
                JsonTokenType.String => long.Parse(reader.GetString()!, System.Globalization.CultureInfo.InvariantCulture),
                _ => throw new JsonException($"Cannot convert {reader.TokenType} to long")
            };
        }
        public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
            => writer.WriteNumberValue(value);
    }

    public sealed class TolerantNullableInt64Converter : JsonConverter<long?>
    {
        public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            if (reader.TokenType == JsonTokenType.String && string.IsNullOrWhiteSpace(reader.GetString())) return null;

            return reader.TokenType switch
            {
                JsonTokenType.Number => reader.GetInt64(),
                JsonTokenType.String => long.Parse(reader.GetString()!, System.Globalization.CultureInfo.InvariantCulture),
                _ => throw new JsonException($"Cannot convert {reader.TokenType} to long?")
            };
        }
        public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
        {
            if (value.HasValue) writer.WriteNumberValue(value.Value);
            else writer.WriteNullValue();
        }
    }

    public sealed class SqliteDateTimeOffsetConverter : JsonConverter<DateTimeOffset?>
    {
        public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;

            // Fall 1: Echte Zahl (Unix Timestamp)
            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out long unixTime))
            {
                return DateTimeOffset.FromUnixTimeSeconds(unixTime);
            }

            // Fall 2: Zahl als String (wie im Screenshot gesehen)
            if (reader.TokenType == JsonTokenType.String)
            {
                string? value = reader.GetString();
                if (string.IsNullOrWhiteSpace(value)) return null;

                if (long.TryParse(value, out long unixTimeFromString))
                {
                    return DateTimeOffset.FromUnixTimeSeconds(unixTimeFromString);
                }

                // Fallback für ISO-8601 Strings
                if (DateTimeOffset.TryParse(value, out var dto))
                {
                    return dto;
                }
            }

            return null;
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteNumberValue(value.Value.ToUnixTimeSeconds());
            else
                writer.WriteNullValue();
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
