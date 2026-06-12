using System;
using System.IO;
using Microsoft.Data.SqlClient;
using Jannesen.Tools.DBTools.Library;

namespace Jannesen.Tools.DBTools.SqlScript
{
    static class Resource
    {
        public  static      Stream          GetScriptStream(string name)
        {
            return typeof(Resource).Assembly.GetManifestResourceStream("Jannesen.Tools.DBTools.SqlScripts." + name)
                        ?? throw new InvalidOperationException("Can't GetString '" + name + "'");
        }
        public  static      string          GetScriptString(string name)
        {
            using (StreamReader reader = new StreamReader(GetScriptStream(name))) {
                return reader.ReadToEnd();
            }
        }
        public  static      void                    ExecuteSqlScriptResource(this SqlConnection sqlConnection, string name)
        {
            using (Stream stream = GetScriptStream(name)) {
                sqlConnection.ExecuteScript("[EMBEDDEDRESOURCE]\\" + name, stream);
            }
        }
    }
}
