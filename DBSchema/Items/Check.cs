using System;
using System.Collections.Generic;
using System.Xml;
using Jannesen.Tools.DBTools.Library;

namespace Jannesen.Tools.DBTools.DBSchema.Item
{
    class SchemaCheck: SchemaItemEntityRename<SchemaCheck>
    {
        public              string                              Definition                      { get; private set; }

        public                                                  SchemaCheck(XmlReader xmlReader): base (xmlReader)
        {
            try {
                Definition = xmlReader.ReadContent();
            }
            catch(Exception err) {
                throw new DBSchemaException("Reading of constraint '" + Name + "' failed.", err);
            }

        }

        public  override    bool                                CompareEqual(SchemaCheck other, DBSchemaCompare compare, CompareTable compareTable, CompareMode mode)
        {
            return base.CompareEqual(other, compare, compareTable, mode) &&
                   this.Definition == other.Definition;
        }

        public              void                                WriteDrop(WriterHelper writer, SqlEntityName tableName)
        {
            writer.Write("ALTER TABLE ");
            writer.Write(tableName);
            writer.Write(" DROP CONSTRAINT ");
            writer.WriteQuoteName(Name.Name);
            writer.WriteNewLine();
        }
        public              void                                WriteCreate(WriterHelper writer, SqlEntityName tableName)
        {
            writer.Write("ALTER TABLE ");
            writer.Write(tableName);
            writer.Write(" ADD CONSTRAINT ");
            writer.WriteQuoteName(Name.Name);

            writer.Write(" CHECK ");

            if (Definition.StartsWith("(") && Definition.EndsWith(")")) {
                writer.Write(Definition);
            }
            else {
                writer.Write("(");
                writer.Write(Definition);
                writer.Write(")");
            }

            writer.WriteNewLine();
        }
        public              void                                WriteRename(WriterHelper writer, SqlEntityName newName)
        {
            writer.WriteSqlRename(Name.Fullname, newName.Name, "OBJECT");
            Name = newName;
        }
        public              void                                WriteRefactor(WriterHelper writer, SqlEntityName tableName)
        {
            if (OrgName != null && OrgName.Schema == tableName.Schema) {
                writer.WriteRefactorOrgName(OrgName.Fullname, tableName, "CONSTRAINT", Name.Name);
            }
        }
    }

    class SchemaCheckCollection: SchemaItemList<SchemaCheck,SqlEntityName>
    {
    }

    class CompareCheck: CompareItem<SchemaCheck,SqlEntityName>
    {
        public  override    CompareFlags                        CompareNewCur(DBSchemaCompare compare, CompareTable compareTable)
        {
            if (!Cur.CompareEqual(New, compare, compareTable, CompareMode.UpdateWithRefactor))
                return CompareFlags.Rebuild;

            if (Cur.Name != New.Name)
                return CompareFlags.Refactor;

            return CompareFlags.None;
        }
        public  override    void                                Init(WriterHelper writer)
        {
            if ((Flags & (CompareFlags.Drop | CompareFlags.Update)) != 0) {
                Cur.WriteDrop(writer, Table.Cur.Name);
                writer.WriteSqlGo();
            }
        }
        public  override    void                                Refactor(WriterHelper writer)
        {
            if ((Flags & CompareFlags.Refactor) != 0) {
                writer.WriteSqlPrint("refactor check " + Table.Cur.Name + "." + WriterHelper.QuoteName(Cur.Name) + " -> " + WriterHelper.QuoteName(New.Name));
                Cur.WriteRename(writer, New.Name);
                writer.WriteSqlGo();
            }
        }
        public  override    void                                Process(DBSchemaCompare dbCompare, WriterHelper writer)
        {
            if ((Flags & (CompareFlags.Create | CompareFlags.Update)) != 0) {
                writer.WriteSqlPrint(((Flags & CompareFlags.Drop) == 0 ? "create check: " : "rebuild check: ") + Table.New.Name + "." + WriterHelper.QuoteName(New.Name.Name));
                New.WriteCreate(writer, Table.New.Name);
                writer.WriteSqlGo();
            }
        }
    }

    class CompareCheckCollection: CompareItemCollection<CompareCheck,SchemaCheck,SqlEntityName>
    {
        public                                                  CompareCheckCollection(DBSchemaCompare compare, CompareTable table, IReadOnlyList<SchemaCheck> curSchema, IReadOnlyList<SchemaCheck> newSchema): base(compare, table, curSchema, newSchema)
        {
        }
    }
}
