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
    /// Unterstützt MEMORY und JSON_HYBRID (SQLite folgt später).
    /// Läuft parallel zum bestehenden QueryExecutor.
    /// </summary>
    public interface ILocalQueryExecutor
    {
        // =========================
        // SCALAR
        // =========================
        void RegisterScalar(
            string key,
            Func<LocalQueryContext, ScalarModel> handler);

        Task<ScalarModel> Scalar(LocalQueryRequest request, ILocalStorage storage);

        // =========================
        // READ
        // =========================
        void RegisterRead(
            string key,
            Func<LocalQueryContext, LocalQueryResult> handler);

        Task<LocalQueryResult> Read(LocalQueryRequest request, ILocalStorage storage);

        // =========================
        // SAVE
        // =========================
        //void RegisterSave(
        //    string key,
        //    Func<LocalQueryContext, ScalarModel> handler);
        void RegisterSave(string key, Func<LocalQueryContext, Task<ScalarModel>> handler);

        Task<ScalarModel> Save(LocalQueryRequest request, ILocalStorage storage);

        // =========================
        // AUTH
        // =========================
        void RegisterAuth(string key, Func<LocalQueryContext, Task<ClientStorageModel>> handler);

        Task<ClientStorageModel> GetTokenTPC(LocalQueryRequest request, ILocalStorage storage);

    }
}