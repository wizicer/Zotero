using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Zotero
{
    [DebuggerDisplay("[{ID, nq}]{Name, nq}")]
    public class Tag : ZoteroObject
    {
        public Tag(string id, string name)
        {
            this.ID = id;
            this.Name = name;
        }

        public string Name { get; init; }
    }

    public class Attachment
    {
        public string Id { get; set; }
        public string Key { get; set; }
        public string Path { get; set; }
    }
}
