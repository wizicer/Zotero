using System;
using System.Collections.Generic;
using System.Text;

namespace Zotero
{
    public class Tag : ZoteroObject
    {
        public Tag(string name) { this.Name = name; }

        public string Name
        {
            get { return this.ID; }
            set
            {
                this.ID = value;
                OnPropertyChanged();
            }
        }
    }

    public class Attachment 
    {
        public string Id { get; set; }
        public string Key { get; set; }
        public string Path { get; set; }
    }
}
