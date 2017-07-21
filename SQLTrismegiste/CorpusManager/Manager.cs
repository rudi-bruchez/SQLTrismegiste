using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using SimpleLogger;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Schema;
using SQLTrismegiste.SqlServer;
using SQLTrismegiste.Tools;
using static System.Net.WebUtility;

//using Saxon.Api;

namespace SQLTrismegiste.CorpusManager
{
    internal class ProcessingOptions
    {
        public bool AddQueryText { get; set; }
        public bool AddQueryPlans { get; set; }
    }

    internal class Manager
    {
        private static readonly Regex MultipleSpaces = new Regex(@" {2,}", RegexOptions.Compiled);
        private static readonly Regex ScientificNotation = new Regex(@"<td>(\d+?\.\d+e[\+\-]\d+)\D", RegexOptions.Compiled); // <td>7.500000000000000e+001

        private SqlConnection _cn;
        public List<string> Databases { get; set; } //= new List<string>();
        public string OutputPath { get; set; }
        public string ConnectionString
        { set
            {
                _cn = new System.Data.SqlClient.SqlConnection(value);

            } }
        public SqlServer.ServerInfo Info { get; set; }
        public ProcessingOptions Options { get; set; }
        //private List<Folder> _folders { get; set; }
        public List<Hermeticus> Hermetica { get; set; }

        private static string Normalize(string input)
        {
            return MultipleSpaces.Replace(input, " ");
        }

        //event handler to manage the errors
        private static void verifyErrors(object sender, ValidationEventArgs args)
        {
            if (args.Severity == XmlSeverityType.Error)
                SimpleLog.Error(args.Message);
        }

        public Manager()
        {
            //ColumnWarnings = new List<ColumnWarning>();
            //RowWarnings = new List<RowWarning>();
            LoadHermetica();
        }

        private Hermeticus Parse(string hermeticus)
        {
            var doc = new XmlDocument();
            try
            {
                doc.Load(hermeticus);
            }
            catch (Exception e)
            {
                SimpleLog.Error($"{App.Localized["msgErrorLoadingHermeticus"]} {hermeticus}. {App.Localized["msgError"]} : {e.Message}");
                return null;
            }

            // --- validate the hermetica ---
            doc.Schemas.Add("http://www.w3.org/2001/XMLSchema", "CorpusHermeticum/Corpus.xsd");
            var veh = new ValidationEventHandler(verifyErrors);
            doc.Validate(veh);
            // ------------------------------

            // TODO Enum.IsDefined ??
            Debug.Assert(doc.DocumentElement != null, "doc.DocumentElement != null");
            var el = doc.DocumentElement;

            var h = new Hermeticus()
            {
                Source = hermeticus,
                Name = el.SelectSingleNode("/hermeticus/@name").InnerText,
                FolderName = el.SelectSingleNode("/hermeticus/@folder").InnerText,
                QueryLevel = (Level)Enum.Parse(typeof(Level), el.SelectSingleNode("/hermeticus/@level").InnerText),
                Queries = new List<Query>(),
                Displays = new Dictionary<string, Display>(),
                Status = ProcessingStatus.Blank
            };

            // extract displays
            var displayNodes = doc.DocumentElement.SelectNodes("/hermeticus/header/description");
            if (displayNodes == null) return null;
            foreach (var dis in from XmlNode xdis in displayNodes select new Display()
            {
                Lang = xdis.Attributes["lang"].Value.ToUpper(),
                Label = xdis.Attributes["label"].Value,
                Tooltip = xdis.Attributes["tooltip"].Value
            })
            {
                h.Displays.Add(dis.Lang,dis);
            }

            // current localized display -- TODO
            h.LocalizedDisplay = h.Displays["FR"].Label;
            h.LocalizedTooltip = h.Displays["FR"].Tooltip;

            // extract queries
            var queryNodes = doc.DocumentElement.SelectNodes("/hermeticus/queries/query");
            if (queryNodes == null) return null;
            foreach (XmlNode node in queryNodes)
            {
                /* changed 2017.03.24
                 * the rule is :
                 * 1. it can be a number. If so, the query is only valid for this version
                 * 2. it can be a star (*), if so, the query is valid for all versions. This will be the default choice
                 * 3. it can be smth like this : "11-*", "*-11", "11-12"
                 */

                var versionString = node.Attributes["versionMajor"].Value;
                byte versionMin = byte.MinValue;
                byte versionMax = byte.MaxValue;

                // the element at index 0 returned by the Groups property contains a string 
                // that matches the entire regular expression pattern
                if (Regex.IsMatch(versionString, @"^\d+$"))
                {
                    versionMin = byte.Parse(versionString);
                    versionMax = versionMin;
                }
                else if (Regex.IsMatch(versionString, @"^\d+-\d+$"))
                {
                    var match = Regex.Match(versionString, @"^(\d+)-(\d+)$");
                    versionMin = byte.Parse(match.Groups[1].Value);
                    versionMax = byte.Parse(match.Groups[2].Value);
                }
                else if (Regex.IsMatch(versionString, @"^\*-\d+$"))
                {
                    var match = Regex.Match(versionString, @"^\*-(\d+)$");
                    versionMax = byte.Parse(match.Groups[1].Value);
                }
                else if (Regex.IsMatch(versionString, @"^\d+-\*$"))
                {
                    var match = Regex.Match(versionString, @"^(\d+)-\*$");
                    versionMin = byte.Parse(match.Groups[1].Value);
                }

                var qry = new Query()
                {
                    VersionMajorMin = versionMin,
                    VersionMajorMax = versionMax,
                    Statement = Normalize(node.InnerText.Trim().TrimEnd(';'))
                };
                h.Queries.Add(qry);
            }

            return h;

        }

