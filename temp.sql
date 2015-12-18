-- oldest plan in cache : MIN()
-- historique des sauvegardes

-- cardinality estimation differences
-- adapted from http://www.sqlskills.com/blogs/joe/detecting-cardinality-estimate-issues-with-sys-dm_exec_query_stats/
;WITH cte AS
(
SELECT  TOP 100 
		LTRIM(t.text) as sql_text,
        p.[query_plan],
        s.[last_execution_time],
		s.execution_count,
        p.[query_plan].value('(//@EstimateRows)[1]', 'varchar(128)') AS [estimated_rows],
        CAST(s.[last_rows] as float) as Last_RowCount
FROM    sys.[dm_exec_query_stats] AS [s]
CROSS APPLY sys.[dm_exec_sql_text](sql_handle) AS [t]
CROSS APPLY sys.[dm_exec_query_plan](plan_handle) AS [p]
WHERE p.[query_plan] IS NOT NULL   
AND DATEDIFF(mi, s.[last_execution_time], GETDATE()) < 1
)
SELECT * 
FROM cte
ORDER BY (ABS(Last_RowCount - estimated_rows)) DESC
OPTION (RECOMPILE);

SELECT  t.text,
        p.[query_plan],
        s.[last_execution_time],
        p.[query_plan].value('(//@EstimateRows)[1]', 'varchar(128)') AS [estimated_rows],
        CAST(s.[last_rows] as float) as Last_RowCount,
		s.*
FROM    sys.[dm_exec_query_stats] AS [s]
CROSS APPLY sys.[dm_exec_sql_text](sql_handle) AS [t]
CROSS APPLY sys.[dm_exec_query_plan](plan_handle) AS [p]
WHERE t.text LIKE '%SELECT "MANDT"%'

USE TempDB
GO
 
SELECT	DF.name AS logical_name
	, DF.physical_name AS physical_name
	, DF.type_desc AS file_type_desc
	, CASE DF.is_percent_growth
		WHEN 0 THEN CASE DF.growth WHEN 0 THEN 'DISABLED' ELSE CAST(DF.growth / 128 AS varchar(20)) + ' MB' END
		ELSE CAST(DF.growth AS varchar(3)) + ' %'
	END AS growth
	, CAST(DF.size / 128.0 AS decimal(14, 2)) AS file_size_MB
	, CAST(FILEPROPERTY(DF.name, 'SpaceUsed') / 128.0 AS decimal(14, 2)) AS occupied_size_MB
	, CASE DF.max_size
		WHEN -1 THEN 'UNLIMITED'
		WHEN 0 THEN 'DISABLED'
		ELSE CAST(CAST(DF.max_size / 128.0 AS bigint) AS varchar(20)) 
	END AS max_size_MB
FROM	sys.database_files AS DF

-- oldest plan in cache : MIN()
-- historique des sauvegardes

-- cardinality estimation differences
-- adapted from http://www.sqlskills.com/blogs/joe/detecting-cardinality-estimate-issues-with-sys-dm_exec_query_stats/
;WITH cte AS
(
SELECT  TOP 100 
		LTRIM(t.text) as sql_text,
        p.[query_plan],
        s.[last_execution_time],
		s.execution_count,
        p.[query_plan].value('(//@EstimateRows)[1]', 'varchar(128)') AS [estimated_rows],
        CAST(s.[last_rows] as float) as Last_RowCount
FROM    sys.[dm_exec_query_stats] AS [s]
CROSS APPLY sys.[dm_exec_sql_text](sql_handle) AS [t]
CROSS APPLY sys.[dm_exec_query_plan](plan_handle) AS [p]
WHERE p.[query_plan] IS NOT NULL   
AND DATEDIFF(mi, s.[last_execution_time], GETDATE()) < 1
)
SELECT * 
FROM cte
ORDER BY (ABS(Last_RowCount - estimated_rows)) DESC
OPTION (RECOMPILE);

