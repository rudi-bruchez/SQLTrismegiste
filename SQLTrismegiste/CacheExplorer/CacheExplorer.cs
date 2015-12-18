using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PoorMansTSqlFormatterLib.Formatters;
using System.IO;
using System.Diagnostics;
using SimpleLogger;
using System.Xml;

namespace SQLTrismegiste.CacheExplorer
{
    internal class PlanCache
    {
        private static TSqlStandardFormatterOptions _formatterOptions = new TSqlStandardFormatterOptions
        {
            KeywordStandardization = true,
            IndentString = "\t",
            SpacesPerTab = 4,
            MaxLineWidth = 999,
            NewStatementLineBreaks = 2,
            NewClauseLineBreaks = 1,
            TrailingCommas = false,
            SpaceAfterExpandedComma = false,
            ExpandBetweenConditions = true,
            ExpandBooleanExpressions = true,
            ExpandCaseStatements = true,
            ExpandCommaLists = true,
            BreakJoinOnSections = false,
            UppercaseKeywords = true,
            ExpandInLists = true
        };

        private SqlConnection _cn;
        private DataTable _dt;

        private readonly Dictionary<int, string> _query = new Dictionary<int, string>()
        {
            { 9, @"SELECT TOP 100 qs.*, st.text, qp.query_plan
                    FROM sys.dm_exec_query_stats qs
                    CROSS APPLY sys.dm_exec_sql_text(qs.plan_handle) st
                    CROSS APPLY sys.dm_exec_query_plan(qs.plan_handle) qp
                    WHERE qs.execution_count > 1
                    ORDER BY qs.execution_count DESC" },
            { 11, @"SELECT TOP 100
                	MIN(st.text) as text, 
                    (SELECT qp.query_plan FROM sys.dm_exec_query_plan(MIN(qs.plan_handle)) qp) as query_plan, 
                    SUM(qs.execution_count) as execution_count,
                    MIN(qs.creation_time) as creation_time, 
                    MAX(qs.last_execution_time) as last_execution_time, 
                    SUM(qs.total_elapsed_time) as total_elapsed_time,
                    SUM(qs.total_worker_time) as total_worker_time, 
                    MIN(qs.min_worker_time) as min_worker_time, 
                    MAX(qs.max_worker_time) as max_worker_time, 
                    SUM(qs.total_logical_writes) as total_logical_writes, 
                    MIN(qs.min_logical_writes) as min_logical_writes, 
                    MAX(qs.max_logical_writes) as max_logical_writes, 
                    SUM(qs.total_logical_reads) as total_logical_reads, 
                    MIN(qs.min_logical_reads) as min_logical_reads, 
                    MAX(qs.max_logical_reads) as max_logical_reads, 
                    SUM(qs.total_clr_time) as total_clr_time, 
                    MIN(qs.min_clr_time) as min_clr_time, 
                    MAX(qs.max_clr_time) as max_clr_time, 
                    SUM(qs.total_elapsed_time) as total_elapsed_time, 
                    MIN(qs.min_elapsed_time) as min_elapsed_time, 
                    MIN(qs.max_elapsed_time) as max_elapsed_time, 
                    SUM(qs.total_rows) as total_rows, 
                    MIN(qs.min_rows) as min_rows, 
                    MAX(qs.max_rows) as max_rows
                    FROM sys.dm_exec_query_stats qs
                    CROSS APPLY sys.dm_exec_sql_text(qs.plan_handle) st
                    GROUP BY qs.query_hash
                    ORDER BY SUM(qs.execution_count) DESC;" }
        };

        public PlanCache(string connectionString)
        {
            _cn = new SqlConnection(connectionString);
        }

        public DataView Explore(int version)
        {
            var v = (from ve in _query.Keys
                    where (ve <= version)
                    orderby ve
                    select ve).FirstOrDefault();

            using (var cmd = new SqlCommand(_query[v], _cn))
            {
                if (_cn.State == ConnectionState.Closed) _cn.Open();

                _dt = new DataTable("PlanCache");
                using (var a = new SqlDataAdapter(cmd))
                {
                    a.Fill(_dt);
                }

                BuildDetails(_dt);
                SetPlanWarnings(_dt);
                SetPlanMissingIndexes(_dt);
                TokenizeSql(_dt, "text");
                return _dt.DefaultView;
            }
        }

        public DataView Filter(string filterValue)
        {
            if (String.IsNullOrWhiteSpace(filterValue))
            {
                return _dt.DefaultView;
            }
            else
            {
                DataView dv = new DataView(_dt);
                dv.RowFilter = $"text LIKE '%{filterValue.Replace("'", "''")}%'";
                return dv;
            }
        }

        private void SetPlanWarnings(DataTable dt)
        {
            var col = dt.Columns.Add("warnings").Ordinal;

            var x = new XmlDocument();
            var mgr = new XmlNamespaceManager(x.NameTable);
            mgr.AddNamespace("ns", "http://schemas.microsoft.com/sqlserver/2004/07/showplan");

            foreach (DataRow r in dt.Rows)
            {
                var plan = r.Field<string>("query_plan");
                if (plan == null) continue;
                x.LoadXml(plan);
                if (x.SelectNodes("//ns:Warnings", mgr).Count > 0)
                {
                    r[col] = "YES";
                }
            }
        }

        private void SetPlanMissingIndexes(DataTable dt)
        {
            var col = dt.Columns.Add("missing_index").Ordinal;

            var x = new XmlDocument();
            var mgr = new XmlNamespaceManager(x.NameTable);
            mgr.AddNamespace("ns", "http://schemas.microsoft.com/sqlserver/2004/07/showplan");

            foreach (DataRow r in dt.Rows)
            {
                var plan = r.Field<string>("query_plan");
                if (plan == null) continue;
                x.LoadXml(plan);
                if (x.SelectNodes("//ns:MissingIndexes", mgr).Count > 0)
                {
                    r[col] = "YES";
                }
            }
        }

        private void BuildDetails(DataTable dt)
        {
            var col = dt.Columns.Add("details").Ordinal;
            foreach (DataRow r in dt.Rows)
            {
                var detail = "";
                var starts = new string[] { "total_", "min_", "max_", "last_" };

                foreach (DataColumn c in dt.Columns.Cast<DataColumn>().Where(
                    c => starts.Any(s => c.ColumnName.StartsWith(s))))
                {
                    long val = 0;
                    long.TryParse(r[c].ToString(), out val);
                    if (val > 6000000 && c.ColumnName.EndsWith("_time"))
                        { detail += $"{c.ColumnName} : {Math.Round((double)val / 60000000, 2)}m - "; }
                    else if (val > 1000000 && c.ColumnName.EndsWith("_time"))
                        { detail += $"{c.ColumnName} : {Math.Round((double)val / 1000000, 2)}s - "; }
                    else if (val > 1000 && c.ColumnName.EndsWith("_time"))
                        { detail += $"{c.ColumnName} : {Math.Round((double)val / 1000, 2)}ms - "; }
                    else if (val > 0)
                        { detail += $"{c.ColumnName} : {val} - "; }
                }
                r[col] = detail;
            }
        }

        private void TokenizeSql(DataTable dt, string columnName)
        {
            var col = dt.Columns.Add("short_query").Ordinal;

            var tokenizer = new PoorMansTSqlFormatterLib.Tokenizers.TSqlStandardTokenizer();
            var parser = new PoorMansTSqlFormatterLib.Parsers.TSqlStandardParser();
            foreach (DataRow r in dt.Rows)
            {
                var qry = r.Field<string>(columnName);
                if (qry.Length <= 100)
                {
                    r[col] = qry;
                }
                else
                {
                    var res = parser.ParseSQL(
                        tokenizer.TokenizeSQL(qry)
                        );
                    var txt = "";
                    var tmp = res.SelectSingleNode("/SqlRoot/SqlStatement/Clause/OtherKeyword");
                    if (tmp != null) txt += tmp.InnerText + " [...] ";

                    var tmp2 = res.SelectNodes("/SqlRoot/SqlStatement/*/SelectionTarget");
                    if (tmp2 != null)
                    {
                        txt += String.Join(", ", tmp2.Cast<XmlNode>().Select(x => x.InnerText).Take(5));
                    }
                    if (String.IsNullOrWhiteSpace(txt)) txt = qry.Substring(1, 100);
                    r[col] = txt;
                }
                //res document xml
            }
        }

        private void FormatSql(DataTable dt, string columnName)
        {
            var formatter = new PoorMansTSqlFormatterLib.Formatters.TSqlStandardFormatter(_formatterOptions);

            //formatter.
        }

        public void ViewQueryPlan(DataRow dr)
        {
            if (dr == null) return;

            try
            {
                var output = $@"{Path.GetTempPath()}\{Guid.NewGuid().ToString()}.sqlplan";
                File.WriteAllText(output, dr.Field<string>("query_plan"));
                Process.Start(output);
            }
            catch (Exception e)
            {
                var msg = $"Error in query plan generation in Cache Explorer. Error is : {e.Message}";
                SimpleLog.Error(msg);
            }
        }

        internal void ViewQueryText(DataRow dr)
        {
            if (dr == null) return;

            try
            {
                var output = $@"{Path.GetTempPath()}\{Guid.NewGuid().ToString()}.sql";
                File.WriteAllText(output, dr.Field<string>("text"));
                Process.Start(output);
            }
            catch (Exception e)
            {
                var msg = $"Error in sql text generation in Cache Explorer. Error is : {e.Message}";
                SimpleLog.Error(msg);
            }
        }
    }
}
