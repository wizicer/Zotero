using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
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
            var list = GenerateYamlNotes(lib);
            GenerateMarkdownList(lib);
            GenerateMarkdown(lib, list);
            CopyAttachments(lib);
        }

        record MarkdownFile(string Title)
        {
            public StringBuilder Content { get; init; }
        }

        static YamlEntity[] GenerateYamlNotes(Library lib)
        {
            var BASE_PATH = @"C:\Users\icer\OneDrive\Work\papers\notes";
            if (!Directory.Exists(BASE_PATH)) Directory.CreateDirectory(BASE_PATH);
            var newList = new List<YamlEntity>();
            RecursiveGet(lib.InnerObjects);
            var notesPath = Path.Combine(BASE_PATH, "Notes.yml");
            var archivePath = Path.Combine(BASE_PATH, "ArchiveNotes.yml");

            var oldList = new List<YamlEntity>();
            if (File.Exists(notesPath))
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build();
                var tl = deserializer.Deserialize<YamlEntity[]>(File.ReadAllText(notesPath));
                if (tl != null) oldList.AddRange(tl);
            }


            var archiveList = new List<YamlEntity>();

            foreach (var oldItem in oldList)
            {
                var newItem = newList.FirstOrDefault(_ => _.Key == oldItem.Key);
                if (newItem == null)
                {
                    // legacy item to archive
                    archiveList.Add(oldItem);
                }
                else
                {
                    // update item info from old list
                    var pos = newList.IndexOf(newItem);
                    newList[pos] = oldItem with { Title = newItem.Title, Collection = newItem.Collection };
                }
            }

            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            File.WriteAllText(notesPath, serializer.Serialize(newList));
            File.WriteAllText(archivePath, serializer.Serialize(archiveList));

            return newList.ToArray();

            void RecursiveGet(ObservableCollection<ZoteroObject> objs, params string[] levels)
            {
                foreach (var obj in objs)
                {
                    switch (obj)
                    {
                        case Collection col:
                            RecursiveGet(col.InnerObjects, levels.Concat(new[] { col.Name }).ToArray());
                            break;
                        case Book paper:
                            SavePaper(paper, levels);
                            break;
                    }
                }
            }

            void SavePaper(Book paper, string[] levels)
            {
                var pos = newList.FindIndex(_ => _.Key == paper.Key);
                if (pos > -1)
                {
                    newList[pos] = newList[pos] with { Collection = newList[pos].Collection + " | " + string.Join(".", levels) };
                }
                else
                {
                    newList.Add(new YamlEntity(paper.Key, string.Join(".", levels), paper.Title, new[] { "" }, string.Empty));
                }
            }
        }

        static void GenerateMarkdownList(Library lib)
        {
            var BASE_PATH = @"C:\Users\icer\OneDrive\Work\papers\notes";
            if (!Directory.Exists(BASE_PATH)) Directory.CreateDirectory(BASE_PATH);
            var sb = new StringBuilder();
            RecursiveSave(lib.InnerObjects);

            File.WriteAllText(Path.Combine(BASE_PATH, "List.md"), sb.ToString());

            void RecursiveSave(ObservableCollection<ZoteroObject> objs, params string[] levels)
            {
                foreach (var obj in objs)
                {
                    switch (obj)
                    {
                        case Collection col:
                            sb.AppendLine($"{new string(' ', levels.Length * 4)}- {col.Name}");
                            sb.AppendLine();
                            var nlevels = levels.Concat(new[] { col.Name }).ToArray();
                            RecursiveSave(col.InnerObjects, nlevels);
                            break;
                        case Book paper:
                            SavePaper(paper, levels);
                            break;
                    }
                }
            }

            void SavePaper(Book paper, string[] levels)
            {
                sb.AppendLine($"{new string(' ', levels.Length * 4)}- {paper.Title}"
                    + (paper.Attachments.Count == 0 ? "" : $" 📄")
                    );
                sb.AppendLine();
            }
        }

        static void GenerateMarkdown(Library lib, YamlEntity[] notes)
        {
            var BASE_PATH = @"C:\Users\icer\OneDrive\Work\papers\notes";
            if (!Directory.Exists(BASE_PATH)) Directory.CreateDirectory(BASE_PATH);

            void MakeSureFile(string filename)
            {
                var file = Path.Combine(BASE_PATH, filename);
                File.Copy(filename, file, true);
            }

            foreach (var item in new[] { "github-markdown.css", "main.css", "app.js" })
            {
                MakeSureFile(item);
            }

            var dictNotes = notes.ToDictionary(_ => _.Key, _ => _);
            var mds = new Dictionary<string, StringBuilder>();
            RecursiveSave(lib.InnerObjects);
            var template = File.ReadAllText("template.html");

            foreach (var md in mds)
            {
                File.WriteAllText(
                    Path.Combine(BASE_PATH, md.Key + ".html"),
                    template.Replace("{{CONTENT}}", md.Value.ToString()).Replace("{{TITLE}}", md.Key));
            }

            void RecursiveSave(ObservableCollection<ZoteroObject> objs, params string[] levels)
            {
                foreach (var obj in objs)
                {
                    switch (obj)
                    {
                        case Collection col:
                            var nlevels = levels.Concat(new[] { col.Name }).ToArray();
                            var filename = nlevels[0];
                            if (!mds.ContainsKey(filename)) mds[filename] = new StringBuilder();
                            var sb = mds[filename];
                            sb.AppendLine($"<h{nlevels.Length}>{col.Name}</h{nlevels.Length}>");
                            sb.AppendLine();
                            RecursiveSave(col.InnerObjects, nlevels);
                            break;
                        case Book paper:
                            SavePaper(paper, levels);
                            break;
                    }
                }
            }

            void SavePaper(Book paper, string[] levels)
            {
                var filename = levels[0];
                var sb = mds[filename];
                var note = dictNotes[paper.Key];

                sb.AppendLine($"<h{levels.Length + 1}>{paper.Title}</h{levels.Length + 1}>");
                sb.AppendLine();
                sb.AppendLine($"<blockquote> {string.Join(", ", paper.Creators.Select(_ => $"{_.FirstName} {_.LastName}"))}" +
                    $" ({string.Join(", ", new[] { paper.Publisher, paper.Date, paper.Type }.Where(_ => !string.IsNullOrWhiteSpace(_)))})" +
                    (paper.URL == null ? "" : $" <a href=\"{paper.URL}\">URL</a>)") +
                    (paper.Attachments.Count == 0 ? "" : $" 📄") +
                    $"</blockquote>");
                sb.AppendLine();
                sb.AppendLine($"<p>Abstract: {AbstractStyler.StyleAbstract(paper.AbstractNote, note.Highlights)}</p>");
                sb.AppendLine();
            }
        }

        static void CopyAttachments(Library lib)
        {
            var BASE_PATH = @"C:\Users\icer\OneDrive\Work\papers\storage";
            var ARCHIVE_PATH = @"C:\Users\icer\OneDrive\Work\papers\archive";
            if (!Directory.Exists(ARCHIVE_PATH)) Directory.CreateDirectory(ARCHIVE_PATH);
            var srcBasePath = Path.Combine(Path.GetDirectoryName(DEFAULT_ZOTERO_SQLITE_STORAGE_PATH), "storage");
            var copies = RecursiveSave(lib.InnerObjects).SelectMany(_ => _);
            var existFiles = RecursiveFill(new DirectoryInfo(BASE_PATH))
                .SelectMany(_ => _)
                .ToArray();

            // add or update (copy)
            foreach (var copy in copies)
            {
                var src = Path.Combine(srcBasePath, copy.Source);
                var tgt = Path.Combine(BASE_PATH, copy.Target);
                var tgtDir = new FileInfo(tgt).Directory;
                if (!tgtDir.Exists) tgtDir.Create();
                if (!File.Exists(tgt)) File.Copy(src, tgt);
            }

            // remove (move)
            foreach (var file in existFiles)
            {
                if (copies.All(_ => _.Target != file))
                {
                    var src = Path.Combine(BASE_PATH, file);
                    var tgt = Path.Combine(ARCHIVE_PATH, file);
                    var tgtDir = new FileInfo(tgt).Directory;
                    if (!tgtDir.Exists) tgtDir.Create();
                    if (!File.Exists(tgt)) File.Move(src, tgt);
                }
            }

            // clean empty directory
            RecursiveClean(new DirectoryInfo(BASE_PATH));

            void RecursiveClean(DirectoryInfo dir)
            {
                foreach (var subdir in dir.EnumerateDirectories())
                {
                    RecursiveClean(subdir);
                }

                if (!dir.EnumerateFiles("*", SearchOption.AllDirectories).Any())
                    dir.Delete();
            }

            IEnumerable<IEnumerable<string>> RecursiveFill(DirectoryInfo dir, string path = "")
            {
                foreach (var subdir in dir.EnumerateDirectories())
                {
                    yield return RecursiveFill(subdir, Path.Combine(path, subdir.Name)).SelectMany(_ => _);
                }

                yield return dir.EnumerateFiles("*", SearchOption.TopDirectoryOnly).Select(_ => Path.Combine(path, _.Name));
            }

            IEnumerable<IEnumerable<CopyInstructor>> RecursiveSave(ObservableCollection<ZoteroObject> objs, string path = "")
            {
                foreach (var obj in objs)
                {
                    switch (obj)
                    {
                        case Collection col:
                            yield return RecursiveSave(col.InnerObjects, Path.Combine(path, col.Name)).SelectMany(_ => _);
                            break;
                        case Book paper:
                            yield return SavePaper(paper, path);
                            break;
                    }
                }
            }

            IEnumerable<CopyInstructor> SavePaper(Book paper, string path)
            {
                var att = paper.Attachments.FirstOrDefault();
                if (att == null) yield break;
                var filename = att.Path.StartsWith("storage:") ? att.Path.Substring("storage:".Length) : throw new NotImplementedException();
                yield return new CopyInstructor(Path.Combine(att.Key, filename), Path.Combine(path, filename));
                //var src = Path.Combine(srcBasePath, att.Key, filename);
                //if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                //var filepath = Path.Combine(path, filename);
                //if (!File.Exists(filepath)) File.Copy(src, filepath);
            }

        }

        public record CopyInstructor(string Source, string Target);
    }

    public record YamlEntity(string Key, string Collection, string Title, string[] Highlights, string Notes)
    {
        public YamlEntity() : this("", "", "", new string[] { }, "") { }
    }
}
