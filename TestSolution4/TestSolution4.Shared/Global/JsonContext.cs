using BlazorCore.Models;
using BlazorCore.Services.Apis;
using BlazorCore.Services.AppState;
using BlazorCore.Services.Dam;
using BlazorCore.Services.GlobalState;
using BlazorCore.Services.SqlClient;
using p11.UI.Models;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace TestSolution4.Shared.Global
{
    //internal class JsonContext
    //{
    //}



    /// <summary>
    /// Diese Klasse übernimmt die Registrierung beim Framework-Start
    /// </summary>
    public static class ProjectTypeInitializer
    {
        public static void Initialize()
        {
            // Wir "füttern" die Registry im Framework mit unseren Projekt-TypInfos
            //AotTypeRegistry.RegisterList<TodoModel>(JsonContextProject.Default.ListTodoModel);
            //AotTypeRegistry.RegisterList<TasksModel>(JsonContextProject.Default.ListTasksModel);
            //AotTypeRegistry.RegisterList<AuthUsersModel>(JsonContextProject.Default.ListAuthUsersModel);
            //AotTypeRegistry.RegisterList<AuthUsersExtendModel>(JsonContextProject.Default.ListAuthUsersExtendModel);
            //AotTypeRegistry.RegisterList<AuthUsersAuthUsersExtendModel>(JsonContextProject.Default.ListAuthUsersAuthUsersExtendModel);
            //AotTypeRegistry.RegisterList<AuthUsersTodoModel>(JsonContextProject.Default.ListAuthUsersTodoModel);
            //AotTypeRegistry.RegisterList<SharingUsersModel>(JsonContextProject.Default.ListSharingUsersModel);
            //AotTypeRegistry.RegisterList<PinsModel>(JsonContextProject.Default.ListPinsModel);
            //AotTypeRegistry.RegisterList<DownloadAppModel>(JsonContextProject.Default.ListDownloadAppModel);

            // Falls du TranslationEntryModel auch im Projekt hast:
            // AotTypeRegistry.RegisterList<TranslationEntryModel>(JsonContextProject.Default.ListTranslationEntryModel);
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
    //[JsonSerializable(typeof(Services.SqlClient.ConnectionStringModel))]
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

    // Siehe [JsonConverter(typeof(SqliteImageConverter))] bei einzelnen Modelen wo ein Bild definiert ist
    //public sealed class SqliteImageConverter : JsonConverter<string?>
    //{
    //    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    //    {
    //        // 1. Wenn es bereits ein String ist (z.B. von der Cloud Web-API)
    //        if (reader.TokenType == JsonTokenType.String)
    //        {
    //            return reader.GetString();
    //        }

    //        // 2. Wenn es ein Byte-Array (BLOB) aus der SQLite-Bridge ist
    //        if (reader.TokenType == JsonTokenType.StartArray)
    //        {
    //            try
    //            {
    //                // Liest die Bytes aus dem JSON-Array
    //                byte[] bytes = JsonSerializer.Deserialize<byte[]>(ref reader, options) ?? Array.Empty<byte>();
    //                if (bytes.Length > 0)
    //                {
    //                    // Automatische Umwandlung in Data-URL für die UI
    //                    return $"data:image/jpeg;base64,{Convert.ToBase64String(bytes)}";
    //                }
    //            }
    //            catch
    //            {
    //                return null;
    //            }
    //        }

    //        if (reader.TokenType == JsonTokenType.Null) return null;

    //        return null;
    //    }

    //    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    //    {
    //        // Beim Schreiben (z.B. Richtung API) senden wir einfach den String zurück
    //        if (value == null) writer.WriteNullValue();
    //        else writer.WriteStringValue(value);
    //    }
    //}


}
