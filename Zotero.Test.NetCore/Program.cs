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
        }
    }
}
