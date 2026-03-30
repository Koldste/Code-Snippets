-- Drop/Create database
USE [monsters_inc]

IF EXISTS (SELECT * FROM sys.tables WHERE [name] = 'database_metadata')
BEGIN
DROP TABLE database_metadata;
END
IF NOT EXISTS (SELECT * FROM sys.tables WHERE [name] = 'database_metadata')
BEGIN
    CREATE TABLE database_metadata (
    [database_name] NVARCHAR(128),  
    [database_size_in_MB] decimal(15, 2),  
    [unallocated_space_in_MB] decimal(15, 2)
	--,  
 --   [reserved_in_KB] decimal(15, 2),  
 --   [data_in_KB] decimal(15, 2),  
 --   [index_size_in_KB] decimal(15, 2),
	--[unused_in_KB] decimal(15, 2)
    );
END

----------------------------------------------------------------------------
-- Drop/Create user
USE [master]
GO

/****** Object:  Login [<REDACTED>]    Script Date: 15-10-2023 09:49:04 ******/
DROP LOGIN [<REDACTED>]
GO

/* For security reasons the login is created disabled and with a random password. */
/****** Object:  Login [<REDACTED>]    Script Date: 15-10-2023 09:49:04 ******/
-- PASSWORD: 
CREATE LOGIN [<REDACTED>] 
	WITH PASSWORD=N'<REDACTED>', 
	DEFAULT_DATABASE=[monsters_inc], 
	DEFAULT_LANGUAGE=[us_english], 
	CHECK_EXPIRATION=OFF, 
	CHECK_POLICY=ON
GO

--ALTER LOGIN [<REDACTED>] DISABLE
GO

----------------------------------------------------------------------------
-- Drop/create credential
IF EXISTS (SELECT 1 FROM sys.credentials WHERE name = 'Credential_For_Monsters_Inc_Agent')
BEGIN
    DROP CREDENTIAL [Credential_For_Monsters_Inc_Agent];
END

CREATE CREDENTIAL [Credential_For_Monsters_Inc_Agent] WITH IDENTITY = '<REDACTED>',   
    SECRET = '<REDACTED>';  
GO  

----------------------------------------------------------------------------
-- Drop/Create proxy.
USE [msdb]
GO

/****** Object:  ProxyAccount [[Proxy_For_Monsters_Inc_Agent]]    Script Date: 15-10-2023 11:53:00 ******/
EXEC msdb.dbo.sp_delete_proxy @proxy_name=N'[Proxy_For_Monsters_Inc_Agent]'
GO

/****** Object:  ProxyAccount [[Proxy_For_Monsters_Inc_Agent]]    Script Date: 15-10-2023 11:53:00 ******/
EXEC msdb.dbo.sp_add_proxy @proxy_name=N'[Proxy_For_Monsters_Inc_Agent]',@credential_name=N'Credential_For_Monsters_Inc_Agent', 
		@enabled=1, 
		@description=N'To run the job by the name ''Database_metadata_update_and_delete_old'', automatically with a shedule.'
GO

EXEC msdb.dbo.sp_grant_login_to_proxy @proxy_name=N'[Proxy_For_Monsters_Inc_Agent]', @login_name=N'<REDACTED>'
GO




----------------------------------------------------------------------------
-- Drop/Create job with command/step and shedule.
USE [msdb]
GO

/****** Object:  Job [Database_metadata_update_and_delete_old]    Script Date: 12-10-2023 17:20:17 ******/
EXEC msdb.dbo.sp_delete_job @job_name=N'Database_metadata_update_and_delete_old', @delete_unused_schedule=1
GO

/****** Object:  Job [Database_metadata_update_and_delete_old]    Script Date: 12-10-2023 17:20:17 ******/
BEGIN TRANSACTION
DECLARE @ReturnCode INT
SELECT @ReturnCode = 0
/****** Object:  JobCategory [Data Collector]    Script Date: 12-10-2023 17:20:17 ******/
IF NOT EXISTS (SELECT name FROM msdb.dbo.syscategories WHERE name=N'Data Collector' AND category_class=1)
BEGIN
EXEC @ReturnCode = msdb.dbo.sp_add_category @class=N'JOB', @type=N'LOCAL', @name=N'Data Collector'
IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback

END

