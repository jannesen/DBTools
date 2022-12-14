using System;
using System.Collections.Generic;
using System.Xml;
using Jannesen.Tools.DBTools.Library;

namespace Jannesen.Tools.DBTools.DBSchema.Item
{
    internal sealed class SchemaIndexColumn: SchemaItemName<SchemaIndexColumn>
    {
        public              string                              Order                   { get; private set; }
        public              bool                                Included                { get; private set; }

        public                                                  SchemaIndexColumn(XmlReader xmlReader): base(xmlReader)
        {
            try {
                Order    = xmlReader.GetValueStringNullable("order");
                Included = xmlReader.GetValueBool("included", false);

                xmlReader.NoChildElements();
            }
            catch(Exception err) {
                throw new DBSchemaException("Reading of index-column '" + Name + "' failed.", err);
            }
        }

        public  override    bool                                CompareEqual(SchemaIndexColumn other, DBSchemaCompare compare, ICompareTable compareTable, CompareMode mode)
        {
            return compareTable.EqualColumn(compare, this.Name, other.Name) &&
                   this.Order    == other.Order && 
                   this.Included == other.Included;
        }
    }

    internal sealed class SchemaIndexColumnCollection: SchemaItemList<SchemaIndexColumn,string>
    {
    }
}
