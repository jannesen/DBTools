using System;
using System.Collections.Generic;
using System.Xml;
using Jannesen.Lib.JDBTools.Library;

namespace Jannesen.Lib.JDBTools.DBSchema.Item
{
    class SchemaColumn: SchemaItemNameRename<SchemaColumn>
    {
        public              string                              Type                { get; private set; }
        public              string                              Identity            { get; private set; }
        public              string                              Collation           { get; private set; }
        public              bool                                isNullable          { get; private set; }
        public              bool                                isRowguid           { get; private set; }
        public              bool                                isFilestream        { get; private set; }
        public              bool                                isXmlDocument       { get; private set; }
        public              bool                                isComputed          { get; private set; }
        public              string                              Default             { get; private set; }
        public              string                              Rule                { get; private set; }

        public                                                  SchemaColumn(XmlReader xmlReader): base(xmlReader)
        {
            try {
                Type          = xmlReader.GetValueString         ("type");
                Identity      = xmlReader.GetValueStringNullable ("identity");
                Collation     = xmlReader.GetValueStringNullable ("collection");
                isNullable    = xmlReader.GetValueBool           ("is-nullable",     false);
                isRowguid     = xmlReader.GetValueBool           ("is-rowguid",      false);
                isFilestream  = xmlReader.GetValueBool           ("is-filestream",   false);
                isXmlDocument = xmlReader.GetValueBool           ("is-xml-document", false);
                isComputed    = xmlReader.GetValueBool           ("is-compute",      false);
                Default       = xmlReader.GetValueStringNullable ("default");
                Rule          = xmlReader.GetValueStringNullable ("rule");

                if (isFilestream)       throw new DBSchemaException("filestream column not supported.");
                if (isXmlDocument)      throw new DBSchemaException("xmldocument column not supported.");
                if (isComputed)         throw new DBSchemaException("Computed column not supported.");

                xmlReader.NoChildElements();
            }
            catch(Exception err) {
                throw new DBSchemaException("Reading of column '" + Name + "' failed.", err);
            }
        }

        public  override    bool                                CompareEqual(SchemaColumn other, CompareTable compareTable, CompareMode mode)
        {
            return (mode == CompareMode.TableCompare || this.Name == other.Name) &&
                   (mode == CompareMode.TableCompare || this.Type == other.Type) &&
                   this.Identity      == other.Identity         &&
                   this.Collation     == other.Collation        &&
                   this.isNullable    == other.isNullable       &&
                   this.isRowguid     == other.isRowguid        &&
                   this.isFilestream  == other.isFilestream     &&
                   this.isXmlDocument == other.isXmlDocument    &&
                   this.isComputed    == other.isComputed       &&
                   this.Default       == other.Default          &&
                   this.Rule          == other.Rule;
        }

        public              void                                WriteRefactor(WriterHelper writer, SqlEntityName tableName)
        {
            if (OrgName != null) {
                writer.WriteRefactorOrgName(OrgName, tableName, "COLUMN", Name);
            }
        }
    }

    class SchemaColumnCollection: SchemaItemList<SchemaColumn,string>
    {
        public              bool                                hasIdentity
        {
            get {
                for (int c = 0 ; c < Count ; ++c) {
                    if (this[c].Identity != null)
                        return true;
                }

                return false;
            }
        }
    }
}
