using System;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Data.SqlClient;

namespace Jannesen.Tools.DBTools.Library;

internal static class SqlConnectionExtension
{
    public  static      SqlConnection           NewConnection(string datasource)
    {
        string  username           = null;
        string  passwd             = null;
        string  serverInstanceName;
        string  databaseName;
        var     s = datasource;
        int     i;

        if ((i = s.IndexOf('@', StringComparison.Ordinal)) > 0) {
            var userPasswd = s.Substring(0, i);
            s = s.Substring(i + 1);

            if ((i = userPasswd.IndexOf(':', StringComparison.Ordinal)) <= 0)
                throw new FormatException("Invalid username:passwd in datasource '" + datasource + "'");

            username = userPasswd.Substring(0, i);
            passwd   = userPasswd.Substring(i + 1);
        }

        if ((i = s.LastIndexOf('\\')) <= 0)
            throw new FormatException("Invalid datasource '" + datasource + "'");

        serverInstanceName = s.Substring(0, i);
        databaseName       = s.Substring(i + 1);

        var connectString = "Server="    + serverInstanceName            +
                            ";Database=" + databaseName                  +
                            ";Application Name=Jannesen.Tools.DBTools"   +
                            ";Current Language=us_english"               +
                            ";Connect Timeout=5"                         +
                            ";Connect Retry Count=0"                     +
                            ";Pooling=false"                             +
                            ";Integrated Security=true"                  +
                            ";Trust Server Certificate=true";

        if (username != null) {
            connectString += ";User ID="  + username +
                                ";Password=" + passwd;
        }
        else
            connectString += ";Integrated Security=true";

        var sqlConnection = new SqlConnection(connectString);

        try {
            sqlConnection.Open();
            return sqlConnection;
        }
        catch(Exception) {
            sqlConnection.Dispose();
            throw;
        }
    }
    public  static      void                    ExecuteText(this SqlConnection sqlConnection, string cmdText, int timeout = 60000)
    {
        using (var sqlCmd = new SqlCommand(cmdText, sqlConnection) { CommandType = System.Data.CommandType.Text, CommandTimeout = timeout}) {
            sqlCmd.ExecuteNonQuery();
        }
    }
    public  static      void                    ExecuteScript(this SqlConnection sqlConnection, string fileName, Stream steam)
    {
        using (var textReader = new StreamReader(steam, Encoding.UTF8, true, 4096, true))
        {
            var lineNumber = 0;
            var lineOffset = 0;
            var cmdText    = new StringBuilder(64000);

            string          line;

            do {
                line = textReader.ReadLine();
                ++lineNumber;

                if (line == null ||
                    (line.Length >= 2 && (line[0] == 'G' || line[0] == 'g') && string.Equals(line.Trim(), "GO", StringComparison.OrdinalIgnoreCase)))
                {
                    if (cmdText.Length > 0) {
                        while (cmdText.Length > 0 && cmdText[^1] == '\n')
                            --cmdText.Length;

                        if (cmdText.Length > 0) {
                            try {
                                sqlConnection.ExecuteText(cmdText.ToString());
                            }
                            catch(SqlException sqlException) {
                                throw new SqlScriptException(fileName, lineOffset, sqlException);
                            }
                        }
                    }

                    cmdText.Length = 0;
                }
                else {
                    if (cmdText.Length == 0)
                        lineOffset = lineNumber;

                    cmdText.Append(line);
                    cmdText.Append('\n');
                }
            }
            while (line != null);
        }
    }
}

internal sealed class SqlScriptException: Exception
{
    public          string                  FileName                { get; private set; }
    public          int                     LineNo                  { get; private set; }
    public          int                     LineOffset              { get; private set; }
    public          SqlErrorCollection      Errors                  { get; private set; }

    public                                  SqlScriptException(string fileName, int lineOffset, SqlException sqlException)
                                                :base(fileName + "(" + (lineOffset + sqlException.LineNumber - 1).ToString(CultureInfo.InvariantCulture) + "): " +  sqlException.Message)
    {
        FileName   = fileName;
        LineNo     = lineOffset + sqlException.LineNumber - 1;
        LineOffset = lineOffset;
        Errors     = sqlException.Errors;
    }
}
