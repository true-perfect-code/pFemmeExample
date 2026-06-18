namespace BlazorCore.Services.SqLite
{
    public enum DB_STATUS
    {
        ERROR = 0,
        INITIALIZING = 1,
        READY = 2,
        NEW = 3,
        NOT_CONNECTED = 4  // Eindeutig: Die Bridge hat noch keine DB-Referenz
    }

    public enum QUERY_TYPE
    {
        scalar = 0,
        execute = 1, // NonQuery
        query = 2, // Reader
        deleteDatabase = 3,
        dropAllTables = 4,
        clearAllData = 5,
        setVersion = 6,
        getVersion = 7,
        executeBatch = 8,
        initConnection = 9,
        getDatabaseStatus = 10,
    }

    /// <summary>
    /// Contains SQL definitions for database initialization and default values.
    /// Tables are optimized for SQLite (e.g., BLOB for binaries, INTEGER for booleans).
    /// </summary>
    public static class SqLiteModel
    {
        public const int CurrentSchemaVersion = 2; // Erhöhen bei jedem Update

        // Migrationen sammeln
        public static readonly Dictionary<int, string> Migrations = new()
        {
            { 2, "ALTER TABLE AppParameter ADD COLUMN NewSetting TEXT;" },
            { 3, "CREATE TABLE IF NOT EXISTS UserNotes (...);" }
        };

        /// <summary>
        /// SQL Script to create all necessary local tables if they do not exist.
        /// Excluding AppMessages as requested.
        /// </summary>
        public static readonly string CreateTablesScript = """
            -- AuthUsers Table
            CREATE TABLE IF NOT EXISTS AuthUsers (
                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                UnixTS TEXT NOT NULL DEFAULT '',
                EmailHash TEXT NOT NULL,
                PasswordHash TEXT NOT NULL,
                active INTEGER NOT NULL DEFAULT 0,
                TermsAccepted INTEGER NOT NULL DEFAULT 0,
                IdP TEXT NULL,
                IdPClientIdent TEXT NULL,
                IdPToken TEXT NULL,
                otp BLOB NULL,
                LastLogin TEXT NULL,
                UserRole TEXT NULL,
                LastUpdateUnixTS INTEGER NULL,
                FailedLoginAttempts INTEGER NOT NULL DEFAULT 0
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_AuthUsers_UnixTS ON AuthUsers (UnixTS);
            CREATE INDEX IF NOT EXISTS IX_AuthUsers_Email ON AuthUsers (EmailHash);

            -- AppParameter Table
            CREATE TABLE IF NOT EXISTS AppParameter (
                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                UnixTS TEXT NOT NULL DEFAULT '',
                AuthUsers_UnixTS TEXT NOT NULL DEFAULT '',
                ParameterName TEXT NULL,
                ParameterValue TEXT NULL,
                Details TEXT NULL,
                Scope TEXT NULL,
                LastUpdateUnixTS INTEGER DEFAULT 0
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_AppParameter_UnixTS ON AppParameter (UnixTS);
            CREATE INDEX IF NOT EXISTS IX_AppParameter_AuthUsers_UnixTS ON AppParameter (AuthUsers_UnixTS);

            -- AuthUsersExtend Table
            CREATE TABLE IF NOT EXISTS AuthUsersExtend (
                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                UnixTS TEXT NOT NULL DEFAULT '',
                AuthUsers_UnixTS TEXT NOT NULL DEFAULT '',
                DisplayName TEXT DEFAULT '',
                imgJpegThumbnail BLOB NULL,
                LastUpdateUnixTS INTEGER DEFAULT 0
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_AuthUsersExtend_UnixTS ON AuthUsersExtend (UnixTS);
            CREATE INDEX IF NOT EXISTS IX_AuthUsersExtend_AuthUsers_UnixTS ON AuthUsersExtend (AuthUsers_UnixTS);

            -- Todo Table
            CREATE TABLE IF NOT EXISTS Todo (
                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                UnixTS TEXT NOT NULL DEFAULT '',
                AuthUsers_UnixTS TEXT NOT NULL DEFAULT '',
                DisplayName TEXT NOT NULL,
                IsChecked INTEGER NOT NULL DEFAULT 0,
                Tasks TEXT NULL,
                IsNotifyActivated INTEGER NOT NULL DEFAULT 0,
                --RecordDateTime TEXT NULL,
                RecordDateTimeUnix INTEGER NOT NULL DEFAULT 0,
                CategoryColor TEXT NOT NULL DEFAULT '#dededc',
                LastUpdateUnixTS INTEGER DEFAULT 0
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_Todo_UnixTS ON Todo (UnixTS);
            CREATE INDEX IF NOT EXISTS IX_Todo_AuthUsers_UnixTS ON Todo (AuthUsers_UnixTS);

            -- Tasks Table
            CREATE TABLE IF NOT EXISTS Tasks (
                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                UnixTS TEXT NOT NULL DEFAULT '',
                AuthUsers_UnixTS TEXT NOT NULL DEFAULT '',
                DisplayName TEXT NOT NULL,
                IsChecked INTEGER NOT NULL DEFAULT 0,
                imgJpeg BLOB NULL,
                imgJpegThumbnail BLOB NULL,
                Todo_UnixTS TEXT NOT NULL DEFAULT '',
                LastUpdateUnixTS INTEGER DEFAULT 0
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_Tasks_UnixTS ON Tasks (UnixTS);
            CREATE INDEX IF NOT EXISTS IX_Tasks_AuthUsers_UnixTS ON Tasks (AuthUsers_UnixTS);
            CREATE INDEX IF NOT EXISTS IX_Tasks_Todo_UnixTS ON Tasks (Todo_UnixTS);

            -- SharingUsers Table
            CREATE TABLE IF NOT EXISTS SharingUsers (
                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                UnixTS TEXT NOT NULL DEFAULT '',
                AuthUsers_UnixTS TEXT NOT NULL DEFAULT '',
                AuthUsers_ShareTo_UnixTS TEXT NOT NULL DEFAULT '',
                SharingStatus INTEGER NOT NULL DEFAULT 0,
                LastUpdateUnixTS INTEGER DEFAULT 0
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_SharingUsers_UnixTS ON SharingUsers (UnixTS);
            CREATE INDEX IF NOT EXISTS IX_SharingUsers_AuthUsers_UnixTS ON SharingUsers (AuthUsers_UnixTS);

            -- AuthUsersTodo Table
            CREATE TABLE IF NOT EXISTS AuthUsersTodo (
                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                UnixTS TEXT NOT NULL DEFAULT '',
                AuthUsers_UnixTS TEXT NOT NULL DEFAULT '',
                AuthUsers_ShareFrom_UnixTS TEXT NOT NULL DEFAULT '',
                DisplayName TEXT NOT NULL,
                Todo_UnixTS TEXT NOT NULL DEFAULT '',
                LastUpdateUnixTS INTEGER DEFAULT 0
            );
            CREATE INDEX IF NOT EXISTS IX_AuthUsersTodo_AuthUsers_UnixTS ON AuthUsersTodo (AuthUsers_UnixTS);
            CREATE INDEX IF NOT EXISTS IX_AuthUsersTodo_AuthUsers_ShareFrom_UnixTS ON AuthUsersTodo (AuthUsers_ShareFrom_UnixTS);
            CREATE INDEX IF NOT EXISTS IX_AuthUsersTodo_Todo_UnixTS ON AuthUsersTodo (Todo_UnixTS);
            """;

        /// <summary>
        /// SQL Script to insert default application parameters if they are missing.
        /// SQLite uses 'INSERT OR IGNORE' or simple 'INSERT' for initialization.
        /// </summary>
        public static readonly string InsertDefaultParametersScript = """
            INSERT OR IGNORE INTO AppParameter (ID, UnixTS, AuthUsers_UnixTS, ParameterName, ParameterValue, Details, Scope, LastUpdateUnixTS) 
            VALUES (1, '1', '0', 'StoreUrl_Microsoft', 'https://apps.microsoft.com/search/publisher?name=True%20Perfect%20Code', 'bi bi-windows', 'app', 0);
            
            INSERT OR IGNORE INTO AppParameter (ID, UnixTS, AuthUsers_UnixTS, ParameterName, ParameterValue, Details, Scope, LastUpdateUnixTS) 
            VALUES (2, '2', '0', 'StoreUrl_Google', 'https://play.google.com/store/apps/developer?id=True+Perfect+Code', 'bi bi-android', 'app', 0);
            
            INSERT OR IGNORE INTO AppParameter (ID, UnixTS, AuthUsers_UnixTS, ParameterName, ParameterValue, Details, Scope, LastUpdateUnixTS) 
            VALUES (3, '3', '0', 'StoreUrl_ApplePhone', 'https://apps.apple.com/us/developer/daniel-simic/id1733470934', 'bi bi-phone', 'app', 0);
            
            INSERT OR IGNORE INTO AppParameter (ID, UnixTS, AuthUsers_UnixTS, ParameterName, ParameterValue, Details, Scope, LastUpdateUnixTS) 
            VALUES (4, '4', '0', 'StoreUrl_AppleMac', 'https://apps.apple.com/us/developer/daniel-simic/id1733470934', 'bi bi-apple', 'app', 0);
            
            INSERT OR IGNORE INTO AppParameter (ID, UnixTS, AuthUsers_UnixTS, ParameterName, ParameterValue, Details, Scope, LastUpdateUnixTS) 
            VALUES (5, '5', '0', 'StoreUrl_Web', 'https://pmunus.de', 'bi bi-globe2', 'app', 0);

            INSERT OR IGNORE INTO AppParameter (ID, UnixTS, AuthUsers_UnixTS, ParameterName, ParameterValue, Details, Scope, LastUpdateUnixTS) 
            VALUES (6, '6', '0', 'StoreUrl_Web', 'https://pwa.pmunus.de', 'bi bi-window-dock', 'app', 0);

            INSERT OR IGNORE INTO AppParameter (ID, UnixTS, AuthUsers_UnixTS, ParameterName, ParameterValue, Details, Scope, LastUpdateUnixTS) 
            VALUES (7, '7', '0', 'StoreUrl_Web', 'https://portable.pmunus.de', 'bi bi-filetype-exe', 'app', 0);
            """;
    }
}

