using System.Data;

namespace SQLTrismegiste.SqlServer
{
    public static class DataReaderExtensions
    {
        // hmm, or value = r[n] as string; ??
        public static string GetStringOrNull(this IDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }

        public static string GetStringOrNull(this IDataReader reader, string columnName)
        {
            return reader.GetStringOrNull(reader.GetOrdinal(columnName));
        }
    }
}