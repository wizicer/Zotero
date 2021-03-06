using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Zotero
{
    public abstract class ZoteroObject
    {
        public string ID { get; init; }
    }

    public abstract class Container : ZoteroObject
    {
        public ObservableCollection<ZoteroObject> InnerObjects = new ObservableCollection<ZoteroObject>(); //TODO: Change to prevent storage of libraries
    }

    public abstract class Library : Container { }
    public class UserLibrary : Library { }
    public class GroupLibrary : Library { }

    [DebuggerDisplay("{Name, nq}")]
    public class Collection : Container
    {
        public string Name { get; set; }
        public string Key { get; set; }
    }

    public abstract class Item : ZoteroObject
    {
        public readonly ObservableCollection<Creator> Creators = new ObservableCollection<Creator>();

        public string Title { get; init; }
        public string Key { get; init; }
        public DateTime Date { get; init; }
        public CultureInfo Language { get; init; }
        public string AbstractNote { get; init; }
        public string ShortTitle { get; init; }
        public DateTime AccessDate { get; init; }
        public string Rights { get; init; }
        public string Extra { get; init; }
        public Uri URL { get; init; }

        public readonly ObservableCollection<Tag> Tags = new ObservableCollection<Tag>();
        public readonly ObservableCollection<Attachment> Attachments = new ObservableCollection<Attachment>();
    }

    public class ZoteroFieldAttribute : Attribute
    {
        public ZoteroFieldAttribute(string fieldName) { this.FieldName = fieldName; }
        public readonly string FieldName;
    }
}
