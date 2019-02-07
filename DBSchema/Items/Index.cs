using System;
using System.Collections.Generic;
using System.Xml;
using Jannesen.Tools.DBTools.Library;

namespace Jannesen.Tools.DBTools.DBSchema.Item
{
    enum SchemaIndexFunction
    {
        PrimaryKey,
        Index,
        Constraint,
        Stat
    }
    enum SchemaIndexType
    {
        Clustered,
        Nonclustered,
    }

    class SchemaIndex: SchemaItemNameRename<SchemaIndex>
    {
        public              SchemaIndexFunction                 Function                            { get; private set; }
        public              SchemaIndexType                     Type                                { get; private set; }
        public              bool                                Unique                              { get; private set; }
        public              int?                                FillFactor                          { get; private set; }
        public              string                              Filter                              { get; private set; }
        public              SchemaIndexColumnCollection         Columns                             { get; private set; }

        public                                                  SchemaIndex(XmlReader xmlReader): base(xmlReader)
        {
            try {
                Function    = _strToIndexFunction(xmlReader.GetValueString("function"));
                Type        = _strToIndexType    (xmlReader.GetValueString("type"));
                FillFactor  = xmlReader.GetValueIntNullable    ("fill-factor");

                switch(Function) {
                case SchemaIndexFunction.PrimaryKey:
                    Unique = true;
                    break;

                case SchemaIndexFunction.Index:
                    Unique    = xmlReader.GetValueBool           ("unique");
                    Filter    = xmlReader.GetValueStringNullable ("filter");
                    break;

                case SchemaIndexFunction.Constraint:
                    Unique = true;
                    break;

                case SchemaIndexFunction.Stat:
                    break;
                }

                Columns = new SchemaIndexColumnCollection();

                if (xmlReader.HasChildren()) {
                    while (xmlReader.ReadNextElement()) {
                        switch(xmlReader.Name) {
                        case "column":      Columns.Add(new SchemaIndexColumn(xmlReader));      break;
                        default:            xmlReader.UnexpectedElement();                      break;
                        }
                    }
                }
                else
                    throw new DBSchemaException("Index has no columns.");
            }
            catch(Exception err) {
                throw new DBSchemaException("Reading of column '" + Name + "' failed.", err);
            }
        }

        public  override    bool                                CompareEqual(SchemaIndex other, DBSchemaCompare compare, CompareTable compareTable, CompareMode mode)
        {
            return base.CompareEqual(other, compare, compareTable, mode)     &&
                   this.Function    == other.Function   &&
                   this.Type        == other.Type       &&
                   this.Unique      == other.Unique     &&
                   (mode != CompareMode.Update || this.FillFactor == other.FillFactor) &&
                   this.Filter      == other.Filter     &&
                   this.Columns.CompareEqual(other.Columns, compare, compareTable, mode);
        }

        public              void                                WriteDrop(WriterHelper writer, SqlEntityName tableName)
        {
            switch(Function) {
            case SchemaIndexFunction.Index:
                writer.Write("DROP INDEX ");
                writer.Write(tableName);
                writer.Write(".");
                writer.WriteQuoteName(Name);
                writer.WriteNewLine();
                break;

            case SchemaIndexFunction.Constraint:
            case SchemaIndexFunction.PrimaryKey:
                writer.Write("ALTER TABLE ");
                writer.Write(tableName);
                writer.Write(" DROP CONSTRAINT ");
                writer.WriteQuoteName(Name);
                writer.WriteNewLine();
                break;

            case SchemaIndexFunction.Stat:
                writer.Write("DROP STATISTICS ");
                writer.Write(tableName);
                writer.Write(".");
                writer.WriteQuoteName(Name);
                writer.WriteNewLine();
                break;
            }
        }
        public              void                                WriteCreate(WriterHelper writer, SqlEntityName tableName, bool tableCreate)
        {
            if (!tableCreate && (Function == SchemaIndexFunction.PrimaryKey || Function == SchemaIndexFunction.Constraint)) {
                writer.Write("ALTER TABLE ");
                writer.Write(tableName);
                writer.Write(" ADD ");
            }

            switch(Function) {
            case SchemaIndexFunction.PrimaryKey:
                writer.Write("CONSTRAINT ");
                writer.WriteWidth(WriterHelper.QuoteName(Name), 36);
                writer.Write(Type == SchemaIndexType.Clustered ? " PRIMARY KEY CLUSTERED ": " PRIMARY KEY NONCLUSTERED ");
                break;

            case SchemaIndexFunction.Constraint:
                writer.Write("CONSTRAINT ");
                writer.WriteWidth(WriterHelper.QuoteName(Name), 36);
                writer.Write(Type == SchemaIndexType.Clustered ? " UNIQUE CLUSTERED ": " UNIQUE NONCLUSTERED ");
                break;

            case SchemaIndexFunction.Index:
                writer.Write("CREATE");
                if (Unique)
                    writer.Write(" UNIQUE");
                writer.Write(Type == SchemaIndexType.Clustered ? " CLUSTERED INDEX ": " NONCLUSTERED INDEX ");
                writer.WriteQuoteName(Name);
                writer.Write(" ON ");
                writer.Write(tableName);
                break;

            case SchemaIndexFunction.Stat:
                writer.Write("CREATE STATISTICS ");
                writer.WriteQuoteName(Name);
                writer.Write(" ON ");
                writer.Write(tableName);
                break;
            }

            writer.Write("(");

            for (int i = 0 ; i < Columns.Count ; ++i) {
                SchemaIndexColumn   column = Columns[i];

                if (i > 0)
                    writer.Write(", ");

                writer.WriteQuoteName(column.Name);

                if (column.Order != null) {
                    writer.Write(" ");
                    writer.Write(column.Order);
                }
            }

            writer.Write(")");

            if (Filter != null) {
                writer.Write(" WHERE ");
                writer.Write(Filter);
            }

            if (FillFactor.HasValue) {
                writer.Write(" WITH FILLFACTOR=");
                writer.Write(FillFactor.Value.ToString());
            }

            if (!tableCreate)
                writer.WriteNewLine();
        }
        public              void                                WriteRename(WriterHelper writer, SqlEntityName tableName, string newName)
        {
            writer.WriteSqlRename(tableName.Fullname + "." + WriterHelper.QuoteName(Name), newName, "INDEX");
            Name = newName;
        }
        public              void                                WriteRefactor(WriterHelper writer, SqlEntityName tableName)
        {
            if (OrgName != null) {
                writer.WriteRefactorOrgName(OrgName, tableName, "INDEX", Name);
            }
        }

