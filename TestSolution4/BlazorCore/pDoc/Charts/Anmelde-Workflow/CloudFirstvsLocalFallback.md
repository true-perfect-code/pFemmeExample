%% https://mermaid.live/

sequenceDiagram
    participant UI as Benutzeroberfläche
    participant DAM as DAM Service (Logik)
    participant L as Lokal (Realm)
    participant C as Cloud (Web-API)

    UI->>DAM: Anfrage Daten (Speichermodus: Cloud_Local)
    DAM->>C: Versuche Web-API Request
    alt Cloud erreichbar
        C-->>DAM: Sende Daten
        DAM->>L: Update lokalen Cache (Hintergrund)
        DAM-->>UI: Daten anzeigen (aktuell)
    else Cloud Offline / Timeout
        Note over DAM: Automatischer Fallback
        DAM->>L: Lese lokalen Stand (Offline-Daten)
        L-->>DAM: Daten vorhanden
        DAM-->>UI: Daten anzeigen (Offline-Mode Warnung)
    end