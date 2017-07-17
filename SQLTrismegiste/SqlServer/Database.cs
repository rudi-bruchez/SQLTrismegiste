using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLTrismegiste.SqlServer
{
    internal enum DatabaseCompatibilityLevel
    {
        v7 = 70,
        v2000 = 80,
        v2005 = 90,
        v2008 = 100,
        v2012 = 110,
        v2014 = 120,
        v2016 = 130
    }

    internal class Database
    {
        public bool Checked { get; set; }
        public string Name { get; set; }
        public DatabaseCompatibilityLevel CompatibilityLevel { get; set; }
    }
}
