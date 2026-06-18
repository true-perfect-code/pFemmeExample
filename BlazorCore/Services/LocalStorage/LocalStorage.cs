using Azure.Core;
using BlazorCore.Models;
using BlazorCore.Services.Dam;
using BlazorCore.Services.GlobalState;
using BlazorCore.Services.LocalJsonFile;
using BlazorCore.Services.LocalQueryExecutor;
using BlazorCore.Services.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.Intrinsics.Arm;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BlazorCore.Services.LocalStorage
{
    /// <summary>
    /// Local storage engine (RAM-based) - formerly MemoryStorageBase.
    /// Serves as the single source of truth for reads in MEMORY and JSON_HYBRID modes.
    /// Delegates all business logic to LocalQueryExecutor.
    /// </summary>
    public class LocalStorage : ILocalStorage
    {
        public bool IsInitialized { get; set; } = false;

        /// <summary>
        /// Hashed user identifier for json-path on local machine (e.g., "user12345hash") - used for JSON_HYBRID file organization.
        /// </summary>
        public string UserAccountHashed { get; set; } = string.Empty;

        protected readonly IServiceProvider _serviceProvider;
        protected ILocalQueryExecutor? _queryExecutor;
        protected IGlobalStateBase? _globalState;
        protected ILocalJsonFile? _localJsonFile;

        /// <summary>
        /// Semaphore for thread-safe access to critical operations.
        /// </summary>
        protected readonly SemaphoreSlim _lock = new(1, 1);

        /// <summary>
        /// The RAM Cache: Key = TableName (e.g., "AuthUsers", "Cycles")
        /// Value = List of data records stored as objects.
        /// SINGLE SOURCE OF TRUTH for reads.
        /// </summary>
        public Dictionary<string, List<object>> RamCache { get; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalStorage"/> class.
        /// </summary>
        /// <param name="serviceProvider">Service provider for dependency resolution.</param>
        public LocalStorage(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        // =====================================================================
        // Initialization
        // =====================================================================

        public async Task<ScalarModel> InitializeAsync(string userAccountHashed)
        {
            ScalarModel result = new();

            if (IsInitialized)
                return new ScalarModel { out_value_bool = true };

            UserAccountHashed = userAccountHashed;

            _globalState = _serviceProvider.GetRequiredService<IGlobalStateBase>();
            _localJsonFile = _serviceProvider.GetRequiredService<ILocalJsonFile>();
            _queryExecutor = _serviceProvider.GetRequiredService<ILocalQueryExecutor>();

            await _lock.WaitAsync();

            try
            {
                // 1. Listen im RAM vorbereiten
                if (_globalState?.Catalog?.TablesMSSQL != null)
                {
                    foreach (var table in _globalState.Catalog.TablesMSSQL)
                    {
                        if (!RamCache.ContainsKey(table))
                        {
                            RamCache.Add(table, new List<object>());
                        }
                    }

                    // 2. Daten laden über den QueryExecutor (JSON_HYBRID)
                    if (_globalState.ConfigGeneral.LocalStorageType == LOCAL_STORAGE_TYPE.JSON_HYBRID)
                    {
                        var initResult = await _queryExecutor.Save(new LocalQueryRequest
                        {
                            Case = "InitializeInternal",
                            //Parameters = new Dictionary<string, string> { { "@UserAccount", UserAccountHashed } }
                        }, this);

                        if (!initResult.out_value_bool)
                        {
                            throw new Exception(initResult.out_err ?? "Initialization failed.");
                        }
                    }
                }

                IsInitialized = true;
                result.out_value_bool = true;
            }
            catch (Exception ex)
            {
                IsInitialized = false;
                result.out_value_bool = false;
                result.out_err = $"LocalStorage Initialization Error: {ex.Message}";
            }
            finally
            {
                _lock.Release();
            }

            return result;
        }


        // =====================================================================
        // Token & User Management
        // =====================================================================   

        /// <inheritdoc />
        public async Task<ClientStorageModel> GetTokenTPC(Dictionary<string, string> dbParams)
        {
            ClientStorageModel result = new();

            try
            {
                if (_queryExecutor == null)
                {
                    result.out_err = "LocalQueryExecutor is not initialized.";
                    return result;
                }

                // Delegate the authentication/token logic to the LocalQueryExecutor
                // We use "Register>>AuthUsers" as the case, consistent with your existing handler registration
                var queryResult = await _queryExecutor.GetTokenTPC(new LocalQueryRequest
                {
                    Case = "Register>>AuthUsers",
                    Parameters = dbParams
                }, this);

                if (queryResult == null)
                {
                    result.out_err = "Authentication handler failed to return a result.";
                    return result;
                }

                result.UnixTS = queryResult.UnixTS;
                result.WebApiToken = queryResult.WebApiToken;
                result.out_err = queryResult.out_err;
            }
            catch (Exception ex)
            {
                result.out_err = $"LocalStorage GetTokenTPC Error: {ex.Message}";
            }

            return result;
        }

        // =====================================================================
        // CRUD Operations (delegieren alle an LocalQueryExecutor)
        // =====================================================================

        /// <inheritdoc />
        public async Task<ScalarModel> Save(Dictionary<string, string> dbParams)
        {
            var result = new ScalarModel();

            try
            {
                if (_queryExecutor == null)
                {
                    result.out_err = "LocalQueryExecutor is not initialized.";
                    return result;
                }

                var case_ = dbParams.GetValueOrDefault("@Case_", "");
                if (string.IsNullOrWhiteSpace(case_))
                {
                    result.out_err = "Case is empty.";
                    return result;
                }

                // ← NEU: Verwende LocalQueryRequest und LocalQueryExecutor
                var queryResult = await _queryExecutor.Save(new LocalQueryRequest
                {
                    Case = case_,
                    Parameters = dbParams
                }, this);

                if (queryResult == null)
                {
                    result.out_err = $"No handler found for case '{case_}'";
                    return result;
                }

                result.out_value_bool = queryResult.out_value_bool;
                result.out_value_str = queryResult.out_value_str;
                result.out_value_int = queryResult.out_value_int;
                result.out_err = queryResult.out_err;
            }
            catch (Exception ex)
            {
                result.out_err = $"LocalStorage Save Error: {ex.Message}";
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<ScalarModel> Scalar(Dictionary<string, string> dbParams)
        {
            var result = new ScalarModel();

            try
            {
                if (_queryExecutor == null)
                {
                    result.out_err = "LocalQueryExecutor is not initialized.";
                    return result;
                }

                var case_ = dbParams.GetValueOrDefault("@Case_", "");
                if (string.IsNullOrWhiteSpace(case_))
                {
                    result.out_err = "Case is empty.";
                    return result;
                }

                // ← NEU: Verwende LocalQueryRequest und LocalQueryExecutor
                var queryResult = await _queryExecutor.Scalar(new LocalQueryRequest
                {
                    Case = case_,
                    Parameters = dbParams
                }, this);

                if (queryResult == null)
                {
                    result.out_err = $"No handler found for case '{case_}'";
                    return result;
                }

                result.out_value_bool = queryResult.out_value_bool;
                result.out_value_str = queryResult.out_value_str;
                result.out_value_int = queryResult.out_value_int;
                result.out_err = queryResult.out_err;
            }
            catch (Exception ex)
            {
                result.out_err = $"LocalStorage Scalar Error: {ex.Message}";
            }

            return result;
        }
        
        /// <inheritdoc />
        public async Task<ScalarModel> ExecQuery(Dictionary<string, string> dbParams)
        {
            // Since ExecQuery is functionally identical to Save(both modify data),
            // we simply forward the request directly to the Save logic.
            return await Save(dbParams);
        }

        /// <inheritdoc />
        public async Task<ReaderModel<T>> Read<T>(Dictionary<string, string> dbParams) where T : new()
        {
            var result = new ReaderModel<T>
            {
                out_list = new List<T?>()
            };

            try
            {
                var case_ = dbParams.GetValueOrDefault("@Case_", "");
                if (string.IsNullOrWhiteSpace(case_))
                    return result;

                var queryResult = await _queryExecutor.Read(new LocalQueryRequest
                {
                    Case = case_,
                    Parameters = dbParams
                }, this);

                if (queryResult == null || !queryResult.success)
                {
                    result.out_err = queryResult?.out_err ?? $"No handler found for case '{case_}'";
                    return result;
                }

                // ============================================================
                // DIREKTE KONVERTIERUNG (wie im DAM) – KEIN JSON!
                // ============================================================
                if (queryResult.out_list != null && queryResult.out_list.Count > 0)
                {
                    var typedList = new List<T?>(queryResult.out_list.Count);
                    foreach (var item in queryResult.out_list)
                    {
                        // Direkter Typ-Check – AOT-sicher, kein Reflection, kein JSON
                        typedList.Add(item is T typedItem ? typedItem : default);
                    }
                    result.out_list = typedList;
                }
                else
                {
                    result.out_list = new List<T?>();
                }

                result.out_data = result.out_list.FirstOrDefault();
                result.out_err = queryResult.out_err;
            }
            catch (Exception ex)
            {
                result.out_err = $"LocalStorage Read Error: {ex.Message}";
            }

            return result;
        }

        // =====================================================================
        // Purge & Reset (Level 1-3)
        // =====================================================================

        /// <inheritdoc />
        public async Task<ScalarModel> ClearAllData()
        {
            ScalarModel result = new();

            try
            {
                foreach (var key in RamCache.Keys.ToList())
                {
                    RamCache[key] = new List<object>();
                }

                IsInitialized = false;

                result.out_value_bool = true;
                result.out_value_str = "deleted:0:0";
            }
            catch (Exception ex)
            {
                result.out_err = $"LocalStorage ClearAllData Error: {ex.Message}";
                result.out_value_bool = false;
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<ScalarModel> DropAllTables()
        {
            ScalarModel result = new();

            try
            {
                RamCache.Clear();
                IsInitialized = false;

                result.out_value_bool = true;
                result.out_value_str = "deleted:0:0";
            }
            catch (Exception ex)
            {
                result.out_err = $"LocalStorage DropAllTables Error: {ex.Message}";
                result.out_value_bool = false;
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<ScalarModel> DeleteDB()
        {
            ScalarModel result = new();

            try
            {
                RamCache.Clear();
                IsInitialized = false;

                result.out_value_bool = true;
                result.out_value_str = "deleted:0:0";
            }
            catch (Exception ex)
            {
                result.out_err = $"LocalStorage DeleteDB Error: {ex.Message}";
                result.out_value_bool = false;
            }

            return result;
        }
    }
}