        /// <summary>
        /// Choose the right query for the current SQL Server version
        /// </summary>
        /// <param name="hermeticus"></param>
        /// <returns></returns>
        private Query ChooseRightQuery(Hermeticus hermeticus)
        {
            return (from q in hermeticus.Queries
                where (Info.VersionMajor >= q.VersionMajorMin && Info.VersionMajor <= q.VersionMajorMax )
                orderby q.VersionMajorMax descending
                select q).FirstOrDefault();
        }

        private DataTable GetDataTable(string qry, Hermeticus h, string db = "master")
        {
            using (var cmd = new SqlCommand($"USE {db};", _cn))
            {
                cmd.CommandTimeout = 240; // 4 min.
                try
                {
                    if (_cn.State == ConnectionState.Closed) _cn.Open();

                    // use the database
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = qry;

                    var tableName = db == "master" ? "server" : db;
                    var dt = new DataTable(tableName);
                    using (var a = new SqlDataAdapter(cmd))
                    {
                        a.Fill(dt);
                    }
                    h.Status = ProcessingStatus.Success;
                    return dt;
                }
                catch (SqlException e)
                {
                    var msg =
                        $"{App.Localized["msgSqlErrorForHermeticus"]} {h.Name}. {App.Localized["msgSqlErrorForHermeticus2"]} : {e.Number}, {e.Message}";
                    SimpleLog.Error(msg);
                    h.Status = ProcessingStatus.Error;
                    h.ErrorMessage = msg;
                    return null;
                }
                catch (Exception e)
                {
                    // TODO log ...
                    //var msg =
                    //    $"SQL Error for hermetica {source}. It could be an error in your SQL syntax, or a problem with the Compatibility level of your database. The SQL exception is : {e.Number}, {e.Message}";
                    SimpleLog.Error(e.Message);
                    h.Status = ProcessingStatus.Error;
                    h.ErrorMessage = e.Message;
                    return null;
                }
            }

        }



        private static DataTable CleanDataTable(DataTable dt)
        {
            // needs to round values ?
            
            var cols = (from c in dt.Columns.Cast<DataColumn>()
                where (c.DataType == typeof (decimal) || c.DataType == typeof (float) || c.DataType == typeof (double))
                select c.Ordinal).ToArray();

            foreach (DataRow row in dt.Rows)
            {
                foreach (var col in cols)
                {
                    try
                    { row[col] = Math.Round(float.Parse(row[col].ToString()), 2); }
                    catch (Exception) { } 
                }
            }
            return dt;
        }

        /// <summary>
        /// create the HTML index of all processed hermetica
        /// </summary>
        internal void CreateIndex()
        {
            var html = new StringBuilder();
            foreach (var folder in Hermetica.Select(h => h.FolderName).Distinct())
            {
                html.Append($"<h2>{folder}<h2>\n<ul>\n");
                foreach (Hermeticus h in Hermetica.Where(h => h.FolderName == folder))
                {
                    switch (h.Status)
                    {
                        case ProcessingStatus.Success:
                            html.Append($"<li><a href='{h.Name}.html'>{h.LocalizedDisplay}</a>{h.LocalizedTooltip}</li>\n");
                            break;
                        case ProcessingStatus.Error:
                            html.Append($"<li>[{h.LocalizedDisplay}] {App.Localized["msgReturnedError"]} : { HtmlEncode(h.ErrorMessage)}</a></li>\n");
                            break;
                        case ProcessingStatus.Blank:
                            html.Append($"<li>{h.Name} {App.Localized["msgNotProcessed"]}</li>\n");
                            break;
                        default:
                            break;
                    }
                }
                html.Append($"</ul>\n");
            }
            // template management
            var param = new Dictionary<string, string>
            {
                {"BODY", html.ToString()},
                {"SERVER", Info.ServerName },
                {"DATE", String.Format("YYYY-MM-DD HH:MM", DateTime.Now) }
            };

            var output = $"{OutputPath}index.html";

            try
            {
                var text = File.ReadAllText("templates/index.html");
                text = Regex.Replace(text, @"\{(.+?)\}", m => param[m.Groups[1].Value]);
                File.WriteAllText(output, text);
            }
            catch (Exception e)
            {
                SimpleLog.Error($"{App.Localized["msgErrorReadingTemplate"]} [{e.Message}]");
            }
        }

