
- In der 'CREATE_TABLES.sql' werden neue Tabellen erfasst
	=> daran denken, dass auch: neues Model in pE.DbApp.Models erstellt werden muss
	=> daran denken, dass auch: neue Tabelle in 'pE.DbApp.Services.SQLite.SqLite.cs' bei 'AllTables' hinzugefügt werden muss:
        // Zentraler Ort für alle Tabellennamen
        protected static readonly string[] AllTables = new[]
        {
            "AuthUsers",
            "AppParameter",
            "AuthUsersExtend",
            "SharingUsers",
        };

- nicht vergessen die json-Dateien zur MSSQL Datenbank Verbinung lokal zu erstellen, damit Blazor Server Projekt lokal gestartet werden kann.

PROJECT
	|web
	|		 bin
	|		 |__ Debug
	|		 |     |__ _Connections
	|		 |     |  |__ APPLICATIONNAME.json
	|		 |     |  |__ APPLICATIONNAME.security.config.json
	|		 |	   |
	|		 |     |__ net9.0 / net10.0 / nwt11.0
	|		 |
	|		 |__ Release
	|
	|webapi
	|		 bin
	|		 |__ Debug
	|		 |     |__ _Connections
	|		 |     |  |__ APPLICATIONNAME.json
	|		 |     |  |__ APPLICATIONNAME.security.config.json
	|		 |	   |
	|		 |	   |__ net9.0 / net10.0 / nwt11.0
	|		 |
	|		 |__ Release

Beispiele:

Der Json Dateiname (hier 'APPLICATIONNAME') wird von der globalen static Eigenschaft Appl.ApplicationName (statische Klasse Utility -> Apple.cs), ermittelt.
Database: Ermittlung erfolgt über BlazorCode.Utility.Appl.ApplicationName => 'tpcdb_' + [ApplicationName]
Database: Ermittlung erfolgt über BlazorCode.Utility.Appl.ApplicationName => 'tpcuser_' + [ApplicationName]
Password: Ermittlung erfolgt in 'BlazorCode.Services.GlobalState.GetConnectionString()' unter 'using (Security aes = new())' -> deterministisch verschlüsseln!
---------------------------------------
Justin-PC
{
  "Server": "DESKTOP\\SQLEXPRESS",
  "Database": "tpcdb_APPLICATIONNAME",
  "User_ID": "tpcuser_APPLICATIONNAME",
  "Password": "fEoGZg6iXtq+LIW0kiiWTONmg",
  "Integrated_Security": false,
  "Pooling": true,
  "TrustServerCertificate": true
}

Sasa-PC
{
  "Server": "DESKTOP\\SQLEXPRESS",
  "Database": "tpcdb_APPLICATIONNAME",
  "User_ID": "tpcuser_APPLICATIONNAME",
  "Password": "Qtc6fEoGZg6iXtq+LIW0kiiWTONmg",
  "Integrated_Security": false,
  "Pooling": true,
  "TrustServerCertificate": true
}

Pepperwert bestimmen: Wird EINMALIG am besten übe rwebApi Projekt Datei 'Endpoints.cs' bestimmt, siehe:
                // Blazor Server / WebApi ASP.NET
                //using (var aes = new SecurityServer())
                //{
                //    string encryptedPepper = aes.GenerateEncryptedPepper();
                //    // ACHTUNG: Blazor WASM Pepper muss in Wasm Program.cs erstellt werden !!!
                //}
Den ermittelten Pepper 'encryptedPepper' in die Datei 'APPLICATIONNAME.security.config.json' speichern (Formatzusammensetzung: [ApplicationName] + '.' + security.config.json)
--------------------
{
  "Pepper": "dPFjpD8P6UmRurWa1Pvo18yav2tVv3qSwu/0G9assrFZf22"
}


Weitere Infos:
--------------
- Mit 'DROP.sql' können die MSSQL Tabellen gelöscht werden
- Mit 'INSERT_DATA.sql' können die Testdaten impoertiert werden in die MSSQL DB auf dem Cloud (oder lokal beim Entwickeln)
- In 'CyclesData.sql' bedinen sich Daten, die man lokal in Json-files importieren kann mit dem PS 'INSERT_DATA_LOCAL.ps1' (siehe dort die Beschreibung)
- in der Datei 'LocalDbQueryRegistry.cs' befindet sich Query-Executor für die lokale Abfragen (ist ein Gegenstück zu T-SQL)