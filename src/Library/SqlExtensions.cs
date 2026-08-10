using System;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Data.SqlClient;

namespace Jannesen.Tools.DBTools.Library;

internal static class SqlExtensions
{
    private static readonly     char[]  _nibbleToHex = [ '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' ];

    public  static      void            WriteSql(this TextWriter writer, string value)
    {
        writer.Write('\'');
        writer.Write(value.Replace("'", "''", StringComparison.Ordinal));
        writer.Write('\'');
    }
    public  static      void            WriteSqlQuoteName(this TextWriter writer, string value)
    {
        writer.Write('[');
        writer.Write(value.Replace("]", "]]", StringComparison.Ordinal));
        writer.Write(']');
    }
    public  static      void            WriteSql(this TextWriter writer, byte value)
    {
        writer.Write(value);
    }
    public  static      void            WriteSql(this TextWriter writer, Int16 value)
    {
        writer.Write(value);
    }
    public  static      void            WriteSql(this TextWriter writer, Int32 value)
    {
        writer.Write(value);
    }
    public  static      void            WriteSql(this TextWriter writer, Int64 value)
    {
        writer.Write(value);
    }
    public  static      void            WriteSql(this TextWriter writer, bool value)
    {
        writer.Write(value ? "1" : "0");
    }
    public  static      void            WriteSql(this TextWriter writer, float value)
    {
        writer.Write(value.ToString(CultureInfo.InvariantCulture));
    }
    public  static      void            WriteSql(this TextWriter writer, double value)
    {
        writer.Write(value.ToString(CultureInfo.InvariantCulture));
    }
    public  static      void            WriteSql(this TextWriter writer, Decimal value)
    {
        writer.Write(value.ToString(CultureInfo.InvariantCulture));
    }
    public  static      void            WriteSql(this TextWriter writer, byte[] value)
    {
        writer.Write("0x");

        foreach(var b in value) {
            writer.Write(_nibbleToHex[(b >> 4) & 0xf]);
            writer.Write(_nibbleToHex[(b     ) & 0xf]);
        }
    }
    public  static      void            WriteSql(this TextWriter writer, DateOnly value)
    {
        writer.Write('\'');
        writer.Write(value.ToString(@"yyyy-MM-dd", CultureInfo.InvariantCulture));
        writer.Write('\'');
    }
    public  static      void            WriteSql(this TextWriter writer, TimeSpan value)
    {
        writer.Write('\'');
        writer.Write(value.ToString(@"hh\:mm\:ss\.fffffff", CultureInfo.InvariantCulture));
        writer.Write('\'');
    }
    public  static      void            WriteSql(this TextWriter writer, DateTime value)
    {
        writer.Write('\'');
        writer.Write(value.ToString(@"yyyy-MM-ddThh\:mm\:ss\.fffffff", CultureInfo.InvariantCulture));
        writer.Write('\'');
    }
    public  static      void            WriteSql(this TextWriter writer, DateTimeOffset value)
    {
        writer.Write('\'');
        writer.Write(value.ToString(@"yyyy-MM-ddTHH:mm:ss.fffffffzzz", CultureInfo.InvariantCulture));
        writer.Write('\'');
    }
}