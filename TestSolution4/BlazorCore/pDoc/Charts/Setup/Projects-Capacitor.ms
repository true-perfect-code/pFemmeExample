%% https://mermaid.live/

graph TD
    %% Global Framework & Base Libraries
    subgraph Global_Framework [Global Framework]
        P11[NuGet: p11 UI Library]
        PE[RCL: pE Core Logic <br/>& Sync Service]
    end

    %% Customer Specific Solution
    subgraph Customer_App_Solution [Customer App Solution]
        AppShared[RCL: App-Shared Business Logic]
        
        subgraph Platforms [Platform Projects]
            WPF[WPF Blazor Desktop]
            Server[Blazor Server Web]
            WASM[WASM + Capacitor Mobile]
        end
    end

    %% Storage & Infrastructure
    DB_Local_PC[(Local SQLite: PC File System)]
    DB_Local_Mobile[(Local SQLite: Native Sandbox)]
    
    API[ASP.NET Minimal API Proxy]
    DB_Central[(Central MSSQL Database)]

    %% Dependencies
    P11 --> PE
    PE --> AppShared
    AppShared --> WPF
    AppShared --> Server
    AppShared --> WASM

    %% Database Connections & Sync Flow
    WPF <-->|Local Data Access| DB_Local_PC
    WASM <-->|Local Data Access| DB_Local_Mobile
    
    %% Synchronization Paths
    WASM <==>|Polling-Bridge & Data Sync| API
    WPF <==>|Data Sync| API
    Server -.->|Direct API Access| API
    
    API <-->|Main Data Flow| DB_Central

    %% Styling
    style P11 fill:#f9f,stroke:#333,stroke-width:2px
    style PE fill:#bbf,stroke:#333,stroke-width:2px
    style API fill:#dfd,stroke:#333,stroke-width:2px
    style WASM fill:#ffd,stroke:#333,stroke-width:2px
    style DB_Local_PC fill:#eee,stroke:#333
    style DB_Local_Mobile fill:#eee,stroke:#333
    style DB_Central fill:#fff4dd,stroke:#d4a017,stroke-width:2px