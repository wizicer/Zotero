﻿using System;
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
            var BASE_PATH = @"C:\Users\icer\OneDrive\Work\papers\";
            var srcBasePath = Path.Combine(Path.GetDirectoryName(DEFAULT_ZOTERO_SQLITE_STORAGE_PATH), "storage");
            var lib = libraries[0];
            RecursiveSave(lib.InnerObjects, BASE_PATH);

            void RecursiveSave(ObservableCollection<ZoteroObject> objs, string path)
            {
                foreach (var obj in objs)
                {
                    if (obj is Collection col)
                    {
                        RecursiveSave(col.InnerObjects, Path.Combine(path, col.Name));
                    }
                    else if (obj is Book paper)
                    {
                        var att = paper.Attachments.FirstOrDefault();
                        if (att == null) continue;
                        var filename = att.Path.StartsWith("storage:") ? att.Path.Substring("storage:".Length) : throw new NotImplementedException();
                        var src = Path.Combine(srcBasePath, att.Key, filename);
                        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                        var filepath = Path.Combine(path, filename);
                        File.Copy(src, filepath);
                    }
                }
            }
        }
    }
}
