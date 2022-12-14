using System;
using System.Collections.Generic;
using System.Xml;
using Jannesen.Tools.DBTools.Library;

namespace Jannesen.Tools.DBTools.DBSchema.Item
{
    internal sealed class SchemaReferenceColumn: SchemaItemName<SchemaReferenceColumn>
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

        public  override    bool                                CompareEqual(SchemaReferenceColumn other, DBSchemaCompare compare, ICompareTable compareTable, CompareMode mode)
        {
            throw new InvalidOperationException("SchemaReferenceColumn.CompareEqual not possible.");
        }
        public              bool                                CompareEqual(SchemaReferenceColumn other, DBSchemaCompare compare, ICompareTable compareTable, ICompareTable referenceTable)
        {
            return compareTable.EqualColumn(compare,this.Name,       other.Name) &&
                   referenceTable.EqualColumn(compare, this.Referenced, other.Referenced);
        }
    }

    internal sealed class SchemaReferenceColumnCollection: SchemaItemList<SchemaReferenceColumn,string>
    {
        public              bool                                CompareEqual(SchemaReferenceColumnCollection other, DBSchemaCompare compare, ICompareTable compareTable, ICompareTable referenceTable)
        {
            if (this.Count != other.Count)
                return false;

            for (int i = 0 ; i < this.Count ; ++i) {
                if (!this[i].CompareEqual(other[i], compare, compareTable, referenceTable))
                    return false;
            }

            return true;
        }

    }
}
