using System;
using System.Collections.Generic;
using System.Xml;
using Jannesen.Tools.DBTools.DBSchema;
using Jannesen.Tools.DBTools.Library;

namespace Jannesen.Tools.DBTools.DBSchema.Item
{
    class SchemaRule: SchemaItemEntityRename<SchemaRule>
    {
        public              string                              Definition                          { get; private set; }

        public                                                  SchemaRule(XmlReader xmlReader): base (xmlReader)
        {
            try {
                Definition = xmlReader.ReadContent();
            }
            catch(Exception err) {
                throw new DBSchemaException("Reading of rule '" + Name + "' failed.", err);
            }

        }

        public  override    bool                                CompareEqual(SchemaRule other, CompareTable compareTable, CompareMode mode)
        {
            return base.CompareEqual(other, compareTable, mode) &&
                   this.Definition == other.Definition;
        }

        public              void                                WriteDrop(WriterHelper writer)
        {
            writer.Write("DROP RULE ");
                writer.Write(Name);
                writer.WriteNewLine();
        }
        public              void                                WriteCreate(WriterHelper writer)
        {
            writer.Write("CREATE RULE ");
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
                writer.WriteRefactorOrgName(OrgName.Fullname, "RULE", Name);
            }
        }
    }

    class SchemaRuleCollection: SchemaItemList<SchemaRule,SqlEntityName>
    {
    }

    class CompareRule: CompareItem<SchemaRule,SqlEntityName>
    {
        public  override    CompareFlags                        CompareNewCur(DBSchemaCompare compare, CompareTable compareTable)
        {
            if (!Cur.CompareEqual(New, compareTable, CompareMode.UpdateWithRefactor))
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
                writer.WriteSqlPrint("refactor rule " + Cur.Name + " -> " + New.Name);
                Cur.WriteRename(writer, New.Name);
                writer.WriteSqlGo();
            }
        }
        public  override    void                                Process(DBSchemaCompare dbCompare, WriterHelper writer)
        {
            if ((Flags & CompareFlags.Create) != 0) {
                writer.WriteSqlPrint("create rule " + New.Name);
                New.WriteCreate(writer);
                writer.WriteSqlGo();
            }
        }
        public  override    void                                Cleanup(WriterHelper writer)
        {
            if ((Flags & CompareFlags.Drop) != 0) {
                New.WriteDrop(writer);
                writer.WriteSqlGo();
            }
        }

        private             void                                _rename(WriterHelper writer, SqlEntityName newName)
        {
        }
    }

    class CompareRuleCollection: CompareItemCollection<CompareRule,SchemaRule,SqlEntityName>
    {
        public                                                  CompareRuleCollection(IReadOnlyList<SchemaRule> curSchema, IReadOnlyList<SchemaRule> newSchema): base(null, curSchema, newSchema)
        {
        }
    }
}
