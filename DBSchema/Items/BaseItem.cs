using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Jannesen.Tools.DBTools.DBSchema;
using Jannesen.Tools.DBTools.Library;

namespace Jannesen.Tools.DBTools.DBSchema.Item
{
    abstract class SchemaItem<TItem,TName> where TName:class
    {
        public              TName                               Name                { get; set; }
        public  virtual     TName                               OrgName             { get { return null; } }

        public                                                  SchemaItem(TName name)
        {
            Name = name;
        }

        public  abstract    bool                                CompareEqual(TItem other, DBSchemaCompare compare, CompareTable compareTable, CompareMode mode);
        public  virtual     bool                                isCodeEqual(TItem other)
        {
            return CompareEqual(other, null, null, CompareMode.Code);
        }
    }

    abstract class SchemaItemRename<TItem,TName>: SchemaItem<TItem,TName> where TName:class
    {
        public  override    TName                               OrgName             { get { return _orgName; } }

        private             TName                               _orgName;

        public                                                  SchemaItemRename(TName name, TName orgName): base(name)
        {
            _orgName = orgName;
        }
    }

    abstract class SchemaItemName<TItem> : SchemaItem<TItem,string> where TItem:SchemaItemName<TItem>
    {
        public                                                  SchemaItemName(XmlReader xmlReader): base(xmlReader.GetValueString("name"))
        {
        }
    }

    abstract class SchemaItemNameRename<TItem> : SchemaItemRename<TItem,string> where TItem:SchemaItemNameRename<TItem>
    {
        public                                                  SchemaItemNameRename(XmlReader xmlReader): base(xmlReader.GetValueString("name"), xmlReader.GetValueStringNullable("orgname"))
        {
        }

        public  override    bool                                CompareEqual(TItem other, DBSchemaCompare compare, CompareTable compareTable, CompareMode mode)
        {
            if (mode == CompareMode.UpdateWithRefactor)
                return true;

            return Name == other.Name;
        }
    }

    abstract class SchemaItemEntity<TItem> : SchemaItem<TItem,SqlEntityName> where TItem:SchemaItemEntity<TItem>
    {
        public                                                  SchemaItemEntity(XmlReader xmlReader): base(new SqlEntityName(xmlReader.GetValueString("name")))
        {
        }
    }

    abstract class SchemaItemEntityRename<TItem> : SchemaItemRename<TItem,SqlEntityName> where TItem:SchemaItemEntityRename<TItem>
    {
        public                                                  SchemaItemEntityRename(XmlReader xmlReader): base(new SqlEntityName(xmlReader.GetValueString("name")), xmlReader.HasValue("orgname") ? new SqlEntityName(xmlReader.GetValueString("orgname")) : null)
        {
        }

        public  override    bool                                CompareEqual(TItem other, DBSchemaCompare compare, CompareTable compareTable, CompareMode mode)
        {
            if (Name.Schema != other.Name.Schema)
                return false;

            if (mode == CompareMode.UpdateWithRefactor)
                return true;

            return Name.Name == other.Name.Name;
        }
    }

    abstract class SchemaItemList<TItem,TName> : List<TItem> where TItem:SchemaItem<TItem,TName>
                                                             where TName:class
    {
        public              bool                                CompareEqual(SchemaItemList<TItem,TName> other, DBSchemaCompare compare, CompareTable compareTable, CompareMode mode)
        {
            if (this.Count != other.Count)
                return false;

            for (int i = 0 ; i < this.Count ; ++i) {
                if (!this[i].CompareEqual(other[i], compare, compareTable, mode))
                    return false;
            }

            return true;
        }
        public              TItem                               Find(TName name)
        {
            for (int i=0 ; i < Count ; ++i) {
                if (this[i].Name.Equals(name))
                    return this[i];
            }

            return null;
        }
    }
}
