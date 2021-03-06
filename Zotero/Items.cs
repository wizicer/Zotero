using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Zotero
{
    [DebuggerDisplay("{Title, nq}")]
    public class Book : Item
    {
        public string Edition { get; init; }
        public string Place { get; init; }
        public string Publisher { get; init; }
        public string Archive { get; init; }
        public string ArchiveLocation { get; init; }
        public string LibraryCatalog { get; init; }
        public string CallNumber { get; init; }
        public string ISBN { get; init; }
        public int NumPages { get; init; }
        public string Series { get; init; }
        public int SeriesNumber { get; init; }
        public string Volume { get; init; }
        public int VolumeNumber { get; init; }
    }
}
