using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Zotero;
using Zotero.Connections;

namespace Zotero.Test.NetCore
{
    class Program
    {
        const string DEFAULT_ZOTERO_SQLITE_STORAGE_PATH = @"C:\Users\icer\OneDrive\Work\zotero\storage\zotero.sqlite";
        static void Main(string[] args)
        {
            ZoteroDatabaseConnection connection = new ZoteroDatabaseConnection(DEFAULT_ZOTERO_SQLITE_STORAGE_PATH);
            connection.Connect();
            Library[] libraries = connection.Dump();
            var lib = libraries[0];
            //CopyAttachments(lib);
        }

        static void CopyAttachments(Library lib)
        {
            var BASE_PATH = @"C:\Users\icer\OneDrive\Work\papers\storage";
            var srcBasePath = Path.Combine(Path.GetDirectoryName(DEFAULT_ZOTERO_SQLITE_STORAGE_PATH), "storage");
            RecursiveSave(lib.InnerObjects, BASE_PATH);

            void RecursiveSave(ObservableCollection<ZoteroObject> objs, string path)
            {
                foreach (var obj in objs)
                {
                    switch (obj)
                    {
                        case Collection col:
                            RecursiveSave(col.InnerObjects, Path.Combine(path, col.Name));
                            break;
                        case Book paper:
                            SavePaper(paper, path);
                            break;
                    }
                }
            }

            void SavePaper(Book paper, string path)
            {
                var att = paper.Attachments.FirstOrDefault();
                if (att == null) return;
                var filename = att.Path.StartsWith("storage:") ? att.Path.Substring("storage:".Length) : throw new NotImplementedException();
                var src = Path.Combine(srcBasePath, att.Key, filename);
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                var filepath = Path.Combine(path, filename);
                if (!File.Exists(filepath)) File.Copy(src, filepath);
            }

        }
    }
}
