%% https://mermaid.live/

graph TD
    A[Start: User Login] --> B{Cloud Login erfolgreich?}
    B -- Nein --> C[Fehlermeldung anzeigen]
    B -- Ja --> D{User lokal vorhanden?}
    
    D -- Nein --> E[Neues Gerät erkannt: CreateLocalAccount]
    E --> F[Login erfolgreich]
    
    D -- Ja --> G{Passwort-Hash korrekt?}
    G -- Ja --> F
    
    G -- Nein --> H{Email & UnixTS Match?}
    H -- Ja --> I[Auto-Repair: Lokalen Hash aktualisieren]
    I --> F
    H -- Nein --> C
    
    F --> J[Gatekeeper: Token in AppState validieren]