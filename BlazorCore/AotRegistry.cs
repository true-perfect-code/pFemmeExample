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
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace BlazorCore
{
    // ========================================================================
    // JSON CONVERTER
    // ========================================================================

    /// <summary>
    /// JSON converter for SQLite boolean values (0/1 or true/false).
    /// </summary>
    public sealed class SqliteBoolConverter : JsonConverter<bool>
    {
        /// <inheritdoc />
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.True => true,
                JsonTokenType.False => false,
                JsonTokenType.Number => reader.TryGetInt32(out var i) ? i != 0 : reader.GetInt64() != 0,
                JsonTokenType.String => reader.GetString() == "1" || reader.GetString()?.Equals("true", StringComparison.OrdinalIgnoreCase) == true,
                _ => throw new JsonException($"Invalid boolean value: {reader.TokenType}")
            };
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
            => writer.WriteBooleanValue(value);
    }

    /// <summary>
    /// Tolerant JSON converter for Int32 values (handles strings and numbers).
    /// </summary>
    public sealed class TolerantInt32Converter : JsonConverter<int>
    {
        /// <inheritdoc />
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.Number => reader.TryGetInt32(out var i) ? i : checked((int)reader.GetInt64()),
                JsonTokenType.String => int.Parse(reader.GetString()!, System.Globalization.CultureInfo.InvariantCulture),
                _ => throw new JsonException($"Cannot convert {reader.TokenType} to int")
            };
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
            => writer.WriteNumberValue(value);
    }

    /// <summary>
    /// Tolerant JSON converter for nullable Int32 values.
    /// </summary>
    public sealed class TolerantNullableInt32Converter : JsonConverter<int?>
    {
        /// <inheritdoc />
        public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            return reader.TokenType switch
            {
                JsonTokenType.Number => reader.TryGetInt32(out var i) ? i : checked((int)reader.GetInt64()),
                JsonTokenType.String => string.IsNullOrWhiteSpace(reader.GetString()) ? null : int.Parse(reader.GetString()!.Trim(), System.Globalization.CultureInfo.InvariantCulture),
                _ => throw new JsonException($"Cannot convert {reader.TokenType} to int?")
            };
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteNumberValue(value.Value);
            else
                writer.WriteNullValue();
        }
    }

    /// <summary>
    /// Tolerant JSON converter for Int64 values.
    /// </summary>
    public sealed class TolerantInt64Converter : JsonConverter<long>
    {
        /// <inheritdoc />
        public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.Number => reader.GetInt64(),
                JsonTokenType.String => long.Parse(reader.GetString()!, System.Globalization.CultureInfo.InvariantCulture),
                _ => throw new JsonException($"Cannot convert {reader.TokenType} to long")
            };
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
            => writer.WriteNumberValue(value);
    }

    /// <summary>
    /// Tolerant JSON converter for nullable Int64 values.
    /// </summary>
    public sealed class TolerantNullableInt64Converter : JsonConverter<long?>
    {
        /// <inheritdoc />
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

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
        {
            if (value.HasValue) writer.WriteNumberValue(value.Value);
            else writer.WriteNullValue();
        }
    }

    /// <summary>
    /// JSON converter for DateTimeOffset values stored as Unix timestamps.
    /// </summary>
    public sealed class SqliteDateTimeOffsetConverter : JsonConverter<DateTimeOffset?>
    {
        /// <inheritdoc />
        public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;

            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out long unixTime))
            {
                return DateTimeOffset.FromUnixTimeSeconds(unixTime);
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                string? value = reader.GetString();
                if (string.IsNullOrWhiteSpace(value)) return null;

                if (long.TryParse(value, out long unixTimeFromString))
                {
                    return DateTimeOffset.FromUnixTimeSeconds(unixTimeFromString);
                }

                if (DateTimeOffset.TryParse(value, out var dto))
                {
                    return dto;
                }
            }

            return null;
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteNumberValue(value.Value.ToUnixTimeSeconds());
            else
                writer.WriteNullValue();
        }
    }

    // ========================================================================
    // JSON CONTEXT
    // ========================================================================

    /// <summary>
    /// JSON source generation context for AOT/Trimming safety.
    /// </summary>
    [JsonSourceGenerationOptions(
        WriteIndented = false,
        PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
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
    [JsonSerializable(typeof(Services.SqlClient.ConnectionStringModel))]
    [JsonSerializable(typeof(AppParameterModel))]
    [JsonSerializable(typeof(List<AppParameterModel>))]
    //[JsonSerializable(typeof(SharingInfoModel))]
    //[JsonSerializable(typeof(List<SharingInfoModel>))]
    //[JsonSerializable(typeof(SharingInfoJsonModel))]
    //[JsonSerializable(typeof(List<SharingInfoJsonModel>))]
    [JsonSerializable(typeof(FontSizeModel))]
    [JsonSerializable(typeof(FontWeightModel))]
    [JsonSerializable(typeof(UserWebApi))]
    [JsonSerializable(typeof(ClientStorageModel))]
    [JsonSerializable(typeof(ScalarModel))]
    [JsonSerializable(typeof(ReaderDynamicModel))]
    [JsonSerializable(typeof(Dictionary<string, object>))]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    [JsonSerializable(typeof(List<Dictionary<string, string>>))]
    //[JsonSerializable(typeof(SharingUsersModel))]
    //[JsonSerializable(typeof(List<SharingUsersModel>))]
    [JsonSerializable(typeof(AuthUsersExtendModel))]
    [JsonSerializable(typeof(List<AuthUsersExtendModel>))]
    [JsonSerializable(typeof(AuthUsersAuthUsersExtendModel))]
    [JsonSerializable(typeof(List<AuthUsersAuthUsersExtendModel>))]
    [JsonSerializable(typeof(AuthUsersModel))]
    [JsonSerializable(typeof(List<AuthUsersModel>))]
    [JsonSerializable(typeof(TranslationEntryModel))]
    [JsonSerializable(typeof(List<TranslationEntryModel>))]
    [JsonSerializable(typeof(PinsModel))]
    [JsonSerializable(typeof(List<PinsModel>))]
    [JsonSerializable(typeof(DownloadAppModel))]
    [JsonSerializable(typeof(List<DownloadAppModel>))]
    [JsonSerializable(typeof(AppParameterJsonModel))]
    [JsonSerializable(typeof(List<AppParameterJsonModel>))]
    public partial class JsonContext : JsonSerializerContext
    {
    }

    // ========================================================================
    // AOT REGISTRY
    // ========================================================================

    /// <summary>
    /// Central registry for AOT-safe JsonTypeInfo objects.
    /// Customer projects can register their own model types here.
    /// </summary>
    public static class AotTypeRegistry
    {
        private static readonly Dictionary<Type, object> _listMap = new();

        /// <summary>
        /// Registers the JsonTypeInfo for a list of a specific model type.
        /// Called by the framework on startup and by customer projects for their own models.
        /// </summary>
        /// <typeparam name="T">The model type to register</typeparam>
        /// <param name="typeInfo">The JsonTypeInfo for List{T?} from the JsonContext</param>
        /// <exception cref="ArgumentNullException">Thrown when typeInfo is null</exception>
        public static void RegisterList<T>(JsonTypeInfo<List<T?>> typeInfo)
        {
            _listMap[typeof(T)] = typeInfo ?? throw new ArgumentNullException(nameof(typeInfo));
        }

        /// <summary>
        /// Retrieves the registered JsonTypeInfo for the specified generic type T.
        /// </summary>
        /// <typeparam name="T">The model type to retrieve</typeparam>
        /// <returns>The JsonTypeInfo for List{T?}</returns>
        /// <exception cref="InvalidOperationException">Thrown when the type is not registered</exception>
        public static JsonTypeInfo<List<T?>> GetListTypeInfo<T>()
        {
            if (_listMap.TryGetValue(typeof(T), out var value))
            {
                return (JsonTypeInfo<List<T?>>)value;
            }

            throw new InvalidOperationException(
                $"AOT TypeInfo for List<{typeof(T).Name}> has not been registered. " +
                "Please call AotTypeRegistry.RegisterList in your startup sequence.");
        }

        /// <summary>
        /// Checks whether a specific type has been registered.
        /// </summary>
        /// <typeparam name="T">The model type to check</typeparam>
        /// <returns>True if the type is registered, otherwise false</returns>
        public static bool IsRegistered<T>()
        {
            return _listMap.ContainsKey(typeof(T));
        }
    }

    /// <summary>
    /// Central AOT utility for obtaining JsonTypeInfo.
    /// Uses AotTypeRegistry for type resolution.
    /// </summary>
    public static class AotUtility
    {
        /// <summary>
        /// Returns the JsonTypeInfo for List{T}.
        /// First searches framework internal types, then falls back to registered customer types.
        /// </summary>
        /// <typeparam name="T">The model type to retrieve</typeparam>
        /// <returns>The JsonTypeInfo for List{T?}</returns>
        /// <exception cref="InvalidOperationException">Thrown when the type cannot be resolved</exception>
        public static JsonTypeInfo<List<T?>> GetListTypeInfo<T>()
        {
            object? listTypeInfoObject = null;

            // Framework internal types
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
            //else if (typeof(T) == typeof(SharingUsersModel))
            //{
            //    listTypeInfoObject = JsonContext.Default.ListSharingUsersModel;
            //}
            else if (typeof(T) == typeof(TranslationEntryModel))
            {
                listTypeInfoObject = JsonContext.Default.ListTranslationEntryModel;
            }
            else if (typeof(T) == typeof(AuthUsersModel))
            {
                listTypeInfoObject = JsonContext.Default.ListAuthUsersModel;
            }
            else if (typeof(T) == typeof(PinsModel))
            {
                listTypeInfoObject = JsonContext.Default.ListPinsModel;
            }
            else if (typeof(T) == typeof(DownloadAppModel))
            {
                listTypeInfoObject = JsonContext.Default.ListDownloadAppModel;
            }
            else if (typeof(T) == typeof(AppParameterJsonModel))
            {
                listTypeInfoObject = JsonContext.Default.ListAppParameterJsonModel;
            }

            if (listTypeInfoObject != null)
            {
                return (JsonTypeInfo<List<T?>>)listTypeInfoObject;
            }

            // Fallback to customer registered types
            return AotTypeRegistry.GetListTypeInfo<T>();
        }
    }

    /// <summary>
    /// Helper class for AOT/Trimming-safe reflection operations.
    /// </summary>
    public static class ReflectionHelper
    {
        /// <summary>
        /// Returns all public properties of type T in an AOT/Trimming-safe manner.
        /// </summary>
        /// <typeparam name="T">The type to inspect</typeparam>
        /// <returns>Array of public instance properties</returns>
        public static PropertyInfo[] GetPropertiesSafe<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties |
                                        DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        T>()
        {
            return typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        }

        /// <summary>
        /// Returns the ID property of type T if it exists (AOT-safe).
        /// </summary>
        /// <typeparam name="T">The type to inspect</typeparam>
        /// <returns>The ID property, or null if not found</returns>
        public static PropertyInfo? GetIdPropertySafe<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
        T>()
        {
            return GetPropertiesSafe<T>()
                .FirstOrDefault(p => p.Name.Equals("ID", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Returns all public properties for a given Type object.
        /// </summary>
        /// <param name="type">The type to inspect</param>
        /// <returns>Array of public instance properties</returns>
        [RequiresUnreferencedCode("This method uses reflection on a Type object.")]
        public static PropertyInfo[] GetPropertiesSafe(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        }
    }

    /// <summary>
    /// Provides helper methods for JSON operations that may require reflection.
    /// </summary>
    public static class JsonUtility
    {
        /// <summary>
        /// Performs deserialization for generic lists.
        /// This method is not fully trimming-safe and should be used with caution.
        /// </summary>
        /// <typeparam name="T">The target list element type</typeparam>
        /// <param name="json">The JSON string to deserialize</param>
        /// <returns>Deserialized list or null</returns>
        [RequiresUnreferencedCode("Reflection on generic type T is required for JSON deserialization.")]
        public static List<T?>? DeserializeListSafe<T>(string json)
        {
            return JsonSerializer.Deserialize<List<T?>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
        }
    }


    // ========================================================================
    // AOT CONVERTER
    // ========================================================================
    /// <summary>
    /// AOT/Trimming-safe type converter from string to primitive types.
    /// This is a static version of GlobalState.ConvertStrPara without instance dependency.
    /// Used in DbQueryRegistry for converting parameter strings to model properties.
    /// </summary>
    public static class AotConverter
    {
        /// <summary>
        /// Converts a string to a byte array.
        /// Supports Base64, Hex string, and comma-separated bytes.
        /// </summary>
        /// <param name="input">String input to convert</param>
        /// <returns>Byte array or empty array if input is null/empty</returns>
        public static byte[] StringToByteArray(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return Array.Empty<byte>();

            try
            {
                // Base64
                return Convert.FromBase64String(input);
            }
            catch (FormatException)
            {
                // Hex string
                if (input.Length % 2 == 0 && System.Text.RegularExpressions.Regex.IsMatch(input, @"\A\b[0-9a-fA-F]+\b\Z", System.Text.RegularExpressions.RegexOptions.NonBacktracking))
                {
                    int length = input.Length / 2;
                    byte[] bytes = new byte[length];
                    for (int i = 0; i < length; i++)
                        bytes[i] = Convert.ToByte(input.Substring(i * 2, 2), 16);
                    return bytes;
                }

                // Comma-separated bytes
                if (input.Contains(','))
                {
                    return input.Split(',')
                                .Select(s => byte.Parse(s.Trim()))
                                .ToArray();
                }

                return Array.Empty<byte>();
            }
        }

        /// <summary>
        /// Converts a string input to the specified type T.
        /// Supports: string, bool, int, long, double, DateTime, DateTimeOffset, Guid
        /// </summary>
        /// <typeparam name="T">Target type</typeparam>
        /// <param name="input">String input to convert</param>
        /// <returns>Converted value of type T</returns>
        /// <exception cref="InvalidOperationException">Thrown when conversion fails or type is not supported</exception>
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(bool))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(int))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(long))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(double))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DateTime))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DateTimeOffset))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Guid))]
        public static T ConvertTo<T>(string? input)
        {
            Type targetType = typeof(T);
            Type? underlyingType = Nullable.GetUnderlyingType(targetType);
            Type effectiveType = underlyingType ?? targetType;

            // String (base case)
            if (effectiveType == typeof(string))
                return (T)(object)(input ?? string.Empty);

            // Empty string handling
            if (string.IsNullOrWhiteSpace(input))
            {
                return default!;
            }

            // Trim input for better parsing
            string trimmedInput = input!.Trim();

            // Boolean
            if (effectiveType == typeof(bool))
            {
                bool result = trimmedInput.ToLowerInvariant() switch
                {
                    "1" or "true" or "yes" or "on" => true,
                    "0" or "false" or "no" or "off" => false,
                    _ => throw new InvalidOperationException($"AOTConverter: Invalid boolean value '{input}'.")
                };
                return (T)(object)result;
            }

            // Integer (Int32)
            if (effectiveType == typeof(int))
            {
                if (int.TryParse(trimmedInput, out int iValue))
                    return (T)(object)iValue;
                throw new InvalidOperationException($"AOTConverter: Invalid int value '{input}'.");
            }

            // Long (Int64)
            if (effectiveType == typeof(long))
            {
                if (long.TryParse(trimmedInput, out long lValue))
                    return (T)(object)lValue;
                throw new InvalidOperationException($"AOTConverter: Invalid long value '{input}'.");
            }

            // Double
            if (effectiveType == typeof(double))
            {
                if (double.TryParse(trimmedInput, out double dValue))
                    return (T)(object)dValue;
                throw new InvalidOperationException($"AOTConverter: Invalid double value '{input}'.");
            }

            // Decimal
            if (effectiveType == typeof(decimal))
            {
                if (decimal.TryParse(trimmedInput, out decimal decValue))
                    return (T)(object)decValue;
                throw new InvalidOperationException($"AOTConverter: Invalid decimal value '{input}'.");
            }

            // DateTime
            if (effectiveType == typeof(DateTime))
            {
                if (DateTime.TryParse(trimmedInput, out DateTime dtValue))
                    return (T)(object)dtValue;
                throw new InvalidOperationException($"AOTConverter: Invalid DateTime value '{input}'.");
            }

            // DateTimeOffset (supports Unix timestamp or ISO string)
            if (effectiveType == typeof(DateTimeOffset))
            {
                // Try Unix timestamp (seconds since epoch)
                if (long.TryParse(trimmedInput, out long unix))
                    return (T)(object)DateTimeOffset.FromUnixTimeSeconds(unix);

                // Try ISO 8601 / standard DateTimeOffset string
                if (DateTimeOffset.TryParse(trimmedInput, out DateTimeOffset dtoValue))
                    return (T)(object)dtoValue;

                throw new InvalidOperationException($"AOTConverter: Invalid DateTimeOffset value '{input}'. Expected Unix timestamp or ISO string.");
            }

            // Guid
            if (effectiveType == typeof(Guid))
            {
                if (Guid.TryParse(trimmedInput, out Guid guidValue))
                    return (T)(object)guidValue;
                throw new InvalidOperationException($"AOTConverter: Invalid Guid value '{input}'.");
            }

            // Enum (numeric values only for AOT safety)
            if (effectiveType.IsEnum)
            {
                if (long.TryParse(trimmedInput, out long enumValue))
                    return (T)Enum.ToObject(effectiveType, enumValue);
                throw new InvalidOperationException(
                    $"AOTConverter: Invalid enum value for '{effectiveType.Name}'. " +
                    $"Numeric values are required because enum name parsing was trimmed during AOT compilation.");
            }

            throw new InvalidOperationException(
                $"AOTConverter: Type '{typeof(T).Name}' is not supported. " +
                "Supported types: string, bool, int, long, double, decimal, DateTime, DateTimeOffset, Guid, Enum.");
        }

        /// <summary>
        /// Converts a string input to the specified type T with a fallback default value.
        /// </summary>
        /// <typeparam name="T">Target type</typeparam>
        /// <param name="input">String input to convert</param>
        /// <param name="defaultValue">Fallback value if conversion fails</param>
        /// <returns>Converted value or default value</returns>
        public static T ConvertToOrDefault<T>(string? input, T defaultValue = default!)
        {
            try
            {
                return ConvertTo<T>(input);
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Checks if a string can be converted to the specified type T.
        /// </summary>
        /// <typeparam name="T">Target type</typeparam>
        /// <param name="input">String input to check</param>
        /// <returns>True if conversion is possible, otherwise false</returns>
        public static bool CanConvert<T>(string? input)
        {
            try
            {
                ConvertTo<T>(input);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}