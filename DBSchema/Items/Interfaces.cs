using System;

namespace Jannesen.Tools.DBTools.DBSchema.Item
{
    interface ICompareTable
    {
        bool        EqualColumn(DBSchemaCompare compare, string thisName, string otherName);
    }
}
