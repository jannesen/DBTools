using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;
using Jannesen.Tools.DBTools.DBSchema;
using Jannesen.Tools.DBTools.Library;

namespace Jannesen.Tools.DBTools.DBSchema.Item
{
    enum SqlCodeObjectType
    {
        View                        = 0,
        SqlScalarFunction,
        SqlInlineTableFunction,
        SqlTableValueFunction,
        SqlStoredProcedure,
        SqlTrigger,
        _MaxValue,
    }

    class SchemaCodeObject: SchemaItemEntity<SchemaCodeObject>
    {
        public              SqlCodeObjectType                   Type                        { get; private set; }
        public              string                              TableName                   { get; private set; }
        public              bool                                opt_ANSI_NULLS              { get; private set; }
        public              bool                                opt_QUOTED_IDENTIFIER       { get; private set; }
        public              string                              Code                        { get; private set; }
        public              SchemaPermissionCollection          Permissions                 { get; private set; }

        public                                                  SchemaCodeObject(XmlReader xmlReader): base(xmlReader)
        {
            try {
                Permissions = new SchemaPermissionCollection();

                Type                  = _parseType(xmlReader.GetValueString("type"));
                TableName             = xmlReader.GetValueStringNullable("table-name");
                opt_ANSI_NULLS        = xmlReader.GetValueBool("opt-ansi_nulls");
                opt_QUOTED_IDENTIFIER = xmlReader.GetValueBool("opt-quoted_identifier");

                if (xmlReader.HasChildren()) {
                    while (xmlReader.ReadNextElement()) {
                        switch (xmlReader.Name) {
                        case "code":                Code        = xmlReader.ReadContent();              break;
                        case "permission":          Permissions.Add(new SchemaPermission(xmlReader));   break;
                        default:                    xmlReader.UnexpectedElement();  break;
                        }
                    }
                }

                _removeSourceLocationFromCode();
            }
            catch(Exception err) {
                throw new DBSchemaException("Reading of code-object '" + Name + "' failed.", err);
            }
        }

        public  override    bool                                CompareEqual(SchemaCodeObject other, DBSchemaCompare compare, CompareTable compareTable, CompareMode mode)
        {
            return this.Name                  == other.Name     &&
                   this.TableName             == other.TableName             &&
                   this.opt_ANSI_NULLS        == other.opt_ANSI_NULLS        &&
                   this.opt_QUOTED_IDENTIFIER == other.opt_QUOTED_IDENTIFIER &&
                   this.Code                  == other.Code                  &&
                   (mode != CompareMode.Update || this.Permissions.CompareEqual(other.Permissions, compare, compareTable, mode));
        }

        public              void                                WriteDrop(WriterHelper writer)
        {
            writer.Write("IF EXISTS (SELECT * from sys.[objects] WHERE [object_id] = OBJECT_ID(");
                writer.WriteString(Name);
                writer.Write(", ");
                switch(Type) {
                case SqlCodeObjectType.View:                        writer.WriteString("V");        break;
                case SqlCodeObjectType.SqlScalarFunction:           writer.WriteString("FN");       break;
                case SqlCodeObjectType.SqlInlineTableFunction:      writer.WriteString("IF");       break;
                case SqlCodeObjectType.SqlTableValueFunction:       writer.WriteString("TF");       break;
                case SqlCodeObjectType.SqlStoredProcedure:          writer.WriteString("P");        break;
                case SqlCodeObjectType.SqlTrigger:                  writer.WriteString("TR");       break;
                }
                writer.Write("))");
                writer.WriteNewLine();

            writer.Write("    ");
                switch(Type) {
                case SqlCodeObjectType.View:                        writer.Write("DROP VIEW ");         break;
                case SqlCodeObjectType.SqlScalarFunction:           writer.Write("DROP FUNCTION ");     break;
                case SqlCodeObjectType.SqlInlineTableFunction:      writer.Write("DROP FUNCTION ");     break;
                case SqlCodeObjectType.SqlTableValueFunction:       writer.Write("DROP FUNCTION ");     break;
                case SqlCodeObjectType.SqlStoredProcedure:          writer.Write("DROP PROCEDURE ");    break;
                case SqlCodeObjectType.SqlTrigger:                  writer.Write("DROP TRIGGER ");      break;
                }
                writer.Write(Name);
                writer.WriteNewLine();

            writer.WriteSqlGo();
        }
        public              void                                WriteCreate(DBSchemaCompare compare, WriterHelper writer)
        {
            string code = Code.Replace("\r\n", "\n").Replace("\n", "\r\n");

            writer.WriteSqlPrint("create: " + Name);

            if ((!opt_ANSI_NULLS) || (!opt_QUOTED_IDENTIFIER)) {
                writer.Write(opt_ANSI_NULLS ? "SET ANSI_NULLS ON" : "SET ANSI_NULLS OFF");
                writer.WriteNewLine();
                writer.Write(opt_QUOTED_IDENTIFIER ? "SET QUOTED_IDENTIFIER ON" : "SET QUOTED_IDENTIFIER OFF");
                writer.WriteNewLine();
                writer.WriteSqlGo();
            }
            writer.Write(code);

            if (!code.EndsWith("\n"))
                writer.WriteNewLine();

            writer.WriteSqlGo();

            if ((!opt_ANSI_NULLS) || (!opt_QUOTED_IDENTIFIER)) {
                writer.Write("SET ANSI_NULLS ON");
                writer.WriteNewLine();
                writer.Write("SET QUOTED_IDENTIFIER ON");
                writer.WriteNewLine();
                writer.WriteSqlGo();
            }

            WriteGrantRevoke(compare, writer, null);
        }
        public              void                                WriteGrantRevoke(DBSchemaCompare compare, WriterHelper writer, SchemaPermissionCollection curPermissions)
        {
            (new ComparePermissionCollection(compare, null, curPermissions, Permissions)).WriteGrantRevoke(writer, CompareFlags.Create, Name, 0);
        }

