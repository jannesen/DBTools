using System;
using System.Collections.Generic;
using System.Xml;
using System.Text;
using Jannesen.Tools.DBTools.DBSchema;
using Jannesen.Tools.DBTools.Library;

namespace Jannesen.Tools.DBTools.DBSchema.Item
{
    class SchemaType: SchemaItemEntityRename<SchemaType>
    {
        public              string                              NativeType          { get; private set; }
        public              SchemaColumnCollection              Columns             { get; private set; }
        public              SchemaIndexCollection               Indexes             { get; private set; }

        public                                                  SchemaType(XmlReader xmlReader): base(xmlReader)
        {
            try {
                if (xmlReader.IsEmptyElement)
                    throw new XmlReaderException("Missing type content.");

                xmlReader.ReadNext();
                xmlReader.SkipCommentWhitespace();

                if (xmlReader.NodeType == XmlNodeType.CDATA || xmlReader.NodeType == XmlNodeType.Text) {
                    var rtn = xmlReader.Value;

                    xmlReader.ReadNext();
                    xmlReader.SkipCommentWhitespace();

                    if (xmlReader.NodeType != XmlNodeType.EndElement)
                        throw new XmlReaderException("Invalid XML: Unexpected node "+xmlReader.NodeType+" expect EndElement.");

                    NativeType = (rtn != null) ? rtn.Replace("\r\n", "\n") : "";
                }
                else if (xmlReader.NodeType == XmlNodeType.Element) {
                    NativeType = "TABLE";
                    Columns    = new SchemaColumnCollection();
                    Indexes    = new SchemaIndexCollection();

                    do {
                        if (xmlReader.NodeType == XmlNodeType.Element) {
                            switch(xmlReader.Name) {
                            case "column":  Columns    .Add(new SchemaColumn(xmlReader));      break;
                            case "index":   Indexes    .Add(new SchemaIndex(xmlReader));       break;

                            default:
                                throw new XmlReaderException("Invalid XML: Unexpected element '" + xmlReader.Name + "'.");
                            }
                        }
                    } while (xmlReader.ReadNext() != XmlNodeType.EndElement);
                }
                else {
                    throw new XmlReaderException("Invalid XML: Unexpected node "+xmlReader.NodeType);
                }
            }
            catch(Exception err) {
                throw new DBSchemaException("Reading of type '" + Name + "' failed.", err);
            }
        }
        public  override    bool                                CompareEqual(SchemaType other, DBSchemaCompare compare, ICompareTable compareTable, CompareMode mode)
        {
            return base.CompareEqual(other, compare, compareTable, mode)  &&
                   this.NativeType == other.NativeType                    &&
                   ((this.Columns == null && other.Columns == null) || (this.Columns != null && other.Columns != null && this.Columns.CompareEqual(other.Columns, compare, compareTable, mode))) &&
                   ((this.Indexes == null && other.Indexes == null) || (this.Indexes != null && other.Indexes != null && this.Indexes.CompareEqual(other.Indexes, compare, compareTable, mode)));
        }

        public              void                                WriteDrop(WriterHelper writer)
        {
            writer.Write("DROP TYPE ");
            writer.Write(Name);
            writer.WriteNewLine();
        }
        public              void                                WriteCreate(WriterHelper writer)
        {
            writer.Write("CREATE TYPE ");
            writer.Write(Name);

            if (Columns != null) {
                writer.WriteNewLine();
                writer.Write("AS TABLE (");
                Columns.WriteColumns(writer);
                writer.WriteNewLine();
                writer.Write(")");
                writer.WriteNewLine();
            }
            else {
                writer.Write(" FROM ");
                writer.Write(NativeType);
                writer.WriteNewLine();
            }
            //!!TODO UDT defaults and rules.
        }
        public              void                                WriteRename(WriterHelper writer, SqlEntityName newName)
        {
            writer.WriteSqlRename(Name.Fullname, newName.Name, "USERDATATYPE");
            Name = newName;
        }
        public              void                                WriteRefactor(WriterHelper writer)
        {
            if (OrgName != null) {
                writer.WriteRefactorOrgName(OrgName.Fullname, "TYPE", Name);
            }
        }

        public  override    string                              ToReportString()
        {
            return NativeType;
        }
    }

    class SchemaTypeCollection: SchemaItemList<SchemaType,SqlEntityName>
    {
    }

    class CompareType: CompareItem<SchemaType,SqlEntityName>, ICompareTable
    {
        public  override    CompareFlags                        CompareNewCur(DBSchemaCompare compare, ICompareTable compareTable)
        {
            if (!Cur.CompareEqual(New, compare, this, CompareMode.UpdateWithRefactor))
                return CompareFlags.Rebuild;

            if (Cur.Name != New.Name)
                return CompareFlags.Refactor;

            return CompareFlags.None;
        }
        public  override    void                                Init(WriterHelper writer)
        {
            if ((Flags & CompareFlags.Drop) != 0) {
                Cur.WriteRename(writer, new SqlEntityName(Cur.Name.Schema, Cur.Name.Name + "~old"));
                writer.WriteSqlGo();
            }
        }
        public  override    void                                Refactor(WriterHelper writer)
        {
            if ((Flags & CompareFlags.Refactor) != 0) {
                writer.WriteSqlPrint("refactor type " + Cur.Name + " -> " + New.Name);
                Cur.WriteRename(writer, New.Name);
                writer.WriteSqlGo();
            }
        }
        public  override    void                                Process(DBSchemaCompare dbCompare, WriterHelper writer)
        {
            if ((Flags & CompareFlags.Create) != 0) {
                writer.WriteSqlPrint("create type " + New.Name);
                New.WriteCreate(writer);
                writer.WriteSqlGo();
            }
        }
        public  override    void                                Cleanup(WriterHelper writer)
        {
            if ((Flags & CompareFlags.Drop) != 0) {
                Cur.WriteDrop(writer);
                writer.WriteSqlGo();
            }
        }

        public              bool                                EqualColumn(string curName, string newName)
        {
            if (Cur != null && New != null) {
                var c = New.Columns.Find(newName);
                return curName == (c.OrgName ?? c.Name);
            }

            return false;
        }
    }

    class CompareTypeCollection: CompareItemCollection<CompareType,SchemaType,SqlEntityName>
    {
        public                                                  CompareTypeCollection(IReadOnlyList<SchemaType> curSchema, IReadOnlyList<SchemaType> newSchema): base(curSchema, newSchema)
        {
        }

        public  override    void                                Process(DBSchemaCompare dbCompare, WriterHelper writer)
        {
            foreach(var cmp in Items) {
                if (cmp.New?.Columns == null) {
                    cmp.Process(dbCompare, writer);
                }
            }

            foreach(var cmp in Items) { 
                if (cmp.New?.Columns != null) {
                    cmp.Process(dbCompare, writer);
                }
            }
        }
        public  override    void                                Cleanup(WriterHelper writer)
        {
            foreach(var cmp in Items) { 
                if (cmp.Cur?.Columns != null) {
                    cmp.Cleanup(writer);
                }
            }

            foreach(var cmp in Items) { 
                if (cmp.Cur?.Columns == null) {
                    cmp.Cleanup(writer);
                }
            }
        }
    }
}
