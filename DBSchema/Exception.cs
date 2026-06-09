using System;

namespace Jannesen.Tools.DBTools.DBSchema;

internal sealed class DBSchemaException: Exception
{
    public                  DBSchemaException(string message): base(message)
    {

    }
    public                  DBSchemaException(string message, Exception innerException): base(message, innerException)
    {
    }
}

