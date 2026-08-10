using System;
using System.IO;
using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;
using Jannesen.Tools.DBTools.Library;
using System.Collections.Generic;
using System.Globalization;

namespace Jannesen.Tools.DBTools;

internal sealed class DataExport
{
    internal enum ValueType: byte
    {
        Image               =  34,
        Text                =  35,
        UniqueIdentifier    =  36,
        Date                =  40,
        Time                =  41,
        DateTime2           =  42,
        DateTimeOffset      =  43,
        TinyInt             =  48,
        SmallInt            =  52,
        Int                 =  56,
        SmallDateTime       =  58,
        Real                =  59,
        Money               =  60,
        DateTime            =  61,
        Float               =  62,
        SqlVariant          =  98,
        Ntext               =  99,
        Bit                 = 104,
        Decimal             = 106,
        Numeric             = 108,
        Smallmoney          = 122,
        Bigint              = 127,
        VarBinary	        = 165,
        VarChar	            = 167,
        Binary	            = 173,
        Char	            = 175,
        Timestamp	        = 189,
        NVarChar	        = 231,
        NChar	            = 239,
        Xml	                = 241
    }

    internal sealed record Column
    {
        public required string      Name         { get; init; }
        public required ValueType   ValueType    { get; init; }
        public required bool        Identity     { get; init; }
    }

    internal static         void                Run(string datasource, string outputFileName, string[] tableNames)
    {
        using (var sqlConnection = SqlConnectionExtension.NewConnection(datasource)) {
            using (var outputStream = new StreamWriter(outputFileName, false, Encoding.UTF8)) {
                _writeHeader(outputStream);

                for(var i = 0 ; i < tableNames.Length ; ++i) {
                    var tableName = tableNames[i];
                    var whereClause = (string)null;
                    var p = tableName.IndexOf(" WHERE ", StringComparison.OrdinalIgnoreCase);
                    if (p >= 0) {
                        whereClause = tableName.Substring(p + 7);
                        tableName = tableName.Substring(0, p);
                    }

                    var columns = _getColumns(sqlConnection, tableName);
                    var orderby = _getOrder(sqlConnection, tableName);

                    _export(sqlConnection, outputStream, tableName, columns, whereClause, orderby);
                }
            }
        }
    }

