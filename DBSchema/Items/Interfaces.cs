﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jannesen.Tools.DBTools.DBSchema.Item
{
    interface ICompareTable
    {
        bool        EqualColumn(DBSchemaCompare compare, string thisName, string otherName);
    }
}
