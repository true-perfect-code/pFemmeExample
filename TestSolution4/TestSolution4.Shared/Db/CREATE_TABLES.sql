
	DECLARE @TABLESCHEMA VARCHAR(50) = 'dbuser_testsolution4'
	--------------------------------------------------------------------------------------------------------------------------------------------------------
	-- TABELLEN
	--------------------------------------------------------------------------------------------------------------------------------------------------------
	DECLARE @TblCreated_AuthUsers BIT = 0
	IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @TABLESCHEMA AND  TABLE_NAME = 'AuthUsers'))
	BEGIN
		SET @TblCreated_AuthUsers = 1

		SET ANSI_NULLS ON
		SET QUOTED_IDENTIFIER ON
		CREATE TABLE [dbuser_testsolution4].[AuthUsers](
			[ID] [int] IDENTITY(1,1) NOT NULL,
			[UnixTS] [varchar](35) COLLATE Latin1_General_BIN NOT NULL,
			[EmailHash] [nvarchar](256) NOT NULL,
			[PasswordHash] [nvarchar](256) NOT NULL,
			[active] [bit] NOT NULL,
			[TermsAccepted] [bit] NOT NULL,
			[IdP] [nvarchar](128) NULL,
			[IdPClientIdent] [nvarchar](128) NULL,
			[IdPToken] [nvarchar](1024) NULL,
			[otp] [VARBINARY](MAX) NULL,
			[LastLogin] [DATETIMEOFFSET] NULL,
			[UserRole] [nvarchar](32) NULL,
			[LastUpdateUnixTS] [BIGINT] NULL,
			[FailedLoginAttempts] [int] NOT NULL,
		 CONSTRAINT [PK_AuthUsers] PRIMARY KEY CLUSTERED 
		(
			[ID] ASC
		)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = ON) ON [PRIMARY]
		) ON [PRIMARY]

		ALTER TABLE [dbuser_testsolution4].[AuthUsers] ADD  CONSTRAINT [DF_AuthUsers_UnixTS]  DEFAULT (('')) FOR [UnixTS]
		ALTER TABLE [dbuser_testsolution4].[AuthUsers] ADD  CONSTRAINT [DF_AuthUsers_active]  DEFAULT ((0)) FOR [active]
		ALTER TABLE [dbuser_testsolution4].[AuthUsers] ADD  CONSTRAINT [DF_AuthUsers_TermsAccepted]  DEFAULT ((0)) FOR [TermsAccepted]
		ALTER TABLE [dbuser_testsolution4].[AuthUsers] ADD  CONSTRAINT [DF_AuthUsers_FailedLoginAttempts]  DEFAULT ((0)) FOR [FailedLoginAttempts]

		CREATE UNIQUE INDEX IX_AuthUsers_UnixTS ON [dbuser_testsolution4].[AuthUsers] ([UnixTS]);
		CREATE NONCLUSTERED INDEX IX_AuthUsers_Email ON [dbuser_testsolution4].[AuthUsers] ([EmailHash]);
	END


	DECLARE @TblCreated_AppParameter BIT = 0
	IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @TABLESCHEMA AND  TABLE_NAME = 'AppParameter'))
	BEGIN
		SET @TblCreated_AppParameter = 1

		SET ANSI_NULLS ON
		--GO
		SET QUOTED_IDENTIFIER ON
		--GO
		CREATE TABLE [dbuser_testsolution4].[AppParameter](
			[ID] [int] IDENTITY(1,1) NOT NULL,
			[UnixTS] [varchar](35) COLLATE Latin1_General_BIN NOT NULL,
			[AuthUsers_UnixTS] [varchar](35) COLLATE Latin1_General_BIN NOT NULL, 
			[ParameterName] [nvarchar](256) NULL,
			[ParameterValue] [nvarchar](4000) NULL,
			[Details] [nvarchar](2000) NULL,
			[Scope] [nvarchar](16) NULL,
			[LastUpdateUnixTS] [BIGINT] NULL,
		 CONSTRAINT [PK_AppParameter] PRIMARY KEY CLUSTERED 
		(
			[ID] ASC
		)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
		) ON [PRIMARY]

		ALTER TABLE [dbuser_testsolution4].[AppParameter] ADD  CONSTRAINT [DF_AppParameter_UnixTS]  DEFAULT (('')) FOR [UnixTS]
		ALTER TABLE [dbuser_testsolution4].[AppParameter] ADD  CONSTRAINT [DF_AppParameter_AuthUsers_UnixTS]  DEFAULT (('')) FOR [AuthUsers_UnixTS]
		ALTER TABLE [dbuser_testsolution4].[AppParameter] ADD  CONSTRAINT [DF_AppParameter_LastUpdateUnixTS]  DEFAULT ((0)) FOR [LastUpdateUnixTS]

		CREATE UNIQUE INDEX IX_AppParameter_UnixTS ON [dbuser_testsolution4].[AppParameter] ([UnixTS]);

		CREATE NONCLUSTERED INDEX IX_AppParameter_AuthUsers_UnixTS ON [dbuser_testsolution4].[AppParameter] ([AuthUsers_UnixTS]);
	END


	DECLARE @TblCreated_AuthUsersExtend BIT = 0
	IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @TABLESCHEMA AND  TABLE_NAME = 'AuthUsersExtend'))
	BEGIN
		SET @TblCreated_AuthUsersExtend = 1

		SET ANSI_NULLS ON
		SET QUOTED_IDENTIFIER ON
		CREATE TABLE [dbuser_testsolution4].[AuthUsersExtend](
			[ID] [int] IDENTITY(1,1) NOT NULL,
			[UnixTS] [varchar](35) COLLATE Latin1_General_BIN NOT NULL,
			[AuthUsers_UnixTS] [varchar](35) COLLATE Latin1_General_BIN NOT NULL, 
			--[AuthUsers_ID] [int] NOT NULL,
			[DisplayName] [nvarchar](256) NULL,
			[imgJpegThumbnail] [VARBINARY](MAX) NULL,
			[LastUpdateUnixTS] [BIGINT] NULL,
		 CONSTRAINT [PK_AuthUsersExtend] PRIMARY KEY CLUSTERED 
		(
			[ID] ASC
		)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = ON) ON [PRIMARY]
		) ON [PRIMARY]

		ALTER TABLE [dbuser_testsolution4].[AuthUsersExtend] ADD  CONSTRAINT [DF_AuthUsersExtend_UnixTS]  DEFAULT (('')) FOR [UnixTS]
		ALTER TABLE [dbuser_testsolution4].[AuthUsersExtend] ADD  CONSTRAINT [DF_AuthUsersExtend_AuthUsers_UnixTS]  DEFAULT (('')) FOR [AuthUsers_UnixTS]
		ALTER TABLE [dbuser_testsolution4].[AuthUsersExtend] ADD  CONSTRAINT [DF_AuthUsersExtend_LastUpdateUnixTS]  DEFAULT ((0)) FOR [LastUpdateUnixTS]
		ALTER TABLE [dbuser_testsolution4].[AuthUsersExtend] ADD  CONSTRAINT [DF_AuthUsersExtend_DisplayName]  DEFAULT (('')) FOR [DisplayName]

		--CREATE NONCLUSTERED INDEX IX_AuthUsersExtend_AuthUsers_ID ON [dbuser_testsolution4].[AuthUsersExtend] ([AuthUsers_ID]);
		CREATE NONCLUSTERED INDEX IX_AuthUsersExtend_AuthUsers_UnixTS ON [dbuser_testsolution4].[AuthUsersExtend] ([AuthUsers_UnixTS]);

		CREATE UNIQUE INDEX IX_AuthUsersExtend_UnixTS ON [dbuser_testsolution4].[AuthUsersExtend] ([UnixTS]);
	END


	DECLARE @TblCreated_SharingUsers BIT = 0
	IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @TABLESCHEMA AND  TABLE_NAME = 'SharingUsers'))
	BEGIN
		SET @TblCreated_SharingUsers = 1

		SET ANSI_NULLS ON
		SET QUOTED_IDENTIFIER ON
		CREATE TABLE [dbuser_testsolution4].[SharingUsers](
			[ID] [int] IDENTITY(1,1) NOT NULL,
			[UnixTS] [varchar](35) COLLATE Latin1_General_BIN NOT NULL,
			[AuthUsers_UnixTS] [varchar](35) COLLATE Latin1_General_BIN NOT NULL,
			[AuthUsers_ShareTo_UnixTS] [varchar](35) COLLATE Latin1_General_BIN NOT NULL,
			[SharingStatus] [int] NOT NULL, --REQUEST = 0, ACCEPT = 1,, REJECT = 2
			[LastUpdateUnixTS] [BIGINT] NULL,
		 CONSTRAINT [PK_SharingUsers] PRIMARY KEY CLUSTERED 
		(
			[ID] ASC
		)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = ON) ON [PRIMARY]
		) ON [PRIMARY]

		ALTER TABLE [dbuser_testsolution4].[SharingUsers] ADD  CONSTRAINT [DF_SharingUsers_UnixTS]  DEFAULT (('')) FOR [UnixTS]
		ALTER TABLE [dbuser_testsolution4].[SharingUsers] ADD  CONSTRAINT [DF_SharingUsers_AuthUsers_UnixTS]  DEFAULT (('')) FOR [AuthUsers_UnixTS]
		ALTER TABLE [dbuser_testsolution4].[SharingUsers] ADD  CONSTRAINT [DF_SharingUsers_LastUpdateUnixTS]  DEFAULT ((0)) FOR [LastUpdateUnixTS]
		ALTER TABLE [dbuser_testsolution4].[SharingUsers] ADD  CONSTRAINT [DF_SharingUsers_AuthUsers_ShareTo_UnixTS]  DEFAULT (('')) FOR [AuthUsers_ShareTo_UnixTS]
		ALTER TABLE [dbuser_testsolution4].[SharingUsers] ADD  CONSTRAINT [DF_SharingUsers_SharingStatus]  DEFAULT ((0)) FOR [SharingStatus]

		--CREATE NONCLUSTERED INDEX IX_SharingUsers_AuthUsers_ID ON [dbuser_testsolution4].[SharingUsers] ([AuthUsers_ID]);
		CREATE NONCLUSTERED INDEX IX_SharingUsers_AuthUsers_UnixTS ON [dbuser_testsolution4].[SharingUsers] ([AuthUsers_UnixTS]);
		--CREATE NONCLUSTERED INDEX IX_SharingUsers_AuthUsers_ShareTo_ID ON [dbuser_testsolution4].[SharingUsers] ([AuthUsers_ShareTo_ID]);

		CREATE UNIQUE INDEX IX_SharingUsers_UnixTS ON [dbuser_testsolution4].[SharingUsers] ([UnixTS]);
	END


	DECLARE @TblCreated_AppMessages BIT = 0
	IF (NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @TABLESCHEMA AND  TABLE_NAME = 'AppMessages'))
	BEGIN
		SET @TblCreated_AppMessages = 1

		SET ANSI_NULLS ON
		SET QUOTED_IDENTIFIER ON
		CREATE TABLE [dbuser_testsolution4].[AppMessages](
			[ID] [int] IDENTITY(1,1) NOT NULL,
			[UnixTS] [varchar](35) COLLATE Latin1_General_BIN NOT NULL,
			[DisplayName] [nvarchar](256) NOT NULL,
			[Title] [nvarchar](4000) NOT NULL,
			[Body] [nvarchar](4000) NOT NULL,
			[imgJpeg] [VARBINARY](MAX) NULL,
			[imgJpegThumbnail] [VARBINARY](MAX) NULL,
			[MsgType] [int] NOT NULL, -- (-1) = Feedback, 0 = System, 1 = Pay, 2 = Greeting
			[RecordDate] [DATETIMEOFFSET] NULL,
			[LinkInfo] [nvarchar](1024) NULL,
			[LastUpdateUnixTS] [BIGINT] NULL,
		 CONSTRAINT [PK_AppMessages] PRIMARY KEY CLUSTERED 
		(
			[ID] ASC
		)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = ON) ON [PRIMARY]
		) ON [PRIMARY]

		ALTER TABLE [dbuser_testsolution4].[AppMessages] ADD  CONSTRAINT [DF_AppMessages_UnixTS]  DEFAULT (('')) FOR [UnixTS]
		ALTER TABLE [dbuser_testsolution4].[AppMessages] ADD  CONSTRAINT [DF_AppMessages_LastUpdateUnixTS]  DEFAULT ((0)) FOR [LastUpdateUnixTS]

		CREATE UNIQUE INDEX IX_AppMessages_UnixTS ON [dbuser_testsolution4].[AppMessages] ([UnixTS]);
	END
	--------------------------------------------------------------------------------------------------------------------------------------------------------
	-- TABELLEN
	--------------------------------------------------------------------------------------------------------------------------------------------------------
	--------------------------------------------------------------------------------------------------------------------------------------------------------




	--------------------------------------------------------------------------------------------------------------------------------------------------------
	--------------------------------------------------------------------------------------------------------------------------------------------------------
	-- REFERENZEN
	--------------------------------------------------------------------------------------------------------------------------------------------------------
	IF (@TblCreated_AuthUsers = 1 AND @TblCreated_AuthUsersExtend = 1)
	BEGIN
		-- Prüfen, ob der Fremdschlüssel bereits existiert
		IF NOT EXISTS (
			SELECT 1
			FROM sys.foreign_keys
			WHERE name = 'FK_AuthUsersExtend_AuthUsers'
			  AND parent_object_id = OBJECT_ID('[dbuser_testsolution4].[AuthUsersExtend]')
		)
		BEGIN
			-- Fremdschlüssel hinzufügen
			ALTER TABLE [dbuser_testsolution4].[AuthUsersExtend]
			WITH CHECK ADD CONSTRAINT [FK_AuthUsersExtend_AuthUsers]
			FOREIGN KEY([AuthUsers_UnixTS])
			REFERENCES [dbuser_testsolution4].[AuthUsers] ([UnixTS]);
			--ON DELETE CASCADE  -- Optional: löscht zugehörige Extends automatisch beim Löschen des Users
			--ON UPDATE CASCADE; -- passt FK an, wenn sich die User-ID ändert

			-- Fremdschlüssel aktivieren
			ALTER TABLE [dbuser_testsolution4].[AuthUsersExtend]
			CHECK CONSTRAINT [FK_AuthUsersExtend_AuthUsers];
		END
	END
	--------------------------------------------------------------------------------------------------------------------------------------------------------
	-- REFERENZEN
	--------------------------------------------------------------------------------------------------------------------------------------------------------
	--------------------------------------------------------------------------------------------------------------------------------------------------------



	--------------------------------------------------------------------------------------------------------------------------------------------------------
	--------------------------------------------------------------------------------------------------------------------------------------------------------
	-- DEFAULT WERTE
	--------------------------------------------------------------------------------------------------------------------------------------------------------
	IF (EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @TABLESCHEMA AND  TABLE_NAME = 'AppParameter'))
	BEGIN
		SET IDENTITY_INSERT [dbuser_testsolution4].AppParameter ON 

		IF (NOT EXISTS (SELECT * FROM [dbuser_testsolution4].AppParameter WHERE AppParameter.ParameterName = 'StoreUrl_Microsoft'))
		BEGIN
			INSERT [dbuser_testsolution4].AppParameter ([ID], [UnixTS], [AuthUsers_UnixTS], [ParameterName], [ParameterValue], [Details], [Scope], [LastUpdateUnixTS]) VALUES (1, '1', '0', 'StoreUrl_Microsoft', 'https://apps.microsoft.com/search/publisher?name=True%20Perfect%20Code', 'bi bi-windows', 'app', 0)
		END

		IF (NOT EXISTS (SELECT * FROM [dbuser_testsolution4].AppParameter WHERE AppParameter.ParameterName = 'StoreUrl_Google'))
		BEGIN
			INSERT [dbuser_testsolution4].AppParameter ([ID], [UnixTS], [AuthUsers_UnixTS], [ParameterName], [ParameterValue], [Details], [Scope], [LastUpdateUnixTS]) VALUES (2, '2', '0', 'StoreUrl_Google', 'https://play.google.com/store/apps/developer?id=True+Perfect+Code', 'bi bi-android', 'app', 0)	
		END

		IF (NOT EXISTS (SELECT * FROM [dbuser_testsolution4].AppParameter WHERE AppParameter.ParameterName = 'StoreUrl_ApplePhone'))
		BEGIN
			INSERT [dbuser_testsolution4].AppParameter ([ID], [UnixTS], [AuthUsers_UnixTS], [ParameterName], [ParameterValue], [Details], [Scope], [LastUpdateUnixTS]) VALUES (3, '3', '0', 'StoreUrl_ApplePhone', 'https://apps.apple.com/us/developer/daniel-simic/id1733470934', 'bi bi-phone', 'app', 0)
		END

		IF (NOT EXISTS (SELECT * FROM [dbuser_testsolution4].AppParameter WHERE AppParameter.ParameterName = 'StoreUrl_AppleMac'))
		BEGIN
			INSERT [dbuser_testsolution4].AppParameter ([ID], [UnixTS], [AuthUsers_UnixTS], [ParameterName], [ParameterValue], [Details], [Scope], [LastUpdateUnixTS]) VALUES (4, '4', '0', 'StoreUrl_AppleMac', 'https://apps.apple.com/us/developer/daniel-simic/id1733470934', 'bi bi-apple', 'app', 0)
		END

		IF (NOT EXISTS (SELECT * FROM [dbuser_testsolution4].AppParameter WHERE AppParameter.ParameterName = 'StoreUrl_Web'))
		BEGIN
			INSERT [dbuser_testsolution4].AppParameter ([ID], [UnixTS], [AuthUsers_UnixTS], [ParameterName], [ParameterValue], [Details], [Scope], [LastUpdateUnixTS]) VALUES (5, '5', '0', 'StoreUrl_Web', 'https://pmunus.de', 'bi bi-globe2', 'app', 0)
		END

		IF (NOT EXISTS (SELECT * FROM [dbuser_testsolution4].AppParameter WHERE AppParameter.ParameterName = 'StoreUrl_Pwa'))
		BEGIN
			INSERT [dbuser_testsolution4].AppParameter ([ID], [UnixTS], [AuthUsers_UnixTS], [ParameterName], [ParameterValue], [Details], [Scope], [LastUpdateUnixTS]) VALUES (6, '6', '0', 'StoreUrl_Pwa', 'https://pwa.pmunus.de', 'bi bi-tux', 'app', 0)
		END

		IF (NOT EXISTS (SELECT * FROM [dbuser_testsolution4].AppParameter WHERE AppParameter.ParameterName = 'StoreUrl_Exe'))
		BEGIN
			INSERT [dbuser_testsolution4].AppParameter ([ID], [UnixTS], [AuthUsers_UnixTS], [ParameterName], [ParameterValue], [Details], [Scope], [LastUpdateUnixTS]) VALUES (7, '7', '0', 'StoreUrl_Exe', 'https://portable.pmunus.de', 'bi bi-filetype-exe', 'app', 0)
		END

		SET IDENTITY_INSERT [dbuser_testsolution4].AppParameter OFF	
	END
	------------------------------------------------------------------------------------------------------------------------------------------------------
	-- DEFAULT WERTE
	--------------------------------------------------------------------------------------------------------------------------------------------------------
	--------------------------------------------------------------------------------------------------------------------------------------------------------
