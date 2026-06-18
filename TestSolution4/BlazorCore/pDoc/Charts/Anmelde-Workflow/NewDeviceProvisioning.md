%% https://mermaid.live/

graph TD
    A[App Start auf neuem Gerät] --> B[Login gegen Cloud Web-API]
    B --> C{Erfolgreich?}
    C -- Ja --> D[Empfange User-Profil + UnixTS + Settings]
    D --> E[Erstelle verschlüsselte Realm-Datei lokal]
    E --> F[Schreibe User-Stammbaum in Realm]
    F --> G[Starte Full-Sync für Feature-Daten]
    G --> H[App ist bereit & offline-fähig]
    
    style E fill:#e1f5fe,stroke:#01579b,stroke-width:2px