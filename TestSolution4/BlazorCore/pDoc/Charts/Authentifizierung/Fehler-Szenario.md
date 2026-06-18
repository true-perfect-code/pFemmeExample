%% https://mermaid.live/

sequenceDiagram
    participant U as User
    participant B as Browser
    participant A as Web-API
    participant M as MAUI App

    U->>B: Klickt 'Abbrechen' bei Google
    B->>A: Meldet Failure (Callback)
    A->>A: OnRemoteFailure ausgelöst
    A-->>B: Rendert Fehler-HTML (Close Window Button)
    
    Note over M: Polling läuft weiter bis Timeout
    M->>M: maxSeconds erreicht (z.B. 30s)
    M-->>U: ShowYesNoAsync (Verifizierung fortsetzen?)
    
    alt User klickt JA
        M->>M: Polling Loop startet von vorn
    else User klickt NEIN
        M->>M: Polling stoppt, Login abgebrochen
    end