%% https://mermaid.live/

sequenceDiagram
    participant L as Lokal (Realm)
    participant D as DAM Service (Logik)
    participant C as Cloud (Web-API)

    Note over L, C: Start Synchronisation
    L->>D: Sende ID & LastUpdateUnixTS
    D->>C: Anfrage: Ist Cloud-Version neuer?
    
    alt Cloud ist neuer
        C-->>D: Sende Cloud-Daten
        D->>L: Update Lokal (Download)
    else Lokal ist neuer
        D->>L: Setze Flag: Int__MigrationToMSSQL
        Note right of D: Zeitstempel wird eingefroren
        L->>C: Upload Daten
        C-->>L: Quittung erhalten
    end
    
    Note over L, C: Beide Seiten sind nun identisch (In Sync)