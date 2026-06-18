%% https://mermaid.live/

sequenceDiagram
    participant M as MAUI Client
    participant A as Web-API (Authorize)

    M->>M: Polling erfolgreich (JWT erhalten)
    M->>M: Speichere JWT in SecureStorage / ClientStorage
    M->>A: GET /api/check (Header: Bearer JWT)
    
    alt Token Valide
        A-->>M: 200 OK
        M->>M: Setze App-Status auf 'Eingeloggt'
    else Token Ungültig / Abgelaufen
        A-->>M: 401 Unauthorized
        M->>M: Lösche lokales Token, zeige Error
    end