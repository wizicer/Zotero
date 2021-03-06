﻿using SQLite.Net;
using SQLite.Net.Platform;
using SQLite.Net.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Zotero.Connections
{
    public class ZoteroDatabaseConnection : Connection
    {
        public ZoteroDatabaseConnection(string zoteroSaveFilePath)
        {
            this.ZoteroSaveFilePath = zoteroSaveFilePath;
        }

        public string ZoteroSaveFilePath { get; private set; }

        private static ISQLitePlatform databasePlatform = new SQLite.Net.Platform.Generic.SQLitePlatformGeneric();
        SQLiteConnection databaseConnection;

        public override void Add(ZoteroObject objectToAdd)
        {
            throw new NotImplementedException();
        }

        private const string TAGS_TABLE_NAME = "tags";
        private const string TAG_ID_KEY = "tagID";
        private const string TAG_NAME_KEY = "name";

        private const string CREATORS_TABLE_NAME = "creators";
        private const string CREATOR_ID_KEY = "creatorID";
        private const string CREATOR_FIRST_NAME_KEY = "firstName";
        private const string CREATOR_LAST_NAME_KEY = "lastName";

        private const string LIBRARIES_TABLE_NAME = "libraries";
        private const string LIBRARY_ID_KEY = "libraryID";
        private const string LIBRARY_TYPE_KEY = "type";
        private const string USER_LIBRARY_TYPE_VALUE = "user";
        private const string GROUP_LIBRARY_TYPE_VALUE = "group";

        private const string COLLECTIONS_TABLE_NAME = "collections";
        private const string COLLECTION_PARENT_KEY = "parentCollectionID";
        private const string COLLECTION_ID_KEY = "collectionID";
        private const string COLLECTION_NAME_KEY = "collectionName";

        private const string ITEMS_COLLECTIONS_MATCHING_TABLE_NAME = "collectionItems";

        private const string ITEMS_TABLE_NAME = "items";
        private const string ITEM_ID_KEY = "itemID";

        private const string ITEMS_FIELDS_TABLE_NAME = "fields";
        private const string ITEM_FIELD_NAME_KEY = "fieldName";
        private const string ITEM_FIELD_ID_KEY = "fieldID";

        private const string ITEMS_VALUE_ID_TABLE_NAME = "itemData";
        private const string VALUE_ID_KEY = "valueID";

        private const string ITEMS_VALUES_TABLE_NAME = "itemDataValues";
        private const string VALUE_KEY = "value";

        private const string ITEM_CREATOR_REFERENCES_TABLE_NAME = "itemCreators";

        private const string ITEM_TAG_REFERENCES_TABLE_NAME = "itemTags";

        public override Library[] Dump()
        {
            List<Library> result = new List<Library>();

            //Import creators
            List<Creator> creators = new List<Creator>();
            SQLiteCommandResult getCreators = this.databaseConnection.CreateCommand("SELECT * FROM " + CREATORS_TABLE_NAME).ExecuteDeferredQuery();
            foreach (SQLiteDataTableRow creatorRow in getCreators.Data)
                creators.Add(new Creator(CreatorType.Author, creatorRow[CREATOR_FIRST_NAME_KEY].ToString(), creatorRow[CREATOR_LAST_NAME_KEY].ToString()) { ID = creatorRow[CREATOR_ID_KEY].ToString() });

            //Import tags
            List<Tag> tags = new List<Tag>();
            string getTagsQuery = String.Format("SELECT * FROM {0}", TAGS_TABLE_NAME);
            SQLiteCommandResult getTags = this.databaseConnection.CreateCommand(getTagsQuery).ExecuteDeferredQuery();
            foreach (SQLiteDataTableRow tagRow in getTags.Data)
                tags.Add(new Tag(tagRow[TAG_ID_KEY].ToString(), tagRow[TAG_NAME_KEY].ToString()));

            //Import item types
            var types = new Dictionary<int, string>();
            string getTypesQuery = $"SELECT * FROM {"itemTypes"}";
            SQLiteCommandResult getTypes = this.databaseConnection.CreateCommand(getTypesQuery).ExecuteDeferredQuery();
            foreach (SQLiteDataTableRow typeRow in getTypes.Data)
                types.Add(int.Parse(typeRow["itemTypeID"].ToString()), typeRow["typeName"].ToString());

            SQLiteCommandResult getLibrariesResult = this.databaseConnection.CreateCommand("SELECT * FROM " + LIBRARIES_TABLE_NAME).ExecuteDeferredQuery();
            foreach (SQLiteDataTableRow libraryRow in getLibrariesResult.Data)
            {
                Library library = null;
                switch (libraryRow[LIBRARY_TYPE_KEY])
                {
                    case USER_LIBRARY_TYPE_VALUE:
                        library = new UserLibrary() { ID = libraryRow[LIBRARY_ID_KEY].ToString() };
                        break;
                    case GROUP_LIBRARY_TYPE_VALUE:
                        library = new GroupLibrary() { ID = libraryRow[LIBRARY_ID_KEY].ToString() };
                        break;
                }

                string getInnerCollectionsQuery = String.Format("SELECT * FROM {0} WHERE {1} IS {2}", COLLECTIONS_TABLE_NAME, LIBRARY_ID_KEY, library.ID);
                SQLiteCommandResult getInnerCollections = this.databaseConnection.CreateCommand(getInnerCollectionsQuery).ExecuteDeferredQuery();
                foreach (SQLiteDataTableRow innerCollectionRow in getInnerCollections.Data.OrderBy(row => row[COLLECTION_PARENT_KEY]))
                {
                    Collection innerCollection = new Collection()
                    {
                        ID = innerCollectionRow[COLLECTION_ID_KEY].ToString(),
                        Name = innerCollectionRow["collectionName"].ToString(),
                        Key = innerCollectionRow["key"].ToString(),
                    };

                    string getInnerItemsQuery = String.Format("SELECT * FROM {0} WHERE {1} IS {2}", ITEMS_COLLECTIONS_MATCHING_TABLE_NAME, COLLECTION_ID_KEY, innerCollection.ID);
                    SQLiteCommandResult getInnerItems = this.databaseConnection.CreateCommand(getInnerItemsQuery).ExecuteDeferredQuery();
                    foreach (SQLiteDataTableRow innerItemRow in getInnerItems.Data)
                    {
                        string itemID = innerItemRow[ITEM_ID_KEY].ToString();
                        string getItemQuery = String.Format("SELECT * FROM {0} WHERE {1} IS {2}", ITEMS_TABLE_NAME, ITEM_ID_KEY, itemID);
                        SQLiteCommandResult getItem = this.databaseConnection.CreateCommand(getItemQuery).ExecuteDeferredQuery();

                        var getFields = Execute($@"select fieldName,value from itemDataValues idv
  join itemData id on id.valueID = idv.valueID
  join fields f on f.fieldID = id.fieldID
where itemID is {itemID}");
                        var fields = getFields.Data
                            .ToDictionary(_ => _["fieldName"].ToString(), _ => _["value"].ToString());

                        //Parse type and create the new object
                        Item item = new Book
                        {
                            Key = getItem.Data[0]["key"].ToString(),
                            Type = types[int.Parse(getItem.Data[0]["itemTypeID"].ToString())],
                            Fields = new System.Collections.ObjectModel.ObservableCollection<KeyValuePair<string, string>>(fields),
                        };
                        //TODO = (Item)Activator.CreateInstance();
                        fields = fields.ToDictionary(_ => _.Key.ToLower(), _ => _.Value);

                        //Fill it with corresponding data
                        foreach (PropertyInfo field in item.GetType().GetRuntimeProperties().Where(property => property.GetSetMethod().IsPublic))
                        {
                            if (!fields.ContainsKey(field.Name.ToLower())) continue;
                            var value = fields[field.Name.ToLower()];

                            if (field.PropertyType == typeof(DateTime))
                            {
                                const string DATE_FORMAT = "yyyy-MM-dd";
                                string rawDate = value.Substring(0, DATE_FORMAT.Length);
                                if (rawDate.EndsWith("00-00"))
                                {
                                    rawDate = rawDate.Substring(0, 4) + "-01-01";
                                }
                                else if (rawDate.Last() == '0')
                                {
                                    StringBuilder tempString = new StringBuilder(rawDate);
                                    tempString[rawDate.Length - 1] = '1';
                                    rawDate = tempString.ToString();
                                }
                                field.SetValue(item, DateTime.ParseExact(rawDate, DATE_FORMAT, null));
                            }
                            else if (field.PropertyType == typeof(string))
                                field.SetValue(item, value);
                            else if (field.PropertyType == typeof(Uri))
                                field.SetValue(item, new Uri(value));
                            else
                                System.Diagnostics.Debugger.Break();
                        }

                        //Add author references
                        string getAuthorReferencesQuery = String.Format("SELECT * FROM {0} WHERE {1} IS {2}", ITEM_CREATOR_REFERENCES_TABLE_NAME, ITEM_ID_KEY, itemID);
                        SQLiteCommandResult getAuthorReference = this.databaseConnection.CreateCommand(getAuthorReferencesQuery).ExecuteDeferredQuery();
                        foreach (SQLiteDataTableRow authorReference in getAuthorReference.Data)
                        {
                            try { item.Creators.Add(creators.First(creator => creator.ID == authorReference[CREATOR_ID_KEY].ToString())); }
                            catch (InvalidOperationException ioex) { }
                        }

                        //Add tag references
                        string getTagReferencesQuery = String.Format("SELECT * FROM {0} WHERE {1} IS {2}", ITEM_TAG_REFERENCES_TABLE_NAME, ITEM_ID_KEY, itemID);
                        SQLiteCommandResult getTagReferences = this.databaseConnection.CreateCommand(getTagReferencesQuery).ExecuteDeferredQuery();
                        foreach (SQLiteDataTableRow tagReference in getTagReferences.Data)
                        {
                            try { item.Tags.Add(tags.First(tag => tag.ID == tagReference[TAG_ID_KEY].ToString())); }
                            catch (InvalidOperationException ioex) { }
                        }

                        //Add attachments
                        string getAttachmentsQuery = $"SELECT * FROM {"itemAttachments"} WHERE {"parentItemId"} IS {itemID}";
                        SQLiteCommandResult getAttachments = this.databaseConnection.CreateCommand(getAttachmentsQuery).ExecuteDeferredQuery();
                        foreach (SQLiteDataTableRow att in getAttachments.Data)
                        {
                            try
                            {
                                var id = att["itemID"].ToString();
                                var itemQuery = $"SELECT * FROM {ITEMS_TABLE_NAME} WHERE {ITEM_ID_KEY} IS {id}";
                                var itemResult = this.databaseConnection.CreateCommand(itemQuery).ExecuteDeferredQuery();
                                item.Attachments.Add(new Attachment
                                {
                                    Path = att["path"].ToString(),
                                    Id = id,
                                    Key = itemResult.Data[0]["key"].ToString(),
                                });
                            }
                            catch (InvalidOperationException) { }
                        }

                        innerCollection.InnerObjects.Add(item);
                    }

                    //Put the collection in the right container
                    int? parentCollectionID = (int?)innerCollectionRow[COLLECTION_PARENT_KEY];
                    if (parentCollectionID == null) //TODO: Check if it not just equals zero
                        library.InnerObjects.Add(innerCollection);
                    else
                        SearchForCollectionByID(library.InnerObjects.Where(innerObject => innerObject is Collection).Cast<Collection>(), parentCollectionID.ToString())
                            .InnerObjects.Add(innerCollection);

                    //Rename collections with real name
                    //string getCollectionNameQuery = String.Format("SELECT * FROM {0} WHERE {1} IS {2}", COLLECTIONS_TABLE_NAME, COLLECTION_ID_KEY, innerCollection.ID);
                    //SQLiteCommandResult getCollectionName = this.databaseConnection.CreateCommand(getCollectionNameQuery).ExecuteDeferredQuery();
                    //innerCollection.ID = getCollectionName.Data[0][COLLECTION_NAME_KEY].ToString();
                }

                //Rename collections with real name
                //string getLibraryNameQuery = String.Format("SELECT * FROM {0} WHERE {1} IS {2}", COLLECTIONS_TABLE_NAME, COLLECTION_ID_KEY, innerCollection.ID);
                //SQLiteCommandResult getLibraryName = this.databaseConnection.CreateCommand(getLibraryNameQuery).ExecuteDeferredQuery();
                //library.ID = getLibraryName.Data[0][COLLECTION_NAME_KEY].ToString();
                //TODO: Rename libraries with real name
                result.Add(library);
            }

            //Restore creators ID
            //foreach (Creator creator in creators)
            //    creator.ID = creator.FirstName + ' ' + creator.LastName;

            return result.ToArray();

            Collection SearchForCollectionByID(IEnumerable<Collection> collectionsCollection, string query, bool recursiveSearch = true)
            {
                try
                {
                    Collection targetCollection = collectionsCollection.Where(innerObject => innerObject is Collection).First(collection => collection.ID == query);
                    return targetCollection;
                }
                catch (InvalidOperationException)
                {
                    if (recursiveSearch)
                        foreach (Collection innerCollection in collectionsCollection)
                            try
                            {
                                return SearchForCollectionByID(innerCollection.InnerObjects.Where(innerObject => innerObject is Collection).Cast<Collection>(), query);
                            }
                            catch (InvalidOperationException) { }
                }
                throw new InvalidOperationException("Corresponding collection not found");
            }
        }

        public override void Remove(string IDToDelete)
        {
            throw new NotImplementedException();
        }

        protected override void ConnectionProcedure()
        {
            this.databaseConnection = new SQLiteConnection(databasePlatform, this.ZoteroSaveFilePath);
        }

        protected override void DisconnectionProcedure()
        {
            throw new NotImplementedException();
        }

        private SQLiteCommandResult Execute(string sql)
            => this.databaseConnection.CreateCommand(sql).ExecuteDeferredQuery();
    }
}
