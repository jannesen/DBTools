using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Jannesen.Tools.DBTools.DBSchema;
using Jannesen.Tools.DBTools.Library;

namespace Jannesen.Tools.DBTools.DBSchema.Item
{
    class SchemaTable: SchemaItemEntityRename<SchemaTable>
    {
        public              SchemaColumnCollection              Columns         { get; private set; }
        public              SchemaCheckCollection               Checks          { get; private set; }
        public              SchemaIndexCollection               Indexes         { get; private set; }
        public              SchemaReferenceCollection           References      { get; private set; }
        public              SchemaPermissionCollection          Permissions     { get; private set; }

        public                                                  SchemaTable(XmlReader xmlReader): base(xmlReader)
        {
            try {
                Columns           = new SchemaColumnCollection();
                Checks            = new SchemaCheckCollection();
                Indexes           = new SchemaIndexCollection();
                Permissions       = new SchemaPermissionCollection();
                References        = new SchemaReferenceCollection();

                if (xmlReader.HasChildren()) {
                    while (xmlReader.ReadNextElement()) {
                        switch (xmlReader.Name) {
                        case "column":              Columns    .Add(new SchemaColumn(xmlReader));      break;
                        case "constraint":          Checks     .Add(new SchemaCheck(xmlReader));       break;
                        case "index":               Indexes    .Add(new SchemaIndex(xmlReader));       break;
                        case "reference":           References .Add(new SchemaReference(xmlReader));   break;
                        case "permission":          Permissions.Add(new SchemaPermission(xmlReader));  break;
                        default:                    xmlReader.UnexpectedElement();  break;
                        }
                    }
                }
                else
                    throw new DBSchemaException("Table has no columns.");
            }
            catch(Exception err) {
                throw new DBSchemaException("Reading of table '" + Name + "' failed.", err);
            }
        }

        public  override    bool                                CompareEqual(SchemaTable other, DBSchemaCompare compare, ICompareTable compareTable, CompareMode mode)
        {
            return base.CompareEqual(other, compare, compareTable, mode)                         &&
                   this.Columns.CompareEqual(other.Columns, compare, compareTable, mode)         &&
                   this.Checks.CompareEqual(other.Checks, compare, compareTable, mode)           &&
                   this.Indexes.CompareEqual(other.Indexes, compare, compareTable, mode)         &&
                   this.References.CompareEqual(other.References, compare, compareTable, mode)   &&
                   (mode != CompareMode.Update || !this.Permissions.CompareEqual(other.Permissions, compare, compareTable, mode));
        }

        public              void                                WriteRefactor(WriterHelper writer)
        {
            if (OrgName != null) {
                writer.WriteRefactorOrgName(OrgName.Fullname, "TABLE", Name);
            }

            foreach(var c in Columns)
                c.WriteRefactor(writer, Name);

            foreach(var c in Indexes)
                c.WriteRefactor(writer, Name);

            foreach(var c in References)
                c.WriteRefactor(writer, Name);

            foreach(var c in Checks)
                c.WriteRefactor(writer, Name);
        }
    }

    class SchemaTableCollection: SchemaItemList<SchemaTable,SqlEntityName>
    {
    }

    class CompareTable: CompareItem<SchemaTable,SqlEntityName>, ICompareTable
    {
        public              CompareCheckCollection              Constraints      { get; private set; }
        public              CompareIndexCollection              Indexes         { get; private set; }
        public              CompareReferenceCollection          References      { get; private set; }

        public  override    void                                InitDepended(DBSchemaCompare compare)
        {
            Constraints = new CompareCheckCollection    (compare, this, Cur?.Checks    , New?.Checks    );
            Indexes     = new CompareIndexCollection    (compare, this, Cur?.Indexes   , New?.Indexes   );
            References  = new CompareReferenceCollection(compare, this, Cur?.References, New?.References);
        }

