using BlazorCore.Services.LocalStorage;
using System;
using System.Collections.Generic;

namespace BlazorCore.Services.LocalQueryExecutor
{
    internal class LocalQueryExecutorModel
    {
        // Platzhalter für zukünftige Erweiterungen
    }

    // =========================
    // REQUEST (INPUT)
    // =========================
    public class LocalQueryRequest
    {
        public string Case { get; set; } = string.Empty;

        public Dictionary<string, string> Parameters { get; set; } = new();

        // Zugriff auf LocalStorage (RAM Cache)
        public ILocalStorage? Storage { get; set; }
    }

    // =========================
    // CONTEXT (RUNTIME)
    // =========================
    public class LocalQueryContext
    {
        public string Case { get; set; } = string.Empty;

        public Dictionary<string, string> Parameters { get; set; } = new();

        // Zugriff auf LocalStorage (RAM Cache)
        public ILocalStorage? Storage { get; set; }

        // Für zukünftige Erweiterungen:
        // - IJsonPersistence? JsonPersister { get; set; }
        // - ISqLiteBase? SqlitePersister { get; set; }
        // - LOCAL_STORAGE_TYPE ActiveStorageType { get; set; }

        // Pipeline / Middleware / Debug / Cache / Logging
        public Dictionary<string, object> Items { get; set; } = new();

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    }

    // =========================
    // RESULT (CORE RESPONSE)
    // =========================
    public class LocalQueryResult
    {
        public List<object?> out_list { get; set; } = new();

        public object? out_data { get; set; }

        public bool success { get; set; } = true;

        public string? out_err { get; set; }
    }
}