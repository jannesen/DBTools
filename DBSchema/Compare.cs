﻿using System;
using System.Collections.Generic;
using System.IO;
using Jannesen.Tools.DBTools.DBSchema;
using Jannesen.Tools.DBTools.DBSchema.Item;

namespace Jannesen.Tools.DBTools.DBSchema
{
    internal sealed class DBSchemaCompare
    {
        public              Options                             Options                             { get; private set; }       
        public              DBSchemaDatabase                    CurSchema                           { get; set; }
        public              DBSchemaDatabase                    NewSchema                           { get; set; }
        public              CompareRoleCollection               CompareRoles                        { get; private set; }
        public              CompareDefaultCollection            CompareDefaults                     { get; private set; }
        public              CompareRuleCollection               CompareRules                        { get; private set; }
        public              CompareTypeCollection               CompareTypes                        { get; private set; }
        public              CompareTableCollection              CompareTables                       { get; private set; }
        public              CompareTypeCodeObjectCollection     CompareTypeCodeObject               { get; private set; }
        public              CompareDiagramCollection            CompareDiagram                      { get; private set; }

        public                                                  DBSchemaCompare(Options options)
        {
            Options   = options;  
            CurSchema = new DBSchemaDatabase(options);
            NewSchema = new DBSchemaDatabase(options);
        }

        public              string                              NativeCurType(string type)
        {
            return type.EndsWith("]", StringComparison.Ordinal) ? CompareTypes.FindByCurName(new Library.SqlEntityName(type)).Cur.NativeType : type;
        }
        public              string                              NativeNewType(string type)
        {
            return type.EndsWith("]", StringComparison.Ordinal) ? CompareTypes.FindByNewName(new Library.SqlEntityName(type)).New.NativeType : type;
        }
        public              bool                                EqualType(string curType, string newType)
        {
            if (curType == newType)
                return true;

            if (curType.EndsWith("]", StringComparison.Ordinal) && newType.EndsWith("]", StringComparison.Ordinal)) {
                return CompareTypes.FindByNewName(new Library.SqlEntityName(newType)).New?.GetOrgName(this) == new Library.SqlEntityName(curType);
            }

            return false;
        }
        public              bool                                EqualTable(Library.SqlEntityName curName, Library.SqlEntityName newName)
        {
            return curName == CompareTables.FindByNewName(newName).New.GetOrgName(this);
        }

        public              void                                Report(string fileName, bool includediff)
        {
            _initSchema();
            _initCode();

            using (WriterHelper writer = new WriterHelper(fileName)) {
                CompareDefaults       .Report(this, writer, "defaults");
                CompareRules          .Report(this, writer, "rules");
                CompareTypes          .Report(this, writer, "datatypes");
                CompareTables         .Report(this, writer, "tables");
                CompareRoles          .Report(this, writer, "rules");
                CompareTypeCodeObject .Report(this, writer, includediff);
                // TODO CompareTables         .ReportPermissions(writer);
                // TODO CompareTypeCodeObject .ReportPermissions(writer);
            }
        }
        public              void                                SchemaUpdate(string fileName, bool create)
        {
            _initSchema();
            _initDiagram();

            using (WriterHelper writer = new WriterHelper(fileName)) {
                writer.WriteLine("SET NOCOUNT            ON");
                writer.WriteLine("SET ANSI_NULLS         ON");
                writer.WriteLine("SET ANSI_PADDING       ON");
                writer.WriteLine("SET ANSI_WARNINGS      ON");
                writer.WriteLine("SET QUOTED_IDENTIFIER  ON");
                writer.WriteLine("SET NUMERIC_ROUNDABORT OFF");
                writer.WriteSqlGo();

                if (CompareTables.HasDropTable()) {
                    writer.Write(SqlScript.Resource.GetScriptString("DropAllCode.sql"));
                }
            // Init
                using (WriterHelper wr = new WriterHelper()) {
                    foreach (var compareTable in CompareTables.Items)
                        compareTable.Constraints.Init(wr);

                    foreach (var compareTable in CompareTables.Items)
                        compareTable.References .Init(wr);

                    foreach (var compareTable in CompareTables.Items)
                        compareTable.Indexes    .Init(wr);

                    CompareDefaults.Init(wr);
                    CompareRules   .Init(wr);
                    CompareTypes   .Init(wr);
                    CompareTables  .Init(wr);
                    writer.WriteSqlSection("init update.", wr);
                }

            // Refactor
                if (!create) {
                    CompareTypes.Refactor(writer);
                    CompareDefaults.Refactor(writer);
                    CompareRules.Refactor(writer);
                    CompareTables.Refactor(writer);
                }

            // Process
                CompareDefaults.Process(this, writer);
                CompareRules.Process(this, writer);
                CompareTypes.Process(this, writer);
                CompareTables.Process(this, writer);

            // Copy data
                if (!create) {
                    foreach (CompareTable compareTable in CompareTables.Items)
                        compareTable.ProcessCopyData(this, writer);
                }

            // Cleanup
                using (WriterHelper wr = new WriterHelper()) {
                    CompareTables  .Cleanup(wr);
                    CompareTypes   .Cleanup(wr);
                    CompareRules   .Cleanup(wr);
                    CompareDefaults.Cleanup(wr);
                    writer.WriteSqlSection("cleanup old objects.", wr);
                }

            // Process checks
                foreach (CompareTable compareTable in CompareTables.Items)
                    compareTable.ProcessChecks(this, writer);

            // Process indexes
                foreach (CompareTable compareTable in CompareTables.Items)
                    compareTable.ProcessIndexes(this, writer);

            // Process references
                foreach (CompareTable compareTable in CompareTables.Items)
                    compareTable.ProcessReferences(this, writer);

            // Process roles
                using (WriterHelper wr = new WriterHelper()) {
                    CompareRoles.Process(this, wr);
                    writer.WriteSqlSection("update roles.", wr);
                }

            // Process table permissions
                using (WriterHelper wr = new WriterHelper()) {
                    foreach (CompareTable compareTable in CompareTables.Items)
                        compareTable.ProcessPermissions(this, wr);

                    writer.WriteSqlSection("update table permissions.", wr);
                }

            // Update diagrams
                using (WriterHelper wr = new WriterHelper()) {
                    if (CompareDiagram.hasChange()) {
                        wr.WriteLine("IF NOT EXISTS (SELECT * FROM sys.objects WHERE [object_id] = OBJECT_ID('dbo.[sysdiagrams]'))");
                        wr.WriteLine("BEGIN");
                        wr.WriteLine("    EXECUTE('CREATE TABLE dbo.[sysdiagrams]");
                        wr.WriteLine("             (");
                        wr.WriteLine("                 [name]          [sysname]                           NOT NULL,");
                        wr.WriteLine("                 [principal_id]  [int]                               NOT NULL,");
                        wr.WriteLine("                 [diagram_id]    [int]               IDENTITY(1,1)   NOT NULL,");
                        wr.WriteLine("                 [version]       [int]                               NULL,");
                        wr.WriteLine("                 [definition]    [varbinary](max)                    NULL,");
                        wr.WriteLine("                 CONSTRAINT [PK_sysdiagrams] PRIMARY KEY CLUSTERED ([diagram_id] ASC) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],");
                        wr.WriteLine("                 CONSTRAINT [UK_principal_name] UNIQUE NONCLUSTERED ([principal_id] ASC,  [name] ASC) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]");
                        wr.WriteLine("             ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]')");
                        wr.WriteLine("    EXEC sys.sp_addextendedproperty @name=N'microsoft_database_tools_support', @value=1 , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'sysdiagrams'");
                        wr.WriteLine("END");
                        wr.WriteSqlGo();
                    }

                    foreach (CompareDiagram compareDiagram in CompareDiagram.Items)
                        compareDiagram.Process(this, wr);

                    writer.WriteSqlSection("update diagrams.", wr);
                }

                // Write refactor
                if (!create && Options.Refactor) { 
                    using (WriterHelper wr = new WriterHelper()) {
                        foreach (var i in CompareDefaults.Items)
                            i.New?.WriteRefactor(wr);

                        foreach (var i in CompareRules.Items)
                            i.New?.WriteRefactor(wr);

                        foreach (var i in CompareTypes.Items)
                            i.New?.WriteRefactor(wr);

                        foreach (var i in CompareTables.Items)
                            i.New?.WriteRefactor(wr);

                        if (wr.hasData) {
                            writer.WriteSqlSection("restore refacor data.", wr);
                        }
                    }
                }
            }
        }
        public              void                                CodeUpdate(string fileName)
        {
            _initSchema();
            _initCode();

            using (WriterHelper writer = new WriterHelper(fileName)) {
                CompareTypeCodeObject.CodeUpdate(this, writer);
            }
        }

