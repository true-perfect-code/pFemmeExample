using BlazorCore.Models;
using BlazorCore.Services.Dam;
using BlazorCore.Services.JsonHybridStorage;
using BlazorCore.Services.MemoryStorage;
using BlazorCore.Services.SqlClient;
using TestSolution4.Shared.Global;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TestSolution4.Shared.Services.JsonHybridStorage
{
    /// <summary>
    /// Base implementation for JSON Hybrid Storage.
    /// Inherits RAM logic from MemoryStorageBase and adds encryption and persistence layers.
    /// Implementation: Shared (BlazorCore). Physical IO is handled by platform-specific overrides.
    /// </summary>
    public abstract class JsonHybridStorageBase : MemoryStorageBase, IJsonHybridStorageBase
    {
        public string DbName;
        protected string _userAccount = string.Empty;

        public JsonHybridStorageBase(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            // Infrastruktur wird durch MemoryStorageBase-Konstruktor bereitgestellt
            DbName = Configuration.ConfigGeneral.ApplicationName; //_globalState?.ConfigGeneral?.ApplicationName ?? "TpcDefaultApp";
        }

        #region Initialization & Metadata

        //public override async Task<ScalarModel> InitializeAsync(bool register, string userAccount)
        //{
        //    _userAccount = userAccount;

        //    // 1. RAM-Infrastruktur und Lock über Basisklasse initialisieren
        //    var res = await base.InitializeAsync(register, _userAccount);

        //    if (res.out_value_bool)
        //    {
        //        await _appState.Log($"[JsonHybrid] RAM initialized for {_userAccount}. Starting disk load...");

        //        // 2. Physische Daten beim Start in den RAM laden (Warm-up)
        //        await PerformInitialLoadAsync(_userAccount);
        //    }

        //    return res;
        //}
        public override async Task<ScalarModel> InitializeAsync(bool register, string userAccount)
        {
            //DbName = _globalState?.ConfigGeneral?.ApplicationName ?? "TpcDefaultApp";
            _userAccount = userAccount;

            // NEU: Plattform-spezifische Vorbereitung (DB öffnen, Ordner prüfen)
            await PreparePhysicalStorageAsync(DbName, _userAccount);

            // 1. RAM-Infrastruktur initialisieren
            var res = await base.InitializeAsync(register, _userAccount);

            if (res.out_value_bool)
            {
                await _appState.Log($"[JsonHybrid] Storage ready for {_userAccount}. Warm-up...");
                await PerformInitialLoadAsync(_userAccount);
            }

            return res;
        }

        // Virtuelle Methode, die standardmäßig nichts tut (für WPF z.B. nicht zwingend nötig)
        protected virtual Task PreparePhysicalStorageAsync(string dbName, string userAccount)
            => Task.CompletedTask;

        public override async Task<ClientStorageModel> GetTokenDataTPC(Dictionary<string, string> dbParams)
        {
            // Nutzt die RAM-Logik der Basis (liest aus der AuthUsers Tabelle im RAM)
            return await base.GetTokenDataTPC(dbParams);
        }

        #endregion

        #region CRUD Operations (Hybrid Orchestration)

        public override async Task<ScalarModel> Save(Dictionary<string, string> dbParams)
        {
            // 1. "Logic First": Erst im RAM speichern via MemoryStorageBase
            var res = await base.Save(dbParams);

            // Falls RAM-Speicherung fehlgeschlagen (Validierung etc.), sofort zurück
            if (!string.IsNullOrEmpty(res.out_err)) return res;

            string case_ = dbParams.GetValueOrDefault("@Case_", "");
            string unixTS = dbParams.GetValueOrDefault("@UnixTS", "");
            string authUsers_UnixTS = dbParams.GetValueOrDefault("@AuthUsers_UnixTS", "");

            // 2. "Persistence": Hier spielt die Musik! 
            // Wir wissen genau, was wir tun.
            switch (case_)
            {
                case "Save>>AuthUsersExtend":
                    {
                        
                        // Wir holen das fertige Objekt direkt aus dem RAM-Cache der Basis
                        var model = _ramCache["AuthUsersExtend"]
                                    .Cast<AuthUsersExtendModel>()
                                    .FirstOrDefault(x => x.AuthUsers_UnixTS == authUsers_UnixTS);

                        if (model != null)
                        {
                            // JSON mit Source Generator (AOT-Safe)
                            string json = System.Text.Json.JsonSerializer.Serialize(model, BlazorCore.JsonContext.Default.AuthUsersExtendModel);
                            await EncryptAndWriteAsync("AuthUsersExtend", $"{authUsers_UnixTS}.json", json);
                        }
                    }
                    break;

                case "Save>>AppParameter":
                    {
                        // Wir suchen das Model im RAM anhand des Namens
                        var model = _ramCache["AppParameter"]
                                    .Cast<AppParameterModel>()
                                    .FirstOrDefault(x => x.UnixTS == unixTS);

                        if (model != null)
                        {
                            // Wir nehmen die UnixTS des Models als Dateinamen für Parität zu SQLite
                            string json = System.Text.Json.JsonSerializer.Serialize(model, BlazorCore.JsonContext.Default.AppParameterModel);
                            await EncryptAndWriteAsync("AppParameter", $"{unixTS}.json", json);
                        }
                    }
                    break;

                    // ... alle anderen Tabellen folgen diesem klaren Muster ...
            }

            return res;
        }

        /// <summary>
        /// Das ist das Ende der Kette im Shared-Teil. 
        /// Hier wird verschlüsselt und die Plattform (WPF/Capacitor) gerufen.
        /// </summary>
        private async Task EncryptAndWriteAsync(string tableName, string fileName, string jsonContent)
        {
            try
            {
                string finalContent = jsonContent;

                // Verschlüsselung nur, wenn in Config aktiv
                if (_globalState.ConfigGeneral.LocalStorageEncrypt)
                {
                    using (var aes = BlazorCore.Services.ServerShared.SecurityServerFactory.Create())
                    {
                        finalContent = await aes.EncryptBase32SecretAsync(jsonContent, Convert.FromBase64String(_appState.Pepper));
                    }
                }

                // Der plattformspezifische Aufruf (WPF.WritePhysicalFileAsync / Capacitor.WritePhysicalFileAsync)
                // Wir kombinieren Tabelle und Dateiname zu einem relativen Pfad
                string relativePath = Path.Combine(tableName, fileName);
                await WritePhysicalFileAsync(_userAccount, relativePath, finalContent);
            }
            catch (Exception ex)
            {
                await _appState.Error($"[JsonHybrid-Shared] Critical Write Error: {ex.Message}");
            }
        }

        public override async Task<ScalarModel> ExecQuery(Dictionary<string, string> dbParams)
        {
            // 1. RAM-Aktion (z.B. Delete) ausführen
            var res = await base.ExecQuery(dbParams);

            // 2. Physisches Löschen bei Erfolg
            if (string.IsNullOrEmpty(res.out_err) && dbParams.ContainsKey("@Case_"))
            {
                string case_ = dbParams["@Case_"];
                string unixTS = dbParams["@UnixTS"];
                string authUsers_UnixTS = dbParams.GetValueOrDefault("@AuthUsers_UnixTS", "");

                switch (case_)
                {
                    case "Delete>>AuthUsersExtend":
                        await DeletePhysicalFileAsync(_userAccount, Path.Combine("AuthUsersExtend", $"{authUsers_UnixTS}.json"));
                        break;

                    case "Delete>>AppParameter":
                        await DeletePhysicalFileAsync(_userAccount, Path.Combine("AppParameter", $"{unixTS}.json"));
                        break;
                }
            }
            return res;
        }

        // Lesende Zugriffe erfolgen zu 100% aus dem RAM (Truth)
        public override async Task<ScalarModel> Scalar(Dictionary<string, string> dbParams) => await base.Scalar(dbParams);

        public override async Task<ReaderModel<T>> Read<T>(Dictionary<string, string> dbParams) => await base.Read<T>(dbParams);

        #endregion

        #region Purge & Reset (Level 1-3)

        public override async Task<ScalarModel> ClearAllData()
        {
            var res = await base.ClearAllData(); // RAM leeren
            if (res.out_value_bool) await DeleteAllFilesAsync(_userAccount); // Alle JSONs löschen
            return res;
        }

        public override async Task<ScalarModel> DropAllTables()
        {
            var res = await base.DropAllTables();
            if (res.out_value_bool) await DeleteAllFilesAsync(_userAccount);
            return res;
        }

        public override async Task<ScalarModel> DeleteDB()
        {
            var res = await base.DeleteDB();
            if (res.out_value_bool) await DeletePhysicalStorageAsync(_userAccount); // Ganzen Ordner entfernen
            return res;
        }

        #endregion

        #region Internal Shared Logic (The "Brain")

        private async Task PerformInitialLoadAsync(string userAccount)
        {
            try
            {
                // Wir gehen alle Tabellen durch, die wir im Framework haben
                foreach (var tableName in MemoryStorageBase.AllTableNames)
                {
                    // 1. Alle Files dieser Tabelle von der Plattform holen (WPF liest den Ordner)
                    var encryptedFiles = await ReadTableFilesAsync(userAccount, tableName);

                    foreach (var encryptedContent in encryptedFiles)
                    {
                        string decryptedJson = encryptedContent;

                        // 2. Entschlüsseln, falls nötig
                        if (_globalState.ConfigGeneral.LocalStorageEncrypt)
                        {
                            using (var aes = BlazorCore.Services.ServerShared.SecurityServerFactory.Create())
                            {
                                decryptedJson = await aes.DecryptBase32SecretAsync(encryptedContent, Convert.FromBase64String(_appState.Pepper));
                            }
                        }

                        // 3. Deserialisieren & in RAM schieben
                        // Hier brauchen wir wieder einen kleinen Switch/Case für den JsonContext
                        await MapJsonToRamAsync(tableName, decryptedJson);
                    }
                }
            }
            catch (Exception ex)
            {
                await _appState.Log($"[JsonHybrid-Shared] Initial Load Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Deserialisiert den JSON-Inhalt typsicher über den JsonContext 
        /// und fügt ihn direkt in den RAM-Cache ein.
        /// </summary>
        private async Task MapJsonToRamAsync(string tableName, string jsonContent)
        {
            try
            {
                switch (tableName)
                {
                    case "AuthUsersExtend":
                        {
                            var model = System.Text.Json.JsonSerializer.Deserialize(
                                jsonContent,
                                BlazorCore.JsonContext.Default.AuthUsersExtendModel
                            );
                            if (model != null) _ramCache["AuthUsersExtend"].Add(model);
                        }
                        break;

                    case "AppParameter":
                        {
                            var model = System.Text.Json.JsonSerializer.Deserialize(
                                jsonContent,
                                BlazorCore.JsonContext.Default.AppParameterModel
                            );
                            if (model != null) _ramCache["AppParameter"].Add(model);
                        }
                        break;

                    // Hier fügst du weitere Tabellen analog hinzu...

                    default:
                        await _appState.Error($"[JsonHybrid-Shared] No mapping found for table: {tableName}");
                        break;
                }
            }
            catch (Exception ex)
            {
                await _appState.Error($"[JsonHybrid-Shared] Deserialization Error in {tableName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Erzeugt einen 10-stelligen SHA-256 Hash für den Dateinamen.
        /// Parität zur JavaScript Krypto-Implementierung.
        /// </summary>
        public string GenerateShortHash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower().Substring(0, 10);
        }

        //private async Task HandlePhysicalDeletionAsync(Dictionary<string, string> dbParams)
        //{
        //    // TODO: Mapping dbParams -> FileName und Aufruf von DeletePhysicalFileAsync(...)
        //}

        #endregion

        #region Abstract Methods (Platform Specific)

        protected abstract Task<List<string>> ReadTableFilesAsync(string userAccount, string tableName);
        protected abstract Task<bool> WritePhysicalFileAsync(string userAccount, string fileName, string encryptedContent);
        protected abstract Task<bool> DeletePhysicalFileAsync(string userAccount, string fileName);
        protected abstract Task<bool> DeleteAllFilesAsync(string userAccount);
        protected abstract Task<bool> DeletePhysicalStorageAsync(string userAccount);

        #endregion
    }
}