SELECT  t.text,
        p.[query_plan],
        s.[last_execution_time],
        p.[query_plan].value('(//@EstimateRows)[1]', 'varchar(128)') AS [estimated_rows],
        CAST(s.[last_rows] as float) as Last_RowCount,
		s.*
FROM    sys.[dm_exec_query_stats] AS [s]
CROSS APPLY sys.[dm_exec_sql_text](sql_handle) AS [t]
CROSS APPLY sys.[dm_exec_query_plan](plan_handle) AS [p]
WHERE t.text LIKE '%SELECT "MANDT"%'

-- triggers
SELECT plan_handle, query_plan, objtype 
FROM sys.dm_exec_cached_plans 
CROSS APPLY sys.dm_exec_query_plan(plan_handle) 
WHERE objtype ='Trigger';
GO

-- set options
SELECT plan_handle, pvt.set_options, pvt.sql_handle
FROM (
      SELECT plan_handle, epa.attribute, epa.value 
      FROM sys.dm_exec_cached_plans 
      OUTER APPLY sys.dm_exec_plan_attributes(plan_handle) AS epa
      WHERE cacheobjtype = 'Compiled Plan'
      ) AS ecpa 
PIVOT (MAX(ecpa.value) FOR ecpa.attribute IN ("set_options", "sql_handle")) AS pvt;
GO

-- memory of caches plans
SELECT plan_handle, ecp.memory_object_address AS CompiledPlan_MemoryObject, 
    omo.memory_object_address, pages_allocated_count, type, page_size_in_bytes 
FROM sys.dm_exec_cached_plans AS ecp 
JOIN sys.dm_os_memory_objects AS omo 
    ON ecp.memory_object_address = omo.memory_object_address 
    OR ecp.memory_object_address = omo.parent_address
WHERE cacheobjtype = 'Compiled Plan';
GO

-- Returns a row for each Transact-SQL execution plan, common language runtime (CLR) 
-- execution plan, and cursor associated with a plan.
-- https://msdn.microsoft.com/en-us/library/ms403826.aspx


-- Top Queries by Query Hash
-- SQLCanada, Mohit K. Gupta
-- Script from https://sqlcan.wordpress.com
-- Last Updated: May 12, 2012
------------------------------------------
 
WITH QSD (SQLStatement, PlanHandle, NumOfExecutions, Duration_ms, CPU_ms, Reads, Writes, QueryHash)
AS (SELECT   SUBSTRING(st.text,
                       (qs.statement_start_offset/2)+1,
                        ((CASE qs.statement_end_offset WHEN -1 THEN
                             DATALENGTH(st.text)
                          ELSE
                             qs.statement_end_offset
                          END - qs.statement_start_offset)/2) + 1)  AS SQLStatement
            , qs.plan_handle AS PlanHandle
            , execution_count AS NumOfExecutions
            , total_elapsed_time/1000 AS Duration_ms
            , total_worker_time/1000 AS CPU_ms
            , total_logical_reads AS Reads
            , total_logical_writes AS Writes
            , query_hash AS QueryHash
       FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text (qs.sql_handle) st
      WHERE query_hash != 0x0000000000000000)
 
  SELECT QSD.QueryHash,
         MIN(QSD.SQLStatement) AS SQLStatement,
          MIN(QSD.PlanHandle)   AS PlanHandle,
         SUM(QSD.NumOfExecutions) AS TotalNumOfExecutions,
         SUM(QSD.Duration_ms)/SUM(QSD.NumOfExecutions) AS AvgDuration_ms,
         SUM(QSD.CPU_ms)/SUM(QSD.NumOfExecutions) AS AvgCPU_ms,
         SUM(QSD.Reads)/SUM(QSD.NumOfExecutions) AS AvgReads,
         SUM(QSD.Writes)/SUM(QSD.NumOfExecutions) AS AvgWrites
    FROM QSD
GROUP BY QueryHash
