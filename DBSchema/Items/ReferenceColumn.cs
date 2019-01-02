using System;
using System.Collections.Generic;
using System.Xml;
using Jannesen.Tools.DBTools.Library;

namespace Jannesen.Tools.DBTools.DBSchema.Item
{
    class SchemaReferenceColumn: SchemaItemName<SchemaReferenceColumn>
    {
        public              string                              Referenced                              { get; private set; }

        public                                                  SchemaReferenceColumn(XmlReader xmlReader): base(xmlReader)
        {
            try {
                Referenced = xmlReader.GetValueStringNullable("referenced");

                xmlReader.NoChildElements();
            }
            catch(Exception err) {
                throw new DBSchemaException("Reading of reference-column '" + Name + "' failed.", err);
            }
        }

        public  override    bool                                CompareEqual(SchemaReferenceColumn other, CompareTable compareTable, CompareMode mode)
        {
            var newColumn = compareTable.New.Columns.Find(other.Name);

            return newColumn != null &&
                   this.Name       == (newColumn.OrgName ?? newColumn.Name) &&
                   this.Referenced == other.Referenced;
        }
    }

    class SchemaReferenceColumnCollection: SchemaItemList<SchemaReferenceColumn,string>
    {
    }
}
