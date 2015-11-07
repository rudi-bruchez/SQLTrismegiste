using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLTrismegiste.CorpusManager
{
    public enum MatchingTypes
    {
        GT,
        LT,
        BETWEEN,
        OUTSIDE,
        EQUALS,
        LIKE
    }

    public abstract class Warning
    {
        public int ColumnPosition { get; set; }
        public MatchingTypes MatchingType { get; set; }
        public byte WarningLevel { get; set; }
        public string IfMatchText { get; set; }
        public string IfNotMatchText { get; set; }
        public string lang { get; set; }
    }

    public class ColumnWarning: Warning
    {

    }

    public class RowWarning: Warning
    {

    }
}
