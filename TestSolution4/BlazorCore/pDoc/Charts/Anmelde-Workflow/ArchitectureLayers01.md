%% https://mermaid.live/

graph TD
    %% Definition der Schichten
    subgraph UI_Layer [UI Schicht - MAUI/Blazor]
        A[View / Page] --> B[ViewModel]
    end

    subgraph Logic_Layer [Business Logic - Services]
        B --> C[Feature Service z.B. TodoService]
        C --> D[DAM Service - Data Access Manager]
        D --- N[Hier liegt dein Compare-Algorithmus]
    end

    subgraph Storage_Layer [Data Layer]
        D --> E{Entscheidung}
        E -- Cloud_Local --> F[MSSQL / Web-API]
        E -- Local_Only --> G[Realm DB - verschlüsselt]
        F -.-> |Background Sync| G
    end

    %% Styling der Elemente
    style D fill:#f9f,stroke:#333,stroke-width:4px
    style N fill:#fffde7,stroke:#ffd600,stroke-dasharray: 5 5,color:#000