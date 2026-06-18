------------------------------------------------------------
------------------------------------------------------------
-- Prozedur CRUD -------------------------------------------
------------------------------------------------------------
------------------------------------------------------------

IF EXISTS ( SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbuser_pFemmeExample.Crud') AND type IN ( N'P', N'PC' ) ) 
BEGIN
	DROP PROCEDURE dbuser_pFemmeExample.Crud
END

GO

CREATE PROCEDURE [dbuser_pfemme].[Crud]

	-- CASE
	@Case_ varchar(100) = '',
	@CaseSub_ varchar(100) = '',

	-- Allgemeine Parameter
	@Json nvarchar(MAX) = '', --This must also be defined in pE.GlobalState => SetDefaultSPpara() !!!
	@imgJpeg nvarchar(MAX) = NULL, --This must also be defined in pE.GlobalState => SetDefaultSPpara() !!!
	@imgJpegThumbnail nvarchar(MAX) = NULL, --This must also be defined in pE.GlobalState => SetDefaultSPpara() !!!
	@Passphrase nvarchar(4000) = '', --This must also be defined in pE.GlobalState => SetDefaultSPpara() !!!

	@ID INT = 0, --This must also be defined in pE.GlobalState => SetDefaultSPpara() !!!
	@UnixTS varchar(35) = '', --This must also be defined in pE.GlobalState => SetDefaultSPpara() !!!
	@UnixTS2 varchar(35) = '', --This must also be defined in pE.GlobalState => SetDefaultSPpara() !!!
	--@AuthUsers_ID INT = 0, --This must also be defined in pE.GlobalState => SetDefaultSPpara() !!!
	@AuthUsers_UnixTS varchar(35) = '', --This must also be defined in pE.GlobalState => SetDefaultSPpara() !!!
	@DisplayName nvarchar(256) = NULL, --This must also be defined in pE.GlobalState => SetDefaultSPpara() !!!
	@Details nvarchar(2048) = NULL, --This must also be defined in pE.GlobalState => SetDefaultSPpara() !!!
	@RecordDate date = NULL, --This must also be defined in pE.GlobalState => SetDefaultSPpara() !!!
	--@RecordDateTime DATETIMEOFFSET = NULL, --This must also be defined in pE.GlobalState => SetDefaultSPpara() !!!
	@RecordDateTimeUnix BIGINT = 0,
	@sorter INT = 0, --This must also be defined in pE.GlobalState => SetDefaultSPpara() !!!
	@LastUpdateUnixTS BIGINT = 0, --This must also be defined in pE.GlobalState => SetDefaultSPpara() !!!
	@LastUpdateUnixTS2 BIGINT = 0, --This must also be defined in pE.GlobalState => SetDefaultSPpara() !!!

	@EmailHash nvarchar(256) = NULL, --This must also be defined in pE.GlobalState => SetDefaultSPpara() !!!
	@PasswordHash nvarchar(256) = NULL, --This must also be defined in pE.GlobalState => SetDefaultSPpara() !!!
	@PasswordHashNew nvarchar(256) = NULL, --This must also be defined in pE.GlobalState => SetDefaultSPpara() !!!

	@TOP INT = 0, --This must also be defined in pE.GlobalState => SetDefaultSPpara() !!!
	@OrderBy nvarchar(256) = NULL, --This must also be defined in pE.GlobalState => SetDefaultSPpara() !!!
	@SearchFields nvarchar(512) = NULL, --This must also be defined in pE.GlobalState => SetDefaultSPpara() !!!
	@TableName nvarchar(128) = NULL, --This must also be defined in pE.GlobalState => SetDefaultSPpara() !!!
	

	-- AppParameter
	@ParameterName nvarchar(256) = NULL,
	@ParameterValue nvarchar(4000) = NULL,
	--@Details nvarchar(2048) = NULL,
	@Scope nvarchar(16) = NULL, --'set'(z.B. User-Settings) , 'app' (z.B. Store-URLs) , 'config' (z.B. Otp backup-Code)
	--@AuthUsers_ID INT = 0,
	--@LastUpdateUnixTS BIGINT = 0,

	-- AuthUsers
	--@UnixTS varchar(35) = '',
	--@EmailHash nvarchar(256) = NULL,
	--@PasswordHash nvarchar(256) = NULL,
	@active BIT = 0,
	@TermsAccepted BIT = 0,
	@IdP nvarchar(128) = NULL,
	@IdPClientIdent nvarchar(128) = NULL,
	@IdPToken nvarchar(1024) = NULL,
	@otp nvarchar(MAX) = NULL,
	--@LastLogin DATETIMEOFFSET = NULL,
	@UserRole nvarchar(32) = NULL,
	--@LastUpdateUnixTS BIGINT = 0,

	-- Cycles
	@bleeding BIT = 0,
	@intensity INT = 0,
	@pain INT = 0,
	@headache INT = 0,
	@fatigue INT = 0,
	@nausea INT = 0,
	@cramps INT = 0,
	@created_at date = NULL,
	@updated_at date = NULL,

	-- Sonstige 
	@SharingUsersUnixTS varchar(MAX) = NULL, --This must also be defined in pE.GlobalState => SetDefaultSPpara() !!!
	@OtpBackupCode nvarchar(512) = NULL, --This must also be defined in pE.GlobalState => SetDefaultSPpara() !!!
	@IsMigration BIT = 0, --This must also be defined in pE.GlobalState => SetDefaultSPpara() !!!

	@OUTPUT_RES AS nvarchar(1000) = '' OUTPUT,
	@OUTPUT_INSERTED_ID AS INT = 0 OUTPUT

WITH ENCRYPTION
AS
BEGIN

	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;



	------------------------------------------------------------
	-- V A R I A B L E N
	------------------------------------------------------------
	CREATE TABLE #InsertedIDs (ID INT);
	CREATE TABLE #InsertedID_UnixTS_EmailHash (
		ID INT,
		UnixTS varchar(35),      -- Annahme: UnixTS ist ein BIGINT (typisch für Unix-Zeitstempel)
		EmailHash nvarchar(256)  -- Annahme: EmailHash ist ein String (Anpassen falls nötig)
	)
	DECLARE @insertedID NVARCHAR(10) = ''
	DECLARE @updatetedID NVARCHAR(10) = ''
	DECLARE @deletedID NVARCHAR(10) = ''
	DECLARE @insertedUnixTS varchar(35) = ''
	DECLARE @updatetedUnixTS varchar(35) = ''
	DECLARE @deletedUnixTS varchar(35) = ''

	IF ISNULL(@RecordDate, '') = ''
	BEGIN
		SET @RecordDate = NULL
	END

	------------------------------------------------------------
	-- V A R I A B L E N
	------------------------------------------------------------


	------------------------------------------------------------
	-- T A B E L L E N I N F O R M A T I O N E N
	------------------------------------------------------------
	DECLARE @TABLESCHEMA VARCHAR(50) = 'dbuser_pFemmeExample'

	IF @Case_ = 'SelectTableninformationen'
	BEGIN
		SELECT 
            COLUMN_NAME, 
            '@' + LTRIM(COLUMN_NAME) AS SP_PARAMETER_NAME, 
            DATA_TYPE, 
            ISNULL(CHARACTER_MAXIMUM_LENGTH, 0) AS COL_SIZE 
        FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = @TableName
	END
	------------------------------------------------------------
	-- T A B E L L E N I N F O R M A T I O N E N
	------------------------------------------------------------



	------------------------------------------------------------
	-- C Y C L E S
	------------------------------------------------------------
	DECLARE @TempDates TABLE (FirstDayCycle DATE);

	IF @Case_ = 'Save>>Cycles'
	BEGIN
		IF (SELECT COUNT(*) FROM dbuser_pFemmeExample.Cycles WHERE UnixTS = @UnixTS) = 0
		BEGIN
			BEGIN TRANSACTION

			INSERT INTO dbuser_pFemmeExample.Cycles (
				UnixTS,
				AuthUsers_UnixTS,
				Details,
				RecordDate,
				bleeding,
				intensity,
				pain,
				headache,
				fatigue,
				nausea,
				cramps,
				created_at,
				updated_at,
				LastUpdateUnixTS
			) 
			OUTPUT INSERTED.ID INTO #InsertedIDs
			VALUES (
				@UnixTS,
				@AuthUsers_UnixTS,
				@Details,
				@RecordDate,
				@bleeding,
				@intensity,
				@pain,
				@headache,
				@fatigue,
				@nausea,
				@cramps,
				@created_at,
				@updated_at,
				@LastUpdateUnixTS
			)

			IF @@ERROR <> 0 
			BEGIN
				ROLLBACK TRANSACTION 

				SELECT @OUTPUT_RES = 'not_saved'

				RETURN @@ERROR
			END
			ELSE
			BEGIN
				SET @insertedID = CAST(ISNULL((SELECT TOP 1 ID FROM #InsertedIDs), 0) AS NVARCHAR(10))
				SELECT @OUTPUT_RES = 'saved:' + LTRIM(RTRIM(@insertedID)) + ':' + LTRIM(RTRIM(@AuthUsers_UnixTS))
			END

			DELETE FROM #InsertedIDs;

			COMMIT TRANSACTION
		END
		ELSE
		BEGIN
			BEGIN TRANSACTION

			UPDATE 
				dbuser_pFemmeExample.Cycles   
			SET 
				--dbuser_pFemmeExample.Cycles.UnixTS = @UnixTS,
				dbuser_pFemmeExample.Cycles.AuthUsers_UnixTS = @AuthUsers_UnixTS,
				dbuser_pFemmeExample.Cycles.Details = @Details,
				dbuser_pFemmeExample.Cycles.RecordDate = @RecordDate,
				dbuser_pFemmeExample.Cycles.bleeding = @bleeding,
				dbuser_pFemmeExample.Cycles.intensity = @intensity,
				dbuser_pFemmeExample.Cycles.pain = @pain,
				dbuser_pFemmeExample.Cycles.headache = @headache,
				dbuser_pFemmeExample.Cycles.fatigue = @fatigue,
				dbuser_pFemmeExample.Cycles.nausea = @nausea,
				dbuser_pFemmeExample.Cycles.cramps = @cramps,
				dbuser_pFemmeExample.Cycles.created_at = @created_at,
				dbuser_pFemmeExample.Cycles.updated_at = @updated_at,
				dbuser_pFemmeExample.Cycles.LastUpdateUnixTS = @LastUpdateUnixTS
			WHERE
				UnixTS = @UnixTS

			IF @@ERROR <> 0 
			BEGIN
				ROLLBACK TRANSACTION 

				SELECT @OUTPUT_RES = 'not_updated'

				RETURN @@ERROR
			END
			ELSE
			BEGIN
				SET @updatetedID = CAST(ISNULL((SELECT TOP 1 ID FROM dbuser_pFemmeExample.Cycles WHERE UnixTS = @UnixTS), 0) AS NVARCHAR(10))
				SELECT @OUTPUT_RES = 'updated:' + LTRIM(RTRIM(@updatetedID)) + ':' + LTRIM(RTRIM(@AuthUsers_UnixTS))
			END

			COMMIT TRANSACTION
		END
	END

	IF @Case_ = 'Delete>>Cycles'
	BEGIN
		DELETE FROM dbuser_pFemmeExample.Cycles WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN

		IF (SELECT Count(*) FROM dbuser_pFemmeExample.Cycles WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN) = 0 
		BEGIN
			---- User-Sharing Verknüpfungen löschen
			--DELETE FROM dbuser_pFemmeExample.AuthUsersTodo
			--WHERE
			--	(AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN AND ISNULL(AuthUsers_ShareFrom_UnixTS, '') <> '')
			--	OR
			--	(ISNULL(AuthUsers_UnixTS, '') <> '' AND AuthUsers_ShareFrom_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN);

			---- User aus der Sharing-Tabelle löschen
			--DELETE FROM dbuser_pFemmeExample.SharingUsers WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN

			SELECT  @OUTPUT_RES = 'deleted:0:' + LTRIM(RTRIM(@AuthUsers_UnixTS))
		END
		ELSE
		BEGIN
			SELECT  @OUTPUT_RES = 'not_deleted'
		END
	END

	IF @Case_ = 'DeleteUnixTS>>Cycles'
	BEGIN
		DELETE FROM dbuser_pFemmeExample.Cycles 
		WHERE 
			UnixTS COLLATE Latin1_General_BIN = @UnixTS COLLATE Latin1_General_BIN
			AND AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN

		IF (
			SELECT Count(*) 
			FROM dbuser_pFemmeExample.Cycles 
			WHERE 
				UnixTS COLLATE Latin1_General_BIN = @UnixTS COLLATE Latin1_General_BIN
				AND AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
			) = 0 
		BEGIN
			---- User-Sharing Verknüpfungen löschen
			--DELETE FROM dbuser_pFemmeExample.AuthUsersTodo
			--WHERE
			--	(AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN AND ISNULL(AuthUsers_ShareFrom_UnixTS, '') <> '')
			--	OR
			--	(ISNULL(AuthUsers_UnixTS, '') <> '' AND AuthUsers_ShareFrom_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN);

			---- User aus der Sharing-Tabelle löschen
			--DELETE FROM dbuser_pFemmeExample.SharingUsers WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN

			SELECT  @OUTPUT_RES = 'deleted:0:' + LTRIM(RTRIM(@AuthUsers_UnixTS))
		END
		ELSE
		BEGIN
			SELECT  @OUTPUT_RES = 'not_deleted'
		END
	END

	IF @Case_ = 'Select>>Cycles'
	BEGIN
		SELECT
			ID,
			UnixTS,
			AuthUsers_UnixTS,
			Details,
			RecordDate,
			bleeding,
			intensity,
			pain,
			headache,
			fatigue,
			nausea,
			cramps,
			created_at,
			updated_at,
			LastUpdateUnixTS
		FROM dbuser_pFemmeExample.Cycles 
		WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
	END

	IF @Case_ = 'SelectDay>>Cycles'
	BEGIN
		SELECT TOP 1
			ID,
			UnixTS,
			AuthUsers_UnixTS,
			Details,
			RecordDate,
			bleeding,
			intensity,
			pain,
			headache,
			fatigue,
			nausea,
			cramps,
			created_at,
			updated_at,
			LastUpdateUnixTS
		FROM dbuser_pFemmeExample.Cycles 
		WHERE 
			AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
			AND YEAR(RecordDate) = YEAR(@RecordDate)
			AND MONTH(RecordDate) = MONTH(@RecordDate)
			AND DAY(RecordDate) = DAY(@RecordDate)
	END

	IF @Case_ = 'SelectTrendsBleeding>>Cycles'
	BEGIN
		SELECT
			RecordDate AS Int__Date,
			intensity AS Int__Value
		FROM dbuser_pFemmeExample.Cycles
		WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN AND bleeding = CAST(1 AS BIT)
	END

	IF @Case_ = 'SelectTrendsPain>>Cycles'
	BEGIN
		SELECT
			RecordDate AS Int__Date,
			pain AS Int__Value
		FROM dbuser_pFemmeExample.Cycles
		WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN AND pain > 0
	END
	------------------------------------------------------------
	-- C Y C L E S
	------------------------------------------------------------




	------------------------------------------------------------
	-- A U T H U S E R S E X T E N D
	------------------------------------------------------------
	IF @Case_ = 'Save>>AuthUsersExtend'
	BEGIN
		-- Prüfen, ob dieser Alias bereits existiert (und nicht diesem User gehört, falls Update)
		IF NOT EXISTS(
			SELECT * 
			FROM dbuser_pFemmeExample.AuthUsersExtend  
			WHERE
				AuthUsers_UnixTS COLLATE Latin1_General_BIN <> @AuthUsers_UnixTS COLLATE Latin1_General_BIN
				AND DisplayName = @DisplayName
		)
		BEGIN
			SET @ID = ISNULL((SELECT TOP 1 ID FROM dbuser_pFemmeExample.AuthUsersExtend WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN), 0)

			IF ISNULL(@ID, 0) = 0 -- Existiert Datensatz (Insert oder Update)
			BEGIN

				BEGIN TRANSACTION

				--Neuen Datensatz einfuegen
				INSERT INTO dbuser_pFemmeExample.AuthUsersExtend (
					UnixTS,
					AuthUsers_UnixTS,
					DisplayName,
					imgJpegThumbnail,
					LastUpdateUnixTS
				) 
				OUTPUT INSERTED.ID INTO #InsertedIDs
				VALUES (
					@UnixTS,
					@AuthUsers_UnixTS,
					@DisplayName,
					CASE WHEN ISNULL(@imgJpegThumbnail, '') = '' THEN NULL ELSE CAST(@imgJpegThumbnail AS VARBINARY(MAX)) END,
					@LastUpdateUnixTS
				)

				IF @@ERROR <> 0 
				BEGIN
					ROLLBACK TRANSACTION 

					SELECT @OUTPUT_RES = 'not_saved'

					RETURN @@ERROR
				END
				ELSE
				BEGIN
					--SET @insertedID = CAST(ISNULL((SELECT TOP 1 ID FROM #InsertedIDs), 0) AS NVARCHAR(10))
					SELECT  @OUTPUT_RES = 'saved:' + LTRIM(RTRIM(@UnixTS)) + ':' + LTRIM(RTRIM(@AuthUsers_UnixTS))
				END

				DELETE FROM #InsertedIDs;

				COMMIT TRANSACTION
			END
			ELSE
			BEGIN
				BEGIN TRANSACTION

				--Update recordset
				UPDATE 
					dbuser_pFemmeExample.AuthUsersExtend   
				SET 
					--dbuser_pFemmeExample.AuthUsersExtend.UnixTS = @UnixTS,
					--dbuser_pFemmeExample.AuthUsersExtend.AuthUsers_UnixTS = @AuthUsers_UnixTS,
					dbuser_pFemmeExample.AuthUsersExtend.DisplayName = @DisplayName,
					dbuser_pFemmeExample.AuthUsersExtend.imgJpegThumbnail = CASE WHEN ISNULL(@imgJpegThumbnail, '') = '' THEN NULL ELSE CAST(@imgJpegThumbnail AS VARBINARY(MAX)) END,
					dbuser_pFemmeExample.AuthUsersExtend.LastUpdateUnixTS = @LastUpdateUnixTS
				WHERE
					ID = @ID

				IF @@ERROR <> 0 
				BEGIN
					ROLLBACK TRANSACTION 

					SELECT @OUTPUT_RES = 'not_updated'

					RETURN @@ERROR
				END
				ELSE
				BEGIN
					--SET @updatetedID = CAST(ISNULL((SELECT TOP 1 ID FROM dbuser_pFemmeExample.AuthUsersExtend  WHERE AuthUsers_ID = @AuthUsers_ID), 0) AS NVARCHAR(10))
					SELECT  @OUTPUT_RES = 'updated:' + LTRIM(RTRIM(@UnixTS)) + ':' + LTRIM(RTRIM(@AuthUsers_UnixTS))
				END

				COMMIT TRANSACTION
			END
		END
		ELSE
		BEGIN
			SELECT @OUTPUT_RES = 'record_exists_no_adding:' + LTRIM(RTRIM(@UnixTS)) + ':' + LTRIM(RTRIM(@AuthUsers_UnixTS))
		END
	END

	IF @Case_ = 'Delete>>AuthUsersExtend'
	BEGIN
		DELETE FROM dbuser_pFemmeExample.AuthUsersExtend WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN

		IF (SELECT Count(*) FROM dbuser_pFemmeExample.AuthUsersExtend WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN) = 0 
		BEGIN
			SELECT  @OUTPUT_RES = 'deleted:0:' + LTRIM(RTRIM(@AuthUsers_UnixTS))
		END
		ELSE
		BEGIN
			SELECT  @OUTPUT_RES = 'not_deleted'
		END
	END

	IF @Case_ = 'Select>>AuthUsersExtend'
	BEGIN
		SELECT TOP 1
			ID,
			UnixTS,
			AuthUsers_UnixTS,
			DisplayName,
			ISNULL(CAST(imgJpegThumbnail AS NVARCHAR(MAX)), '') As imgJpegThumbnail,
			LastUpdateUnixTS
		FROM dbuser_pFemmeExample.AuthUsersExtend 
		WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
	END

	IF @Case_ = 'SelectAlias>>AuthUsersExtend'
	BEGIN
		SELECT TOP 1 ISNULL(DisplayName, '') As RES
		FROM dbuser_pFemmeExample.AuthUsersExtend 
		WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
	END

	IF @Case_ = 'SelectAuthUsersData>>AuthUsersExtend'
	BEGIN
		SELECT TOP 1
			'empty for security reasons' AS EmailHash, 
			'empty for security reasons' AS PasswordHash, 
			dbuser_pFemmeExample.AuthUsers.active, 
			dbuser_pFemmeExample.AuthUsers.TermsAccepted, 
			dbuser_pFemmeExample.AuthUsers.IdP,
			dbuser_pFemmeExample.AuthUsersExtend.DisplayName,
			ISNULL(CAST(dbuser_pFemmeExample.AuthUsersExtend.imgJpegThumbnail AS NVARCHAR(MAX)), '') AS imgJpegThumbnail
		FROM            
			dbuser_pFemmeExample.AuthUsers INNER JOIN
            dbuser_pFemmeExample.AuthUsersExtend ON dbuser_pFemmeExample.AuthUsers.UnixTS COLLATE Latin1_General_BIN = dbuser_pFemmeExample.AuthUsersExtend.AuthUsers_UnixTS COLLATE Latin1_General_BIN
		WHERE dbuser_pFemmeExample.AuthUsersExtend.AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
	END

	IF @Case_ = 'SelectByDisplayName>>AuthUsersExtend'
	BEGIN
		SELECT TOP 1
			dbuser_pFemmeExample.AuthUsersExtend.ID,
			dbuser_pFemmeExample.AuthUsersExtend.UnixTS,
			dbuser_pFemmeExample.AuthUsersExtend.AuthUsers_UnixTS,
			dbuser_pFemmeExample.AuthUsersExtend.DisplayName,
			ISNULL(CAST(dbuser_pFemmeExample.AuthUsersExtend.imgJpegThumbnail AS NVARCHAR(MAX)), '') AS imgJpegThumbnail,
			dbuser_pFemmeExample.AuthUsersExtend.LastUpdateUnixTS,
			ISNULL(dbuser_pFemmeExample.AuthUsers.UnixTS, '') AS Int__AuthUsers_UnixTS
		FROM
			dbuser_pFemmeExample.AuthUsers LEFT OUTER JOIN
            dbuser_pFemmeExample.AuthUsersExtend ON dbuser_pFemmeExample.AuthUsers.UnixTS COLLATE Latin1_General_BIN = dbuser_pFemmeExample.AuthUsersExtend.AuthUsers_UnixTS COLLATE Latin1_General_BIN
		WHERE DisplayName = @DisplayName
	END

	IF @Case_ = 'ExistsDisplayName>>AuthUsersExtend'
	BEGIN
		SELECT  
			CASE 
				WHEN EXISTS (
					SELECT 1
					FROM dbuser_pFemmeExample.AuthUsersExtend 
					WHERE 
						-- Prüfen, ob ein anderer Benutzer bereits dieses Alias hat 
						AuthUsers_UnixTS COLLATE Latin1_General_BIN <> @AuthUsers_UnixTS COLLATE Latin1_General_BIN
						AND ISNULL(DisplayName, '') = @DisplayName
				) 
				THEN CAST(1 AS BIT)
				ELSE CAST(0 AS BIT)
			END AS RES;
	END

	IF @Case_ = 'ExistsByAuthUsers_UnixTS>>AuthUsersExtend'
	BEGIN
		SELECT  
			CASE 
				WHEN EXISTS (
					SELECT 1
					FROM dbuser_pFemmeExample.AuthUsersExtend 
					WHERE 
						-- Prüfen, ob der Benutzer bereits dieses Alias hat 
						AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
				) 
				THEN CAST(1 AS BIT)
				ELSE CAST(0 AS BIT)
			END AS RES;
	END
	------------------------------------------------------------
	-- A U T H U S E R S E X T E N D
	------------------------------------------------------------



	------------------------------------------------------------
	-- A P P   P A R A M E T E R
	------------------------------------------------------------
	IF @Case_ = 'Save>>AppParameter'
	BEGIN		
		SET @ID = ISNULL((SELECT TOP 1 ID FROM dbuser_pFemmeExample.AppParameter WHERE ParameterName = @ParameterName AND AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN AND Scope = @Scope), 0)

		IF @ID = 0
		BEGIN
			BEGIN TRANSACTION

			--Neuen Datensatz einfuegen
			INSERT INTO dbuser_pFemmeExample.AppParameter (
				UnixTS,
				ParameterName,
				ParameterValue,
				Details,
				Scope,
				AuthUsers_UnixTS,
				LastUpdateUnixTS
			)
			OUTPUT INSERTED.ID INTO #InsertedIDs
			VALUES (
				@UnixTS,
				@ParameterName, 
				@ParameterValue,
				@Details,
				@Scope, --'set'(z.B. User-Settings) , 'app' (z.B. Store-URLs) , 'config' (z.B. Otp backup-Code)
				@AuthUsers_UnixTS,
				@LastUpdateUnixTS
			)

			IF @@ERROR <> 0 
			BEGIN
				ROLLBACK TRANSACTION 

				SELECT @OUTPUT_RES = 'not_saved'

				RETURN @@ERROR
			END
			ELSE
			BEGIN
				SELECT  @OUTPUT_RES = 'saved:' + LTRIM(RTRIM(@UnixTS)) + ':' + LTRIM(RTRIM(@AuthUsers_UnixTS))
			END

			DELETE FROM #InsertedIDs;

			COMMIT TRANSACTION
		END
		ELSE
		BEGIN
			BEGIN TRANSACTION

			--Update recordset
			UPDATE 
				dbuser_pFemmeExample.AppParameter   
			SET 
				dbuser_pFemmeExample.AppParameter.ParameterValue = @ParameterValue,
				dbuser_pFemmeExample.AppParameter.Details = @Details,
				dbuser_pFemmeExample.AppParameter.LastUpdateUnixTS = @LastUpdateUnixTS
			WHERE
				AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN AND ParameterName = @ParameterName AND Scope = @Scope

			IF @@ERROR <> 0 
			BEGIN
				ROLLBACK TRANSACTION 

				SELECT @OUTPUT_RES = 'not_saved'

				RETURN @@ERROR
			END
			ELSE
			BEGIN
				SELECT  @OUTPUT_RES = 'updated:' + LTRIM(RTRIM(@UnixTS)) + ':' + LTRIM(RTRIM(@AuthUsers_UnixTS))
			END

			COMMIT TRANSACTION
		END
	END

	IF @Case_ = 'SaveJson>>AppParameter'
	BEGIN	
		-- 1. Deklariere und fülle die temporäre Tabelle aus dem JSON-String
		DECLARE @JsonData TABLE (
			UnixTS VARCHAR(35),
			ParameterName NVARCHAR(256),
			ParameterValue NVARCHAR(4000),
			AuthUsers_UnixTS VARCHAR(35) COLLATE Latin1_General_BIN NOT NULL,
			LastUpdateUnixTS BIGINT
		);	

		-- Parsen des JSON-Strings in die Tabellenvariable
		INSERT INTO @JsonData (UnixTS, ParameterName, ParameterValue, AuthUsers_UnixTS, LastUpdateUnixTS)
		SELECT
			UnixTS,
			ParameterName,
			ParameterValue,
			AuthUsers_UnixTS,
			LastUpdateUnixTS
		FROM OPENJSON(@Json)
		WITH (
			UnixTS VARCHAR(35),
			ParameterName NVARCHAR(256),
			ParameterValue NVARCHAR(4000),
			AuthUsers_UnixTS VARCHAR(35) COLLATE Latin1_General_BIN,
			LastUpdateUnixTS BIGINT
		);

		BEGIN TRANSACTION;

		-- 2. Verwende MERGE, um die Zieltabelle zu aktualisieren oder neue Datensätze einzufügen
        MERGE dbuser_pFemmeExample.AppParameter AS Target
        USING @JsonData AS Source
        ON (Target.ParameterName = Source.ParameterName AND Target.AuthUsers_UnixTS COLLATE Latin1_General_BIN = Source.AuthUsers_UnixTS COLLATE Latin1_General_BIN)
        WHEN MATCHED THEN 
            -- UPDATE, wenn der Datensatz existiert
            UPDATE SET 
                Target.ParameterValue = Source.ParameterValue,
                Target.LastUpdateUnixTS = Source.LastUpdateUnixTS
        WHEN NOT MATCHED THEN
            -- INSERT, wenn der Datensatz nicht existiert (dürfte nicht vorkommen)
            INSERT (UnixTS, ParameterName, ParameterValue, Scope, AuthUsers_UnixTS, LastUpdateUnixTS)
            VALUES (
				Source.UnixTS,
                Source.ParameterName,
                Source.ParameterValue,
                'set', -- 'set'(z.B. User-Settings) , 'app' (z.B. Store-URLs) , 'config' (z.B. Otp backup-Code)
                Source.AuthUsers_UnixTS,
                Source.LastUpdateUnixTS
            );
        
		IF @@ERROR <> 0 
		BEGIN
			ROLLBACK TRANSACTION 

			SELECT @OUTPUT_RES = 'not_saved'

			RETURN @@ERROR
		END
		ELSE
		BEGIN
			SELECT  @OUTPUT_RES = 'updated:-1:' + LTRIM(RTRIM(@AuthUsers_UnixTS))
		END

        COMMIT TRANSACTION;
	END

	IF @Case_ = 'Select>>AppParameter'
	BEGIN	
		SELECT * FROM dbuser_pFemmeExample.AppParameter WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN AND Scope = @Scope
	END

	IF @Case_ = 'SelectStoreUrl>>AppParameter'
	BEGIN	
		SELECT * FROM dbuser_pFemmeExample.AppParameter WHERE LEN(AuthUsers_UnixTS) < 35 AND Scope = 'app' AND ParameterName LIKE 'StoreUrl_%'
	END

	IF @Case_ = 'ExistsStoreUrl>>AppParameter'
	BEGIN	
		-- Wird nur in der lokalen DB benutzt, da in der MSSQL nach Installation automatisch Store-Urls eingefügt werden
		SELECT 1 AS RES
	END

	IF @Case_ = 'SelectAppSettings>>AppParameter'
	BEGIN
		IF ISNULL(@AuthUsers_UnixTS, '') = ''
		BEGIN
			SET @AuthUsers_UnixTS = ISNULL((SELECT TOP 1 UnixTS COLLATE Latin1_General_BIN FROM dbuser_pFemmeExample.AuthUsers WHERE EmailHash = @EmailHash AND PasswordHash = @PasswordHash), '')
		END
		SELECT 
			ISNULL((
				SELECT TOP 1 ParameterValue FROM dbuser_pFemmeExample.AppParameter WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN AND ParameterName = @ParameterName AND Scope = @Scope
			), '') AS RES
	END

	IF @Case_ = 'DeleteAuthUsers_UnixTS>>AppParameter'
	BEGIN
		DELETE FROM dbuser_pFemmeExample.AppParameter WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN

		IF (SELECT Count(*) FROM dbuser_pFemmeExample.AppParameter WHERE  AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN) = 0 
		BEGIN
			SELECT  @OUTPUT_RES = 'deleted:' + LTRIM(RTRIM(@AuthUsers_UnixTS)) + ':' + LTRIM(RTRIM(@AuthUsers_UnixTS))
		END
		ELSE
		BEGIN
			SELECT  @OUTPUT_RES = 'not_deleted'
		END
	END

	IF @Case_ = 'AvailableAi>>AppParameter'
	BEGIN	
		IF NOT EXISTS(
			SELECT * 
			FROM dbuser_pFemmeExample.AppParameter 
			WHERE 
				AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN 
				AND ParameterName = 'AvailableAi'
		)
		BEGIN
			--Neuen Datensatz einfuegen
			INSERT INTO dbuser_pFemmeExample.AppParameter (
				UnixTS,
				ParameterName,
				ParameterValue,
				Details,
				Scope,
				AuthUsers_UnixTS,
				LastUpdateUnixTS
			)
			OUTPUT INSERTED.ID INTO #InsertedIDs
			VALUES (
				@UnixTS,
				'AvailableAi', 
				RTRIM(LTRIM(STR(YEAR(GETDATE())))) + '-' + RTRIM(LTRIM(STR(MONTH(GETDATE())-1))), -- (-1) Monat weniger, damit erste Abfrage nach Erfassung durchkommt
				'',
				'set', -- 'set'(z.B. User-Settings) , 'app' (z.B. Store-URLs) , 'config' (z.B. Otp backup-Code)
				@AuthUsers_UnixTS,
				@LastUpdateUnixTS
			)
		END

		IF EXISTS(
			SELECT * 
			FROM dbuser_pFemmeExample.AppParameter 
			WHERE 
				AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN 
				AND ParameterName = 'AvailableAi'
				AND ParameterValue = RTRIM(LTRIM(STR(YEAR(GETDATE())))) + '-' + RTRIM(LTRIM(STR(MONTH(GETDATE())))) -- aktuelle Jahr/Monat Kombination
		)
		BEGIN
			SELECT CAST(0 AS BIT) AS RES -- Ai ist nur einmal im Monat kostenlos verfügbar
		END
		ELSE
		BEGIN
			UPDATE 
				dbuser_pFemmeExample.AppParameter   
			SET 
				dbuser_pFemmeExample.AppParameter.ParameterValue = RTRIM(LTRIM(STR(YEAR(GETDATE())))) + '-' + RTRIM(LTRIM(STR(MONTH(GETDATE()))))
			WHERE
				AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN 
				AND ParameterName = 'AvailableAi'
		
			SELECT CAST(1 AS BIT) AS RES -- Ai ist verfügbar
		END
		
	END
	------------------------------------------------------------
	--  A P P   P A R A M E T E R
	------------------------------------------------------------



	------------------------------------------------------------
	-- A U T H U S E R S
	------------------------------------------------------------
	IF @Case_ = 'SelectAuthUsersEmail'
	BEGIN	
		IF (
			SELECT COUNT(*)
			FROM dbuser_pFemmeExample.AuthUsers
			WHERE 
				dbuser_pFemmeExample.AuthUsers.EmailHash = @EmailHash 
				AND dbuser_pFemmeExample.AuthUsers.PasswordHash = @PasswordHash
				AND active = CAST(1 AS BIT)
		) > 0
		BEGIN
			UPDATE 
				dbuser_pFemmeExample.AuthUsers   
			SET 
				dbuser_pFemmeExample.AuthUsers.LastLogin = GETUTCDATE() --GETDATE()
			WHERE
				dbuser_pFemmeExample.AuthUsers.EmailHash = @EmailHash
				AND dbuser_pFemmeExample.AuthUsers.PasswordHash = @PasswordHash
				AND active = CAST(1 AS BIT)
            
			SELECT TOP 1 dbuser_pFemmeExample.AuthUsers.UnixTS COLLATE Latin1_General_BIN
			FROM dbuser_pFemmeExample.AuthUsers
			WHERE 
				dbuser_pFemmeExample.AuthUsers.EmailHash = @EmailHash 
				AND dbuser_pFemmeExample.AuthUsers.PasswordHash = @PasswordHash
				AND active = CAST(1 AS BIT)
		END
		ELSE
		BEGIN
			SELECT TOP 1 'no_user' AS RES
		END
	END

	IF @Case_ = 'SelectByIdPClientIdent>>AuthUsers'
	BEGIN	
		IF (
			SELECT COUNT(*)
			FROM dbuser_pFemmeExample.AuthUsers
			WHERE 
				dbuser_pFemmeExample.AuthUsers.IdPClientIdent = @IdPClientIdent AND active = CAST(1 AS BIT)
		) > 0
		BEGIN
			SET  @IdPToken = ISNULL((
				SELECT TOP 1 dbuser_pFemmeExample.AuthUsers.UnixTS COLLATE Latin1_General_BIN
				FROM dbuser_pFemmeExample.AuthUsers
				WHERE 
					dbuser_pFemmeExample.AuthUsers.IdPClientIdent = @IdPClientIdent
					AND active = CAST(1 AS BIT)
			), '')

			UPDATE 
				dbuser_pFemmeExample.AuthUsers   
			SET 
				dbuser_pFemmeExample.AuthUsers.IdPClientIdent = '',
				dbuser_pFemmeExample.AuthUsers.IdPToken = '',
				dbuser_pFemmeExample.AuthUsers.LastUpdateUnixTS = @LastUpdateUnixTS
			WHERE
				dbuser_pFemmeExample.AuthUsers.IdPClientIdent = @IdPClientIdent
				AND active = CAST(1 AS BIT)
            
			SELECT @IdPToken AS IdPToken
		END
		ELSE
		BEGIN
			SELECT 'no_user' AS RES
		END
	END

	IF @Case_ = 'Register>>AuthUsers'
	BEGIN
		IF (SELECT Count(*) FROM dbuser_pFemmeExample.AuthUsers WHERE EmailHash = @EmailHash) = 0 -- Existiert der Benutzer
		BEGIN
			BEGIN TRANSACTION

			--Neuen Datensatz einfuegen
			INSERT INTO dbuser_pFemmeExample.AuthUsers (
				UnixTS,
				EmailHash,
				PasswordHash,
				active,
				TermsAccepted,
				IdP,
				otp,
				LastLogin,
				LastUpdateUnixTS
			) 
			--OUTPUT INSERTED.ID INTO #InsertedIDs
			OUTPUT 
				INSERTED.ID, 
				INSERTED.UnixTS, 
				INSERTED.EmailHash 
			INTO #InsertedID_UnixTS_EmailHash (ID, UnixTS, EmailHash)  -- Spalten in der Zieltabelle angeben
			VALUES (
				@UnixTS,
				@EmailHash,
				@PasswordHash,
				CAST(1 AS BIT), --@active,
				@TermsAccepted,
				@IdP,
				CONVERT(varbinary(max), @otp),
				GETUTCDATE(),
				@LastUpdateUnixTS
			)

			IF @@ERROR <> 0 
			BEGIN
				ROLLBACK TRANSACTION 

				SELECT @OUTPUT_RES = 'not_saved'

				RETURN @@ERROR
			END
			ELSE
			BEGIN
				SELECT  @OUTPUT_RES = LTRIM(RTRIM(@UnixTS))
			END

			DELETE FROM #InsertedIDs;

			COMMIT TRANSACTION
		END
		ELSE
		BEGIN
			SELECT @OUTPUT_RES = 'record_exists_no_adding'
		END
	END
		
	IF @Case_ = 'UpdateTermsAccepted>>AuthUsers'
	BEGIN
		IF (
			SELECT COUNT(*)
			FROM dbuser_pFemmeExample.AuthUsers
			WHERE 
				dbuser_pFemmeExample.AuthUsers.UnixTS COLLATE Latin1_General_BIN = @UnixTS COLLATE Latin1_General_BIN AND active = CAST(1 AS BIT)
		) > 0
		BEGIN
			UPDATE 
				dbuser_pFemmeExample.AuthUsers   
			SET 
				dbuser_pFemmeExample.AuthUsers.TermsAccepted = CAST(1 AS BIT)
			WHERE
				dbuser_pFemmeExample.AuthUsers.UnixTS COLLATE Latin1_General_BIN = @UnixTS COLLATE Latin1_General_BIN
				AND active = CAST(1 AS BIT)
            
			SELECT  @OUTPUT_RES = 'updated:' + LTRIM(RTRIM(@UnixTS)) + ':' + LTRIM(RTRIM(@UnixTS))
		END
		ELSE
		BEGIN
			SELECT @OUTPUT_RES = 'not_updated'
		END
	END

	IF @Case_ = 'DeleteOtp>>AuthUsers'
	BEGIN
		IF ISNULL(@EmailHash, '') <> '' AND ISNULL(@PasswordHash, '') <> ''
		BEGIN
			SET @AuthUsers_UnixTS = ISNULL((SELECT TOP 1 UnixTS COLLATE Latin1_General_BIN FROM dbuser_pFemmeExample.AuthUsers WHERE EmailHash = @EmailHash AND PasswordHash = @PasswordHash AND active = CAST(1 AS BIT)), '')
		END

		-- Prüfen, ob das Passwort korrekt ist
		IF (
			SELECT COUNT(*)
			FROM dbuser_pFemmeExample.AuthUsers
			WHERE 
				dbuser_pFemmeExample.AuthUsers.UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
		) > 0
		BEGIN
			IF (
				SELECT COUNT(*)
				FROM dbuser_pFemmeExample.AppParameter
				WHERE 
					dbuser_pFemmeExample.AppParameter.AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
					AND dbuser_pFemmeExample.AppParameter.Scope = 'config'
					AND dbuser_pFemmeExample.AppParameter.ParameterName = 'OtpBackupCode'
					AND dbuser_pFemmeExample.AppParameter.ParameterValue = @OtpBackupCode
			) > 0		
			BEGIN
				-- Otp mit NULL überschreiben in AuthUsers (Zurücksetzen von 2FA)
				UPDATE 
					dbuser_pFemmeExample.AuthUsers   
				SET 
					dbuser_pFemmeExample.AuthUsers.otp = NULL
				WHERE
					dbuser_pFemmeExample.AuthUsers.UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN

				-- Backupcode löschen aus AppParameter
				DELETE dbuser_pFemmeExample.AppParameter 
				WHERE 
					AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
					AND dbuser_pFemmeExample.AppParameter.Scope = 'config'
					AND dbuser_pFemmeExample.AppParameter.ParameterName = 'OtpBackupCode'

				-- Anzahl Versuche zurücksetzen
				UPDATE dbuser_pFemmeExample.AuthUsers
				SET 
					FailedLoginAttempts = 0,
					LastLogin = GETUTCDATE()
				WHERE 
					dbuser_pFemmeExample.AuthUsers.UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
					
				SELECT  @OUTPUT_RES = 'updated:' + LTRIM(RTRIM(@UnixTS)) + ':' + LTRIM(RTRIM(@UnixTS))
			END
			ELSE
			BEGIN
				-- HIER: Counter hochzählen, da User einen OTP-Versuch startet
				UPDATE dbuser_pFemmeExample.AuthUsers
				SET FailedLoginAttempts = FailedLoginAttempts + 1,
					LastLogin = GETUTCDATE()
				WHERE 
					EmailHash = @EmailHash -- Für den Fall, dass Passwort falsch ist, findet man dann Benutzer über Account

				SELECT @OUTPUT_RES = 'no_userotpbackupcode1'
			END
		END
		ELSE
		BEGIN
			-- HIER: Counter hochzählen, da User einen OTP-Versuch startet
			UPDATE dbuser_pFemmeExample.AuthUsers
			SET FailedLoginAttempts = FailedLoginAttempts + 1,
				LastLogin = GETUTCDATE()
			WHERE 
				EmailHash = @EmailHash -- Für den Fall, dass Passwort falsch ist, findet man dann Benutzer über Account

			SELECT @OUTPUT_RES = 'no_userotpbackupcode2'
		END
	END

	IF @Case_ = 'DeleteOtpByAuthUsers_UnixTS>>AuthUsers'
	BEGIN
		-- Otp mit NULL überschreiben in AuthUsers (Zurücksetzen von 2FA)
		UPDATE 
			dbuser_pFemmeExample.AuthUsers   
		SET 
			dbuser_pFemmeExample.AuthUsers.otp = NULL
		WHERE
			dbuser_pFemmeExample.AuthUsers.UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN

		-- Backupcode löschen aus AppParameter
		DELETE dbuser_pFemmeExample.AppParameter 
		WHERE 
			AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
			AND dbuser_pFemmeExample.AppParameter.Scope = 'config'
			AND dbuser_pFemmeExample.AppParameter.ParameterName = 'OtpBackupCode'

		-- Anzahl Versuche zurücksetzen
		UPDATE dbuser_pFemmeExample.AuthUsers
		SET 
			FailedLoginAttempts = 0,
			LastLogin = GETUTCDATE()
		WHERE 
			dbuser_pFemmeExample.AuthUsers.UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN;
					
		SELECT  @OUTPUT_RES = 'updated:' + LTRIM(RTRIM(@UnixTS)) + ':' + LTRIM(RTRIM(@UnixTS))
	END

	IF @Case_ = 'DeleteLocalData'
	BEGIN
		-- Wird nur lokal ausgeführt über 'await DeleteData(string authUsers_ID = "0")' !!!
		SELECT 1 AS RES
		-- Wird nur lokal ausgeführt über 'await DeleteData(string authUsers_ID = "0")' !!!
	END

	IF @Case_ = 'DeleteLocalAccount'
	BEGIN
		-- Wird nur lokal ausgeführt über 'await DeleteDB()' !!!
		SELECT 1 AS RES
		-- Wird nur lokal ausgeführt über 'await DeleteDB()' !!!
	END

	IF @Case_ = 'DeleteCloudData' OR @Case_ = 'DeleteAccount'
	BEGIN
		-- Benutzer UnixTS ermitteln
		DECLARE @_deleteDataUserUnixTS varchar(35) = ''
		SET @_deleteDataUserUnixTS = ISNULL((@UnixTS), '')

		-- Benutzerdaten löschen
		IF @Case_ = 'DeleteCloudData' OR @Case_ = 'DeleteAccount'
		BEGIN
			-- AuthUsersTodo
			DELETE dbuser_pFemmeExample.AuthUsersTodo WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @_deleteDataUserUnixTS COLLATE Latin1_General_BIN
			DELETE dbuser_pFemmeExample.AuthUsersTodo WHERE AuthUsers_ShareFrom_UnixTS COLLATE Latin1_General_BIN = @_deleteDataUserUnixTS COLLATE Latin1_General_BIN

			-- SharingUsers
			DELETE dbuser_pFemmeExample.SharingUsers WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @_deleteDataUserUnixTS COLLATE Latin1_General_BIN
			DELETE dbuser_pFemmeExample.SharingUsers WHERE AuthUsers_ShareTo_UnixTS COLLATE Latin1_General_BIN = @_deleteDataUserUnixTS COLLATE Latin1_General_BIN

			-- Tasks
			DELETE dbuser_pFemmeExample.Tasks WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @_deleteDataUserUnixTS COLLATE Latin1_General_BIN

			-- Todo
			DELETE dbuser_pFemmeExample.Todo WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @_deleteDataUserUnixTS COLLATE Latin1_General_BIN

			-- AppParameter
			DELETE dbuser_pFemmeExample.AppParameter WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @_deleteDataUserUnixTS COLLATE Latin1_General_BIN


			IF (
				(SELECT TOP 1 Count(*) FROM dbuser_pFemmeExample.AuthUsersTodo WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @_deleteDataUserUnixTS COLLATE Latin1_General_BIN)
				+
				(SELECT TOP 1 Count(*) FROM dbuser_pFemmeExample.SharingUsers WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @_deleteDataUserUnixTS COLLATE Latin1_General_BIN)
				+
				(SELECT TOP 1 Count(*) FROM dbuser_pFemmeExample.Tasks WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @_deleteDataUserUnixTS COLLATE Latin1_General_BIN)
				+
				(SELECT TOP 1 Count(*) FROM dbuser_pFemmeExample.Todo WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @_deleteDataUserUnixTS COLLATE Latin1_General_BIN)
				+
				(SELECT TOP 1 Count(*) FROM dbuser_pFemmeExample.AppParameter WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @_deleteDataUserUnixTS COLLATE Latin1_General_BIN)
			) = 0 
			BEGIN
				SELECT  @OUTPUT_RES = 'deleted:0:' + LTRIM(RTRIM(@_deleteDataUserUnixTS))
			END
			ELSE
			BEGIN
				SELECT  @OUTPUT_RES = 'not_deleted'
			END
		END

		-- Benutzeraccount löschen
		IF @Case_ = 'DeleteAccount'
		BEGIN
			--Erweiterte Benutzerdaten (Extended) löschen
			DELETE dbuser_pFemmeExample.AuthUsersExtend WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @_deleteDataUserUnixTS COLLATE Latin1_General_BIN

			--Benuutzer löschen
			DELETE dbuser_pFemmeExample.AuthUsers WHERE AuthUsers.UnixTS COLLATE Latin1_General_BIN = @_deleteDataUserUnixTS COLLATE Latin1_General_BIN

			IF (
				(SELECT Count(*) FROM dbuser_pFemmeExample.AuthUsers WHERE AuthUsers.UnixTS COLLATE Latin1_General_BIN = @_deleteDataUserUnixTS COLLATE Latin1_General_BIN)
				+
				(SELECT Count(*) FROM dbuser_pFemmeExample.AuthUsersExtend WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @_deleteDataUserUnixTS COLLATE Latin1_General_BIN)
			) = 0 
			BEGIN
				SELECT  @OUTPUT_RES = 'deleted:0:' + LTRIM(RTRIM(@_deleteDataUserUnixTS))
			END
			ELSE
			BEGIN
				SELECT  @OUTPUT_RES = 'not_deleted'
			END
		END
	END
		
	IF @Case_ = 'UpdateIdPToken>>AuthUsers'
	BEGIN	
		IF (
			SELECT COUNT(*)
			FROM dbuser_pFemmeExample.AuthUsers
			WHERE 
				dbuser_pFemmeExample.AuthUsers.UnixTS COLLATE Latin1_General_BIN = @UnixTS COLLATE Latin1_General_BIN AND active = CAST(1 AS BIT)
		) > 0
		BEGIN
			UPDATE 
				dbuser_pFemmeExample.AuthUsers   
			SET 
				dbuser_pFemmeExample.AuthUsers.TermsAccepted = CAST(1 AS BIT),
				dbuser_pFemmeExample.AuthUsers.IdPClientIdent = @IdPClientIdent,
				dbuser_pFemmeExample.AuthUsers.IdPToken = @IdPToken
			WHERE
				dbuser_pFemmeExample.AuthUsers.UnixTS COLLATE Latin1_General_BIN = @UnixTS COLLATE Latin1_General_BIN
				AND active = CAST(1 AS BIT)
            
			SELECT  @OUTPUT_RES = 'updated:' + LTRIM(RTRIM(@UnixTS)) + ':' + LTRIM(RTRIM(@UnixTS))
		END
		ELSE
		BEGIN
			SELECT @OUTPUT_RES = 'not_updated'
		END
	END

	IF @Case_ = 'ChangePassword>>AuthUsers'
	BEGIN
		IF (
			SELECT COUNT(*)
			FROM dbuser_pFemmeExample.AuthUsers
			WHERE 
				dbuser_pFemmeExample.AuthUsers.UnixTS COLLATE Latin1_General_BIN = @UnixTS COLLATE Latin1_General_BIN
				AND dbuser_pFemmeExample.AuthUsers.PasswordHash = @PasswordHash
				AND active = CAST(1 AS BIT)
		) > 0
		BEGIN
			-- Neues Passwort setzen
			UPDATE 
				dbuser_pFemmeExample.AuthUsers   
			SET 
				dbuser_pFemmeExample.AuthUsers.PasswordHash = @PasswordHashNew,
				dbuser_pFemmeExample.AuthUsers.LastUpdateUnixTS = @LastUpdateUnixTS
			WHERE
				dbuser_pFemmeExample.AuthUsers.UnixTS COLLATE Latin1_General_BIN = @UnixTS COLLATE Latin1_General_BIN
				AND dbuser_pFemmeExample.AuthUsers.PasswordHash = @PasswordHash
				AND dbuser_pFemmeExample.AuthUsers.active = CAST(1 AS BIT)

			-- Prüfen, ob neues Passwort gesetzt wurde
			IF (
				SELECT COUNT(*)
				FROM dbuser_pFemmeExample.AuthUsers
				WHERE 
					dbuser_pFemmeExample.AuthUsers.UnixTS COLLATE Latin1_General_BIN = @UnixTS COLLATE Latin1_General_BIN
					AND dbuser_pFemmeExample.AuthUsers.PasswordHash = @PasswordHashNew
					AND dbuser_pFemmeExample.AuthUsers.active = CAST(1 AS BIT)
			) > 0
			BEGIN
				SELECT  @OUTPUT_RES = 'updated:' + LTRIM(RTRIM(@UnixTS)) + ':' + LTRIM(RTRIM(@UnixTS))
			END
			ELSE
			BEGIN
				SELECT @OUTPUT_RES = 'not_updated:0:0'
			END
		END
		ELSE
		BEGIN
			IF (
				SELECT COUNT(*)
				FROM dbuser_pFemmeExample.AuthUsers
				WHERE 
					dbuser_pFemmeExample.AuthUsers.UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
					AND ISNULL(IdP, '') <> ''
					AND active = CAST(1 AS BIT)
			) > 0
			BEGIN
				-- Achtung: Bei externen ID-Provider kann das Passwort nicht geändert werden (liegt ja bei Google, MS oder apple, nicht hier)
				SELECT @OUTPUT_RES = 'not_updated:-1:-1'
			END
			ELSE
			BEGIN
				SELECT @OUTPUT_RES = 'not_updated:0:0'
			END
		END
	END

	IF @Case_ = 'SelectOtp>>AuthUsers'
	BEGIN
		IF ISNULL(@EmailHash, '') <> '' AND ISNULL(@PasswordHash, '') <> ''
		BEGIN
			SET @AuthUsers_UnixTS = ISNULL((SELECT TOP 1 UnixTS COLLATE Latin1_General_BIN FROM dbuser_pFemmeExample.AuthUsers WHERE EmailHash = @EmailHash AND PasswordHash = @PasswordHash AND active = CAST(1 AS BIT)), '')
		END

		-- VOR-ABFRAGE: Prüfe OTP-Versuch Status (Sperrung)
		DECLARE @OtpStatus varchar(10)
		DECLARE @CurrentAttempts int
    
		SELECT TOP 1
			@OtpStatus = CAST((
				CASE 
					-- OTP-Versuche gesperrt (>5 Versuche UND innerhalb 15 Min)
					WHEN FailedLoginAttempts >= 5 
						 AND LastLogin > DATEADD(MINUTE, -15, GETUTCDATE()) 
					THEN 'locked'
                
					-- Sperrzeit abgelaufen (>5 Versuche ABER >15 Min her)
					WHEN FailedLoginAttempts >= 5 
						 AND LastLogin <= DATEADD(MINUTE, -15, GETUTCDATE()) 
					THEN 'expired'
                
					-- Kein OTP vorhanden
					WHEN ISNULL(CAST(otp AS NVARCHAR(MAX)), '') = ''
					THEN 'no_otp'
                
					-- Alles OK
					ELSE 'ok'
				END
			) AS VARCHAR(10)),
			@CurrentAttempts = FailedLoginAttempts
		FROM dbuser_pFemmeExample.AuthUsers
		WHERE
			UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN

		-- Wenn kein User gefunden
		IF @OtpStatus IS NULL
		BEGIN
			IF ISNULL(@EmailHash, '') <> ''
			BEGIN
				-- Counter hochzählen, da User einen OTP-Versuch startet => Geht nur wenn Email korrekt ist!
				UPDATE dbuser_pFemmeExample.AuthUsers
				SET FailedLoginAttempts = FailedLoginAttempts + 1,
					LastLogin = GETUTCDATE()
				WHERE 
					EmailHash = @EmailHash
			END
			ELSE
			BEGIN
				IF ISNULL(@AuthUsers_UnixTS, '') <> ''
				BEGIN
					-- Counter hochzählen, da User einen OTP-Versuch startet => Geht nur wenn UnixTS vorhanden!
					UPDATE dbuser_pFemmeExample.AuthUsers
					SET FailedLoginAttempts = FailedLoginAttempts + 1,
						LastLogin = GETUTCDATE()
					WHERE 
						UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
				END
			END

			SELECT TOP 1 'no_user' AS Result
			RETURN
		END

		-- Wenn Account gesperrt, sofort abbrechen (KEIN Update!)
		IF @OtpStatus = 'locked'
		BEGIN
			SELECT TOP 1 'locked' AS Result
			RETURN
		END

		-- Wenn Sperrzeit abgelaufen, Counter zurücksetzen
		IF @OtpStatus = 'expired'
		BEGIN
			UPDATE dbuser_pFemmeExample.AuthUsers
			SET FailedLoginAttempts = 0
			WHERE 
				UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
        
			SET @OtpStatus = 'ok'
			SET @CurrentAttempts = 0
		END

		-- Wenn kein OTP vorhanden
		IF @OtpStatus = 'no_otp'
		BEGIN
			SELECT TOP 1 'no_otp' AS Result
			RETURN
		END

		-- HIER: Counter hochzählen, da User einen OTP-Versuch startet
		UPDATE dbuser_pFemmeExample.AuthUsers
		SET FailedLoginAttempts = FailedLoginAttempts + 1,
			LastLogin = GETUTCDATE()
		WHERE 
			UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN

		-- HAUPT-ABFRAGE: OTP-Code zurückgeben
		SELECT 
			ISNULL(CAST(dbuser_pFemmeExample.AuthUsers.otp AS NVARCHAR(MAX)), '') AS otp
		FROM dbuser_pFemmeExample.AuthUsers
		WHERE 
			UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
	END

	IF @Case_ = 'SelectIdP>>AuthUsers'
	BEGIN
		SELECT ISNULL((
			SELECT IdP
			FROM dbuser_pFemmeExample.AuthUsers
			WHERE 
				dbuser_pFemmeExample.AuthUsers.UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN AND active = CAST(1 AS BIT)
		), '') AS RES
	END

	IF @Case_ = 'SelectTermsAccepted>>AuthUsers'
	BEGIN
		SELECT ISNULL((
			SELECT CAST(dbuser_pFemmeExample.AuthUsers.TermsAccepted AS BIT) AS TermsAccepted
			FROM dbuser_pFemmeExample.AuthUsers
			WHERE 
				dbuser_pFemmeExample.AuthUsers.UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN AND active = CAST(1 AS BIT)
		), 0) AS RES
	END

	IF @Case_ = 'SaveOtp>>AuthUsers'
	BEGIN
		IF ISNULL(@EmailHash, '') <> '' AND ISNULL(@PasswordHash, '') <> ''
		BEGIN
			SET @AuthUsers_UnixTS = ISNULL((SELECT TOP 1 UnixTS COLLATE Latin1_General_BIN FROM dbuser_pFemmeExample.AuthUsers WHERE EmailHash = @EmailHash AND PasswordHash = @PasswordHash AND active = CAST(1 AS BIT)), '')
		END

		IF (
			SELECT COUNT(*)
			FROM dbuser_pFemmeExample.AuthUsers
			WHERE 
				dbuser_pFemmeExample.AuthUsers.UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
				AND active = CAST(1 AS BIT)
		) > 0
		BEGIN
			-- Otp-Code beim User setzen
			UPDATE 
				dbuser_pFemmeExample.AuthUsers   
			SET 
				dbuser_pFemmeExample.AuthUsers.otp = CASE WHEN ISNULL(@otp, '') = '' THEN NULL ELSE CAST(@otp AS VARBINARY(MAX)) END
			WHERE
				dbuser_pFemmeExample.AuthUsers.UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
            
			-- Backup Code des Benutzers in den Settings löschen (falls vorhanden)
			DELETE FROM dbuser_pFemmeExample.AppParameter 
			WHERE 
				AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
				AND ParameterValue = 'OtpBackupCode'

			-- Backup Code in den Settings speichern
			INSERT INTO dbuser_pFemmeExample.AppParameter (
				UnixTS,
				ParameterName,
				ParameterValue,
				Details,
				Scope,
				AuthUsers_UnixTS,
				LastUpdateUnixTS
			)
			OUTPUT INSERTED.ID INTO #InsertedIDs
			VALUES (
				@UnixTS,
				'OtpBackupCode', 
				@OtpBackupCode,
				'Keep secret! Used to deactivate 2FA if you lose your device with the authenticator app.',
				'config',
				@AuthUsers_UnixTS,
				@LastUpdateUnixTS
			)

			SELECT  @OUTPUT_RES = 'updated:' + LTRIM(RTRIM(@AuthUsers_UnixTS)) + ':' + LTRIM(RTRIM(@UnixTS))
		END
		ELSE
		BEGIN
			SELECT @OUTPUT_RES = 'not_updated'
		END
	END

	IF @Case_ = 'CheckAccount>>AuthUsers'
	BEGIN
		IF ISNULL(@EmailHash, '') <> '' AND ISNULL(@PasswordHash, '') <> ''
		BEGIN
			SET @AuthUsers_UnixTS = ISNULL((SELECT TOP 1 UnixTS COLLATE Latin1_General_BIN FROM dbuser_pFemmeExample.AuthUsers WHERE EmailHash = @EmailHash AND PasswordHash = @PasswordHash AND active = CAST(1 AS BIT)), '')
		END

		SELECT TOP 1 
			CASE 
				WHEN otp IS NOT NULL AND otp != '' THEN CAST(1 AS BIT)
				ELSE CAST(0 AS BIT)
			END AS HasOTP
		FROM dbuser_pFemmeExample.AuthUsers
		WHERE 
			UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
	END

	IF @Case_ = 'ExistsOtp>>AuthUsers'
	BEGIN
		SELECT  
			CASE 
				WHEN EXISTS (
					SELECT 1
					FROM dbuser_pFemmeExample.AuthUsers
					WHERE otp IS NOT NULL
					  AND UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
					  AND active = CAST(1 AS BIT)
				) 
				THEN CAST(1 AS BIT)
				ELSE CAST(0 AS BIT)
			END AS RES;
	END

	IF @Case_ = 'ExistsUnixTS>>AuthUsers'
	BEGIN
		SELECT  
			CASE 
				WHEN EXISTS (
					SELECT 1
					FROM dbuser_pFemmeExample.AuthUsers
					WHERE UnixTS COLLATE Latin1_General_BIN = @UnixTS COLLATE Latin1_General_BIN AND active = CAST(1 AS BIT)
				) 
				THEN CAST(1 AS BIT)
				ELSE CAST(0 AS BIT)
			END AS RES;
	END

	IF @Case_ = 'ExistsEmailHashPasswordHash>>AuthUsers'
	BEGIN
		SELECT  
			CASE 
				WHEN EXISTS (
					SELECT 1
					FROM dbuser_pFemmeExample.AuthUsers
					WHERE 
						dbuser_pFemmeExample.AuthUsers.EmailHash = @EmailHash 
						AND dbuser_pFemmeExample.AuthUsers.PasswordHash = @PasswordHash
						AND active = CAST(1 AS BIT)
				) 
				THEN CAST(1 AS BIT)
				ELSE CAST(0 AS BIT)
			END AS RES;
	END

	IF @Case_ = 'ResetLoginAttempts>>AuthUsers'
	BEGIN
		IF ISNULL(@EmailHash, '') <> '' AND ISNULL(@PasswordHash, '') <> ''
		BEGIN
			SET @AuthUsers_UnixTS = ISNULL((SELECT TOP 1 UnixTS COLLATE Latin1_General_BIN FROM dbuser_pFemmeExample.AuthUsers WHERE EmailHash = @EmailHash AND PasswordHash = @PasswordHash AND active = CAST(1 AS BIT)), '')
		END

		-- Erfolgreicher Login: Counter zurücksetzen
		UPDATE dbuser_pFemmeExample.AuthUsers
		SET FailedLoginAttempts = 0,
			LastLogin = GETUTCDATE()
		WHERE
			UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN

		SELECT @OUTPUT_RES = 'saved'
	END

	IF @Case_ = 'CheckPassword>>AuthUsers'
	BEGIN
		-- Existiert nur in der lokalen DB
		SELECT 1 AS RES 
	END

	IF @Case_ = 'CheckEmail>>AuthUsers'
	BEGIN
		-- Existiert nur in der lokalen DB
		SELECT 1 AS RES 
	END
	------------------------------------------------------------
	--  U S E R S
	------------------------------------------------------------


END

------------------------------------------------------------
------------------------------------------------------------
-- Prozedur CRUD
------------------------------------------------------------
------------------------------------------------------------
