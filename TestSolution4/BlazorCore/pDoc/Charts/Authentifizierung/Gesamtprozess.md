%% https://mermaid.live/

sequenceDiagram
    participant M as MAUI App (Client)
    participant B as System-Browser
    participant A as Web-API (Server)
    participant D as SQL-Datenbank
    participant P as Identity Provider (Google/MS)

    Note over M, A: 1. Initiierung
    M->>M: Generiere PollingID
    M->>B: Öffne /auth/external?state=...
    
    Note over M, D: 2. Warteschleife (Parallel)
    loop Polling alle 1s
        M->>A: GetTokenIDP(PollingID)
        A->>D: Suche JWT für PollingID
        D-->>A: Noch kein Token
        A-->>M: Null / Empty
    end

    Note over B, P: 3. Authentifizierung
    B->>A: Request /auth/external
    A-->>B: Zeige Spinner HTML (nur Desktop)
    B->>A: /auth/external/start
    A->>P: Redirect zu Google/MS Login
    P-->>B: Login-Maske anzeigen
    B->>P: User gibt Daten ein
    P-->>A: Callback mit Auth-Code & State
    
    Note over A, D: 4. Ticket-Erstellung
    A->>A: OnCreatingTicket (Hash UserID)
    A->>A: Generiere JWT & UnixTS
    A->>D: Speichere JWT unter PollingID
    A-->>B: Redirect zu /auth/close-browser
    
    Note over M, D: 5. Erfolg
    M->>A: GetTokenIDP(PollingID)
    A->>D: Suche JWT
    D-->>A: JWT gefunden!
    A-->>M: Sende JWT & UnixTS
    M->>M: Speichere Token & Login Erfolg