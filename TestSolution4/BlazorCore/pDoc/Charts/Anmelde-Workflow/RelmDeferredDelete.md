%% https://mermaid.live/

graph TD
    A[User fordert Account-Löschung] --> B[Markierung in App-Preferences: DeleteRequested = true]
    B --> C[Sofortiger Logout & UI-Reset]
    C --> D{App Neustart / MauiProgram.cs}
    D --> E{Prüfe Preferences: DeleteRequested?}
    E -- Ja --> F[Physisches Löschen der Realm-Datei & Cache]
    F --> G[Reset Preferences & Initialer Boot]
    E -- Nein --> H[Normaler App-Start]
    G --> H