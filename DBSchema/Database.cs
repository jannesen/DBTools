using System;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Text;
using System.Xml;
using Jannesen.Tools.DBTools.Library;
using Jannesen.Tools.DBTools.SqlScript;
using Jannesen.Tools.DBTools.DBSchema.Item;
using System.Collections.Generic;

namespace Jannesen.Tools.DBTools.DBSchema
{
    internal sealed class DBSchemaDatabase
    {
        public              Options                             Options         { get; private set; }
        public              string                              Collate         { get; private set; }
        public              SchemaRoleCollection                Roles           { get; private set; }
        public              SchemaDefaultCollection             Defaults        { get; private set; }
        public              SchemaRuleCollection                Rules           { get; private set; }
        public              SchemaTypeCollection                Types           { get; private set; }
        public              SchemaTableCollection               Tables          { get; private set; }
        public              SchemaCodeObjectCollection          CodeObjects     { get; private set; }
        public              SchemaDiagramCollection             Diagrams        { get; private set; }

        public                                                  DBSchemaDatabase(Options options)
        {
            Options     = options;
            Roles       = new SchemaRoleCollection();
            Defaults    = new SchemaDefaultCollection();
            Rules       = new SchemaRuleCollection();
            Types       = new SchemaTypeCollection();
            Tables      = new SchemaTableCollection();
            CodeObjects = new SchemaCodeObjectCollection();
            Diagrams    = new SchemaDiagramCollection();
        }

        public              void                                LoadFrom(string sourceName)
        {
            if (sourceName.StartsWith("sql:", StringComparison.Ordinal)) {
                LoadFromDatabase(sourceName.Substring(4));
                return;
            }

            if (sourceName.StartsWith("file:", StringComparison.Ordinal)) {
                LoadFromFile(sourceName.Substring(5));
                return;
            }

            LoadFromFile(sourceName);
            return;
        }
        public              void                                LoadFromFile(string fileName)
        {
            using (var xmlReader = new XmlTextReader(fileName) { DtdProcessing=DtdProcessing.Prohibit, XmlResolver=null }) {
                _parseDatabase(xmlReader);
            }

            if (!Options.IncludeCode) {
                RemoveCode();
            }
        }
        public              void                                LoadFromDatabase(string databaseName)
        {
            using (SqlConnection sqlConnection = SqlConnectionExtension.NewConnection(databaseName)) {
                sqlConnection.ExecuteSqlScriptResource("DBSchemaExportToXml-pre.sql");

                using (var sqlCmd = new SqlCommand(_DBSchemaExportToXml(Options), sqlConnection) { CommandType = System.Data.CommandType.Text, CommandTimeout = 60000 }) {
                    using (XmlReader xmlReader = sqlCmd.ExecuteXmlReader()) {
                        _parseDatabase(xmlReader);
                    }
                }

                sqlConnection.ExecuteSqlScriptResource("DBSchemaExportToXml-post.sql");
            }

            if (!Options.IncludeCode) {
                RemoveCode();
            }
        }
        public  static      void                                ExportToFile(Options options, string serverDatabaseName, string fileName)
        {
            using (SqlConnection sqlConnection = SqlConnectionExtension.NewConnection(serverDatabaseName)) {
                sqlConnection.ExecuteSqlScriptResource("DBSchemaExportToXml-pre.sql");

                using (var sqlCmd = new SqlCommand(_DBSchemaExportToXml(options), sqlConnection) { CommandType = System.Data.CommandType.Text, CommandTimeout = 60000 }) {
                    using (XmlReader xmlReader = sqlCmd.ExecuteXmlReader()) {
                        using (XmlWriter writer = XmlWriter.Create(fileName, new XmlWriterSettings() {
                                                                                    CloseOutput = true,
                                                                                    Encoding    = System.Text.Encoding.UTF8,
                                                                                    Indent      = true,
                                                                                    IndentChars = "\t"
                                                                             }))
                                writer.WriteNode(xmlReader, true);
                    }
                }

                sqlConnection.ExecuteSqlScriptResource("DBSchemaExportToXml-post.sql");
            }
        }
        public              void                                CodeGrep(string pattern)
        {
            Regex   regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Multiline);

            CodeObjects.CodeGrep(regex);
        }

        public              void                                RemoveCode()
        {
            var usedTypes = new HashSet<string>();

            CodeObjects.Clear();
            foreach (var table in Tables) {
                foreach (var column in table.Columns) {
                    var type = column.Type;
                    if (!usedTypes.Contains(type)) {
                        usedTypes.Add(type);
                    }
                }
            }

            var newTypes = new SchemaTypeCollection();

            foreach (var type in Types) {
                if (usedTypes.Contains(type.Name.Fullname)) {
                    newTypes.Add(type);
                }
            }

            Types = newTypes;
        }

        private             void                                _parseDatabase(XmlReader xmlReader)
        {
            xmlReader.ReadRootNode("database");

            Collate = xmlReader.GetValueString("collate");

            if (xmlReader.HasChildren()) {
                while (xmlReader.ReadNextElement()) {
                    switch(xmlReader.Name) {
                    case "role":        Roles      .Add(new SchemaRole(xmlReader));         break;
                    case "default":     Defaults   .Add(new SchemaDefault(xmlReader));      break;
                    case "rule":        Rules      .Add(new SchemaRule(xmlReader));         break;
                    case "type":        Types      .Add(new SchemaType(xmlReader));         break;
                    case "table":       Tables     .Add(new SchemaTable(xmlReader));        break;
                    case "code-object": CodeObjects.Add(new SchemaCodeObject(xmlReader));   break;
                    case "diagram":     Diagrams   .Add(new SchemaDiagram(xmlReader));      break;
                    default:            xmlReader.UnexpectedElement();                      break;
                    }
                }
            }
        }

        private static      string                              _DBSchemaExportToXml(Options options)
        {
            var cmd = new StringBuilder();

            cmd.AppendLine("select [collate]    = convert(nvarchar(128), databasepropertyex(db_name(), 'Collation'))");

            _DBSchemaExportToXmlAddString(cmd, "role");
            _DBSchemaExportToXmlAddString(cmd, "default");
            _DBSchemaExportToXmlAddString(cmd, "rule");
            _DBSchemaExportToXmlAddString(cmd, (options.IncludeCode ? "type-with-code" : "type"));
            _DBSchemaExportToXmlAddString(cmd, "table");
            _DBSchemaExportToXmlAddString(cmd, "diagram");

            if (options.IncludeCode) {
                _DBSchemaExportToXmlAddString(cmd, "code-object");
            }

            cmd.AppendLine("   for xml raw('database'), type");

            return cmd.ToString();
        }
        private static      void                                _DBSchemaExportToXmlAddString(StringBuilder cmd, string name)
        {
            cmd.AppendLine(",(");
            cmd.AppendLine(Resource.GetScriptString("DBSchemaExportToXml-" + name + ".sql"));
            cmd.AppendLine(")");
        }
    }
}
