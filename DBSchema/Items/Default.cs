using System;
using System.Collections.Generic;
using System.Xml;
using Jannesen.Tools.DBTools.DBSchema;
using Jannesen.Tools.DBTools.Library;

namespace Jannesen.Tools.DBTools.DBSchema.Item
{
    class SchemaDefault: SchemaItemEntityRename<SchemaDefault>
    {
        public              string                              Definition                      { get; private set; }

        public                                                  SchemaDefault(XmlReader xmlReader): base(xmlReader)
        {
            try {
                Definition = xmlReader.ReadContent();
            }
            catch(Exception err) {
                throw new DBSchemaException("Reading of default '" + Name + "' failed.", err);
            }
        }

        public  override    bool                                CompareEqual(SchemaDefault other, DBSchemaCompare compare, CompareTable compareTable, CompareMode mode)
        {
            return base.CompareEqual(other, compare, compareTable, mode) &&
                   this.Definition == other.Definition;
        }

        public              void                                WriteDrop(WriterHelper writer)
        {
            writer.Write("DROP DEFAULT ");
                writer.Write(Name);
                writer.WriteNewLine();
        }
        public              void                                WriteCreate(WriterHelper writer)
        {
            writer.Write("CREATE DEFAULT ");
                writer.Write(Name);
                writer.Write(" AS ");
                writer.Write(Definition);
                writer.WriteNewLine();
        }
        public              void                                WriteRename(WriterHelper writer, SqlEntityName newName)
        {
            writer.WriteSqlRename(Name.Fullname, newName.Name, "OBJECT");
            Name = newName;
        }
        public              void                                WriteRefactor(WriterHelper writer)
        {
            if (OrgName != null) {
                writer.WriteRefactorOrgName(OrgName.Fullname, "DEFAULT", Name);
            }
        }
    }

    class SchemaDefaultCollection: SchemaItemList<SchemaDefault,SqlEntityName>
    {
    }

    class CompareDefault: CompareItem<SchemaDefault,SqlEntityName>
    {
        public  override    CompareFlags                        CompareNewCur(DBSchemaCompare compare, CompareTable compareTable)
        {
            if (!Cur.CompareEqual(New, compare, compareTable, CompareMode.UpdateWithRefactor))
                return CompareFlags.Update;

            if (Cur.Name != New.Name)
                return CompareFlags.Refactor;

            return CompareFlags.None;
        }
        public  override    void                                Init(WriterHelper writer)
        {
            if ((Flags & CompareFlags.Drop) != 0) {
                Cur.WriteRename(writer, new SqlEntityName(Cur.Name.Schema, Cur.Name.Name + "~old"));
            }
        }
        public  override    void                                Refactor(WriterHelper writer)
        {
            if ((Flags & CompareFlags.Refactor) != 0) {
                writer.WriteSqlPrint("refactor default " + Cur.Name + " -> " + New.Name);
                Cur.WriteRename(writer, New.Name);
                writer.WriteSqlGo();
            }
        }
        public  override    void                                Process(DBSchemaCompare dbCompare, WriterHelper writer)
        {
            if ((Flags & CompareFlags.Create) != 0) {
                writer.WriteSqlPrint("create default " + Cur.Name + " -> " + New.Name);
                New.WriteCreate(writer);
                writer.WriteSqlGo();
            }
        }
        public  override    void                                Cleanup(WriterHelper writer)
        {
            if ((Flags & CompareFlags.Drop) != 0) {
                New.WriteCreate(writer);
                writer.WriteSqlGo();
            }
        }
    }

    class CompareDefaultCollection: CompareItemCollection<CompareDefault,SchemaDefault,SqlEntityName>
    {
        public                                                  CompareDefaultCollection(IReadOnlyList<SchemaDefault> curSchema, IReadOnlyList<SchemaDefault> newSchema): base(curSchema, newSchema)
        {
        }
    }
}
