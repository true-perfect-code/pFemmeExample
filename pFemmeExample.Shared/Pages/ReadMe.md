
Unter Page findet man folgende folder und Dateien:
- Components => hier sollen wir alle unsere neu erstellten App-Formulare (Komponente) platzieren
- Help => hier befinden sich die standard Help-Formulare (Komponenten). Neu fügt man hier hinzu.
- die Page Home.razor ist die root-Page und beinhaltet in der Regel eine Weiche zwischen der App und Landingpage
- die Landingpage ist die Webseite der App und beinhaltet eine Presentation wie auch Login zur App
- die Pages: Imprint.razor, Privacy.razor und Terms.razor sind standardmässig immer drin, können aber bei Bedarf angepasst werden

Todo's:
- Falls MSSQL benutzt werden soll, dann in BlazorCore >...> GlobalStateBase > GetConnectionString() das Passwort für JSON file [YOUR_PROJECT_NAME].json ermitteln (siehe Beschreibung unter 'aes')
- Unter Shared > Global > StateInit.cs die entsprechende einstellungen kontrollieren/anpassen