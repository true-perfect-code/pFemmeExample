%% https://mermaid.live/

graph TD
    %% Global Framework & Base Libraries
    subgraph Global_Framework [Global Framework]
        P11[NuGet: p11 UI Library]
        PE[RCL: pE Core Logic <br/>& Sync Service]
    end

    %% Customer Specific Solution
    subgraph Customer_App_Solution [App Solution: pMunus]
        AppShared[RCL: App-Shared Business Logic]
        
        subgraph Platforms [Platform Projects]
            MAUI[<b>MAUI Blazor App</b><br/>Multi-Platform Host<br/><i>Android, iOS, macOS, Windows</i>]
            Server[Blazor Server Web]
        end
    end

    %% Storage & Infrastructure
    DB_Local_MAUI[(Local SQLite: Native Device Storage)]
    
    API[ASP.NET Minimal API Proxy]
    DB_Central[(Central MSSQL Database)]

    %% Dependencies
    P11 --> PE
    PE --> AppShared
    AppShared --> MAUI
    AppShared --> Server

    %% Database Connections & Sync Flow
    MAUI <-->|Local Data Access<br/>SQLite-net-pcl| DB_Local_MAUI
    
    %% Synchronization Paths
    MAUI <==>|Polling-Bridge & Data Sync| API
    Server -.->|Direct API Access| API
    
    API <-->|Main Data Flow| DB_Central

    %% Styling
    style P11 fill:#f9f,stroke:#333,stroke-width:2px
    style PE fill:#bbf,stroke:#333,stroke-width:2px
    style API fill:#dfd,stroke:#333,stroke-width:2px
    style MAUI fill:#512bd4,color:#fff,stroke:#333,stroke-width:2px
    style DB_Local_MAUI fill:#eee,stroke:#333
    style DB_Central fill:#fff4dd,stroke:#d4a017,stroke-width:2px