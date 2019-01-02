using System;
using System.Collections.Generic;
using System.IO;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Text.RegularExpressions;
using System.Xml;
using Jannesen.Tools.DBTools.Library;
using Jannesen.Tools.DBTools.SqlScript;
using Jannesen.Tools.DBTools.DBSchema.Item;

namespace Jannesen.Tools.DBTools.DBSchema
{
    class DBSchemaDatabase
    {
        public              string                              Name            { get; private set; }
        public              string                              Collate         { get; private set; }
        public              SchemaRoleCollection                Roles           { get; private set; }
        public              SchemaDefaultCollection             Defaults        { get; private set; }
        public              SchemaRuleCollection                Rules           { get; private set; }
        public              SchemaTypeCollection                Types           { get; private set; }
        public              SchemaTableCollection               Tables          { get; private set; }
        public              SchemaCodeObjectCollection          CodeObjects     { get; private set; }
        public              SchemaDiagramCollection             Diagrams        { get; private set; }

        public                                                  DBSchemaDatabase()
        {
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
            if (sourceName.StartsWith("sql:")) {
                LoadFromDatabase(sourceName.Substring(4));
                return;
            }

            if (sourceName.StartsWith("file:")) {
                LoadFromFile(sourceName.Substring(5));
                return;
            }

            LoadFromFile(sourceName);
            return;
        }
        public              void                                LoadFromFile(string fileName)
        {
            using (StreamReader textReader = new StreamReader(fileName))
            {
                _parseDatabase(new XmlTextReader(textReader));
            }
        }
        public              void                                LoadFromDatabase(string databaseName)
        {
            using (SqlConnection sqlConnection = SqlConnectionExtension.NewConnection(databaseName))
            {
                sqlConnection.ExecuteSqlScriptResource("DBSchemaExportToXml-pre.sql");

                using (XmlReader xmlReader = sqlConnection.ExecuteXmlReader(Resource.GetScriptString("DBSchemaExportToXml.sql")))
                    _parseDatabase(xmlReader);

                sqlConnection.ExecuteSqlScriptResource("DBSchemaExportToXml-post.sql");
            }
        }
        public  static      void                                ExportToFile(string serverDatabaseName, string fileName)
        {
            using (SqlConnection sqlConnection = SqlConnectionExtension.NewConnection(serverDatabaseName))
            {
                sqlConnection.ExecuteSqlScriptResource("DBSchemaExportToXml-pre.sql");

                using (XmlReader xmlReader = sqlConnection.ExecuteXmlReader(Resource.GetScriptString("DBSchemaExportToXml.sql")))
                {
                    using (XmlWriter writer = XmlWriter.Create(fileName, new XmlWriterSettings() {
                                                                                CloseOutput = true,
                                                                                Encoding    = System.Text.Encoding.UTF8,
                                                                                Indent      = true,
                                                                                IndentChars = "\t"
                                                                         }))
                            writer.WriteNode(xmlReader, true);
                }

                sqlConnection.ExecuteSqlScriptResource("DBSchemaExportToXml-post.sql");
            }
        }
        public              void                                CodeGrep(string pattern)
        {
            Regex   regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Multiline);

            CodeObjects.CodeGrep(regex);
        }
        private             void                                _parseDatabase(XmlReader xmlReader)
        {
            xmlReader.ReadRootNode("database");

            Name    = xmlReader.GetValueString("name");
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
    }
}
