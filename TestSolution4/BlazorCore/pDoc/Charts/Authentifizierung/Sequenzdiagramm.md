%% https://mermaid.live/

sequenceDiagram
    participant M as MAUI App (Client)
    participant B as System-Browser
    participant A as Web-API (Server)
    participant D as SQL-Datenbank
    participant P as Identity Provider (Google)

    Note over M, A: 1. Vorbereitung & Start
    M->>M: Erzeuge PollingID (GUID)
    M->>B: Öffne Browser mit Base64-State
    
    par Polling Loop (Parallel)
        M->>A: Polling: Gibt es Token für PollingID?
        A->>D: SELECT WebApiToken WHERE IdPClientIdent = PollingID
        D-->>A: Result: NULL
        A-->>M: Warte weiter...
    and Auth Workflow
        B->>A: GET /auth/external
        A-->>B: HTML Spinner (Desktop UX)
        B->>A: GET /auth/external/start
        A->>P: Challenge (Redirect zu Google)
        P->>B: Login Maske anzeigen
        B->>P: User Authentifizierung
        P-->>A: Callback mit Auth-Code
    end

    Note over A, D: 2. Ticket Verarbeitung
    A->>A: OnCreatingTicket: Hash Email/Sub
    A->>A: Generiere JWT & UnixTS
    A->>D: UPDATE AuthUsers SET IdPToken = JWT WHERE ...
    A-->>B: Redirect /auth/close-browser
    
    Note over M, D: 3. Finalisierung
    M->>A: Polling: Gibt es Token?
    A->>D: SELECT WebApiToken...
    D-->>A: JWT gefunden
    A-->>M: Return JWT & UnixTS
    M->>M: Speichere JWT lokal & Login beendet