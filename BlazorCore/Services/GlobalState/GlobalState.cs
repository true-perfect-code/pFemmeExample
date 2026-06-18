#pragma warning disable CA1416

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.RegularExpressions;
using BlazorCore.Models;
using BlazorCore.Services.AppState;
using BlazorCore.Services.Platform;
using BlazorCore.Services.SqlClient;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorCore.Services.GlobalState
{
    /// <summary>
    /// Default implementation of <see cref="IGlobalStateBase"/>.
    /// Provides globally shared, read-only state across the entire application.
    /// </summary>
    public class GlobalStateBase : IGlobalStateBase
    {
        private readonly IServiceProvider _serviceProvider;
        private bool _isInitialized;

        // =====================================================================
        // STATIC CONFIGURATION
        // =====================================================================

        /// <inheritdoc />
        public ConfigurationGeneral ConfigGeneral { get; private set; } = new();

        /// <inheritdoc />
        public ConfigurationWebapi ConfigWebapi { get; private set; } = new();

        /// <inheritdoc />
        public Sections Catalog { get; private set; } = new();

        /// <inheritdoc />
        public Translations Translations { get; private set; } = new();

        /// <inheritdoc />
        public string ConnectionString { get; private set; } = string.Empty;

        // =====================================================================
        // CONSTRUCTOR
        // =====================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalStateBase"/> class.
        /// </summary>
        /// <param name="serviceProvider">Service provider for dependency resolution.</param>
        public GlobalStateBase(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        /// <inheritdoc />
        public void GlobalInit(ConfigurationGeneral configGeneral, ConfigurationWebapi configWebapi, Sections? catalog)
        {
            ConfigGeneral = configGeneral ?? throw new ArgumentNullException(nameof(configGeneral));
            ConfigWebapi = configWebapi ?? throw new ArgumentNullException(nameof(configWebapi));
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        /// <inheritdoc />
        public async Task EnsureInitializedAsync()
        {
            if (_isInitialized)
                return;

            await Task.Delay(20); // Allow async initialization

            using var scope = _serviceProvider.CreateScope();
            var platform = scope.ServiceProvider.GetRequiredService<IPlatformBase>();

            var formFactor = await platform.GetFormFactor();
            bool isWeb = formFactor.Equals("web", StringComparison.OrdinalIgnoreCase);

            // Load connection string only on server-side (web platform)
            if (isWeb)
            {
                Scalar result = GetConnectionString();
                if (result.Success)
                    ConnectionString = result.ValString ?? string.Empty;
                else
                    throw new Exception($"Failed to load connection string: {result.Error}");
            }

            // Load translations from embedded resources
            await SetTranslations();

            _isInitialized = true;
        }

        // =====================================================================
        // DATABASE CONNECTION (server-side only)
        // =====================================================================

        /// <summary>
        /// Gets the MSSQL connection string from the configuration file.
        /// </summary>
        private Scalar GetConnectionString()
        {
            Scalar result = new();

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var platform = scope.ServiceProvider.GetRequiredService<IPlatformBase>();

                string basedir = platform.GetBaseDirectory();
                if (!string.IsNullOrEmpty(basedir))
                {
                    DirectoryInfo? parentbasedir = Directory.GetParent(basedir);
                    if (parentbasedir != null && parentbasedir.Parent != null)
                    {
                        string connectionStringFolder = Path.Combine(
                            parentbasedir.Parent.FullName,
                            ConfigGeneral.ConnectionsServerFolder);

                        string connectionStringPath = Path.Combine(
                            connectionStringFolder,
                            $"{ConfigGeneral.ApplicationName}{ConfigGeneral.FileExtensionJson}");

                        if (File.Exists(connectionStringPath))
                        {
                            var connectionString = JsonSerializer.Deserialize(
                                File.ReadAllText(connectionStringPath),
                                JsonContext.Default.ConnectionStringModel)!;

                            if (connectionString != null)
                            {
                                using var aes = new ServerShared.Security(
                                    ConfigGeneral.ApplicationName,
                                    ConfigGeneral.TableSchema);

                                connectionString.Password = aes.Decrypt(connectionString.Password!);
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

        /// <summary>
        /// Generates a SQL connection string from the connection string model.
        /// </summary>
        public Scalar GenerateConnectionString(ConnectionStringModel connectionString)
        {
            Scalar result = new();

            try
            {
                if (string.IsNullOrWhiteSpace(connectionString.Server) ||
                    string.IsNullOrWhiteSpace(connectionString.Database))
                {
                    throw new ArgumentException("Server and Database are required fields.");
                }

                var builder = new SqlConnectionStringBuilder
                {
                    DataSource = connectionString.Server,
                    InitialCatalog = connectionString.Database,
                    IntegratedSecurity = connectionString.Integrated_Security,
                    Encrypt = connectionString.Encrypt,
                    TrustServerCertificate = connectionString.TrustServerCertificate
                };

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

        // =====================================================================
        // TRANSLATIONS
        // =====================================================================

        /// <summary>
        /// Loads translations from embedded resources and merges them into the Translations collection.
        /// </summary>
        public async Task SetTranslations(
            List<(System.Reflection.Assembly Assembly, string ResourceName)>? additionalSources = null)
        {
            if (Translations == null)
                Translations = new Translations();

            // Initialize language map structure if empty
            if (Translations.LanguageMaps.Count == 0)
            {
                foreach (var langCode in ConfigGeneral.AllSupportedLanguageCodes.Split(','))
                {
                    Translations.LanguageMaps[langCode] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
            }

            // Load framework translations
            if (additionalSources == null)
            {
                var frameworkAssembly = typeof(BlazorCore.Utilities).Assembly;
                var frameworkResourceName = "BlazorCore.wwwroot.languages.BlazorCore.json";

                await LoadAndMergeTranslations(frameworkAssembly, frameworkResourceName, Translations);
            }
            else
            {
                // Load additional project-specific translations
                foreach (var source in additionalSources)
                {
                    try
                    {
                        await LoadAndMergeTranslations(source.Assembly, source.ResourceName, Translations);
                    }
                    catch (Exception ex) when (ex.InnerException is InvalidOperationException ioe &&
                                               ioe.Message.Contains("not found"))
                    {
                        // Optional resource not found - ignore
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(
                            $"Failed to load external translation file '{source.ResourceName}'.",
                            ex);
                    }
                }
            }
        }

        /// <summary>
        /// Loads and merges translations from a single embedded resource.
        /// </summary>
        private async Task LoadAndMergeTranslations(
            System.Reflection.Assembly assembly,
            string resourceName,
            Translations targetTranslations)
        {
            List<TranslationEntryModel>? deserializedEntries;

            try
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                    throw new InvalidOperationException(
                        $"Embedded resource '{resourceName}' not found in assembly '{assembly.FullName}'.");

                using var reader = new StreamReader(stream);
                string jsonLanguages = await reader.ReadToEndAsync();

                var typeInfo = BlazorCore.JsonContext.Default.ListTranslationEntryModel;
                deserializedEntries = JsonSerializer.Deserialize(jsonLanguages, typeInfo);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load or deserialize translation resource '{resourceName}'.", ex);
            }

            if (deserializedEntries == null)
                return;

            // Merge entries using array index access for performance
            foreach (var entry in deserializedEntries)
            {
                if (string.IsNullOrEmpty(entry.EN))
                    continue;

                string key = entry.EN;

                // Order: EN(0), DE(1), SRL(2), SRC(3), FR(4), IT(5),
                //        AR(6), ZH(7), HI(8), ES(9), ID(10), PT(11)
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

        // =====================================================================
        // UTILITY METHODS
        // =====================================================================

        /// <inheritdoc />
        public byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2:
                    base64 += "==";
                    break;
                case 3:
                    base64 += "=";
                    break;
            }
            return Convert.FromBase64String(base64);
        }

        /// <inheritdoc />
        public byte[] StringToByteArray(string hex)
        {
            int numberChars = hex.Length / 2;
            byte[] bytes = new byte[numberChars];

            using var stringReader = new StringReader(hex);
            for (int i = 0; i < numberChars; i++)
            {
                char[] hexChars = new char[2] { (char)stringReader.Read(), (char)stringReader.Read() };
                bytes[i] = Convert.ToByte(new string(hexChars), 16);
            }

            return bytes;
        }
                
        /// <inheritdoc />
        public string SerializeDictionaryTpc(Dictionary<string, string> dictionary)
        {
            var options = new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                TypeInfoResolver = JsonContext.Default,
                WriteIndented = false
            };

            return JsonSerializer.Serialize(dictionary, options);
        }

        /// <inheritdoc />
        public Dictionary<string, string> DeserializeDictionaryTpc(string jsonString)
        {
            var result = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);

            if (result == null)
                throw new InvalidOperationException("Deserialization returned null.");

            return result;
        }
    }
}
#pragma warning restore CA1416