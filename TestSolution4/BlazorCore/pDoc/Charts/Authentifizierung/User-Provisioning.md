%% https://mermaid.live/

flowchart TD
    Start([OnCreatingTicket]) --> GetSub[Extrahiere 'sub' vom IdP]
    GetSub --> Hash[Erzeuge EmailHash & PasswordHash]
    Hash --> CheckUser{Existiert User in DB?}
    
    CheckUser -- Nein --> Register[Register >> AuthUsers: Erzeuge neuen User]
    CheckUser -- Ja --> GetUnixTS[Lade existierenden UnixTS / User-Daten]
    
    Register --> CreateJWT[JwtHelper: Erzeuge Token mit UnixTS]
    GetUnixTS --> CreateJWT
    
    CreateJWT --> UpdateDB[UpdateIdPToken: JWT + PollingID in DB speichern]
    UpdateDB --> Redirect[Redirect zu /auth/close-browser]
    Redirect --> End([Ende])