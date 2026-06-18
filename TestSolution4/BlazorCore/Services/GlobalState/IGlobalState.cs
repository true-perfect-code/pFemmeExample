using BlazorCore.Services.AppState;
using BlazorCore.Services.SqlClient;

namespace BlazorCore.Services.GlobalState
{
    public interface IGlobalStateBase
    {
        ConfigurationGeneral ConfigGeneral { get; }
        ConfigurationWebapi ConfigWebapi { get; }
        Sections Catalog { get; }
        //Dataset Data { get; }
        void GlobalInit(ConfigurationGeneral configGeneral, ConfigurationWebapi configWebapi, Sections catalog); //, Sections catalog, Dataset data);


        /// <summary>
        /// Ensures that the global state is initialized.
        /// This method should load and cache any required data such as configuration
        /// or translations if it has not been loaded already.
        /// </summary>
        /// <returns>
        /// A task that completes when the initialization process is finished.
        /// </returns>
        Task EnsureInitializedAsync();

        ///// <summary>
        ///// Gets the dictionary of translation key-value pairs
        ///// that can be used for application-wide localization.
        ///// </summary>
        //Dictionary<string, string> Translations { get; }
        //Platform.Translations Translations { get; }

        /// <summary>
        /// Gets the centralized, thread-safe collection of all application translations
        /// aggregated across all supported languages and modules.
        /// </summary>
        // Wir verwenden den neuen, performanten Typ, der die Dictionary<Sprache, Dictionary<Key, Value>> enthält
        public Translations Translations { get; }

        /// <summary>
        /// Gets the global mssql connectionstring configuration.
        /// This typically contains settings that are constant for all users.
        /// </summary>
        string ConnectionString { get; }

        /// <summary>
        /// Gets the Security ConfigFile.
        /// This typically contains settings that are constant for all users.
        /// </summary>
        //string SecurityConfigFile { get; }

        //Platform.LocalStorageModel LocalStorage { get; }

        // Standard MSSQL SP-Parameter
        //List<string> Tables { get; set; }
        //List<TableDefinition> Parameters { get; }
        //List<TableDefinition> SetDefaultSPpara();


        //string WebUrl { get; }
        //string WebApiUrl { get; }

        //bool EncryptDecryptWebApi { get; }
        //string ConnectionsServerFolder { get; }
        //string SecurityConfigJsonFilename { get; }
        //string FileExtensionJson { get; }
        //string TableSchema { get; }
        //string RealmDB { get; }
        //bool ShowConnectionStringByError { get; }
        // WebapiExceptionError { get; }

        ////TpcWebApiEndpointsModel tpcWebApiEndpoints { get; }
        //TpcWebApiEndpointsModel tpcWebApiEndpoints { get; }
        //TpcWebApiModel tpcWebApi { get; }

        //string xxx { get; }
        //string xxx { get; }
        //string xxx { get; }
        //string xxx { get; }

        //Scalar GetSecurityConfigurationFile();
        Scalar GetConnectionString();
        //T? ReadJson<T>(string filePath);
        byte[] ParseBase64WithoutPadding(string _base64);
        Scalar GenerateConnectionString(ConnectionStringModel connectionString);
        byte[] StringToByteArray(string hex);
        T ConvertStrPara<T>(string input, bool forRealm = false);

        //void InitializeEmail(string email);
        //void InitializeDeviceId(string deviceInfo);
        //string GenerateUniqueId();

        string SerializeDictionaryTpc(Dictionary<string, string> dictionary);
        Dictionary<string, string> DeserializeDictionaryTpc(string jsonString);

        // Hier werden die Sharing Anfragen erfasst
        List<string> SharingRequests { get; set; }



        //// Basisversion (lädt nur BlazorCore.json)
        //Task<Translations> GetTranslations();

        // Erweiterte Version (lädt BlazorCore.json + zusätzliche Ressourcen)
        Task SetTranslations(List<(System.Reflection.Assembly Assembly, string ResourceName)> additionalSources);

        //Task SetTranslationsAsync(
        //    List<(System.Reflection.Assembly Assembly, string ResourceName)>? additionalSources = null);

    }
}
