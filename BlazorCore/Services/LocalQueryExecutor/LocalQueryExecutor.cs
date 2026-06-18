using BlazorCore.Services.Dam;
using BlazorCore.Services.LocalStorage;
using BlazorCore.Services.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlazorCore.Services.LocalQueryExecutor
{
    /// <summary>
    /// NEUER QueryExecutor für die vereinheitlichte Storage-Architektur.
    /// </summary>
    public class LocalQueryExecutor : ILocalQueryExecutor
    {
        // =========================
        // HANDLERS
        // =========================
        private readonly Dictionary<string, Func<LocalQueryContext, ScalarModel>> _scalarHandlers = new();
        private readonly Dictionary<string, Func<LocalQueryContext, LocalQueryResult>> _readHandlers = new();
        private readonly Dictionary<string, Func<LocalQueryContext, Task<ScalarModel>>> _saveHandlers = new();
        private readonly Dictionary<string, Func<LocalQueryContext, Task<ClientStorageModel>>> _authHandlers = new();

        // =========================
        // REGISTER SCALAR
        // =========================
        public void RegisterScalar(string key, Func<LocalQueryContext, ScalarModel> handler)
        {
            _scalarHandlers[key] = handler;
        }

        // =========================
        // REGISTER READ
        // =========================
        public void RegisterRead(string key, Func<LocalQueryContext, LocalQueryResult> handler)
        {
            _readHandlers[key] = handler;
        }

        // =========================
        // REGISTER SAVE (async)
        // =========================
        public void RegisterSave(string key, Func<LocalQueryContext, Task<ScalarModel>> handler)
        {
            _saveHandlers[key] = handler;
        }

        // =========================
        // REGISTER AUTH
        // =========================
        public void RegisterAuth(string key, Func<LocalQueryContext, Task<ClientStorageModel>> handler)
        {
            _authHandlers[key] = handler;
        }

        // =========================
        // SCALAR EXECUTION
        // =========================
        public Task<ScalarModel> Scalar(LocalQueryRequest request, ILocalStorage storage)
        {
            if (request == null)
                return Task.FromResult(new ScalarModel { out_err = "Request is null" });

            if (!_scalarHandlers.TryGetValue(request.Case, out var handler))
                return Task.FromResult(new ScalarModel { out_err = $"No scalar handler: {request.Case}" });

            var ctx = new LocalQueryContext
            {
                Case = request.Case,
                Parameters = request.Parameters ?? new(),
                Storage = storage
            };

            try
            {
                return Task.FromResult(handler(ctx));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ScalarModel { out_err = ex.Message });
            }
        }

        // =========================
        // READ EXECUTION
        // =========================
        public Task<LocalQueryResult> Read(LocalQueryRequest request, ILocalStorage storage)
        {
            if (request == null)
                return Task.FromResult(new LocalQueryResult
                {
                    success = false,
                    out_err = "Request is null"
                });

            if (!_readHandlers.TryGetValue(request.Case, out var handler))
                return Task.FromResult(new LocalQueryResult
                {
                    success = false,
                    out_err = $"No read handler: {request.Case}"
                });

            var ctx = new LocalQueryContext
            {
                Case = request.Case,
                Parameters = request.Parameters ?? new(),
                Storage = storage
            };

            try
            {
                var result = handler(ctx);

                if (result == null)
                {
                    return Task.FromResult(new LocalQueryResult
                    {
                        success = false,
                        out_err = "Handler returned null"
                    });
                }

                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                return Task.FromResult(new LocalQueryResult
                {
                    success = false,
                    out_err = ex.Message
                });
            }
        }

        // =========================
        // SAVE EXECUTION (async)
        // =========================
        public async Task<ScalarModel> Save(LocalQueryRequest request, ILocalStorage storage) // ← GEÄNDERT: async Task
        {
            if (request == null)
                return new ScalarModel { out_err = "Request is null" };

            if (!_saveHandlers.TryGetValue(request.Case, out var handler))
                return new ScalarModel { out_err = $"No save handler: {request.Case}" };

            var ctx = new LocalQueryContext
            {
                Case = request.Case,
                Parameters = request.Parameters ?? new(),
                Storage = storage
            };

            try
            {
                // ← GEÄNDERT: await handler
                return await handler(ctx);
            }
            catch (Exception ex)
            {
                return new ScalarModel { out_err = ex.Message };
            }
        }

        // =========================
        // AUTH EXECUTION
        // =========================
        public async Task<ClientStorageModel> GetTokenTPC(LocalQueryRequest request, ILocalStorage storage)
        {
            if (request == null)
                return new ClientStorageModel { out_err = "Request is null" };

            if (!_authHandlers.TryGetValue(request.Case, out var handler))
                return new ClientStorageModel { out_err = $"No auth handler: {request.Case}" };

            var ctx = new LocalQueryContext
            {
                Case = request.Case,
                Parameters = request.Parameters ?? new(),
                Storage = storage
            };

            try
            {
                return await handler(ctx);
            }
            catch (Exception ex)
            {
                return new ClientStorageModel { out_err = ex.Message };
            }
        }
    }
}