%% https://mermaid.live/

graph TD
    subgraph MAUI_CLIENT [MAUI Client]
        S1[PollingID] --> S2["Base64 State: Platform | IdP | PollingID"]
    end

    subgraph WEB_API [Web-API]
        S2 --> S3{/auth/external}
        S3 --> S4[AuthenticationProperties]
        S4 --> S5[Encrypted Auth-Cookie]
        S5 --> S6[OnCreatingTicket Event]
    end

    subgraph DB [SQL Datenbank]
        S6 --> S7[(Tabelle: AuthUsers)]
        S7 --> S8[Spalte: IdPClientIdent = PollingID]
        S7 --> S9[Spalte: IdPToken = JWT]
    end

    S1 -.->|Wartet auf| S9