        public              void                                CodeGrep(Regex regex)
        {
            if (regex.IsMatch(Code)) {
                Console.WriteLine(Name + " " + Type.ToString());
            }
        }

        private             SqlCodeObjectType                   _parseType(string s)
        {
            s = s.ToUpper();
            switch(s) {
            case "V":   case "VIEW":                                return SqlCodeObjectType.View;
            case "FN":  case "SQL_SCALAR_FUNCTION":                 return SqlCodeObjectType.SqlScalarFunction;
            case "IF":  case "SQL_INLINE_TABLE_VALUED_FUNCTION":    return SqlCodeObjectType.SqlInlineTableFunction;
            case "TF":  case "SQL_TABLE_VALUED_FUNCTION":           return SqlCodeObjectType.SqlTableValueFunction;
            case "P":   case "SQL_STORED_PROCEDURE":                return SqlCodeObjectType.SqlStoredProcedure;
            case "TR":  case "SQL_TRIGGER":                         return SqlCodeObjectType.SqlTrigger;
            default:    throw new Exception("Unknown type '" + s + "'.");
            }
        }

        private             void                                _removeSourceLocationFromCode()
        {
            if (Code.StartsWith("--@")) {
                Code = Code.Substring(Code.IndexOf("\n")+1);
            }
        }
    }

    class SchemaCodeObjectCollection: SchemaItemList<SchemaCodeObject,SqlEntityName>
    {
        public              void                                CodeGrep(Regex regex)
        {
            foreach(SchemaCodeObject schemaCodeObject in this)
                schemaCodeObject.CodeGrep(regex);
        }
    }

    class CompareCodeObject: CompareItem<SchemaCodeObject,SqlEntityName>
    {
        public              void                                CodeUpdateDrop(WriterHelper writer, SqlCodeObjectType type)
        {
            if ((New == null && Cur != null) ||
                (New != null && (Cur == null || !New.isCodeEqual(Cur))))
            {
                (Cur ?? New).WriteDrop(writer);
            }
        }
        public              void                                CodeUpdateCreate(DBSchemaCompare compare, WriterHelper writer, SqlCodeObjectType type)
        {
            if (New != null && (Cur == null || !New.CompareEqual(Cur, compare, null, CompareMode.Code))) {
                New.WriteCreate(compare, writer);
            }
            else
            if (New != null && Cur != null && !New.Permissions.CompareEqual(Cur.Permissions, compare, null, CompareMode.Update)) {
                New.WriteGrantRevoke(compare, writer, Cur.Permissions);
            }
        }
    }