        private DataTable ProcessDataTable(DataTable dt)
        {
            if (!(Options.AddQueryPlans || Options.AddQueryText)) return dt;
            // TODO
            return dt;
        }

        public void CleanOutputFolder()
        {
            DirectoryInfo di = new DirectoryInfo(OutputPath);

            foreach (FileInfo file in di.GetFiles()) { file.Delete(); }
        }

        public string Run(Hermeticus h)
        {
            // some checking
            Debug.Assert(_cn != null, "_cn != null");
            Debug.Assert(Databases != null, "Databases != null");
            Debug.Assert(Info != null, "Info != null");
            Debug.Assert(Options != null, "Options != null");

            if (h == null) return null; // TODO -- log error

            var qry = ChooseRightQuery(h);
            if (qry == null)
            {
                var msg =
                    $"{App.Localized["msgNoValidQueryFound"]} {h.Name}. Server version {Info.VersionMajor}. {App.Localized["msgNotProcessing"]}";
                SimpleLog.Error(msg);
                h.ErrorMessage = msg;
                return null;
            }

            var ds = new DataSet("results");

            if (h.QueryLevel == Level.Server)
            {
                //var qry = h.Queries[0] + " FOR XML RAW, ELEMENTS, ROOT('Result')"; // TODO
                var table = GetDataTable(qry.Statement, h);
                if (table == null) return null; // SQL error ...
                table = CleanDataTable(table);
                table = ProcessDataTable(table);
                table.TableName = "Server";
                if (Options.AddQueryPlans || Options.AddQueryText)
                {
                    var ce = new CacheExtractor(_cn);
                    if (Options.AddQueryPlans) ce.ExtractFromDataTable(ExtractionType.QueryPlans, table);
                    if (Options.AddQueryText) ce.ExtractFromDataTable(ExtractionType.SqlText, table);
                }
                ds.Tables.Add(table);
            }
            else
            {
                foreach (var db in Databases)
                {
                    var table = GetDataTable(qry.Statement, h, db);
                    if (table == null) return null; // SQL error ...
                    table = CleanDataTable(table);
                    table = ProcessDataTable(table);
                    table.TableName = db;
                    if (Options.AddQueryPlans || Options.AddQueryText)
                    {
                        var ce = new CacheExtractor(_cn);
                        if (Options.AddQueryPlans) ce.ExtractFromDataTable(ExtractionType.QueryPlans, table);
                        if (Options.AddQueryText) ce.ExtractFromDataTable(ExtractionType.SqlText, table);
                    }
                    ds.Tables.Add(table);
                }
            }

            if (ds.Tables.Count == 0) return null;
            return SaveHtml(ds, h);
        }

        private string SaveHtml(DataSet ds, Hermeticus h)
        {
            // template management
            var param = new Dictionary<string, string>
            {
                {"TITLE", h.Displays["FR"].Label},
                {"DESCRIPTION", h.Displays["FR"].Tooltip},
                {"BODY", ds.ToHtml(new HtmlSettings() { WithLineFeeds = true})},
                {"SERVER", Info.ServerName },
                {"DATE", String.Format("YYYY-MM-DD HH:MM", DateTime.Now) }
            };

            var output = $"{OutputPath}{h.Name}.html";
            
            try
            {
                var text = File.ReadAllText("templates/result.html");
                text = Regex.Replace(text, @"\{(.+?)\}", m => param[m.Groups[1].Value]);
                File.WriteAllText(output, text);
            }
            catch (Exception e)
            {
                SimpleLog.Error($"error reading 'templates/result.html' or writing file {output}. Error [{e.Message}]");
            }
            return output;
        }

        private void LoadHermetica()
        {
            var files = Directory.GetFiles("CorpusHermeticum/", "*.xml", SearchOption.TopDirectoryOnly);

            Hermetica = new List<Hermeticus>();

            foreach (var f in files.Where(f => !f.EndsWith("Folders.xml")))
            {
                var h = Parse(f);
                if (h != null) Hermetica.Add(h);
            }
        }

    }
}
