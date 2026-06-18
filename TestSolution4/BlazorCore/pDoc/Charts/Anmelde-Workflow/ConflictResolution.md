%% https://mermaid.live/

graph TD
    A[Sync Start: Datensatz ID 123] --> B{Vergleiche LastUpdateUnixTS}
    
    B -- Cloud TS > Lokal TS --> C[Cloud gewinnt]
    C --> D[Überschreibe Realm mit Cloud-Daten]
    
    B -- Lokal TS > Cloud TS --> E[Lokal gewinnt]
    E --> F[MigrationToMSSQL Flag setzen]
    F --> G[Upload zu Web-API / MSSQL]
    
    B -- TS sind identisch --> H[Keine Aktion nötig]
    
    D --> I[Integrität bestätigt]
    G --> I
    H --> I