using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLTrismegiste
{
    public enum StatsToClear
    {
        Waits,
        Latches
    }

    public enum ProcessingStatus
    {
        Success,
        Error,
        Blank
    }

    public enum Level
    {
        Server,
        Database
    }

    public class Display
    {
        public string Lang { get; set; }
        public string Tooltip { get; set; }
        public string Label { get; set; }
    }

    public class Query
    {
        public byte VersionMajorMin { get; set; }
        public byte VersionMajorMax { get; set; }
        public string Statement { get; set; }
    }

    public class Hermeticus
    {
        public string Name { get; set; }
        public string FolderName { get; set; }
        public string Source { get; set; }
        public List<Query> Queries { get; set; }
        public Level QueryLevel { get; set; }
        public Dictionary<string, Display> Displays { get; set; }
        public ProcessingStatus Status { get; set; }
        public string ErrorMessage { get; set; }
        public string LocalizedDisplay { get; set; }
        public string LocalizedTooltip { get; set; }
        //public List<ColumnWarning> ColumnWarnings { get; private set; }
        //public List<RowWarning> RowWarnings { get; private set; }
    }

    public class Folder 
    {
        public List<Hermeticus> Hermetica { get; set; } = new List<Hermeticus>();

        public string Name { get; set; }
        public string Display { get; set; }
        public string Tooltip { get; set; }
    }
}