    class CompareCodeObjectCollection: CompareItemCollection<CompareCodeObject,SchemaCodeObject,SqlEntityName>
    {
        public                                                  CompareCodeObjectCollection(DBSchemaCompare compare, IReadOnlyList<SchemaCodeObject> curSchema, IReadOnlyList<SchemaCodeObject> newSchema): base(compare, null, curSchema, newSchema)
        {
        }

        public              void                                ReportChanges(DBSchemaCompare compare, WriterHelper writer, string typeName, bool includediff)
        {
            int     extralines = 3;

            foreach(CompareCodeObject cmp in Items) {
                if (cmp.Cur != null && cmp.New != null && !cmp.New.CompareEqual(cmp.Cur, compare, null, CompareMode.Report)) {
                    if (includediff) {
                        writer.Write("------------------------------------------------------------------------------------------------------------------------");
                        writer.WriteNewLine();
                    }

                    writer.WriteWidth(typeName, 10);
                    writer.Write(cmp.Cur.Name);
                    writer.WriteNewLine();

                    if (includediff) {
                        writer.Write("------------------------------------------------------------------------------------------------------------------------");
                        writer.WriteNewLine();

                        if (cmp.Cur.opt_ANSI_NULLS != cmp.New.opt_ANSI_NULLS) {
                            writer.Write("- set ansi_nulls " + (cmp.Cur.opt_ANSI_NULLS ? "on":"off"));
                            writer.WriteNewLine();
                            writer.Write("+ set ansi_nulls " + (cmp.New.opt_ANSI_NULLS ? "on":"off"));
                            writer.WriteNewLine();
                        }

                        if (cmp.Cur.opt_QUOTED_IDENTIFIER != cmp.New.opt_QUOTED_IDENTIFIER) {
                            writer.Write("- set quoted_identifier " + (cmp.Cur.opt_QUOTED_IDENTIFIER ? "on":"off"));
                            writer.WriteNewLine();
                            writer.Write("+ set quoted_identifier " + (cmp.New.opt_QUOTED_IDENTIFIER ? "on":"off"));
                            writer.WriteNewLine();
                        }

                        string[]    curLines = cmp.Cur.Code.Replace("\r", "").Split('\n');
                        string[]    newLines = cmp.New.Code.Replace("\r", "").Split('\n');

                        Diff.Item[] diffs    = Diff.DiffText(curLines, newLines);

                        for (int n = 0 ; n < diffs.Length ; ++n) {
                            Diff.Item   diff  = diffs[n];
                            int         npre  = diff.StartB                                                                 - (n > 0 ? diffs[n-1].EndB : 0);
                            int         npost = (n < diffs.Length - 1 ? diffs[n + 1].StartB - extralines : newLines.Length) - diff.EndB;

                            if (npre  < 0) npre  = 0;
                            if (npost < 0) npost = 0;
                            if (npre  > 3) npre  = extralines;
                            if (npost > 3) npost = extralines;

                            int     lc;
                            int     ln;

                            for (ln = diff.StartB - npre ; ln < diff.StartB ; ++ln)
                                writer.WriteDiff(ln, ' ', newLines[ln]);

                            for (lc = diff.StartA ; lc < diff.EndA ; ++lc,++ln)
                                writer.WriteDiff(ln, '-', curLines[lc]);

                            for (ln = diff.StartB ; ln < diff.EndB ; ++ln)
                                writer.WriteDiff(ln, '+', newLines[ln]);

                            for (ln = diff.EndB ; ln < diff.EndB + npost ; ++ln)
                                writer.WriteDiff(ln, ' ', newLines[ln]);

                            if (n >= diffs.Length - 1 || diffs[n].EndB < diffs[n + 1].StartB - (extralines * 2))
                                writer.WriteNewLine();
                        }
                    }
                }
            }
        }
        public              void                                ReportNew(WriterHelper writer, string typeName)
        {
            foreach(CompareCodeObject cmp in Items) {
                if (cmp.Cur == null && cmp.New != null) {
                    writer.WriteWidth(typeName, 10);
                    writer.Write(cmp.New.Name);
                    writer.WriteNewLine();
                }
            }
        }
        public              void                                ReportDeleted(WriterHelper writer, string typeName)
        {
            foreach(CompareCodeObject cmp in Items) {
                if (cmp.Cur != null && cmp.New == null) {
                    writer.WriteWidth(typeName, 10);
                    writer.Write(cmp.Cur.Name);
                    writer.WriteNewLine();
                }
            }
        }
        public              void                                CodeUpdateDrop(WriterHelper writer, SqlCodeObjectType type)
        {
            foreach(CompareCodeObject cmp in Items)
                cmp.CodeUpdateDrop(writer, type);
        }
        public              void                                CodeUpdateCreate(DBSchemaCompare compare, WriterHelper writer, SqlCodeObjectType type)
        {
            foreach(CompareCodeObject cmp in Items)
                cmp.CodeUpdateCreate(compare, writer, type);
        }
    }

