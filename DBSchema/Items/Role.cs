﻿using System;
using System.Collections.Generic;
using System.Xml;
using Jannesen.Tools.DBTools.DBSchema;
using Jannesen.Tools.DBTools.Library;

namespace Jannesen.Tools.DBTools.DBSchema.Item
{
    internal sealed class SchemaRole: SchemaItemNameRename<SchemaRole>
    {
        public              List<string>                        Members                         { get; private set; }

        public                                                  SchemaRole(XmlReader xmlReader): base (xmlReader)
        {
            try {
                Members = new List<string>();

                if (xmlReader.HasChildren()) {
                    while (xmlReader.ReadNextElement()) {
                        switch(xmlReader.Name) {
                        case "member": {
                                Members.Add(xmlReader.GetValueString("name"));
                                xmlReader.NoChildElements();
                            }
                            break;

                        default:
                            xmlReader.UnexpectedElement();
                            break;
                        }
                    }
                }
            }
            catch(Exception err) {
                throw new DBSchemaException("Reading of role '" + Name + "' failed.", err);
            }
        }

        public  override    bool                                CompareEqual(SchemaRole other, DBSchemaCompare compare, ICompareTable compareTable, CompareMode mode)
        {
            if (this.Name        != other.Name)
                return false;

            foreach(string member in this.Members) {
                if (!other.Members.Contains(member))
                    return false;
            }

            foreach(string member in other.Members) {
                if (!this.Members.Contains(member))
                    return false;
            }

            return true;
        }

        public              void                                WriteDrop(WriterHelper writer)
        {
            writer.Write("DROP ROLE ");
            writer.WriteQuoteName(Name);
            writer.WriteNewLine();
        }
        public              void                                WriteCreate(WriterHelper writer)
        {
            writer.Write("CREATE ROLE ");
            writer.WriteQuoteName(Name);
            writer.WriteNewLine();
        }
        public              void                                WriteDropMember(WriterHelper writer, string member)
        {
            writer.Write("ALTER ROLE ");
            writer.WriteQuoteName(Name);
            writer.Write(" DROP MEMBER ");
            writer.WriteQuoteName(member);
            writer.WriteNewLine();
        }
        public              void                                WriteAddMember(WriterHelper writer, string member)
        {
            writer.Write("ALTER ROLE ");
            writer.WriteQuoteName(Name);
            writer.Write(" ADD MEMBER ");
            writer.WriteQuoteName(member);
            writer.WriteNewLine();
        }

        public  override    string                              ToReportString()
        {
            string rtn = null;

            foreach(var m in Members) {
                rtn = (rtn != null) ? rtn + "," + m : m;
            }

            return rtn ?? "";
        }
    }

    internal sealed class SchemaRoleCollection: SchemaItemList<SchemaRole,string>
    {
    }

    internal sealed class CompareRole: CompareItem<SchemaRole,string>
    {
        public  override    CompareFlags                        CompareNewCur(DBSchemaCompare compare, ICompareTable compareTable)
        {
            if (!Cur.CompareEqual(New, compare, compareTable, CompareMode.UpdateWithRefactor))
                return CompareFlags.Update;

            if (Cur.Name != New.Name)
                return CompareFlags.Refactor;

            return CompareFlags.None;
        }
        public  override    void                                Process(DBSchemaCompare dbCompare, WriterHelper writer)
        {
            if ((Flags & (CompareFlags.Drop | CompareFlags.Create | CompareFlags.Update)) != 0) {
                if ((Flags & CompareFlags.Create) != 0)
                    New.WriteCreate(writer);

                if ((Flags & CompareFlags.Update) != 0) {
                    foreach(string member in Cur.Members) {
                        if (!New.Members.Contains(member))
                            New.WriteDropMember(writer, member);
                    }
                }

                if ((Flags & (CompareFlags.Create | CompareFlags.Update)) != 0) {
                    foreach(string member in New.Members) {
                        if ((Flags & CompareFlags.Create) != 0 || !Cur.Members.Contains(member))
                            New.WriteAddMember(writer, member);
                    }
                }

                if ((Flags & CompareFlags.Drop) != 0)
                    Cur.WriteDrop(writer);

                writer.WriteSqlGo();
            }
        }
    }

    internal sealed class CompareRoleCollection: CompareItemCollection<CompareRole,SchemaRole,string>
    {
        public                                                  CompareRoleCollection(DBSchemaCompare compare, IReadOnlyList<SchemaRole> curSchema, IReadOnlyList<SchemaRole> newSchema): base(compare, curSchema, newSchema)
        {
        }
    }
}
