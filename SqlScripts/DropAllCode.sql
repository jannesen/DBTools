
--==============================================================================
PRINT '# kill database connection'
GO
DECLARE @cmd        varchar(128)

DECLARE ccmd CURSOR LOCAL STATIC
    FOR SELECT 'KILL '+convert(varchar, s.[session_id])
          FROM (
                    SELECT DISTINCT [request_session_id]
                      FROM master.sys.[dm_tran_locks]
                     WHERE [resource_type]        = 'DATABASE'
                       AND [resource_database_id] = db_id()
               ) l
               INNER JOIN master.sys.[dm_exec_sessions] s ON s.[session_id] = l.[request_session_id]
         WHERE s.[session_id]         <> @@spid
           AND s.[is_user_process]    =  1

OPEN ccmd
FETCH ccmd INTO @cmd
WHILE @@fetch_status=0
BEGIN
    EXECUTE(@cmd)
    FETCH ccmd INTO @cmd
END
DEALLOCATE ccmd
GO

--==============================================================================
PRINT '# drop all code.'
GO
DECLARE @cmd    NVARCHAR(1024);

DECLARE ccmd CURSOR LOCAL STATIC
    FOR SELECT [cmd]
          FROM (
                    SELECT [order]  = CASE [type] WHEN 'TR' THEN 1
                                                  WHEN 'TA' THEN 1
                                                  WHEN 'P'  THEN 2
                                                  WHEN 'PC' THEN 2
                                                  WHEN 'TF' THEN 4
                                                  WHEN 'FT' THEN 4
                                                  WHEN 'FN' THEN 5
                                                  WHEN 'FS' THEN 5
                                                  WHEN 'IF' THEN 6
                                                  WHEN 'AF' THEN 7
                                                  WHEN 'V'  THEN 8
                                      END,
                           [cmd]    = CASE [type] WHEN 'TR' THEN N'DROP TRIGGER   '
                                                  WHEN 'TA' THEN N'DROP TRIGGER   '
                                                  WHEN 'P'  THEN N'DROP PROCEDURE '
                                                  when 'PC' THEN N'DROP PROCEDURE '
                                                  WHEN 'TF' THEN N'DROP FUNCTION  '
                                                  WHEN 'FT' THEN N'DROP FUNCTION  '
                                                  WHEN 'FN' THEN N'DROP FUNCTION  '
                                                  WHEN 'FS' THEN N'DROP FUNCTION  '
                                                  WHEN 'IF' THEN N'DROP FUNCTION  '
                                                  WHEN 'AF' THEN N'DROP FUNCTION  '
                                                  WHEN 'V'  THEN N'DROP VIEW      '
                                      END +
                                      QUOTENAME(SCHEMA_NAME([schema_id]))+'.'+QUOTENAME([name])
                      FROM sys.[objects] o
                     WHERE [is_ms_shipped] = 0
                       AND NOT EXISTS (SELECT *
                                         FROM sys.extended_properties p
                                        WHERE p.[major_id] = o.object_id
                                          AND p.[minor_id] = 0
                                          AND p.[class]    = 1
                                          AND p.[name]     = N'microsoft_database_tools_support')
                    UNION ALL
                    SELECT [order]  = 10,
                           [cmd]    = 'DROP TYPE ' + quotename(schema_name(t.[schema_id])) + N'.' + quotename(t.[name])
                      FROM sys.types t
                     WHERE t.[is_user_defined] = 1
                       AND NOT EXISTS (SELECT *
                                         FROM sys.columns c
                                              INNER JOIN sys.objects o on o.[object_id] = c.[object_id]
                                        WHERE c.[user_type_id] = t.[user_type_id]
                                          AND o.[type] = 'U')
               ) x
         WHERE [cmd] is not null
      ORDER BY [order], [cmd];

OPEN ccmd;

FETCH ccmd INTO @cmd;
WHILE @@fetch_status = 0
BEGIN
    EXECUTE(@cmd);
    FETCH ccmd INTO @cmd;
END

DEALLOCATE ccmd;
GO
