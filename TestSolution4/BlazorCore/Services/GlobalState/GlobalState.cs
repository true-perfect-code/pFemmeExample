#pragma warning disable CA1416 // Disables CA1416 for the Encrypt call
using BlazorCore.Models;
using BlazorCore.Services.AppState;
using BlazorCore.Services.Platform;
using BlazorCore.Services.SqlClient;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BlazorCore.Services.GlobalState
{
    public class GlobalStateBase : IGlobalStateBase
    {
        private readonly IServiceProvider _serviceProvider;

        public ConfigurationGeneral ConfigGeneral { get; private set; } = new();
        public ConfigurationWebapi ConfigWebapi { get; private set; } = new();

        public Sections Catalog { get; private set; } = new();
        public Dictionary<string, Dictionary<string, string>> LanguageMaps { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Translations Translations { get; private set; } = new();
        public string ConnectionString { get; private set; } = string.Empty;
        public List<string> SharingRequests { get; set; } = new();

        private bool _isInitialized;


        public GlobalStateBase(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void GlobalInit(ConfigurationGeneral configGeneral, ConfigurationWebapi configWebapi, Sections? catalog)
        {
            ConfigGeneral = configGeneral ?? throw new ArgumentNullException(nameof(configGeneral));
            ConfigWebapi = configWebapi ?? throw new ArgumentNullException(nameof(configWebapi));
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }


        public async Task EnsureInitializedAsync()
        {
            //Console.WriteLine("[Blazor GlobalStat EnsureInitializedAsync] START");

            if (_isInitialized) return;
            await Task.Delay(20); // Simulate async delay for initialization

            // Lazy laden
            using var scope = _serviceProvider.CreateScope();
            var platform = scope.ServiceProvider.GetRequiredService<IPlatformBase>();

            var formFactor = await platform.GetFormFactor();
            bool isWeb = formFactor.Equals("web", StringComparison.OrdinalIgnoreCase);

            // 1. Load the connection string (only if using a web platform). The devices run via web API, so they do not communicate directly with MSSQL.
            if (isWeb)
            {
                Scalar result = GetConnectionString();
                if (result.Success)
                    ConnectionString = result.ValString ?? string.Empty;
                else
                    throw new Exception($"Failed to load connection string: {result.Error}");
            }

            // 2. Load XML language table (translations)
            //Console.WriteLine("[Blazor GlobalStat EnsureInitializedAsync] SetTranslations()");
            await SetTranslations();

            _isInitialized = true;

            //Console.WriteLine("[Blazor GlobalStat EnsureInitializedAsync] END");
        }

        public Scalar GetConnectionString()
        {
            Scalar result = new();
            try
            {
                // Lazy laden
                using var scope = _serviceProvider.CreateScope();
                var platform = scope.ServiceProvider.GetRequiredService<IPlatformBase>();

                string basedir = platform.GetBaseDirectory();
                if (!string.IsNullOrEmpty(basedir))
                {
                    DirectoryInfo? parentbasedir = Directory.GetParent(basedir);
                    if (parentbasedir != null && parentbasedir.Parent != null)
                    {
                        string connectionstringfolder = Path.Combine(parentbasedir.Parent.FullName, ConfigGeneral.ConnectionsServerFolder);
                        string connectionstringpath = Path.Combine(connectionstringfolder, $"{ConfigGeneral.ApplicationName}{ConfigGeneral.FileExtensionJson}");
                        if (File.Exists(connectionstringpath))
                        {
                            var connectionString = System.Text.Json.JsonSerializer.Deserialize(
                                File.ReadAllText(connectionstringpath),
                                JsonContext.Default.ConnectionStringModel // <-- Typ-Resolver
                            )!;

                            if (connectionString != null)
                            {

                                using (BlazorCore.Services.ServerShared.Security aes = new(ConfigGeneral.ApplicationName, ConfigGeneral.TableSchema))
                                {
                                    // On first launch, the key should be generated first and then saved to the JSON file (perfect[PROJECT].json)
                                    // Uncomment the next line and restart the app:
                                    // string key = aes.Encrypt([PASSWORD FROM YOUR HOSTING e.g. dbs.md FIRESTORMCLOUD]);
                                    // Then save the determined ‘key’ to the JSON file [YOUR_PROJECT_NAME].json under ‘“Password”:’.
                                    // Comment out the line ‘string key = aes.Encrypt(...’ again and restart the app. Check whether the password is decrypted.
                                    connectionString.Password = aes.Decrypt(connectionString.Password!);
                                }

                                result = GenerateConnectionString(connectionString);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Error = "pEngine error: GetConnectionString() => " + ex.Message;
            }
            return result;
        }

        public async Task SetTranslations(
            List<(System.Reflection.Assembly Assembly, string ResourceName)>? additionalSources = null)
        {
            //Console.WriteLine("[Blazor GlobalStat SetTranslations] START");

            // 1. Wenn Translations noch NICHT initialisiert ist → vorbereiten
            if (Translations == null)
                Translations = new Translations();

            // 2. Dictionary-Struktur nur einmal initialisieren
            if (Translations.LanguageMaps.Count == 0)
            {
                foreach (var langCode in ConfigGeneral.AllSupportedLanguageCodes.Split(','))
                {
                    Translations.LanguageMaps[langCode] =
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
            }

            // 3 (BlazorCore.json) laden
            //Console.WriteLine($"[Blazor GlobalStat SetTranslations] additionalSources (Count, Array): {additionalSources?.Count} , {additionalSources?.ToArray()}");
            if (additionalSources == null)
            {
                var frameworkAssembly = typeof(BlazorCore.Utilities).Assembly;
                var frameworkResourceName = "BlazorCore.wwwroot.languages.BlazorCore.json";

                //Console.WriteLine($"[Blazor GlobalStat SetTranslations] additionalSources == null: {frameworkResourceName.ToString()}");
                await LoadAndMergeTranslations(
                    frameworkAssembly,
                    frameworkResourceName,
                    Translations);
            }
            else
            {
                // 4. Wenn es zusätzliche Quellen gibt → additiv mergen
                if (additionalSources != null)
                {
                    foreach (var source in additionalSources)
                    {
                        try
                        {
                            await LoadAndMergeTranslations(
                                source.Assembly,
                                source.ResourceName,
                                Translations);
                        }
                        catch (Exception ex) when (ex.InnerException is InvalidOperationException ioe
                                                   && ioe.Message.Contains("not found"))
                        {
                            //Console.WriteLine($"[Blazor GlobalStat SetTranslations] ERROR 1: {ex.Message}");
                            // Optionale Datei nicht gefunden → ignorieren
                        }
                        catch (Exception ex)
                        {
                            //Console.WriteLine($"[Blazor GlobalStat SetTranslations] ERROR 2: {ex.Message}");
                            throw new Exception(
                                $"Kritischer Fehler: Konnte externe Sprachdatei '{source.ResourceName}' nicht laden/deserialisieren.",
                                ex);
                        }
                    }
                }
            }

            //Console.WriteLine("[Blazor GlobalStat SetTranslations] END");
        }
                
        private async Task LoadAndMergeTranslations(
            System.Reflection.Assembly assembly,
            string resourceName,
            Translations targetTranslations)
        {
            List<TranslationEntryModel>? deserializedEntries = null;

            //Console.WriteLine("[Blazor GlobalStat LoadAndMergeTranslations] START");

            try
            {
                //Console.WriteLine($"[Blazor GlobalStat LoadAndMergeTranslations] BlazorCore.wwwroot.languages.BlazorCore.json: {resourceName}");

                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                    throw new InvalidOperationException(
                        $"Embedded resource '{resourceName}' not found in assembly '{assembly.FullName}'.");

                using var reader = new StreamReader(stream);
                string jsonLanguages = await reader.ReadToEndAsync();

                //Console.WriteLine($"[Blazor GlobalStat LoadAndMergeTranslations] ReadToEndAsync: {jsonLanguages}");

                // AOT-sichere Deserialisierung
                var typeInfo = BlazorCore.JsonContext.Default.ListTranslationEntryModel;
                deserializedEntries = System.Text.Json.JsonSerializer.Deserialize(jsonLanguages, typeInfo);
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"[Blazor GlobalStat LoadAndMergeTranslations] ERROR: {ex.Message}");
                throw new Exception(
                    $"Failed to load or deserialize translation resource '{resourceName}'.",
                    ex);
            }

            if (deserializedEntries == null)
                return;

            // MERGE-LOGIK mit Array-Zugriff (schneller!)
            foreach (var entry in deserializedEntries)
            {
                if (string.IsNullOrEmpty(entry.EN))
                    continue;

                string key = entry.EN;

                // Direkter Array-Zugriff statt Dictionary-Lookup
                // Reihenfolge: EN(0), DE(1), SRL(2), SRC(3), FR(4), IT(5), 
                //              AR(6), ZH(7), HI(8), ES(9), ID(10), PT(11)
                targetTranslations.GetLanguageMapByIndex(0)[key] = entry.EN;
                targetTranslations.GetLanguageMapByIndex(1)[key] = entry.DE ?? key;
                targetTranslations.GetLanguageMapByIndex(2)[key] = entry.SRL ?? key;
                targetTranslations.GetLanguageMapByIndex(3)[key] = entry.SRC ?? key;
                targetTranslations.GetLanguageMapByIndex(4)[key] = entry.FR ?? key;
                targetTranslations.GetLanguageMapByIndex(5)[key] = entry.IT ?? key;
                targetTranslations.GetLanguageMapByIndex(6)[key] = entry.AR ?? key;
                targetTranslations.GetLanguageMapByIndex(7)[key] = entry.ZH ?? key;
                targetTranslations.GetLanguageMapByIndex(8)[key] = entry.HI ?? key;
                targetTranslations.GetLanguageMapByIndex(9)[key] = entry.ES ?? key;
                targetTranslations.GetLanguageMapByIndex(10)[key] = entry.ID ?? key;
                targetTranslations.GetLanguageMapByIndex(11)[key] = entry.PT ?? key;
            }
        }


        /// <summary>
        /// Methode passt das Padding des Base64-Strings dynamisch an
        /// </summary>
        /// <param name="_base64">Base64-codierter String</param>
        /// <returns>Rückgabewert ist ein  Byte-Array</returns>
        public byte[] ParseBase64WithoutPadding(string _base64)
        {
            switch (_base64.Length % 4)
            {
                case 2:
                    _base64 += "==";
                    break;
                case 3:
                    _base64 += "=";
                    break;
            }
            return Convert.FromBase64String(_base64);
        }

        /// <summary>
        /// Generiert Connectionstring für SqlClient
        /// </summary>
        /// <param name="connectionString">String</param>
        /// <returns>Rückgabewert ist ein  Scalar</returns>
        //public Scalar GenerateConnectionString(ConnectionStringModel connectionString)
        //{
        //    Scalar result = new();
        //    try
        //    {
        //        if (!string.IsNullOrEmpty(connectionString.Server) && !string.IsNullOrEmpty(connectionString.Database)
        //            && !string.IsNullOrEmpty(connectionString.User_ID) && !string.IsNullOrEmpty(connectionString.Password))
        //        {
        //            result.ValString += "Server=" + connectionString.Server + "; Database=" + connectionString.Database + "; User ID=" + connectionString.User_ID + "; Password=" + connectionString.Password;

        //            result.ValString += (connectionString.Encrypt ? ";Encrypt=True" : "");
        //            result.ValString += (connectionString.Integrated_Security ? ";Integrated Security=True" : ";Integrated Security=False"); // Ist ab MS SQL 2022 ogligatorisch anzugeben
        //            result.ValString += (connectionString.Connection_Timeout > 0 ? ";Connection Timeout=" + connectionString.Connection_Timeout.ToString() : "");
        //            result.ValString += (connectionString.Pooling ? "" : ";Pooling=False");
        //            result.ValString += (connectionString.Min_Pool_Size > 0 ? ";Min Pool Size=" + connectionString.Min_Pool_Size.ToString() : "");
        //            result.ValString += (connectionString.Max_Pool_Size > 0 ? ";Max Pool Size=" + connectionString.Max_Pool_Size.ToString() : "");
        //            result.ValString += (connectionString.MultipleActiveResultSets ? ";MultipleActiveResultSets=True" : "");
        //            result.ValString += (!string.IsNullOrEmpty(connectionString.Application_Name) ? ";Application Name=" + connectionString.Application_Name : "");
        //            result.ValString += (connectionString.TrustServerCertificate ? ";TrustServerCertificate=True" : "");
        //            //result.ValString += (!string.IsNullOrEmpty("en-US") ? ";Current Language=" + connectionString.Current_Language : "");
        //            result.ValString += (connectionString.Packet_Size > 0 ? ";Packet Size=" + connectionString.Packet_Size.ToString() : "");
        //            result.ValString += (!string.IsNullOrEmpty(connectionString.Workstation_ID) ? ";Workstation ID=" + connectionString.Workstation_ID : "");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        result.Error = ex.Message;
        //    }

        //    return result;
        //}
        public Scalar GenerateConnectionString(ConnectionStringModel connectionString)
        {
            Scalar result = new();

            try
            {
                if (string.IsNullOrWhiteSpace(connectionString.Server) ||
                    string.IsNullOrWhiteSpace(connectionString.Database))
                {
                    throw new ArgumentException("Server und Database sind Pflichtfelder.");
                }

                var builder = new SqlConnectionStringBuilder
                {
                    DataSource = connectionString.Server,
                    InitialCatalog = connectionString.Database,
                    IntegratedSecurity = connectionString.Integrated_Security,
                    Encrypt = connectionString.Encrypt,
                    TrustServerCertificate = connectionString.TrustServerCertificate
                };

                // Nur setzen wenn NICHT Integrated Security
                if (!connectionString.Integrated_Security)
                {
                    builder.UserID = connectionString.User_ID;
                    builder.Password = connectionString.Password;
                }

                if (connectionString.Connection_Timeout > 0)
                    builder.ConnectTimeout = connectionString.Connection_Timeout;

                if (!connectionString.Pooling)
                    builder.Pooling = false;

                if (connectionString.Min_Pool_Size > 0)
                    builder.MinPoolSize = connectionString.Min_Pool_Size;

                if (connectionString.Max_Pool_Size > 0)
                    builder.MaxPoolSize = connectionString.Max_Pool_Size;

                if (connectionString.MultipleActiveResultSets)
                    builder.MultipleActiveResultSets = true;

                if (!string.IsNullOrWhiteSpace(connectionString.Application_Name))
                    builder.ApplicationName = connectionString.Application_Name;

                if (connectionString.Packet_Size > 0)
                    builder.PacketSize = connectionString.Packet_Size;

                if (!string.IsNullOrWhiteSpace(connectionString.Workstation_ID))
                    builder.WorkstationID = connectionString.Workstation_ID;

                result.ValString = builder.ConnectionString;
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
            }

            return result;
        }

        // Methode zur Konvertierung von hexadezimalen Zeichenfolgen in Byte-Arrays
        public byte[] StringToByteArray(string hex)
        {
            int numberChars = hex.Length / 2;
            byte[] bytes = new byte[numberChars];
            using (var sr = new StringReader(hex))
            {
                for (int i = 0; i < numberChars; i++)
                {
                    bytes[i] = Convert.ToByte(new string(new char[2] { (char)sr.Read(), (char)sr.Read() }), 16);
                }
            }
            return bytes;
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(bool))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(int))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(long))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(double))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DateTime))]
        // Optional: Füge hier weitere Typen hinzu, die dynamisch unterstützt werden müssen, z.B.:
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Guid))]
        // Hinweis: Enums sind komplexer, siehe Kommentar unten
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicFields, typeof(SHARING_STATUS))]
        public T ConvertStrPara<T>(string input, bool forRealm = false)
        {
            Type targetType = typeof(T);
            Type? underlyingType = Nullable.GetUnderlyingType(targetType);
            Type effectiveType = underlyingType ?? targetType;

            // 1. String (Basis-Fall)
            if (effectiveType == typeof(string))
                return (T)(object)(input ?? string.Empty);

            // 2. Behandlung von leeren Strings
            if (string.IsNullOrWhiteSpace(input))
            {
                // Für Realm verwenden wir Standardwerte (kein DBNull.Value), ansonsten default! (z.B. null für Nullable)
                if (forRealm)
                {
                    if (effectiveType == typeof(DateTime))
                        return (T)(object)DateTime.MinValue;
                    if (effectiveType == typeof(bool))
                        return (T)(object)false;
                    if (effectiveType == typeof(int) || effectiveType == typeof(long))
                        return (T)(object)0;
                    if (effectiveType == typeof(double))
                        return (T)(object)0.0;
                    // Wenn der Typ ein Enum ist, wird es zu 0 konvertiert
                    if (effectiveType.IsEnum)
                        return (T)(object)0;

                    // Fällt auf default zurück, wenn kein Standardwert gefunden wird (z.B. Guid, byte[])
                }

                // Wenn es ein Nullable-Typ ist (z.B. int?), ist default! null. 
                // Wenn es ein Werttyp ist (z.B. int), ist default! 0.
                return default!;
            }

            // 3. Bool (Speziell wegen der String-Werte und Realm 1/0 Konvertierung)
            if (effectiveType == typeof(bool))
            {
                bool result = input.Trim().ToLowerInvariant() switch
                {
                    "1" or "true" or "yes" or "on" => true,
                    "0" or "false" or "no" or "off" => false,
                    _ => throw new InvalidOperationException($"Ungültiger bool-Wert: '{input}'.")
                };

                if (forRealm)
                {
                    TypeCode code = Type.GetTypeCode(targetType);

                    // Für Realm als Integer (1/0) zurückgeben
                    object intValue = result ? 1 : 0;
                    // Wir verwenden Convert.ChangeType, da die Typen im DynamicDependency-Attribut gesichert sind.
                    //return (T)Convert.ChangeType(intValue, targetType);
                    switch (code)
                    {
                        case TypeCode.Boolean:
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                        case TypeCode.Int16:
                        case TypeCode.Byte:
                        case TypeCode.Double:
                        case TypeCode.Single:
                        case TypeCode.Decimal:
                            return (T)Convert.ChangeType(intValue, targetType);

                        default:
                            throw new NotSupportedException(
                                $"AOT/Trimming-SICHERHEIT: Typ '{targetType.FullName}' ist hier nicht erlaubt. " +
                                $"Nur primitive numerische Typen und bool sind zulässig."
                            );
                    }
                }

                return (T)(object)result;
            }

            // 4. Standard-Typen (Int32, Int64, Double, DateTime, Dezimal)
            if (effectiveType == typeof(int) && int.TryParse(input, out int iValue))
                return (T)(object)iValue;

            if (effectiveType == typeof(long) && long.TryParse(input, out long lValue))
                return (T)(object)lValue;

            if (effectiveType == typeof(double) && double.TryParse(input, out double dValue))
                return (T)(object)dValue;

            if (effectiveType == typeof(DateTime) && DateTime.TryParse(input, out DateTime dtValue))
                return (T)(object)dtValue;

            if (effectiveType == typeof(decimal) && decimal.TryParse(input, out decimal decValue))
                return (T)(object)decValue;

            // 4.5 DateTimeOffset (AOT-sicher, explizit)
            if (effectiveType == typeof(DateTimeOffset))
            {
                // 1️⃣ Unix Timestamp?
                if (long.TryParse(input, out long unix))
                    return (T)(object)DateTimeOffset.FromUnixTimeSeconds(unix);

                // 2️⃣ Vollwertiger DateTimeOffset-String (mit Z / Offset)
                if (DateTimeOffset.TryParse(
                        input,
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.RoundtripKind,
                        out var dto))
                {
                    return (T)(object)dto;
                }

                // 3️⃣ Fallback: DateTime ohne Offset → explizit als UTC behandeln
                if (DateTime.TryParse(
                        input,
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                        out var dt))
                {
                    return (T)(object)new DateTimeOffset(dt, TimeSpan.Zero);
                }

                throw new InvalidOperationException(
                    $"Ungültiger DateTimeOffset-Wert (kein Offset enthalten): '{input}'"
                );
            }

            // 5. GUID (AOT-sicher)
            if (effectiveType == typeof(Guid) && Guid.TryParse(input, out Guid gValue))
                return (T)(object)gValue;

            //// 6. Enums (AOT-sicher durch TryParse basierend auf dem Wert, nicht dem Namen)
            //// Wenn Sie Enums nach Namen parsen müssen, wird es komplizierter bzgl. AOT!
            //if (effectiveTyBlazorCore.IsEnum && int.TryParse(input, out int enumInt))
            //{
            //    // Cast des Integer-Werts ist AOT-sicherer als Enum.Parse(string name)
            //    return (T)Enum.ToObject(effectiveType, enumInt);
            //}
            // 6. Enums (AOT-sicher und robust)
            if (effectiveType.IsEnum)
            {
                // 6.1 Versuch, als Integer-Wert zu parsen (Bevorzugt für DB-Mapping: "0", "1", "2")
                if (long.TryParse(input, out long enumLong))
                {
                    // AOT-sicher: Konvertiert den numerischen Wert direkt in den Enum-Typ
                    return (T)Enum.ToObject(effectiveType, enumLong);
                }

                //// 6.2 Fallback: Versuch, als Enum-Namen zu parsen ("REQUEST", "ACCEPT")
                //try
                //{
                //    // Achtung: Enum.Parse(Type, string) ist AOT-unsicher. 
                //    // Wir verwenden TyBlazorCore.GetTypeCode() und eine sichere Enum.Parse-Variante, 
                //    // die auf Boxing/Unboxing des Enum-Werts basiert.

                //    object enumValue = Enum.Parse(effectiveType, input, ignoreCase: true);

                //    // Um den Enum-Wert AOT-sicher nach T zu konvertieren, verwenden wir Convert.ChangeTyBlazorCore.
                //    // Dies funktioniert, da wir wissen, dass Enum-Typen primitiven numerischen Typen entsprechen.
                //    return (T)Convert.ChangeType(enumValue, effectiveType);
                //}
                //catch (ArgumentException ex)
                //{
                //    // Weder Integer noch gültiger Name gefunden
                //    throw new InvalidOperationException($"Ungültiger Enum-Wert für '{effectiveTyBlazorCore.Name}'. Weder ein gültiger Integer noch ein Enum-Name gefunden: '{input}'", ex);
                //}

                // 6.2 Fallback: Ungültiger Wert oder Name (AOT-sicherer Fehler)
                // Wenn es kein numerischer String ist, ist es entweder ein ungültiger Wert oder ein Enum-Name. 
                // Da Namens-Parsing AOT-unsicher ist, werfen wir hier eine klare Exception.
                throw new InvalidOperationException(
                    $"AOT-ERROR: Invalid enum value for '{effectiveType.Name}'. " +
                    $"Numeric values are required because enum name parsing ('{input}') " +
                    "was trimmed during AOT compilation.");
            }


            // 7. Byte[] (Ihre Logik ist komplex, aber notwendig für verschiedene Formate)
            if (effectiveType == typeof(byte[]))
            {
                try
                {
                    // Versuch 1: Base64
                    return (T)(object)Convert.FromBase64String(input);
                }
                catch (FormatException)
                {
                    // Versuch 2: Hex-String (Ihr Regex.IsMatch muss mit dem Trimmer überleben)
                    //if (input.Length % 2 == 0 && Regex.IsMatch(input, @"\A\b[0-9a-fA-F]+\b\Z"))
                    if (input.Length % 2 == 0 && Regex.IsMatch(input, @"\A\b[0-9a-fA-F]+\b\Z", RegexOptions.NonBacktracking))
                    {
                        int len = input.Length / 2;
                        byte[] bytes = new byte[len];
                        for (int i = 0; i < len; i++)
                            bytes[i] = Convert.ToByte(input.Substring(i * 2, 2), 16);
                        return (T)(object)bytes;
                    }

                    // Versuch 3: Komma-separierte Bytes
                    if (input.Contains(","))
                    {
                        var bytes = input.Split(',')
                                         .Select(s => byte.Parse(s.Trim()))
                                         .ToArray();
                        return (T)(object)bytes;
                    }

                    throw new InvalidOperationException($"Das Format des Byte[]-Strings '{input}' ist nicht erkannt worden.");
                }
            }

            // 8. AOT-sicherer Fallback für unbekannte Typen 
            // Der Code muss hier abbrechen, da der ursprüngliche TypDescriptor Call AOT-unsicher ist.

            throw new InvalidOperationException($"Der angegebene Typ '{typeof(T).Name}' wird nicht unterstützt. (AOT-sicherer Konverter-Fehler)");
        }
        
        // Hilfsmethoden für die Serialisierung, da Tuple nicht direkt serialisiert werden können
        public string SerializeDictionaryTpc(Dictionary<string, string> dictionary)
        {
            //return System.Text.Json.JsonSerializer.Serialize(dictionary);

            //return System.Text.Json.JsonSerializer.Serialize(dictionary, JsonContext.Default.DictionaryStringString);

            // Wir erstellen neue Optionen, die aber auf deinen generierten Context aufsetzen
            var options = new JsonSerializerOptions
            {
                // 1. Der wichtigste Teil: Verhindert das Aufblähen des Bild-Strings
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,

                // 2. Wir sagen den Optionen, dass sie deinen generierten Context nutzen sollen
                // Das macht die Sache AOT-sicher!
                TypeInfoResolver = JsonContext.Default,

                WriteIndented = false
            };

            // Wir nutzen die Überladung, die nur das Objekt und die Optionen nimmt.
            // Da wir den TypeInfoResolver oben gesetzt haben, weiß .NET trotzdem genau, was zu tun ist.
            return JsonSerializer.Serialize(dictionary, options);
        }

        public Dictionary<string, string> DeserializeDictionaryTpc(string jsonString)
        {
            var result = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);

            if (result == null)
                throw new InvalidOperationException("Deserialization returned zero.");

            return result;
        }

    }
}
#pragma warning restore CA1416