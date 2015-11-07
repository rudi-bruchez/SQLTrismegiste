using SimpleLogger;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;

namespace SQLTrismegiste.SqlServer
{
    internal enum ExtractionType
    {
        QueryPlans,
        SqlText
    }

    internal class CacheObject
    {
        public string PlanHandle { get; set; }
        public string SqlHandle { get; set; }
        public string SqlText { get; set; }
        public string QueryPlan { get; set; }
        public string OutputFile { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// plan_handle is varbinary(64)
    internal class CacheExtractor
    {
        public Dictionary<string, CacheObject> HandlesList { get; set; } = new Dictionary<string, CacheObject>();
        public string OutputParentFolder { get; set; }
        private SqlConnection _cn;

        public CacheExtractor(SqlConnection cn)
        {
            _cn = cn;
        }

        private string XmlParameterList()
        {
            return "<params><p>" + String.Join("</p><p>", HandlesList.Keys.ToArray()) + "</p></params>";
        }

        public bool DetectFromDataTable(ExtractionType et, DataTable dt)
        {
            return dt.Columns.Cast<DataColumn>().Any(
                    c => (c.ColumnName == "query_hash" && et == ExtractionType.QueryPlans)
                    ||
                    ((c.ColumnName == "query_hash" || c.ColumnName == "sql_hash") && et == ExtractionType.SqlText));
        }

        public bool ExtractFromDataTable(ExtractionType et, DataTable dt)
        {
            if (!DetectFromDataTable(et, dt)) return false;

            var col = (dt.Columns.Cast<DataColumn>().First(
                    c => (c.ColumnName == "query_hash" && et == ExtractionType.QueryPlans)
                    ||
                    ((c.ColumnName == "query_hash" || c.ColumnName == "sql_hash") && et == ExtractionType.SqlText))).Ordinal;

            HandlesList = dt.Rows.Cast<DataRow>().Select(r => r.Field<String>(col)).ToDictionary(k => k, v => new CacheObject() );

            if (Extract(et))
            {
                try
                {
                    SaveAll(et);
                }
                catch (Exception e)
                {
                    SimpleLog.Error(e.Message);
                    return false;
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        public bool Extract(ExtractionType et)
        {
            var qry = new StringBuilder("DECLARE @params as XML;");
            qry.Append($"SET @params = '{XmlParameterList()}';");
            qry.Append("WITH params AS (SELECT p.value('.', 'varbinary(64)') as handle FROM @params.nodes('/params/p') n(p)) "); // TODO
            switch (et)
            {
                case ExtractionType.QueryPlans:
                    qry.Append("SELECT params.handle, qp.query_plan FROM params CROSS APPLY sys.dm_exec_query_plan(params.handle) as qp");
                    break;
                case ExtractionType.SqlText:
                    qry.Append("SELECT params.handle, st.text FROM params CROSS APPLY sys.dm_exec_sql_text(params.handle) as st");
                    break;
                default:
                    //throw new InvalidOperationException("Unknown ExtractionType");
                    return false;
                    break;
            }

            try
            {
                if (_cn.State == ConnectionState.Closed) _cn.Open();

                using (var cmd = new SqlCommand(qry.ToString(), _cn))
                {
                    var rd = cmd.ExecuteReader();
                    while (rd.Read())
                    {
                        var co = new CacheObject();

                        switch (et)
                        {
                            case ExtractionType.QueryPlans:
                                co.PlanHandle = rd.GetString(0);
                                co.QueryPlan = rd.GetString(1);
                                break;
                            case ExtractionType.SqlText:
                                co.SqlHandle = rd.GetString(0);
                                co.SqlText = rd.GetString(1);
                                break;
                            default:
                                break;
                        }

                        HandlesList[rd.GetString(0)] = co;
                    }
                    rd.Close();
                }
            }
            catch (SqlException e)
            {
                var msg =
                    $"SQL exception : {e.Number}, {e.Message}";
                SimpleLog.Error(msg);
                return false;
            }
            return true;
        }

        public void SaveAll(ExtractionType et)
        {
            var outputFolder = $"{OutputParentFolder}\\{et.ToString()}\\";
            string extension;
            switch (et)
            {
                case ExtractionType.QueryPlans:
                    extension = ".sqlplan";
                    break;
                case ExtractionType.SqlText:
                    extension = ".sql";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(et), et, null);
            }

            foreach (var co in HandlesList)
            {
                var file = $"{outputFolder}{co.Key}{extension}";
                switch (et)
                {
                    case ExtractionType.QueryPlans:
                        File.WriteAllText(file, co.Value.QueryPlan);
                        break;
                    case ExtractionType.SqlText:
                        File.WriteAllText(file, co.Value.SqlText);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(et), et, null);
                }
                co.Value.OutputFile = file;
            }
        }

    }
}
