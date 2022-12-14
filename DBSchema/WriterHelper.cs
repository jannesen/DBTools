using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Jannesen.Tools.DBTools.Library;
using Jannesen.Tools.DBTools.DBSchema.Item;

namespace Jannesen.Tools.DBTools.DBSchema
{
    internal sealed class WriterHelper: IDisposable
    {
        private static readonly char[]                  _hexTable = new char[] {'0','1','2','3','4','5','6','7','8','9','A','B','C','D','E','F'};

        private readonly        TextWriter              _script;
        private                 bool                    _hasData;

        public                  bool                    hasData
        {
            get {
                return _hasData;
            }
        }

        public                                          WriterHelper()
        {
            _script = new StringWriter();
        }
        public                                          WriterHelper(string fileName)
        {
            _script = new StreamWriter(fileName, false, System.Text.Encoding.UTF8);
        }

        public                  void                    Dispose()
        {
            _script.Dispose();
        }

        public                  void                    WriteReportSection(string description, WriterHelper w)
        {
            if (w.hasData) {
                if (hasData)
                    WriteNewLine();

                Write("########################################################################################################################");
                WriteNewLine();

                Write("# ");
                WriteWidth(description,117);
                Write("#");
                WriteNewLine();

                Write("########################################################################################################################");
                WriteNewLine();

                Write(w.ToString());
            }
        }
        public                  void                    WriteSqlSection(string description, WriterHelper w)
        {
            if (w.hasData) {
                WriteSqlPrint(description);
                Write(w.ToString());
            }
        }
        public                  void                    WriteSection(WriterHelper w)
        {
            if (w.hasData) {
                if (hasData)
                    WriteNewLine();

                Write(w.ToString());
            }
        }
        public                  void                    Write(char c)
        {
            _script.Write(c);
            _hasData = true;
        }
        public                  void                    Write(string s)
        {
            _script.Write(s);
            _hasData = true;
        }
        public                  void                    Write(SqlEntityName n)
        {
            Write(n.Fullname);
        }
        public                  void                    WriteLine(string s)
        {
            Write(s);
            Write("\r\n");
        }
        public                  void                    WriteWidth(string s, int w)
        {
            Write(s);
            w -= s.Length;

            do {
                Write(' ');
            }
            while (--w > 0);
        }
        public                  void                    WriteWidth(SqlEntityName n, int w)
        {
            WriteWidth(n.Fullname, w);
        }
        public                  void                    WriteNewLine()
        {
            Write("\r\n");
        }
        public                  void                    WriteQuoteName(object n)
        {
            if (n is SqlEntityName entityName) {
                Write(entityName);
            }
            else {
                WriteQuoteName(n.ToString());
            }
        }
        public                  void                    WriteQuoteName(string s)
        {
            Write('[');
            Write(s.Replace("]", "]]"));
            Write(']');
        }
        public                  void                    WriteQuoteName(SqlEntityName n)
        {
            Write(n.Fullname);
        }
        public                  void                    WriteString(string s)
        {
            Write('\'');
            Write(s.Replace("'", "''"));
            Write('\'');
        }
        public                  void                    WriteString(SqlEntityName n)
        {
            WriteString(n.Fullname);
        }
        public                  void                    WriteData(byte[] d)
        {
            Write("0x");

            for(int i = 0 ; i < d.Length ; ++i) {
                byte b = d[i];

                _script.Write(_hexTable[(b >> 4) & 0xf]);
                _script.Write(_hexTable[(b     ) & 0xf]);
            }
        }
        public                  void                    WriteSqlPrint(string s)
        {
            if (hasData)
                WriteNewLine();

            Write("--==============================================================================");
            WriteNewLine();
            Write("PRINT '# ");
            Write(s.Replace("'", "''"));
            Write("'");
            WriteNewLine();
            Write("GO");
            WriteNewLine();
        }
        public                  void                    WriteSqlGo()
        {
            if (hasData) {
                Write("GO");
                WriteNewLine();
            }
        }
        public                  void                    WriteSqlRename(string objName, string newName, string objType)
        {
            Write("EXEC sp_rename @objname=");          WriteString(objName);
                        Write(", @newname=");           WriteString(newName);
                        Write(", @objtype=");           WriteString(objType);
            WriteNewLine();
        }
        public                  void                    WriteDiff(int lineno, char c, string line)
        {
            string  ln = lineno.ToString(CultureInfo.InvariantCulture);

            for (int n = ln.Length ; n < 5 ; ++n)
                Write(' ');

            Write(ln);
            Write(' ');
            Write(c);
            Write(' ');
            Write(line);
            WriteNewLine();
        }
        public                  void                    WriteRefactorOrgName(string value, string level1type, SqlEntityName name)
        {
            _writeRefactorOrgName(value, "SCHEMA", name.Schema, level1type, name.Name);
        }
        public                  void                    WriteRefactorOrgName(string value, SqlEntityName name, string level2type, string level2name)
        {
            _writeRefactorOrgName(value, "SCHEMA", name.Schema, "TABLE", name.Name, level2type, level2name);
        }
        public  override        string                  ToString()
        {
            return _script.ToString();
        }

        private                 void                    _writeRefactorOrgName(string value, string level0type, string level0name, string level1type, string level1name, string level2type = null, string level2name = null)
        {
            Write("EXEC sp_addextendedproperty 'refactor:orgname',");
            WriteString(value);
            Write(", ");    WriteString(level0type);    Write(", ");    WriteString(level0name);
            Write(", ");    WriteString(level1type);    Write(", ");    WriteString(level1name);

            if (level2name != null) {
                Write(", ");    WriteString(level2type);    Write(", ");    WriteString(level2name);
            }

            WriteNewLine();
            WriteSqlGo();
        }

        public  static          string                  QuoteName(string s)
        {
            return "[" + s.Replace("]", "]]") + "]";
        }
        public  static          string                  QuoteName(object n)
        {
            if (n is SqlEntityName entityName)
                return entityName.Fullname;

            return QuoteName(n.ToString());
        }
    }
}
