------------------------------------------------------------
------------------------------------------------------------
-- Prozedur CRUD -------------------------------------------
------------------------------------------------------------
------------------------------------------------------------

IF EXISTS ( SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbuser_testsolution4.Crud') AND type IN ( N'P', N'PC' ) ) 
BEGIN
	DROP PROCEDURE dbuser_testsolution4.Crud
END

GO

CREATE PROCEDURE [dbuser_testsolution4].[Crud]

	-- CASE
	@Case_ varchar(2000) = '',

	-- Allgemeine Parameter
	@Json nvarchar(MAX) = '', --Muss auch in pE.GlobalState => SetDefaultSPpara() definiert werden !!!
	@imgJpeg nvarchar(MAX) = NULL, --Muss auch in pE.GlobalState => SetDefaultSPpara() definiert werden !!!
	@imgJpegThumbnail nvarchar(MAX) = NULL, --Muss auch in pE.GlobalState => SetDefaultSPpara() definiert werden !!!
	@Passphrase nvarchar(4000) = '', --Muss auch in pE.GlobalState => SetDefaultSPpara() definiert werden !!!

	@ID INT = 0, --Muss auch in pE.GlobalState => SetDefaultSPpara() definiert werden !!!
	@UnixTS varchar(35) = '', --Muss auch in pE.GlobalState => SetDefaultSPpara() definiert werden !!!
	@UnixTS2 varchar(35) = '', --Muss auch in pE.GlobalState => SetDefaultSPpara() definiert werden !!!
	--@AuthUsers_ID INT = 0, --Muss auch in pE.GlobalState => SetDefaultSPpara() definiert werden !!!
	@AuthUsers_UnixTS varchar(35) = '', --Muss auch in pE.GlobalState => SetDefaultSPpara() definiert werden !!!
	@DisplayName nvarchar(256) = NULL, --Muss auch in pE.GlobalState => SetDefaultSPpara() definiert werden !!!
	@Details nvarchar(2048) = NULL, --Muss auch in pE.GlobalState => SetDefaultSPpara() definiert werden !!!
	@RecordDate date = NULL, --Muss auch in pE.GlobalState => SetDefaultSPpara() definiert werden !!!
	--@RecordDateTime DATETIMEOFFSET = NULL, --Muss auch in pE.GlobalState => SetDefaultSPpara() definiert werden !!!
	@RecordDateTimeUnix BIGINT = 0,
	@sorter INT = 0, --Muss auch in pE.GlobalState => SetDefaultSPpara() definiert werden !!!
	@LastUpdateUnixTS BIGINT = 0, --Muss auch in pE.GlobalState => SetDefaultSPpara() definiert werden !!!
	@LastUpdateUnixTS2 BIGINT = 0, --Muss auch in pE.GlobalState => SetDefaultSPpara() definiert werden !!!

	@EmailHash nvarchar(256) = NULL, --Muss auch in pE.GlobalState => SetDefaultSPpara() definiert werden !!!
	@PasswordHash nvarchar(256) = NULL, --Muss auch in pE.GlobalState => SetDefaultSPpara() definiert werden !!!
	@PasswordHashNew nvarchar(256) = NULL, --Muss auch in pE.GlobalState => SetDefaultSPpara() definiert werden !!!

	@TOP INT = 0, --Muss auch in pE.GlobalState => SetDefaultSPpara() definiert werden !!!
	@OrderBy nvarchar(256) = NULL, --Muss auch in pE.GlobalState => SetDefaultSPpara() definiert werden !!!
	@SearchFields nvarchar(512) = NULL, --Muss auch in pE.GlobalState => SetDefaultSPpara() definiert werden !!!
	@TableName nvarchar(128) = NULL, --Muss auch in pE.GlobalState => SetDefaultSPpara() definiert werden !!!
	

	-- AppParameter
	@ParameterName nvarchar(256) = NULL,
	@ParameterValue nvarchar(4000) = NULL,
	--@Details nvarchar(2048) = NULL,
	@Scope nvarchar(16) = NULL,
	--@AuthUsers_ID INT = 0,
	--@LastUpdateUnixTS BIGINT = 0,

	-- AppMessages
	--@DisplayName nvarchar(256) = NULL,
	@Title nvarchar(4000) = '',
	@Body nvarchar(4000) = '',
	--@imgJpeg nvarchar(MAX) = NULL,
	--@imgJpegThumbnail nvarchar(MAX) = NULL,
	@MsgType INT = 0,
	--@RecordDate date = NULL,
	@LinkInfo nvarchar(1024) = NULL,
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

	-- SharingUsers
	--@UnixTS varchar(35) = '',
	--@AuthUsers_ID INT = 0,
	@AuthUsers_ShareTo_UnixTS varchar(35) = '',
	@SharingStatus INT = 0, -- 0=pending, 1=accepted, 2=rejected
	--@LastUpdateUnixTS BIGINT = 0,

	-- Sonstige 
	@SharingUsersUnixTS varchar(MAX) = NULL, --Muss auch in pE.GlobalState => SetDefaultSPpara() definiert werden !!!
	@OtpBackupCode nvarchar(512) = NULL, --Muss auch in pE.GlobalState => SetDefaultSPpara() definiert werden !!!
	@IsMigration BIT = 0, --Muss auch in pE.GlobalState => SetDefaultSPpara() definiert werden !!!

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
	DECLARE @TABLESCHEMA VARCHAR(50) = 'dbuser_testsolution4'

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
	-- A U T H U S E R S E X T E N D
	------------------------------------------------------------
	IF @Case_ = 'Save>>AuthUsersExtend'
	BEGIN
		-- Prüfen, ob dieser Alias bereits existiert (und nicht diesem User gehört, falls Update)
		IF NOT EXISTS(
			SELECT * 
			FROM dbuser_testsolution4.AuthUsersExtend  
			WHERE
				AuthUsers_UnixTS COLLATE Latin1_General_BIN <> @AuthUsers_UnixTS COLLATE Latin1_General_BIN
				AND DisplayName = @DisplayName
		)
		BEGIN
			SET @ID = ISNULL((SELECT TOP 1 ID FROM dbuser_testsolution4.AuthUsersExtend WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN), 0)

			IF ISNULL(@ID, 0) = 0 -- Existiert Datensatz (Insert oder Update)
			BEGIN

				BEGIN TRANSACTION

				--Neuen Datensatz einfuegen
				INSERT INTO dbuser_testsolution4.AuthUsersExtend (
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
					dbuser_testsolution4.AuthUsersExtend   
				SET 
					--dbuser_testsolution4.AuthUsersExtend.UnixTS = @UnixTS,
					--dbuser_testsolution4.AuthUsersExtend.AuthUsers_UnixTS = @AuthUsers_UnixTS,
					dbuser_testsolution4.AuthUsersExtend.DisplayName = @DisplayName,
					dbuser_testsolution4.AuthUsersExtend.imgJpegThumbnail = CASE WHEN ISNULL(@imgJpegThumbnail, '') = '' THEN NULL ELSE CAST(@imgJpegThumbnail AS VARBINARY(MAX)) END,
					dbuser_testsolution4.AuthUsersExtend.LastUpdateUnixTS = @LastUpdateUnixTS
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
					--SET @updatetedID = CAST(ISNULL((SELECT TOP 1 ID FROM dbuser_testsolution4.AuthUsersExtend  WHERE AuthUsers_ID = @AuthUsers_ID), 0) AS NVARCHAR(10))
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
		DELETE FROM dbuser_testsolution4.AuthUsersExtend WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN

		IF (SELECT Count(*) FROM dbuser_testsolution4.AuthUsersExtend WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN) = 0 
		BEGIN
			-- User-Sharing Verknüpfungen löschen
			DELETE FROM dbuser_testsolution4.AuthUsersTodo
			WHERE
				(AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN AND ISNULL(AuthUsers_ShareFrom_UnixTS, '') <> '')
				OR
				(ISNULL(AuthUsers_UnixTS, '') <> '' AND AuthUsers_ShareFrom_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN);

			-- User aus der Sharing-Tabelle löschen
			DELETE FROM dbuser_testsolution4.SharingUsers WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN

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
		FROM dbuser_testsolution4.AuthUsersExtend 
		WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
	END

	IF @Case_ = 'SelectAlias>>AuthUsersExtend'
	BEGIN
		SELECT TOP 1 ISNULL(DisplayName, '') As RES
		FROM dbuser_testsolution4.AuthUsersExtend 
		WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
	END

	IF @Case_ = 'SelectAuthUsersData>>AuthUsersExtend'
	BEGIN
		SELECT TOP 1
			'empty for security reasons' AS EmailHash, 
			'empty for security reasons' AS PasswordHash, 
			dbuser_testsolution4.AuthUsers.active, 
			dbuser_testsolution4.AuthUsers.TermsAccepted, 
			dbuser_testsolution4.AuthUsers.IdP,
			dbuser_testsolution4.AuthUsersExtend.DisplayName,
			ISNULL(CAST(dbuser_testsolution4.AuthUsersExtend.imgJpegThumbnail AS NVARCHAR(MAX)), '') AS imgJpegThumbnail
		FROM            
			dbuser_testsolution4.AuthUsers INNER JOIN
            dbuser_testsolution4.AuthUsersExtend ON dbuser_testsolution4.AuthUsers.UnixTS COLLATE Latin1_General_BIN = dbuser_testsolution4.AuthUsersExtend.AuthUsers_UnixTS COLLATE Latin1_General_BIN
		WHERE dbuser_testsolution4.AuthUsersExtend.AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
	END

	IF @Case_ = 'SelectByDisplayName>>AuthUsersExtend'
	BEGIN
		SELECT TOP 1
			dbuser_testsolution4.AuthUsersExtend.ID,
			dbuser_testsolution4.AuthUsersExtend.UnixTS,
			dbuser_testsolution4.AuthUsersExtend.AuthUsers_UnixTS,
			dbuser_testsolution4.AuthUsersExtend.DisplayName,
			ISNULL(CAST(dbuser_testsolution4.AuthUsersExtend.imgJpegThumbnail AS NVARCHAR(MAX)), '') AS imgJpegThumbnail,
			dbuser_testsolution4.AuthUsersExtend.LastUpdateUnixTS,
			ISNULL(dbuser_testsolution4.AuthUsers.UnixTS, '') AS Int__AuthUsers_UnixTS
		FROM
			dbuser_testsolution4.AuthUsers LEFT OUTER JOIN
            dbuser_testsolution4.AuthUsersExtend ON dbuser_testsolution4.AuthUsers.UnixTS COLLATE Latin1_General_BIN = dbuser_testsolution4.AuthUsersExtend.AuthUsers_UnixTS COLLATE Latin1_General_BIN
		WHERE DisplayName = @DisplayName
	END

	IF @Case_ = 'ExistsDisplayName>>AuthUsersExtend'
	BEGIN
		SELECT  
			CASE 
				WHEN EXISTS (
					SELECT 1
					FROM dbuser_testsolution4.AuthUsersExtend 
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
					FROM dbuser_testsolution4.AuthUsersExtend 
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
	-- S H A R I N G U S E R S
	------------------------------------------------------------
	IF @Case_ = 'Save>>SharingUsers'
	BEGIN
		SET @ID = ISNULL((SELECT TOP 1 ID FROM dbuser_testsolution4.SharingUsers WHERE UnixTS COLLATE Latin1_General_BIN = @UnixTS COLLATE Latin1_General_BIN), 0)

		IF ISNULL(@ID, 0) = 0 -- Existiert Datensatz (Insert oder Update)
		BEGIN
			--Pruefen zuerst, ob es Datensatz bereits in der Tabelle gibt
			IF (SELECT Count(*) FROM dbuser_testsolution4.SharingUsers WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN AND AuthUsers_ShareTo_UnixTS = @AuthUsers_ShareTo_UnixTS COLLATE Latin1_General_BIN) > 0 
			BEGIN	
				SELECT @OUTPUT_RES = 'record_exists_no_adding:' + LTRIM(RTRIM(@UnixTS)) + ':' + LTRIM(RTRIM(@AuthUsers_UnixTS))
			END	
			ELSE
			BEGIN

				BEGIN TRANSACTION

				--Neuen Datensatz einfuegen
				INSERT INTO dbuser_testsolution4.SharingUsers (
					UnixTS,
					AuthUsers_UnixTS,
					AuthUsers_ShareTo_UnixTS,
					SharingStatus,
					LastUpdateUnixTS
				) 
				OUTPUT INSERTED.ID INTO #InsertedIDs
				VALUES (
					@UnixTS,
					@AuthUsers_UnixTS,
					@AuthUsers_ShareTo_UnixTS,
					@SharingStatus,
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
		END
		ELSE
		BEGIN
			IF (SELECT Count(*) FROM dbuser_testsolution4.SharingUsers WHERE ID <> @ID AND AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN AND AuthUsers_ShareTo_UnixTS = @AuthUsers_ShareTo_UnixTS COLLATE Latin1_General_BIN) > 0 
			BEGIN	
				SELECT @OUTPUT_RES = 'record_exists_no_update'
			END	
			ELSE
			BEGIN
				BEGIN TRANSACTION

				--Update recordset
				UPDATE 
					dbuser_testsolution4.SharingUsers   
				SET 
					dbuser_testsolution4.SharingUsers.AuthUsers_ShareTo_UnixTS = @AuthUsers_ShareTo_UnixTS,
					dbuser_testsolution4.SharingUsers.SharingStatus = @SharingStatus,
					dbuser_testsolution4.SharingUsers.LastUpdateUnixTS = @LastUpdateUnixTS
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
					SELECT  @OUTPUT_RES = 'updated:' + LTRIM(RTRIM(@UnixTS)) + ':' + LTRIM(RTRIM(@AuthUsers_UnixTS))
				END

				COMMIT TRANSACTION
			END
		END
	END

	IF @Case_ = 'Delete>>SharingUsers'
	BEGIN
		DELETE FROM dbuser_testsolution4.SharingUsers WHERE UnixTS COLLATE Latin1_General_BIN = @UnixTS COLLATE Latin1_General_BIN

		IF (SELECT Count(*) FROM dbuser_testsolution4.SharingUsers WHERE UnixTS COLLATE Latin1_General_BIN = @UnixTS COLLATE Latin1_General_BIN) = 0 
		BEGIN
			-- User-Sharing Verknüpfungen löschen
			DELETE FROM dbuser_testsolution4.AuthUsersTodo
			WHERE
				(AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN AND ISNULL(AuthUsers_ShareFrom_UnixTS, '') <> '')
				OR
				(ISNULL(AuthUsers_UnixTS, '') <> '' AND AuthUsers_ShareFrom_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN);

			SELECT  @OUTPUT_RES = 'deleted:' + LTRIM(RTRIM(@UnixTS)) + ':' + LTRIM(RTRIM(@AuthUsers_UnixTS))
		END
		ELSE
		BEGIN
			SELECT  @OUTPUT_RES = 'not_deleted'
		END
	END

	IF @Case_ = 'ChangeSharingStatus>>SharingUsers'
	BEGIN
		BEGIN TRANSACTION

		--Update recordset
		UPDATE 
			dbuser_testsolution4.SharingUsers   
		SET 
			dbuser_testsolution4.SharingUsers.SharingStatus = @SharingStatus,
			dbuser_testsolution4.SharingUsers.LastUpdateUnixTS = @LastUpdateUnixTS
		WHERE
			UnixTS COLLATE Latin1_General_BIN = @UnixTS COLLATE Latin1_General_BIN

		IF @@ERROR <> 0 
		BEGIN
			ROLLBACK TRANSACTION 

			SELECT @OUTPUT_RES = 'not_updated'

			RETURN @@ERROR
		END
		ELSE
		BEGIN
			SELECT  @OUTPUT_RES = 'updated:' + LTRIM(RTRIM(@UnixTS)) + ':0'
		END

		COMMIT TRANSACTION
	END
		
	IF @Case_ = 'Select>>SharingUsers'
	BEGIN
		--DECLARE @SearchFields nvarchar(512) = '',
		--	@IsChecked BIT = 0, 
		--	@AuthUsers_UnixTS varchar(35) = 'T0002040835292673893712595881003425'

		SELECT
			tblSU.*,
			-- Wenn gesendete User-UnixTS ein Master in der Sharing-Tabelle, 
			--    dann liefern wir To-User-UnixTS, ansonsten 
			--    ansonsten liefern wir die Master User-UnixTS
			ISNULL((
				CASE 
					WHEN tblSU.AuthUsers_UnixTS COLLATE Latin1_General_BIN = 
						 @AuthUsers_UnixTS COLLATE Latin1_General_BIN 
					THEN
						tblSU.AuthUsers_ShareTo_UnixTS
					ELSE
						tblSU.AuthUsers_UnixTS
				END
			), '') AS Int__AuthUsers_UnixTS,
			CAST(0 AS BIT) AS Int__IsChecked,
			ISNULL((
				-- Wenn gesendete User-UnixTS ein Master in der Sharing-Tabelle, 
				--    dann liefern wir To-User-UnixTS-Alias, ansonsten 
				--    ansonsten liefern wir die Master User-UnixTS-Alias
				CASE WHEN tblSU.AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN THEN
						ISNULL((
							SELECT TOP 1 DisplayName 
							FROM dbuser_testsolution4.AuthUsersExtend 
							WHERE 
								dbuser_testsolution4.AuthUsersExtend.AuthUsers_UnixTS COLLATE Latin1_General_BIN = tblSU.AuthUsers_ShareTo_UnixTS COLLATE Latin1_General_BIN
						), '')
					ELSE
						ISNULL((
							SELECT TOP 1 DisplayName 
							FROM dbuser_testsolution4.AuthUsersExtend
							WHERE dbuser_testsolution4.AuthUsersExtend.AuthUsers_UnixTS COLLATE Latin1_General_BIN = tblSU.AuthUsers_UnixTS COLLATE Latin1_General_BIN
						), '')
				END
			), '') AS Int__Alias,
			ISNULL((
				-- Wenn gesendete User-UnixTS ein Master in der Sharing-Tabelle, 
				--    dann liefern wir To-User-UnixTS-Alias-Image, ansonsten 
				--    ansonsten liefern wir die Master User-UnixTS-Alias-Image
				CASE WHEN tblSU.AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN THEN
						ISNULL((
							SELECT TOP 1 ISNULL(CAST(imgJpegThumbnail AS NVARCHAR(MAX)), '') AS RES 
							FROM dbuser_testsolution4.AuthUsersExtend 
							WHERE 
								dbuser_testsolution4.AuthUsersExtend.AuthUsers_UnixTS COLLATE Latin1_General_BIN = tblSU.AuthUsers_ShareTo_UnixTS COLLATE Latin1_General_BIN
						), '')
					ELSE
						ISNULL((
							SELECT TOP 1 ISNULL(CAST(imgJpegThumbnail AS NVARCHAR(MAX)), '') AS RES 
							FROM dbuser_testsolution4.AuthUsersExtend
							WHERE dbuser_testsolution4.AuthUsersExtend.AuthUsers_UnixTS COLLATE Latin1_General_BIN = tblSU.AuthUsers_UnixTS COLLATE Latin1_General_BIN
						), '')
				END
			), '') AS Int__AliasImgJpegThumbnail
		FROM dbuser_testsolution4.SharingUsers AS tblSU
		WHERE 
			(
				tblSU.AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN 
				OR tblSU.AuthUsers_ShareTo_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
			)

		ORDER BY ID DESC
	END

	IF @Case_ = 'SelectRequest>>SharingUsers'
	BEGIN
		SELECT * FROM dbuser_testsolution4.SharingUsers WHERE SharingStatus = 0
	END

	IF @Case_ = 'SelectByAuthUsers_UnixTS>>SharingUsers'
	BEGIN
		SELECT * FROM dbuser_testsolution4.SharingUsers 
		WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
	END
	------------------------------------------------------------
	-- S H A R I N G U S E R S
	------------------------------------------------------------



	------------------------------------------------------------
	-- A P P   P A R A M E T E R
	------------------------------------------------------------
	IF @Case_ = 'Save>>AppParameter'
	BEGIN		
		SET @ID = ISNULL((SELECT TOP 1 ID FROM dbuser_testsolution4.AppParameter WHERE ParameterName = @ParameterName AND AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN AND Scope = @Scope), 0)

		IF @ID = 0
		BEGIN
			BEGIN TRANSACTION

			--Neuen Datensatz einfuegen
			INSERT INTO dbuser_testsolution4.AppParameter (
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
				@Scope,
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
				dbuser_testsolution4.AppParameter   
			SET 
				dbuser_testsolution4.AppParameter.ParameterValue = @ParameterValue,
				dbuser_testsolution4.AppParameter.Details = @Details,
				dbuser_testsolution4.AppParameter.LastUpdateUnixTS = @LastUpdateUnixTS
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
        MERGE dbuser_testsolution4.AppParameter AS Target
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
                'set', -- Fester 'set' Wert für Scope
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
		SELECT * FROM dbuser_testsolution4.AppParameter WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN AND Scope = @Scope
	END

	IF @Case_ = 'SelectStoreUrl>>AppParameter'
	BEGIN	
		SELECT * FROM dbuser_testsolution4.AppParameter WHERE LEN(AuthUsers_UnixTS) < 35 AND Scope = 'app' AND ParameterName LIKE 'StoreUrl_%'
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
			SET @AuthUsers_UnixTS = ISNULL((SELECT TOP 1 UnixTS COLLATE Latin1_General_BIN FROM dbuser_testsolution4.AuthUsers WHERE EmailHash = @EmailHash AND PasswordHash = @PasswordHash), '')
		END
		SELECT 
			ISNULL((
				SELECT TOP 1 ParameterValue FROM dbuser_testsolution4.AppParameter WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN AND ParameterName = @ParameterName AND Scope = @Scope
			), '') AS RES
	END

	IF @Case_ = 'DeleteAuthUsers_UnixTS>>AppParameter'
	BEGIN
		DELETE FROM dbuser_testsolution4.AppParameter WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN

		IF (SELECT Count(*) FROM dbuser_testsolution4.AppParameter WHERE  AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN) = 0 
		BEGIN
			SELECT  @OUTPUT_RES = 'deleted:' + LTRIM(RTRIM(@AuthUsers_UnixTS)) + ':' + LTRIM(RTRIM(@AuthUsers_UnixTS))
		END
		ELSE
		BEGIN
			SELECT  @OUTPUT_RES = 'not_deleted'
		END
	END
	------------------------------------------------------------
	--  A P P   P A R A M E T E R
	------------------------------------------------------------



	------------------------------------------------------------
	--  A P P   M E S S A G E S
	------------------------------------------------------------
	IF @Case_ = 'SaveFeedback>>AppMessages'
	BEGIN
		DECLARE @CurrentMinuteTS BIGINT = DATEDIFF(MINUTE, '2025-01-01', GETUTCDATE());

		-- Prüfen, ob innerhalb der letzten 10 Minuten Feedback kam
		IF EXISTS (
			SELECT 1 
			FROM dbuser_testsolution4.AppMessages 
			WHERE 
				MsgType = -1 -- Feedback
				AND LastUpdateUnixTS IS NOT NULL
				AND @CurrentMinuteTS - LastUpdateUnixTS < 10
		)
		BEGIN
			SELECT @OUTPUT_RES = 'too_soon'
			RETURN
		END

		-- Prüfen auf Duplikate
		IF EXISTS (
			SELECT 1 
			FROM dbuser_testsolution4.AppMessages 
			WHERE Title = @Title AND Body = @Body
		)
		BEGIN	
			SELECT @OUTPUT_RES = 'record_exists_no_adding:0:0'
			RETURN
		END

		BEGIN TRANSACTION

		INSERT INTO dbuser_testsolution4.AppMessages (
			DisplayName,
			Title,
			Body,
			MsgType,
			RecordDate,
			LastUpdateUnixTS
		) 
		OUTPUT INSERTED.ID INTO #InsertedIDs
		VALUES (
			'Feedback' + CASE WHEN ISNULL(@DisplayName, '') <> '' THEN ' from ' + @DisplayName ELSE '' END,
			@Title,
			@Body,
			-1, -- Feedback
			GETUTCDATE(),
			@CurrentMinuteTS
		)

		IF @@ERROR <> 0 
		BEGIN
			ROLLBACK TRANSACTION 
			SELECT @OUTPUT_RES = 'not_saved'
			RETURN @@ERROR
		END
		ELSE
		BEGIN
			SELECT @OUTPUT_RES = 'saved:0:0'
		END

		DELETE FROM #InsertedIDs
		COMMIT TRANSACTION
	END
	------------------------------------------------------------
	--  A P P   M E S S A G E S
	------------------------------------------------------------



	------------------------------------------------------------
	-- A U T H U S E R S
	------------------------------------------------------------
	IF @Case_ = 'SelectAuthUsersEmail'
	BEGIN	
		IF (
			SELECT COUNT(*)
			FROM dbuser_testsolution4.AuthUsers
			WHERE 
				dbuser_testsolution4.AuthUsers.EmailHash = @EmailHash 
				AND dbuser_testsolution4.AuthUsers.PasswordHash = @PasswordHash
				AND active = CAST(1 AS BIT)
		) > 0
		BEGIN
			UPDATE 
				dbuser_testsolution4.AuthUsers   
			SET 
				dbuser_testsolution4.AuthUsers.LastLogin = GETUTCDATE() --GETDATE()
			WHERE
				dbuser_testsolution4.AuthUsers.EmailHash = @EmailHash
				AND dbuser_testsolution4.AuthUsers.PasswordHash = @PasswordHash
				AND active = CAST(1 AS BIT)
            
			SELECT TOP 1 dbuser_testsolution4.AuthUsers.UnixTS COLLATE Latin1_General_BIN
			FROM dbuser_testsolution4.AuthUsers
			WHERE 
				dbuser_testsolution4.AuthUsers.EmailHash = @EmailHash 
				AND dbuser_testsolution4.AuthUsers.PasswordHash = @PasswordHash
				AND active = CAST(1 AS BIT)
		END
		ELSE
		BEGIN
			SELECT TOP 1 'no_user' AS RES
		END
	END

	IF @Case_ = 'SelectByUnixTS>>AuthUsers'
	BEGIN	
		SELECT * 
		FROM dbuser_testsolution4.AuthUsers 
		WHERE 
			UnixTS COLLATE Latin1_General_BIN = @UnixTS COLLATE Latin1_General_BIN
			AND active = CAST(1 AS BIT)
	END

	IF @Case_ = 'SelectByIdPClientIdent>>AuthUsers'
	BEGIN	
		IF (
			SELECT COUNT(*)
			FROM dbuser_testsolution4.AuthUsers
			WHERE 
				dbuser_testsolution4.AuthUsers.IdPClientIdent = @IdPClientIdent AND active = CAST(1 AS BIT)
		) > 0
		BEGIN
			SET  @IdPToken = ISNULL((
				SELECT TOP 1 dbuser_testsolution4.AuthUsers.UnixTS COLLATE Latin1_General_BIN
				FROM dbuser_testsolution4.AuthUsers
				WHERE 
					dbuser_testsolution4.AuthUsers.IdPClientIdent = @IdPClientIdent
					AND active = CAST(1 AS BIT)
			), '')

			UPDATE 
				dbuser_testsolution4.AuthUsers   
			SET 
				dbuser_testsolution4.AuthUsers.IdPClientIdent = '',
				dbuser_testsolution4.AuthUsers.IdPToken = '',
				dbuser_testsolution4.AuthUsers.LastUpdateUnixTS = @LastUpdateUnixTS
			WHERE
				dbuser_testsolution4.AuthUsers.IdPClientIdent = @IdPClientIdent
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
		IF (SELECT Count(*) FROM dbuser_testsolution4.AuthUsers WHERE EmailHash = @EmailHash) = 0 -- Existiert der Benutzer
		BEGIN
			BEGIN TRANSACTION

			--Neuen Datensatz einfuegen
			INSERT INTO dbuser_testsolution4.AuthUsers (
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
			FROM dbuser_testsolution4.AuthUsers
			WHERE 
				dbuser_testsolution4.AuthUsers.UnixTS COLLATE Latin1_General_BIN = @UnixTS COLLATE Latin1_General_BIN AND active = CAST(1 AS BIT)
		) > 0
		BEGIN
			UPDATE 
				dbuser_testsolution4.AuthUsers   
			SET 
				dbuser_testsolution4.AuthUsers.TermsAccepted = CAST(1 AS BIT)
			WHERE
				dbuser_testsolution4.AuthUsers.UnixTS COLLATE Latin1_General_BIN = @UnixTS COLLATE Latin1_General_BIN
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
			SET @AuthUsers_UnixTS = ISNULL((SELECT TOP 1 UnixTS COLLATE Latin1_General_BIN FROM dbuser_testsolution4.AuthUsers WHERE EmailHash = @EmailHash AND PasswordHash = @PasswordHash AND active = CAST(1 AS BIT)), '')
		END

		-- Prüfen, ob das Passwort korrekt ist
		IF (
			SELECT COUNT(*)
			FROM dbuser_testsolution4.AuthUsers
			WHERE 
				dbuser_testsolution4.AuthUsers.UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
		) > 0
		BEGIN
			IF (
				SELECT COUNT(*)
				FROM dbuser_testsolution4.AppParameter
				WHERE 
					dbuser_testsolution4.AppParameter.AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
					AND dbuser_testsolution4.AppParameter.Scope = 'config'
					AND dbuser_testsolution4.AppParameter.ParameterName = 'OtpBackupCode'
					AND dbuser_testsolution4.AppParameter.ParameterValue = @OtpBackupCode
			) > 0		
			BEGIN
				-- Otp mit NULL überschreiben in AuthUsers (Zurücksetzen von 2FA)
				UPDATE 
					dbuser_testsolution4.AuthUsers   
				SET 
					dbuser_testsolution4.AuthUsers.otp = NULL
				WHERE
					dbuser_testsolution4.AuthUsers.UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN

				-- Backupcode löschen aus AppParameter
				DELETE dbuser_testsolution4.AppParameter 
				WHERE 
					AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
					AND dbuser_testsolution4.AppParameter.Scope = 'config'
					AND dbuser_testsolution4.AppParameter.ParameterName = 'OtpBackupCode'

				-- Anzahl Versuche zurücksetzen
				UPDATE dbuser_testsolution4.AuthUsers
				SET 
					FailedLoginAttempts = 0,
					LastLogin = GETUTCDATE()
				WHERE 
					dbuser_testsolution4.AuthUsers.UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
					
				SELECT  @OUTPUT_RES = 'updated:' + LTRIM(RTRIM(@UnixTS)) + ':' + LTRIM(RTRIM(@UnixTS))
			END
			ELSE
			BEGIN
				-- HIER: Counter hochzählen, da User einen OTP-Versuch startet
				UPDATE dbuser_testsolution4.AuthUsers
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
			UPDATE dbuser_testsolution4.AuthUsers
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
			dbuser_testsolution4.AuthUsers   
		SET 
			dbuser_testsolution4.AuthUsers.otp = NULL
		WHERE
			dbuser_testsolution4.AuthUsers.UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN

		-- Backupcode löschen aus AppParameter
		DELETE dbuser_testsolution4.AppParameter 
		WHERE 
			AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
			AND dbuser_testsolution4.AppParameter.Scope = 'config'
			AND dbuser_testsolution4.AppParameter.ParameterName = 'OtpBackupCode'

		-- Anzahl Versuche zurücksetzen
		UPDATE dbuser_testsolution4.AuthUsers
		SET 
			FailedLoginAttempts = 0,
			LastLogin = GETUTCDATE()
		WHERE 
			dbuser_testsolution4.AuthUsers.UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN;
					
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
			DELETE dbuser_testsolution4.AuthUsersTodo WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @_deleteDataUserUnixTS COLLATE Latin1_General_BIN
			DELETE dbuser_testsolution4.AuthUsersTodo WHERE AuthUsers_ShareFrom_UnixTS COLLATE Latin1_General_BIN = @_deleteDataUserUnixTS COLLATE Latin1_General_BIN

			-- SharingUsers
			DELETE dbuser_testsolution4.SharingUsers WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @_deleteDataUserUnixTS COLLATE Latin1_General_BIN
			DELETE dbuser_testsolution4.SharingUsers WHERE AuthUsers_ShareTo_UnixTS COLLATE Latin1_General_BIN = @_deleteDataUserUnixTS COLLATE Latin1_General_BIN

			-- Tasks
			DELETE dbuser_testsolution4.Tasks WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @_deleteDataUserUnixTS COLLATE Latin1_General_BIN

			-- Todo
			DELETE dbuser_testsolution4.Todo WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @_deleteDataUserUnixTS COLLATE Latin1_General_BIN

			-- AppParameter
			DELETE dbuser_testsolution4.AppParameter WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @_deleteDataUserUnixTS COLLATE Latin1_General_BIN


			IF (
				(SELECT TOP 1 Count(*) FROM dbuser_testsolution4.AuthUsersTodo WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @_deleteDataUserUnixTS COLLATE Latin1_General_BIN)
				+
				(SELECT TOP 1 Count(*) FROM dbuser_testsolution4.SharingUsers WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @_deleteDataUserUnixTS COLLATE Latin1_General_BIN)
				+
				(SELECT TOP 1 Count(*) FROM dbuser_testsolution4.Tasks WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @_deleteDataUserUnixTS COLLATE Latin1_General_BIN)
				+
				(SELECT TOP 1 Count(*) FROM dbuser_testsolution4.Todo WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @_deleteDataUserUnixTS COLLATE Latin1_General_BIN)
				+
				(SELECT TOP 1 Count(*) FROM dbuser_testsolution4.AppParameter WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @_deleteDataUserUnixTS COLLATE Latin1_General_BIN)
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
			DELETE dbuser_testsolution4.AuthUsersExtend WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @_deleteDataUserUnixTS COLLATE Latin1_General_BIN

			--Benuutzer löschen
			DELETE dbuser_testsolution4.AuthUsers WHERE AuthUsers.UnixTS COLLATE Latin1_General_BIN = @_deleteDataUserUnixTS COLLATE Latin1_General_BIN

			IF (
				(SELECT Count(*) FROM dbuser_testsolution4.AuthUsers WHERE AuthUsers.UnixTS COLLATE Latin1_General_BIN = @_deleteDataUserUnixTS COLLATE Latin1_General_BIN)
				+
				(SELECT Count(*) FROM dbuser_testsolution4.AuthUsersExtend WHERE AuthUsers_UnixTS COLLATE Latin1_General_BIN = @_deleteDataUserUnixTS COLLATE Latin1_General_BIN)
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
			FROM dbuser_testsolution4.AuthUsers
			WHERE 
				dbuser_testsolution4.AuthUsers.UnixTS COLLATE Latin1_General_BIN = @UnixTS COLLATE Latin1_General_BIN AND active = CAST(1 AS BIT)
		) > 0
		BEGIN
			UPDATE 
				dbuser_testsolution4.AuthUsers   
			SET 
				dbuser_testsolution4.AuthUsers.TermsAccepted = CAST(1 AS BIT),
				dbuser_testsolution4.AuthUsers.IdPClientIdent = @IdPClientIdent,
				dbuser_testsolution4.AuthUsers.IdPToken = @IdPToken
			WHERE
				dbuser_testsolution4.AuthUsers.UnixTS COLLATE Latin1_General_BIN = @UnixTS COLLATE Latin1_General_BIN
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
			FROM dbuser_testsolution4.AuthUsers
			WHERE 
				dbuser_testsolution4.AuthUsers.UnixTS COLLATE Latin1_General_BIN = @UnixTS COLLATE Latin1_General_BIN
				AND dbuser_testsolution4.AuthUsers.PasswordHash = @PasswordHash
				AND active = CAST(1 AS BIT)
		) > 0
		BEGIN
			-- Neues Passwort setzen
			UPDATE 
				dbuser_testsolution4.AuthUsers   
			SET 
				dbuser_testsolution4.AuthUsers.PasswordHash = @PasswordHashNew,
				dbuser_testsolution4.AuthUsers.LastUpdateUnixTS = @LastUpdateUnixTS
			WHERE
				dbuser_testsolution4.AuthUsers.UnixTS COLLATE Latin1_General_BIN = @UnixTS COLLATE Latin1_General_BIN
				AND dbuser_testsolution4.AuthUsers.PasswordHash = @PasswordHash
				AND dbuser_testsolution4.AuthUsers.active = CAST(1 AS BIT)

			-- Prüfen, ob neues Passwort gesetzt wurde
			IF (
				SELECT COUNT(*)
				FROM dbuser_testsolution4.AuthUsers
				WHERE 
					dbuser_testsolution4.AuthUsers.UnixTS COLLATE Latin1_General_BIN = @UnixTS COLLATE Latin1_General_BIN
					AND dbuser_testsolution4.AuthUsers.PasswordHash = @PasswordHashNew
					AND dbuser_testsolution4.AuthUsers.active = CAST(1 AS BIT)
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
				FROM dbuser_testsolution4.AuthUsers
				WHERE 
					dbuser_testsolution4.AuthUsers.UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
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
			SET @AuthUsers_UnixTS = ISNULL((SELECT TOP 1 UnixTS COLLATE Latin1_General_BIN FROM dbuser_testsolution4.AuthUsers WHERE EmailHash = @EmailHash AND PasswordHash = @PasswordHash AND active = CAST(1 AS BIT)), '')
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
		FROM dbuser_testsolution4.AuthUsers
		WHERE
			UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN

		-- Wenn kein User gefunden
		IF @OtpStatus IS NULL
		BEGIN
			IF ISNULL(@EmailHash, '') <> ''
			BEGIN
				-- Counter hochzählen, da User einen OTP-Versuch startet => Geht nur wenn Email korrekt ist!
				UPDATE dbuser_testsolution4.AuthUsers
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
					UPDATE dbuser_testsolution4.AuthUsers
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
			UPDATE dbuser_testsolution4.AuthUsers
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
		UPDATE dbuser_testsolution4.AuthUsers
		SET FailedLoginAttempts = FailedLoginAttempts + 1,
			LastLogin = GETUTCDATE()
		WHERE 
			UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN

		-- HAUPT-ABFRAGE: OTP-Code zurückgeben
		SELECT 
			ISNULL(CAST(dbuser_testsolution4.AuthUsers.otp AS NVARCHAR(MAX)), '') AS otp
		FROM dbuser_testsolution4.AuthUsers
		WHERE 
			UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
	END

	IF @Case_ = 'ResetFailedAttempts>>AuthUsers'
	BEGIN
		IF ISNULL(@EmailHash, '') <> '' AND ISNULL(@PasswordHash, '') <> ''
		BEGIN
			SET @AuthUsers_UnixTS = ISNULL((SELECT TOP 1 UnixTS COLLATE Latin1_General_BIN FROM dbuser_testsolution4.AuthUsers WHERE EmailHash = @EmailHash AND PasswordHash = @PasswordHash AND active = CAST(1 AS BIT)), '')
		END

		UPDATE dbuser_testsolution4.AuthUsers
		SET 
			FailedLoginAttempts = 0,
			LastLogin = GETUTCDATE()
		WHERE 
			UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
    
		SELECT 'saved' AS Result
	END

	IF @Case_ = 'SelectIdP>>AuthUsers'
	BEGIN
		SELECT ISNULL((
			SELECT IdP
			FROM dbuser_testsolution4.AuthUsers
			WHERE 
				dbuser_testsolution4.AuthUsers.UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN AND active = CAST(1 AS BIT)
		), '') AS RES
	END

	IF @Case_ = 'SelectTermsAccepted>>AuthUsers'
	BEGIN
		SELECT ISNULL((
			SELECT CAST(dbuser_testsolution4.AuthUsers.TermsAccepted AS BIT) AS TermsAccepted
			FROM dbuser_testsolution4.AuthUsers
			WHERE 
				dbuser_testsolution4.AuthUsers.UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN AND active = CAST(1 AS BIT)
		), 0) AS RES
	END

	IF @Case_ = 'SaveOtp>>AuthUsers'
	BEGIN
		IF ISNULL(@EmailHash, '') <> '' AND ISNULL(@PasswordHash, '') <> ''
		BEGIN
			SET @AuthUsers_UnixTS = ISNULL((SELECT TOP 1 UnixTS COLLATE Latin1_General_BIN FROM dbuser_testsolution4.AuthUsers WHERE EmailHash = @EmailHash AND PasswordHash = @PasswordHash AND active = CAST(1 AS BIT)), '')
		END

		IF (
			SELECT COUNT(*)
			FROM dbuser_testsolution4.AuthUsers
			WHERE 
				dbuser_testsolution4.AuthUsers.UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
				AND active = CAST(1 AS BIT)
		) > 0
		BEGIN
			-- Otp-Code beim User setzen
			UPDATE 
				dbuser_testsolution4.AuthUsers   
			SET 
				dbuser_testsolution4.AuthUsers.otp = CASE WHEN ISNULL(@otp, '') = '' THEN NULL ELSE CAST(@otp AS VARBINARY(MAX)) END
			WHERE
				dbuser_testsolution4.AuthUsers.UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
            
			-- Backup Code des Benutzers in den Settings löschen (falls vorhanden)
			DELETE FROM dbuser_testsolution4.AppParameter 
			WHERE 
				AuthUsers_UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
				AND ParameterValue = 'OtpBackupCode'

			-- Backup Code in den Settings speichern
			INSERT INTO dbuser_testsolution4.AppParameter (
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
			SET @AuthUsers_UnixTS = ISNULL((SELECT TOP 1 UnixTS COLLATE Latin1_General_BIN FROM dbuser_testsolution4.AuthUsers WHERE EmailHash = @EmailHash AND PasswordHash = @PasswordHash AND active = CAST(1 AS BIT)), '')
		END

		SELECT TOP 1 
			CASE 
				WHEN otp IS NOT NULL AND otp != '' THEN CAST(1 AS BIT)
				ELSE CAST(0 AS BIT)
			END AS HasOTP
		FROM dbuser_testsolution4.AuthUsers
		WHERE 
			UnixTS COLLATE Latin1_General_BIN = @AuthUsers_UnixTS COLLATE Latin1_General_BIN
	END

	IF @Case_ = 'ExistsOtp>>AuthUsers'
	BEGIN
		SELECT  
			CASE 
				WHEN EXISTS (
					SELECT 1
					FROM dbuser_testsolution4.AuthUsers
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
					FROM dbuser_testsolution4.AuthUsers
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
					FROM dbuser_testsolution4.AuthUsers
					WHERE 
						dbuser_testsolution4.AuthUsers.EmailHash = @EmailHash 
						AND dbuser_testsolution4.AuthUsers.PasswordHash = @PasswordHash
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
			SET @AuthUsers_UnixTS = ISNULL((SELECT TOP 1 UnixTS COLLATE Latin1_General_BIN FROM dbuser_testsolution4.AuthUsers WHERE EmailHash = @EmailHash AND PasswordHash = @PasswordHash AND active = CAST(1 AS BIT)), '')
		END

		-- Erfolgreicher Login: Counter zurücksetzen
		UPDATE dbuser_testsolution4.AuthUsers
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
