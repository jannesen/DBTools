using System;
using System.IO;
using System.Data.SqlClient;
using Jannesen.Tools.DBTools.Library;

namespace Jannesen.Tools.DBTools.SqlScript
{
    static class Resource
    {
        public  static      Stream          GetScriptStream(string name)
        {
            Stream  stream = typeof(Resource).Assembly.GetManifestResourceStream("Jannesen.Tools.DBTools.SqlScripts." + name);

            if (stream == null)
                throw new Exception("Can't GetString '" + name + "'");

            return stream;
        }
        public  static      string          GetScriptString(string name)
        {
            using (StreamReader reader = new StreamReader(GetScriptStream(name)))
                return reader.ReadToEnd();
        }
        public  static      void                    ExecuteSqlScriptResource(this SqlConnection sqlConnection, string name)
        {
            using (Stream stream = GetScriptStream(name))
                sqlConnection.ExecuteScript("[EMBEDDEDRESOURCE]\\" + name, stream);
        }
    }
}
