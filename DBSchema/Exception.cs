using System;
using System.Collections.Generic;

namespace Jannesen.Tools.DBTools.DBSchema
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable")]
    class DBSchemaException: Exception
    {
        public                  DBSchemaException(string message): base(message)
        {

        }
        public                  DBSchemaException(string message, Exception innerException): base(message, innerException)
        {
        }
    }
}
