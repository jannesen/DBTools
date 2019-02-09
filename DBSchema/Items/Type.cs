using System;
using System.Collections.Generic;
using System.Xml;
using Jannesen.Tools.DBTools.DBSchema;
using Jannesen.Tools.DBTools.Library;

namespace Jannesen.Tools.DBTools.DBSchema.Item
{
    class SchemaType: SchemaItemEntityRename<SchemaType>
    {
        public              string                              NativeType          { get; private set; }

        public                                                  SchemaType(XmlReader xmlReader): base(xmlReader)
        {
            try {
                NativeType = xmlReader.ReadContent();
            }
            catch(Exception err) {
                throw new DBSchemaException("Reading of column '" + Name + "' failed.", err);
            }
        }
        public  override    bool                                CompareEqual(SchemaType other, DBSchemaCompare compare, CompareTable compareTable, CompareMode mode)
        {
            return base.CompareEqual(other, compare, compareTable, mode)        &&
                   this.NativeType == other.NativeType;
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
            writer.Write(" FROM ");
            writer.Write(NativeType);
            writer.WriteNewLine();
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

    class CompareType: CompareItem<SchemaType,SqlEntityName>
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
    }

    class CompareTypeCollection: CompareItemCollection<CompareType,SchemaType,SqlEntityName>
    {
        public                                                  CompareTypeCollection(IReadOnlyList<SchemaType> curSchema, IReadOnlyList<SchemaType> newSchema): base(curSchema, newSchema)
        {
        }
    }
}
