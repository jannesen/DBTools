using System;
using System.Collections.Generic;
using System.Xml;
using Jannesen.Tools.DBTools.Library;

namespace Jannesen.Tools.DBTools.DBSchema.Item
{
    class SchemaIndexColumn: SchemaItemName<SchemaIndexColumn>
    {
        public              string                              Order                   { get; private set; }

        public                                                  SchemaIndexColumn(XmlReader xmlReader): base(xmlReader)
        {
            try {
                Order = xmlReader.GetValueStringNullable("order");

                xmlReader.NoChildElements();
            }
            catch(Exception err) {
                throw new DBSchemaException("Reading of index-column '" + Name + "' failed.", err);
            }
        }

        public  override    bool                                CompareEqual(SchemaIndexColumn other, CompareTable compareTable, CompareMode mode)
        {
            var newColumn = compareTable.New.Columns.Find(other.Name);

            return newColumn  != null &&
                   this.Name  == (newColumn.OrgName ?? newColumn.Name) &&
                   this.Order == other.Order;
        }
    }

    class SchemaIndexColumnCollection: SchemaItemList<SchemaIndexColumn,string>
    {
    }
}
