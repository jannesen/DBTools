using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using Jannesen.Tools.DBTools.DBSchema;
using Jannesen.Tools.DBTools.Library;

namespace Jannesen.Tools.DBTools.DBSchema.Item
{
    class SchemaDiagram: SchemaItemEntity<SchemaDiagram>
    {
        public          int?                                    Version                     { get; private set; }
        public          byte[]                                  Definition                  { get; private set; }

        public                                                  SchemaDiagram(XmlReader xmlReader): base(xmlReader)
        {
            Version    = xmlReader.GetValueIntNullable("version");
            Definition = Convert.FromBase64String(xmlReader.ReadContent());
        }

        public  override    bool                                CompareEqual(SchemaDiagram other, DBSchemaCompare compare, CompareTable compareTable, CompareMode mode)
        {
            return this.Name    == other.Name     &&
                   this.Version == other.Version &&
                   _cmpbytearray(this.Definition, other.Definition);
        }

        private static      bool                                _cmpbytearray(byte[] array1, byte[] array2)
        {
            if (array1.Length != array2.Length)
                return false;

            for (int i = 0 ; i < array1.Length ; ++i) {
                if (array1[i] != array2[i])
                    return false;
            }

            return true;
        }
    }

    class SchemaDiagramCollection: SchemaItemList<SchemaDiagram, SqlEntityName>
    {
    }

    class CompareDiagram: CompareItem<SchemaDiagram, SqlEntityName>
    {
        public  override    void                                Process(DBSchemaCompare dbCompare, WriterHelper writer)
        {
            if ((Flags & CompareFlags.Drop) != 0) {
                writer.Write("DELETE FROM dbo.[sysdiagrams] WHERE [principal_id]=DATABASE_PRINCIPAL_ID(");
                    writer.WriteString(Cur.Name.Schema);
                    writer.Write(") AND [name]=");
                    writer.WriteString(Cur.Name.Name);
                    writer.WriteNewLine();
            }

            if ((Flags & CompareFlags.Create) != 0) {
                writer.Write("INSERT INTO dbo.[sysdiagrams]([principal_id], [name], [version], [definition])");
                    writer.WriteNewLine();
                writer.Write("SELECT ");
                    writer.Write("DATABASE_PRINCIPAL_ID(");
                    writer.WriteString(New.Name.Schema);
                    writer.Write("), ");
                    writer.WriteString(New.Name.Name);
                    writer.Write(", ");
                    writer.Write(New.Version.HasValue ? New.Version.Value.ToString(CultureInfo.InvariantCulture) : "NULL");
                    writer.Write(", ");
                    writer.WriteData(New.Definition);
                    writer.WriteNewLine();
                writer.WriteSqlGo();
            }
        }
    }

    class CompareDiagramCollection: CompareItemCollection<CompareDiagram,SchemaDiagram,SqlEntityName>
    {
        public                                                  CompareDiagramCollection(IReadOnlyList<SchemaDiagram> curSchema, IReadOnlyList<SchemaDiagram> newSchema): base(curSchema, newSchema)
        {
        }
    }
}