        private             void                                _initSchema()
        {
            if (CompareRoles == null) {
                CompareRoles    = new CompareRoleCollection   (this, CurSchema.Roles,    NewSchema.Roles   );
                CompareDefaults = new CompareDefaultCollection(this, CurSchema.Defaults, NewSchema.Defaults);
                CompareRules    = new CompareRuleCollection   (this, CurSchema.Rules,    NewSchema.Rules   );
                CompareTypes    = new CompareTypeCollection   (this, CurSchema.Types,    NewSchema.Types   );
                CompareTables   = new CompareTableCollection  (this, CurSchema.Tables,   NewSchema.Tables  );

                CompareRoles   .Compare(this, null);
                CompareDefaults.Compare(this, null);
                CompareRules   .Compare(this, null);
                CompareTypes   .Compare(this, null);
                CompareTables  .Compare(this, null);
                _compareRebuildReference();
                _dropTypes();
            }
        }
        private             void                                _initCode()
        {
            if (CompareTypeCodeObject == null) {
                (CompareTypeCodeObject    = new CompareTypeCodeObjectCollection()   ).Fill(this, CurSchema.CodeObjects,    NewSchema.CodeObjects   );

                CompareTypeCodeObject   .Compare(this);
            }
        }
        private             void                                _initDiagram()
        {
            if (CompareDiagram == null) {
                CompareDiagram = new CompareDiagramCollection(this, CurSchema.Diagrams, NewSchema.Diagrams);
                CompareDiagram.Compare(this, null);
            }
        }
        private             void                                _compareRebuildReference()
        {
            foreach(var targetTable in CompareTables.Items) {
                foreach (var reference in targetTable.References.Items) {
                    if (reference.Cur != null) {
                        if (CompareTables.FindByCurName(reference.Cur.Referenced, out var sourceTable)) {
                            if ((sourceTable.Flags & CompareFlags.Create) != 0)
                                reference.SetRebuild();
                        }
                    }
                }
            }
        }
        private             void                                _dropTypes()
        {
            var  usedTypes = new HashSet<string>();

            foreach(var tables in CompareTables.Items) {
                if (tables.Cur != null) {
                    foreach(var colums in tables.Cur.Columns) {
                        if (colums.Type.EndsWith("]", StringComparison.Ordinal)) {
                            if (!usedTypes.Contains(colums.Type)) {
                                usedTypes.Add(colums.Type);
                            }
                        }
                    }
                }
            }

            foreach(var t in CompareTypes.Items) {
                if (t.Flags == CompareFlags.Drop) {
                    if (!usedTypes.Contains(t.Cur.Name.ToString())) {
                        t.Flags = CompareFlags.None;
                    }
                }
            }
        }
    }
}