        private             SchemaIndexFunction                 _strToIndexFunction(string s)
        {
            switch(s) {
            case "primary-key":     return  SchemaIndexFunction.PrimaryKey;
            case "constraint":      return  SchemaIndexFunction.Constraint;
            case "index":           return  SchemaIndexFunction.Index;
            case "stat":            return  SchemaIndexFunction.PrimaryKey;
            default:                throw new Exception("Unknown function '" + s + "'.");
            }
        }
        private             SchemaIndexType                     _strToIndexType(string s)
        {
            switch(s) {
            case "clustered":       return  SchemaIndexType.Clustered;
            case "nonclustered":    return  SchemaIndexType.Nonclustered;
            default:                throw new Exception("Unknown type '" + s + "'.");
            }
        }
    }

    class SchemaIndexCollection: SchemaItemList<SchemaIndex,string>
    {
        public          SchemaIndex                             PrimaryKey
        {
            get {
                for (int i = 0 ; i < Count ; ++i) {
                    if (this[i].Function == SchemaIndexFunction.PrimaryKey)
                        return this[i];
                }

                return null;
            }
        }
        public          SchemaIndex                             Clustered
        {
            get {
                for (int i = 0 ; i < Count ; ++i) {
                    if (this[i].Type == SchemaIndexType.Clustered)
                        return this[i];
                }

                return null;
            }
        }
    }

    class CompareIndex: CompareItem<SchemaIndex,string>
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
                if (Cur.Type == SchemaIndexType.Clustered)
                    Cur.WriteRename(writer, Table.Cur.Name, Cur.Name + "~old");
                else
                    Cur.WriteDrop(writer, Table.Cur.Name);

                writer.WriteSqlGo();
            }
        }
        public  override    void                                Refactor(WriterHelper writer)
        {
            if ((Flags & CompareFlags.Refactor) != 0) {
                writer.WriteSqlPrint("refactor index " + Table.Cur.Name + "." + WriterHelper.QuoteName(Cur.Name) + " -> " + WriterHelper.QuoteName(New.Name));
                Cur.WriteRename(writer, Table.Cur.Name, New.Name);
                writer.WriteSqlGo();
            }
        }
        public  override    void                                Process(DBSchemaCompare dbCompare, WriterHelper writer)
        {
            if ((Flags & CompareFlags.Create) != 0) {
                if (New.Function == SchemaIndexFunction.Index && New.Type != SchemaIndexType.Clustered) {
                    writer.WriteSqlPrint(((Flags & CompareFlags.Drop) == 0 ? "create index: " : "rebuild index: ") + Table.New.Name + "." + WriterHelper.QuoteName(New.Name));
                    New.WriteCreate(writer, Table.New.Name, false);
                    writer.WriteSqlGo();
                }

                if (New.Function == SchemaIndexFunction.Stat) {
                    writer.WriteSqlPrint(((Flags & CompareFlags.Drop) != 0 ? "create statistics: " : "rebuild statistics: ") + Table.New.Name + "." + WriterHelper.QuoteName(New.Name));
                    New.WriteCreate(writer, Table.New.Name, false);
                    writer.WriteSqlGo();
                }
            }
        }
        public  override    void                                Cleanup(WriterHelper writer)
        {
            if ((Flags & CompareFlags.Drop) != 0 && Cur.Type == SchemaIndexType.Clustered) {
                Cur.WriteDrop(writer, Table.Cur.Name);
                writer.WriteSqlGo();
            }
        }
    }

    class CompareIndexCollection: CompareItemCollection<CompareIndex,SchemaIndex,string>
    {
        public                                                  CompareIndexCollection(DBSchemaCompare compare, CompareTable table, IReadOnlyList<SchemaIndex> curSchema, IReadOnlyList<SchemaIndex> newSchema): base(compare, table, curSchema, newSchema)
        {
        }
    }
}