DECLARE @jobId BINARY(16)
EXEC @ReturnCode =  msdb.dbo.sp_add_job @job_name=N'Database_metadata_update_and_delete_old', 
		@enabled=1, 
		@notify_level_eventlog=0, 
		@notify_level_email=0, 
		@notify_level_netsend=0, 
		@notify_level_page=0, 
		@delete_level=0, 
		@description=N'Gemmer information om størrelse og ledig plads for hver database, undtagen de 4 systemdatabaser. Denne data vil blive gemt i databasen ''database_metadata''  Alt der er mere end 6 måneder gammelt vil blive slettet. Det samme gælder alle nyere entries, på den første entry for hver måned samt en daglig entry.', 
		@category_name=N'Data Collector', 
		@owner_login_name=N'<REDACTED>', @job_id = @jobId OUTPUT
IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback
/****** Object:  Step [Update_database_metadata]    Script Date: 12-10-2023 17:20:18 ******/
EXEC @ReturnCode = msdb.dbo.sp_add_jobstep @job_id=@jobId, @step_name=N'Update_database_metadata', 
		@step_id=1, 
		@cmdexec_success_code=0, 
		@on_success_action=1, 
		@on_success_step_id=0, 
		@on_fail_action=2, 
		@on_fail_step_id=0, 
		@retry_attempts=3, 
		@retry_interval=5, 
		@os_run_priority=0, @subsystem=N'TSQL', 
		@command=N'EXEC sp_MSforeachdb N''
USE [?];

IF DB_ID() > 4 -- Exclude system databases 
BEGIN  
USE [?];

    DECLARE @temp_spaceused TABLE  
    (  
        [database_name] NVARCHAR(128),  
	    [database_size] VARCHAR(18),
		[unallocated space] VARCHAR(18),
        [reserved] NVARCHAR(18),  
        [data] NVARCHAR(18),  
        [index_size] NVARCHAR(18),  
        [unused] NVARCHAR(18)  
    )  
  
    INSERT INTO @temp_spaceused EXEC sp_spaceused @oneresultset = 1;  

    INSERT INTO monsters_inc.dbo.database_metadata
    SELECT   
        [database_name],   
        CAST(REPLACE([database_size], '''' MB'''', '''''''') AS decimal(17, 2)) AS [database_size_in_MB],  
		CAST(REPLACE([unallocated space], '''' MB'''', '''''''') AS decimal(17, 2)) AS [unallocated_space_in_MB]
		--,
		--CAST(REPLACE([reserved], '''' KB'''', '''''''') AS decimal(17, 2)) AS [reserved_in_KB], 
  --      CAST(REPLACE([data], '''' KB'''', '''''''') AS decimal(17, 2)) AS [data_in_KB],  
  --      CAST(REPLACE([index_size], '''' KB'''', '''''''') AS decimal(17, 2)) AS [index_size_in_KB],  
  --      CASTREPLACE([unused], '''' KB'''', '''''''') AS decimal(17, 2)) AS [unused_in_KB]
    FROM @temp_spaceused;

SELECT * FROM @temp_spaceused; -- SLET IGEN.

END;  
'';', 
		@database_name=N'monsters_inc', 
		@flags=0
IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback
EXEC @ReturnCode = msdb.dbo.sp_update_job @job_id = @jobId, @start_step_id = 1
IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback
EXEC @ReturnCode = msdb.dbo.sp_add_jobschedule @job_id=@jobId, @name=N'Dayíly_at_midnight', 
		@enabled=1, 
		@freq_type=4, 
		@freq_interval=1, 
		@freq_subday_type=1, 
		@freq_subday_interval=0, 
		@freq_relative_interval=0, 
		@freq_recurrence_factor=0, 
		@active_start_date=20231012, 
		@active_end_date=99991231, 
		@active_start_time=0, 
		@active_end_time=235959, 
		@schedule_uid=N'<REDACTED>'
IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback
EXEC @ReturnCode = msdb.dbo.sp_add_jobserver @job_id = @jobId, @server_name = N'(local)'
IF (@@ERROR <> 0 OR @ReturnCode <> 0) GOTO QuitWithRollback
COMMIT TRANSACTION
GOTO EndSave
QuitWithRollback:
    IF (@@TRANCOUNT > 0) ROLLBACK TRANSACTION
EndSave:
GO