        public  override    CompareFlags                        CompareNewCur(DBSchemaCompare compare, ICompareTable compareTable)
        {
            CompareFlags    rtnFlags = CompareFlags.None;

            if (Cur.Name != New.Name) {
                rtnFlags = CompareFlags.Refactor;
            }

            if (Cur.Columns.Count != New.Columns.Count)
                return CompareFlags.Rebuild;

            for (int i = 0 ; i < Cur.Columns.Count ; ++i) {
                var curColumn = Cur.Columns[i];
                var newColumn = New.Columns[i];

                if (!curColumn.CompareEqual(newColumn, compare, compareTable, CompareMode.TableCompare))
                    return CompareFlags.Rebuild;

                if (curColumn.Name != newColumn.Name) {
                    if (curColumn.Name != newColumn.OrgName)
                        return CompareFlags.Rebuild;

                    rtnFlags |= CompareFlags.Refactor;
                }

                if (curColumn.Type != newColumn.Type) {
                    if (compare.NativeCurType(curColumn.Type) != compare.NativeNewType(newColumn.Type))
                        return CompareFlags.Rebuild;

                    rtnFlags |= CompareFlags.Update;
                }

                if ((                             curColumn.Type   .EndsWith("]", StringComparison.Ordinal) && (compare.CompareTypes   .FindByCurName(new SqlEntityName(curColumn.Type   )).Flags & CompareFlags.Create) != 0) ||
                    (curColumn.Default != null && curColumn.Default.EndsWith("]", StringComparison.Ordinal) && (compare.CompareDefaults.FindByCurName(new SqlEntityName(curColumn.Default)).Flags & CompareFlags.Create) != 0) ||
                    (curColumn.Rule    != null && curColumn.Rule   .EndsWith("]", StringComparison.Ordinal) && (compare.CompareRules   .FindByCurName(new SqlEntityName(curColumn.Rule   )).Flags & CompareFlags.Create) != 0) )
                    return CompareFlags.Rebuild;
            }

            var curClusteredIndex = Cur.Indexes.Clustered;
            var newClusteredIndex = New.Indexes.Clustered;

            if ((curClusteredIndex != null && newClusteredIndex == null) ||
                (curClusteredIndex == null && newClusteredIndex != null) ||
                (curClusteredIndex != null && newClusteredIndex != null && !curClusteredIndex.CompareEqual(newClusteredIndex, compare, this, CompareMode.UpdateWithRefactor)))
                return CompareFlags.Rebuild;

            return rtnFlags;
        }
        public  override    bool                                CompareDepended(DBSchemaCompare dbCompare, CompareFlags status)
        {
            Constraints.Compare(dbCompare, this);
            Indexes    .Compare(dbCompare, this);
            References .Compare(dbCompare, this);

            if ((status & (CompareFlags.Create)) != 0) {
                Constraints.SetRebuild();
                Indexes.SetRebuild();
                References.SetRebuild();
            }

            return Constraints.hasChange() || Indexes.hasChange() || References.hasChange();
        }
        public  override    void                                ReportUpdate(DBSchemaCompare compare, WriterHelper reportWriter)
        {
            bool    report = false;

            using (var writer = new WriterHelper()) {
                writer.Write("------------------------------------------------------------------------------------------------------------------------");
                writer.WriteNewLine();
                writer.Write(Cur.Name);
                if (!Cur.Name.Equals(New.Name)) {
                    writer.Write(" => ");
                    writer.Write(New.Name);
                    report = true;
                }
                writer.WriteNewLine();
                writer.Write("------------------------------------------------------------------------------------------------------------------------");
                writer.WriteNewLine();

                var cmp = new CompareSchemaColumn(Cur.Columns, New.Columns);

                foreach(var c in cmp.Items) {
                    var curColumn = c.Cur;
                    var newColumn = c.New;

                    if (curColumn != null && newColumn != null) {
                        if (!curColumn.Name.Equals(newColumn.Name, StringComparison.Ordinal)) {
                            writer.WriteWidth(WriterHelper.QuoteName(curColumn.Name), 48);
                            writer.Write("=> ");
                            writer.Write(WriterHelper.QuoteName(newColumn.Name));
                            writer.WriteNewLine();
                        }

                        if (!curColumn.CompareEqual(newColumn, compare, this, CompareMode.Report)) {
                            writer.WriteWidth(!curColumn.Name.Equals(newColumn.Name, StringComparison.Ordinal) ? "" : WriterHelper.QuoteName(newColumn.Name), 48);
                            writer.Write(": ");
                            var cn = compare.NativeCurType(curColumn.Type);
                            var nn = compare.NativeNewType(newColumn.Type);

                            if (cn != nn) {
                                writer.Write(" { ");

                                writer.Write(curColumn.Type);
                                if (curColumn.Type != cn) {
                                    writer.Write(" (");
                                    writer.Write(cn);
                                    writer.Write(")");
                                }

                                writer.Write(" -> ");
                                writer.Write(newColumn.Type);
                                if (newColumn.Type != nn) {
                                    writer.Write(" (");
                                    writer.Write(nn);
                                    writer.Write(")");
                                }

                                writer.Write(" }");
                            }

                            if (curColumn.isNullable != newColumn.isNullable) {
                                writer.Write(" { ");
                                writer.Write(curColumn.isNullable ? "IS NULL" : "IS NOT NULL");
                                writer.Write(" -> ");
                                writer.Write(newColumn.isNullable ? "IS NULL" : "IS NOT NULL");
                                writer.Write(" }");
                            }

                            writer.WriteNewLine();
                            report = true;
                        }
                    }
                }

                foreach(var c in cmp.Items) {
                    if (c.Cur == null) {
                        var newColumn = c.New;
                        writer.Write("column ");
                        writer.WriteWidth(WriterHelper.QuoteName(newColumn.Name), 48);
                        writer.Write(": new ");
                        writer.Write(newColumn.Type);
                        var nn = compare.NativeNewType(newColumn.Type);
                        if (newColumn.Type != nn) {
                            writer.Write(" (");
                            writer.Write(nn);
                            writer.Write(")");
                        }
                        writer.WriteNewLine();
                        report = true;
                    }
                }

                foreach(var c in cmp.Items) {
                    if (c.New == null) {
                        var curColumn = c.Cur;
                        writer.Write("column ");
                        writer.WriteWidth(WriterHelper.QuoteName(curColumn.Name), 48);
                        writer.Write(": delete");
                        writer.WriteNewLine();
                        report = true;
                    }
                }

                if (Constraints .ReportDepended(writer, compare, this, "check ")) {
                    report = true;
                }
                if (Indexes     .ReportDepended(writer, compare, this, "index ")) {
                    report = true;
                }
                if (References  .ReportDepended(writer, compare, this, "refer ")) {
                    report = true;
                }

                if (report) {
                    reportWriter.WriteSection(writer);
                }
            }
        }
        public  override    void                                Init(WriterHelper writer)
        {
            if ((Flags & CompareFlags.Drop) != 0) {
                _rename(writer, new SqlEntityName(Cur.Name.Schema, Cur.Name.Name + "~old"));
                writer.WriteSqlGo();
            }
        }
        public  override    void                                Refactor(WriterHelper writer)
        {
            if ((Flags & CompareFlags.Refactor) != 0) {
                if (Cur.Name != New.Name) {
                    writer.WriteSqlPrint("refactor table " + Cur.Name + " -> " + New.Name);
                    _rename(writer, New.Name);
                    writer.WriteSqlGo();
                }

                for (int i = 0 ; i < Cur.Columns.Count ; ++i) {
                    var curColumn = Cur.Columns[i];
                    var newColumn = New.Columns[i];

                    if (curColumn.Name != newColumn.Name) {
                        writer.WriteSqlPrint("refactor column " + Cur.Name.Fullname + "." +WriterHelper.QuoteName(curColumn.Name) + " -> " + newColumn.Name);
                        writer.WriteSqlRename(Cur.Name.Fullname + "." +WriterHelper.QuoteName(curColumn.Name), newColumn.Name, "COLUMN");
                        curColumn.Name = newColumn.Name;
                        writer.WriteSqlGo();
                    }
                }
            }

            if ((Flags & (CompareFlags.Drop|CompareFlags.Create)) == 0) {
                Constraints.Refactor(writer);
                Indexes    .Refactor(writer);
                References .Refactor(writer);
            }
        }
        public  override    void                                Process(DBSchemaCompare compare, WriterHelper writer)
        {
            if ((Flags & CompareFlags.Create) != 0) {
                writer.WriteSqlPrint(((Flags & CompareFlags.Drop) == 0 ? "create  table: " :  "rebuild table: ") + New.Name);
                writer.Write("CREATE TABLE ");
                writer.Write(New.Name);
                writer.WriteNewLine();

                writer.Write("(");
                New.Columns.WriteColumns(writer);

                {
                    SchemaIndex     primaryKey = New.Indexes.PrimaryKey;

                    if (primaryKey != null) {
                        writer.Write(',');
                        writer.WriteNewLine();
                        writer.Write("    ");
                        primaryKey.WriteCreate(writer, New.Name, true);
                    }
                }

                {
                    foreach (SchemaIndex index in New.Indexes) {
                        if (index.Function == SchemaIndexFunction.Constraint) {
                            writer.Write(',');
                            writer.WriteNewLine();
                            writer.Write("    ");
                            index.WriteCreate(writer, New.Name, true);
                        }
                    }
                }

                writer.WriteNewLine();
                writer.Write(")");
                writer.WriteNewLine();

                {
                    foreach (SchemaIndex index in New.Indexes) {
                        if (index.Function == SchemaIndexFunction.Index && index.Type == SchemaIndexType.Clustered)
                            index.WriteCreate(writer, New.Name, false);
                    }
                }
                writer.WriteSqlGo();
            }

            if ((Flags & CompareFlags.Update) != 0) {
                bool    alterTable = false;

                for (int i = 0 ; i < New.Columns.Count ; ++i) {
                    var c = Cur.Columns[i];
                    var n = New.Columns[i];

                    if (!compare.EqualType(c.Type, n.Type)) {
                        if (!alterTable) {
                            writer.WriteSqlPrint("alter   table: " + New.Name);
                            alterTable = true;
                        }

                        writer.Write("ALTER TABLE ");
                        writer.Write(New.Name);
                        writer.Write(" ALTER COLUMN ");
                        writer.WriteWidth(WriterHelper.QuoteName(c.Name), 40);
                        writer.Write(n.Type);
                        writer.Write(n.isNullable ? " NULL" : " NOT NULL");
                        writer.WriteNewLine();
                    }
                }

                if (alterTable)
                    writer.WriteSqlGo();
            }
        }
        public              void                                ProcessCopyData(WriterHelper writer)
        {
            if ((Flags & (CompareFlags.Drop | CompareFlags.Create)) == (CompareFlags.Drop | CompareFlags.Create)) {
                bool n;

                writer.WriteSqlPrint("copy data into table: " + New.Name);

                if (New.Columns.hasIdentity) {
                    writer.Write("SET IDENTITY_INSERT ");
                        writer.Write(New.Name);
                        writer.Write(" ON");
                        writer.WriteNewLine();
                }

                writer.Write("INSERT INTO ");
                    writer.Write(New.Name);
                    writer.WriteNewLine();

                writer.Write("      (");
                    n = false;
                    for(int c=0 ; c < New.Columns.Count ; ++c) {
                        if (!New.Columns[c].isComputed) { 
                            if (n)
                                writer.Write(", ");
                            else
                                n = true;

                            writer.WriteQuoteName(New.Columns[c].Name);
                        }
                    }
                    writer.Write(")");
                    writer.WriteNewLine();

                for(int c=0 ; c < New.Columns.Count ; ++c) {
                    SchemaColumn    newColumn = New.Columns[c];
                    if (!New.Columns[c].isComputed) { 
                        SchemaColumn    oldColumn = Cur.Columns.Find(newColumn.OrgName ?? newColumn.Name);

                        writer.Write((c == 0) ? "SELECT " : "       ");
                        writer.WriteWidth(WriterHelper.QuoteName(newColumn.Name), 40);

                        if (oldColumn != null) {
                            writer.Write(" = old.");
                            writer.WriteQuoteName(oldColumn.Name);
                        }
                        else {
                            writer.Write(newColumn.isNullable ? " = NULL" : " = /*!!TODO!!*/");
                        }

                        if (c < New.Columns.Count - 1)
                            writer.Write(",");

                        writer.WriteNewLine();
                    }
                }

                writer.Write("  FROM ");
                    writer.Write(Cur.Name);
                    writer.Write(" old");
                    writer.WriteNewLine();

                if (New.Columns.hasIdentity) {
                    writer.Write("SET IDENTITY_INSERT ");
                        writer.Write(New.Name);
                        writer.Write(" OFF");
                        writer.WriteNewLine();
                }

                writer.WriteSqlGo();
            }

            if ((Flags & (CompareFlags.Drop | CompareFlags.Create)) == (CompareFlags.Create)) {
                writer.WriteSqlPrint("copy data into table: " + New.Name);
                    writer.Write("--INSERT INTO ");
                    writer.Write(New.Name);

                writer.Write(" (");
                    for(int c=0 ; c < New.Columns.Count ; ++c) {
                        if (c > 0)
                            writer.Write(", ");

                        writer.WriteQuoteName(New.Columns[c].Name);
                    }
                    writer.Write(")");
                    writer.WriteNewLine();

                writer.WriteSqlGo();
            }
        }
        public              void                                ProcessChecks(DBSchemaCompare compare, WriterHelper writer)
        {
            if (New != null)
                Constraints.Process(compare, writer);
        }
        public              void                                ProcessIndexes(DBSchemaCompare compare, WriterHelper writer)
        {
            if (New != null)
                Indexes.Process(compare, writer);
        }
        public              void                                ProcessReferences(DBSchemaCompare compare, WriterHelper writer)
        {
            if (New != null)
                References.Process(compare, writer);
        }
        public              void                                ProcessPermissions(DBSchemaCompare compare, WriterHelper writer)
        {
            if (New != null) {
                (new ComparePermissionCollection(compare, this, Cur?.Permissions, New.Permissions)).WriteGrantRevoke(writer, Flags, New.Name, 30);
            }
        }
        public  override    void                                Cleanup(WriterHelper writer)
        {
            if ((Flags & CompareFlags.Drop) != 0) {
                writer.Write("DROP TABLE ");
                writer.Write(Cur.Name);
                writer.WriteNewLine();
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

        private             void                                _rename(WriterHelper writer, SqlEntityName newName)
        {
            writer.WriteSqlRename(Cur.Name.Fullname, newName.Name, "OBJECT");
            Cur.Name = newName;
        }
    }

    class CompareTableCollection: CompareItemCollection<CompareTable,SchemaTable,SqlEntityName>
    {
        public                                                  CompareTableCollection(IReadOnlyList<SchemaTable> curSchema, IReadOnlyList<SchemaTable> newSchema): base(curSchema, newSchema)
        {
        }

        public              bool                                HasDropTable()
        {
            foreach(var i in Items) {
                if ((i.Flags & CompareFlags.Drop) != 0)
                    return true;
            }

            return false;
        }
    }
}