    class CompareTypeCodeObjectCollection
    {
        public              CompareCodeObjectCollection[]       CompareCollections          { get; private set; }

        public              CompareCodeObjectCollection         this[SqlCodeObjectType type]
        {
            get {
                return CompareCollections[(int)type];
            }
        }

        public                                                  CompareTypeCodeObjectCollection()
        {
        }

        public              void                                Fill(DBSchemaCompare compare, SchemaCodeObjectCollection curSchema, SchemaCodeObjectCollection newSchema)
        {
            var curSplitSchema = _split(curSchema);
            var newSplitSchema = _split(newSchema);

            CompareCollections = new CompareCodeObjectCollection[(int)SqlCodeObjectType._MaxValue];

            for (int i=0 ; i < (int)SqlCodeObjectType._MaxValue ; ++i)
                CompareCollections[i] = new CompareCodeObjectCollection(compare, curSplitSchema[i], newSplitSchema[i]);
        }
        public              void                                Compare(DBSchemaCompare compare)
        {
            foreach(CompareCodeObjectCollection c in CompareCollections)
                c.Compare(compare, null);
        }
        public              void                                Report(DBSchemaCompare compare, WriterHelper writer, bool includediff)
        {
            using (WriterHelper     wr = new WriterHelper())
            {
                for (SqlCodeObjectType type = 0 ; type < SqlCodeObjectType._MaxValue ; ++type)
                    this[type].ReportNew(wr, _typeToName(type));

                writer.WriteReportSection("new code", wr);
            }

            using (WriterHelper     wr = new WriterHelper())
            {
                for (SqlCodeObjectType type = 0 ; type < SqlCodeObjectType._MaxValue ; ++type)
                    this[type].ReportDeleted(wr, _typeToName(type));

                writer.WriteReportSection("deleted code", wr);
            }

            using (WriterHelper     wr = new WriterHelper())
            {
                for (SqlCodeObjectType type = 0 ; type < SqlCodeObjectType._MaxValue ; ++type)
                    this[type].ReportChanges(compare, wr, _typeToName(type), includediff);

                writer.WriteReportSection("updated code", wr);
            }
        }
        public              void                                ReportPermissions(WriterHelper writer)
        {
            using (WriterHelper     wr = new WriterHelper())
            {
            }
        }
        public              void                                CodeUpdate(DBSchemaCompare compare, WriterHelper writer)
        {
            for (SqlCodeObjectType type = 0 ; type < SqlCodeObjectType._MaxValue ; ++type)
                this[type].CodeUpdateDrop(writer, type);

            for (SqlCodeObjectType type = 0 ; type < SqlCodeObjectType._MaxValue ; ++type)
                this[type].CodeUpdateCreate(compare, writer, type);
        }

        private             string                              _typeToName(SqlCodeObjectType type)
        {
            switch(type) {
            case SqlCodeObjectType.View:                    return "view";
            case SqlCodeObjectType.SqlScalarFunction:       return "function";
            case SqlCodeObjectType.SqlInlineTableFunction:  return "function";
            case SqlCodeObjectType.SqlTableValueFunction:   return "function";
            case SqlCodeObjectType.SqlStoredProcedure:      return "procedure";
            case SqlCodeObjectType.SqlTrigger:          return "trigger";
            default:                                        return "?????";
            }
        }
        private static      List<SchemaCodeObject>[]            _split(SchemaCodeObjectCollection schema)
        {
            var rtn = new List<SchemaCodeObject>[(int)SqlCodeObjectType._MaxValue];

            for (int i=0 ; i < (int)SqlCodeObjectType._MaxValue ; ++i)
                rtn[i] = new List<SchemaCodeObject>();

            foreach(SchemaCodeObject item in schema)
                rtn[(int)item.Type].Add(item);

            return rtn;
        }
    }
}
