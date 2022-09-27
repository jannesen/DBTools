using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using Jannesen.Tools.DBTools.Library;

namespace Jannesen.Tools.DBTools.DBSchema.Item
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
        public              bool                                isPersisted         { get; private set; }
        public              string                              Default             { get; private set; }
        public              string                              Rule                { get; private set; }
        public              string                              Compute             { get; private set; }

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
                isComputed    = xmlReader.GetValueBool           ("is-computed",     false);
                isPersisted   = xmlReader.GetValueBool           ("is-persisted",    false);
                Default       = xmlReader.GetValueStringNullable ("default");
                Rule          = xmlReader.GetValueStringNullable ("rule");
                Compute       = xmlReader.GetValueStringNullable ("compute");

                if (isFilestream)       throw new DBSchemaException("filestream column not supported.");
                if (isXmlDocument)      throw new DBSchemaException("xmldocument column not supported.");

                xmlReader.NoChildElements();
            }
            catch(Exception err) {
                throw new DBSchemaException("Reading of column '" + Name + "' failed.", err);
            }
        }

        public  override    bool                                CompareEqual(SchemaColumn other, DBSchemaCompare compare, ICompareTable compareTable, CompareMode mode)
        {
            return (mode == CompareMode.TableCompare || mode == CompareMode.Report || this.Name == other.Name) &&
                   (mode == CompareMode.TableCompare || compare.EqualType(this.Type, other.Type)) &&
                   this.Identity      == other.Identity         &&
                   this.Collation     == other.Collation        &&
                   this.isNullable    == other.isNullable       &&
                   this.isRowguid     == other.isRowguid        &&
                   this.isFilestream  == other.isFilestream     &&
                   this.isXmlDocument == other.isXmlDocument    &&
                   this.isComputed    == other.isComputed       &&
                   this.isPersisted   == other.isPersisted      &&
                   this.Default       == other.Default          &&
                   this.Rule          == other.Rule             &&
                   this.Compute       == other.Compute;
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

        public              void                                WriteColumns(WriterHelper writer)
        {
            for (int c = 0 ; c < this.Count ; ++c) {
                SchemaColumn column = this[c];

                if (c > 0)
                    writer.Write(",");

                writer.WriteNewLine();
                writer.Write("    ");
                writer.WriteWidth(WriterHelper.QuoteName(column.Name), 48);

                if (!column.isComputed) {
                    writer.WriteWidth(column.Type, 32);

                    if (column.Collation != null) {
                        writer.Write(" COLLATE ");
                        writer.Write(column.Collation);
                    }

                    writer.Write(column.isNullable ? " NULL" : " NOT NULL");

                    if (column.Default != null) {
                        writer.Write(" DEFAULT ");
                        writer.Write(column.Default);
                    }

                    if (column.Identity != null) {
                        writer.Write(" IDENTITY(");
                        writer.Write(column.Identity);
                        writer.Write(")");
                    }

                    if (column.isRowguid) {
                        writer.Write(" ROWGUIDCOL");
                    }
                }
                else {
                    writer.Write("AS ");
                    writer.Write(column.Compute);

                    if (column.isPersisted) {
                        writer.Write(" PERSISTED");
                    }
                    if (column.isNullable) {
                        writer.Write(" NOT NULL");
                    }
                }
            }
        }
    }

    class CompareColumn: CompareItem<SchemaColumn, string>
    {
    }

    class CompareSchemaColumn: CompareItemCollection<CompareColumn, SchemaColumn, string>
    {
        public                      CompareSchemaColumn(DBSchemaCompare compare, IReadOnlyList<SchemaColumn> curSchema, IReadOnlyList<SchemaColumn> newSchema): base(compare, curSchema, newSchema)
        {
        }
    }
}
