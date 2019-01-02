using System;
using System.Collections.Generic;
using System.Xml;
using Jannesen.Tools.DBTools.Library;

namespace Jannesen.Tools.DBTools.DBSchema.Item
{
    class SchemaReference: SchemaItemEntityRename<SchemaReference>
    {
        public              SqlEntityName                       Referenced                          { get; private set; }
        public              bool                                isDisabled                          { get; private set; }
        public              string                              Update_action                       { get; private set; }
        public              string                              Delete_action                       { get; private set; }
        public              SchemaReferenceColumnCollection     Columns                             { get; private set; }

        public                                                  SchemaReference(XmlReader xmlReader): base(xmlReader)
        {
            try {
                Referenced    = new SqlEntityName(xmlReader.GetValueString("referenced"));
                isDisabled    = xmlReader.GetValueBool  ("is-disabled", false);
                Update_action = xmlReader.GetValueString("update-action");
                Delete_action = xmlReader.GetValueString("delete-action");

                Columns = new SchemaReferenceColumnCollection();

                if (xmlReader.HasChildren()) {
                    while (xmlReader.ReadNextElement()) {
                        switch(xmlReader.Name) {
                        case "column":      Columns.Add(new SchemaReferenceColumn(xmlReader));      break;
                        default:            xmlReader.UnexpectedElement();                          break;
                        }
                    }
                }
                else
                    throw new DBSchemaException("Index has no columns.");
            }
            catch(Exception err) {
                throw new DBSchemaException("Reading of reference '" + Name + "' failed.", err);
            }
        }

        public              void                                WriteDrop(SchemaTable table, WriterHelper writer)
        {
            writer.Write("ALTER TABLE ");
                writer.Write(table.Name);
                writer.Write(" DROP CONSTRAINT ");
                writer.WriteQuoteName(Name.Name);
                writer.WriteNewLine();
        }
        public              void                                WriteCreate(SchemaTable table, WriterHelper writer)
        {
            bool    f;

            writer.Write("ALTER TABLE ");
                writer.Write(table.Name);
                if (isDisabled)
                    writer.Write(" WITH NOCHECK");

                writer.Write(" ADD CONSTRAINT ");
                writer.WriteQuoteName(Name.Name);
                writer.Write(" FOREIGN KEY (");
                f = false;
                foreach(SchemaReferenceColumn c in Columns) {
                    if (f)
                        writer.Write(", ");

                    writer.WriteQuoteName(c.Name);
                    f = true;
                }
                writer.Write(") REFERENCES ");
                writer.Write(Referenced);
                writer.Write("(");
                f = false;
                foreach(SchemaReferenceColumn c in Columns) {
                    if (f)
                        writer.Write(", ");

                    writer.WriteQuoteName(c.Referenced);
                    f = true;
                }
                writer.Write(")");

                if (!String.IsNullOrEmpty(Delete_action)) {
                    writer.Write(" ON DELETE ");
                    writer.Write(Delete_action.ToUpper());
                }

                if (!String.IsNullOrEmpty(Update_action)) {
                    writer.Write(" ON UPDATE ");
                    writer.Write(Update_action.ToUpper());
                }

                writer.WriteNewLine();

            if (isDisabled) {
                writer.Write("ALTER TABLE ");
                    writer.Write(table.Name);
                    writer.Write(" NOCHECK CONSTRAINT ");
                    writer.WriteQuoteName(Name.Name);
                    writer.WriteNewLine();
            }
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

        public  override    bool                                CompareEqual(SchemaReference other, CompareTable compareTable, CompareMode mode)
        {
            return base.CompareEqual(other, compareTable, mode)            &&
                   this.Referenced    == other.Referenced    &&
                   this.isDisabled    == other.isDisabled    &&
                   this.Update_action == other.Update_action &&
                   this.Delete_action == other.Delete_action &&
                   this.Columns.CompareEqual(other.Columns, compareTable, mode);
        }
    }

    class SchemaReferenceCollection: SchemaItemList<SchemaReference,SqlEntityName>
    {
    }

    class CompareReference: CompareItem<SchemaReference,SqlEntityName>
    {
        public  override    CompareFlags                        CompareNewCur(DBSchemaCompare compare, CompareTable compareTable)
        {
            if (!Cur.CompareEqual(New, compareTable, CompareMode.UpdateWithRefactor))
                return CompareFlags.Rebuild;

            if (Cur.Name != New.Name)
                return CompareFlags.Refactor;

            return CompareFlags.None;
        }
        public  override    void                                Init(WriterHelper writer)
        {
            if ((Flags & CompareFlags.Drop) != 0) {
                Cur.WriteDrop(Table.Cur, writer);
                writer.WriteSqlGo();
            }
        }
        public  override    void                                Refactor(WriterHelper writer)
        {
            if ((Flags & CompareFlags.Refactor) != 0) {
                writer.WriteSqlPrint("refactor reference " + Table.Cur.Name + "." + WriterHelper.QuoteName(Cur.Name) + " -> " + WriterHelper.QuoteName(New.Name));
                Cur.WriteRename(writer, New.Name);
                writer.WriteSqlGo();
            }
        }
        public  override    void                                Process(DBSchemaCompare dbCompare, WriterHelper writer)
        {
            if ((Flags & CompareFlags.Create) != 0) {
                writer.WriteSqlPrint(((Flags & CompareFlags.Drop) == 0 ? "create reference: " : "rebuild reference: ") + Table.New.Name + "." + WriterHelper.QuoteName(New.Name.Name));
                New.WriteCreate(Table.New, writer);
                writer.WriteSqlGo();
            }
        }
    }

    class CompareReferenceCollection: CompareItemCollection<CompareReference,SchemaReference,SqlEntityName>
    {
        public                                                  CompareReferenceCollection(CompareTable table, IReadOnlyList<SchemaReference> curSchema, IReadOnlyList<SchemaReference> newSchema): base(table, curSchema, newSchema)
        {
        }
    }
}