    private  static         List<Column>        _getColumns(SqlConnection sqlConnection, string tableName)
    {
        using (var sqlCmd = new SqlCommand() {
                                Connection  = sqlConnection,
                                CommandType = CommandType.Text,
                                CommandText = "select [name], [system_type_id], [is_identity]" +
                                               " from sys.columns c" +
                                              " where [object_id] = object_id(" + Library.Library.QuoteString(tableName) + ")" +
                                                " and [is_computed] = 0" +
                                                " and [is_filestream] = 0" +
                                           " order by [column_id]"
                            }) {
            using (var reader = sqlCmd.ExecuteReader()) {
                var o_name           = reader.GetOrdinal("name");
                var o_system_type_id = reader.GetOrdinal("system_type_id");
                var o_is_identity    = reader.GetOrdinal("is_identity");
                var rtn = new List<Column>();

                while (reader.Read()) {
                    rtn.Add(new Column() {
                                Name      = reader.GetString(o_name),
                                ValueType = (ValueType)reader.GetByte(o_system_type_id),
                                Identity  = reader.GetBoolean(o_is_identity)
                            });
                }

                if (rtn.Count == 0) {
                    throw new Exception($"Invalid tablename '{tableName}'.");
                }

                return rtn;
            }
        }
    }
    private  static         List<string>        _getOrder(SqlConnection sqlConnection, string tableName)
    {
        using (var sqlCmd = new SqlCommand() {
                                Connection  = sqlConnection,
                                CommandType = CommandType.Text,
                                CommandText = "select c.[name]" +
                                               " from sys.indexes i" +
                                                    " inner join sys.index_columns ic on ic.[object_id] = i.[object_id]" +
                                                                                   " and ic.[index_id] = i.[index_id]" +
                                                    " inner join sys.columns c on c.[object_id] = ic.[object_id]" +
                                                                            " and c.[column_id] = ic.[column_id]" +
                                              " where i.[object_id] = object_id(" + Library.Library.QuoteString(tableName) + ")" +
                                                " and i.[is_primary_key] = 1" +
                                           " order by c.[column_id]"
                            }) {
            using (var reader = sqlCmd.ExecuteReader()) {
                var rtn = new List<string>();

                while (reader.Read()) {
                    rtn.Add(reader.GetString(0));
                }

                return rtn;
            }
        }
    }
    private  static         void                _export(SqlConnection sqlConnection, StreamWriter outputStream, string tableName, List<Column> columns, string whereClause, List<string> orderby)
    {
        var n = 0;
        using (var sqlCmd = new SqlCommand() {
                                Connection  = sqlConnection,
                                CommandType = CommandType.Text,
                                CommandText = _createSelect(tableName, columns, whereClause, orderby),
                            }) {
            try {
                using (var reader = sqlCmd.ExecuteReader()) {
                    while (reader.Read()) {
                        if (n == 0) {
                            _writeInsertInto(outputStream, tableName, columns);
                            outputStream.Write("\nVALUES");
                        }
                        else {
                            outputStream.Write(",\n      ");
                        }

                        outputStream.Write("(");
                        _writeColumnData(outputStream, reader, columns);
                        outputStream.Write(")");
                        ++n;
                    }
                }
            }
            catch(SqlException ex) {
                throw new Exception($"sql query '{sqlCmd.CommandText}' failed.", ex);
            }
            outputStream.Write("\nGO\n\n");
        }
    }
    private  static         string              _createSelect(string tableName, List<Column> columns, string whereClause, List<string> orderby)
    {
        var rtn = new StringBuilder();

        rtn.Append("SELECT ");

        for(var i = 0 ; i < columns.Count ; ++i) {
            if (i > 0) rtn.Append(',');
            rtn.Append(Library.Library.QuoteName(columns[i].Name));
        }

        rtn.Append(" FROM ");
        rtn.Append(tableName);

        if (whereClause != null) {
            rtn.Append(" WHERE ");
            rtn.Append(whereClause);
        }

        if (orderby.Count > 0) {
            rtn.Append(" ORDER BY ");

            for(var i = 0 ; i < orderby.Count ; ++i) {
                if (i > 0) rtn.Append(',');
                rtn.Append(Library.Library.QuoteName(orderby[i]));
            }
        }

        return rtn.ToString();
    }
    private  static         void                _writeHeader(StreamWriter outputStream)
    {
        outputStream.Write("SET NOCOUNT            ON\n");
        outputStream.Write("SET ANSI_NULLS         ON\n");
        outputStream.Write("SET ANSI_PADDING       ON\n");
        outputStream.Write("SET ANSI_WARNINGS      ON\n");
        outputStream.Write("SET QUOTED_IDENTIFIER  ON\n");
        outputStream.Write("SET NUMERIC_ROUNDABORT OFF\n");
        outputStream.Write("GO\n");
    }
    private  static         void                _writeInsertInto(StreamWriter outputStream, string tableName, List<Column> columns)
    {
        outputStream.Write("--==================================================================================================\n");
        outputStream.Write($"PRINT 'insert data into: {tableName}'\n");
        outputStream.Write("GO\n");

        outputStream.Write("INSERT INTO ");
        outputStream.Write(tableName);
        outputStream.Write("(");

        for(var i = 0 ; i < columns.Count ; ++i) {
            if (i > 0) outputStream.Write(',');
            outputStream.Write(Library.Library.QuoteName(columns[i].Name));
        }

        outputStream.Write(")");
    }
    private  static         void                _writeColumnData(StreamWriter outputStream, SqlDataReader reader, List<Column> columns)
    {
        for (var i = 0 ; i < columns.Count ; ++i) {
            var col = columns[i];

            if (i > 0) {
                outputStream.Write(',');
            }

            try {
                if (!reader.IsDBNull(i)) {
                    switch(col.ValueType) {
                    case ValueType.TinyInt:             outputStream.WriteSql(reader.GetByte(i));                   break;
                    case ValueType.SmallInt:            outputStream.WriteSql(reader.GetInt16(i));                  break;
                    case ValueType.Int:                 outputStream.WriteSql(reader.GetInt32(i));                  break;
                    case ValueType.Bigint:              outputStream.WriteSql(reader.GetInt64(i));                  break;
                    case ValueType.Bit:                 outputStream.WriteSql(reader.GetBoolean(i));                break;

                    case ValueType.VarChar:
                    case ValueType.Char:
                    case ValueType.NVarChar:
                    case ValueType.NChar:
                    case ValueType.Text:
                    case ValueType.Ntext:               outputStream.WriteSql(reader.GetString(i));                 break;

                    case ValueType.Real:                outputStream.WriteSql(reader.GetFloat(i));                  break;
                    case ValueType.Float:               outputStream.WriteSql(reader.GetDouble(i));                 break;

                    case ValueType.Decimal:
                    case ValueType.Numeric:             outputStream.WriteSql(reader.GetDecimal(i));                break;
                    case ValueType.Smallmoney:
                    case ValueType.Money:               outputStream.WriteSql(reader.GetSqlMoney(i).Value);         break;

                    case ValueType.VarBinary:
                    case ValueType.Binary:
                    case ValueType.Image:               outputStream.WriteSql(reader.GetSqlBytes(i).Value);         break;


                    case ValueType.Date:                outputStream.WriteSql(reader.GetFieldValue<DateOnly>(i));   break;
                    case ValueType.Time:                outputStream.WriteSql(reader.GetTimeSpan(i));               break;

                    case ValueType.DateTime:
                    case ValueType.SmallDateTime:
                    case ValueType.DateTime2:           outputStream.WriteSql(reader.GetDateTime(i));               break;
                    case ValueType.DateTimeOffset:      outputStream.WriteSql(reader.GetDateTimeOffset(i));         break;

                    //case ValueType.UniqueIdentifier:
                    //case ValueType.SqlVariant:

                    //case ValueType.Timestamp:
                    //case ValueType.Xml:

                    default:                    throw new InvalidOperationException($"Unsupport type {col.ValueType}");
                    }
                }
                else {
                    outputStream.Write("NULL");
                }
            }
            catch(Exception ex) {
                throw new Exception($"Convert of '{col.Name}' type '{col.ValueType}' failed.", ex);
            }
        }
    }
}
