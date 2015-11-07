using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebUtility;

namespace SQLTrismegiste.Tools
{
    public class HtmlSettings
    {
        public bool WithLineFeeds { get; set; }
    }

    public static class ExtensionMethods
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataTable"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        /// part of code borrowed from http://stackoverflow.com/questions/19682996/datatable-to-html-table
        public static string ToHtmlTable(this DataTable dataTable, HtmlSettings settings = null)
        {
            if (settings == null)
            {
                settings = new HtmlSettings()
                {
                    WithLineFeeds = false
                };
            }

            var separator = settings.WithLineFeeds ? "" : "\n";

            var table = new string[dataTable.Rows.Count + 1];
            long counter = 1;
            table[0] = "<tr><th>" + String.Join("</th><th>", (from c in dataTable.Columns.Cast<DataColumn>() select HtmlEncode(c.ColumnName)).ToArray()) + $"</td></tr>{separator}";
            foreach (DataRow row in dataTable.Rows)
            {
                table[counter] = "<tr><td>" + String.Join("</td><td>", (from o in row.ItemArray select HtmlEncode(o.ToString())).ToArray()) + $"</td></tr>{separator}";
                counter++;
            }

            return "<table>" + String.Join("", table) + "</table>";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static string ToHtml(this DataSet dataSet, HtmlSettings settings = null)
        {
            if (settings == null)
            {
                settings = new HtmlSettings()
                {
                    WithLineFeeds = false
                };
            }

            var separator = settings.WithLineFeeds ? "" : "\n";

            var sb = new StringBuilder();
            sb.Append($"<h2>{dataSet.DataSetName}</h2>{separator}"); // DataSetName never null ?

            var i = 1;
            foreach (DataTable table in dataSet.Tables)
            {
                sb.Append($"<h3>{table.TableName ?? $"Table{i}"}</h3>{separator}");
                sb.Append(table.ToHtmlTable(settings));
                i++;
            }

            return sb.ToString();
        }
    }
}
