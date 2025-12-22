using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Jannesen.Tools.DBTools.Library;
using Jannesen.Tools.DBTools.DBSchema.Item;

namespace Jannesen.Tools.DBTools.DBSchema
{
    internal sealed class SqlFileWriter: IDisposable
    {
        private readonly        string                  _filename;
        private                 StreamWriter            _writer;

        public                                          SqlFileWriter(string fileName)
        {
            _filename = fileName.Replace("/", "\\");

            if (!fileName.EndsWith("\\", StringComparison.Ordinal)) {
                _writer = _createFile(fileName);
            }
            else {
                var dirname = fileName.Substring(0, fileName.Length - 1);

                if (!Directory.Exists(dirname)) {
                    Directory.CreateDirectory(dirname);
                }
            }
        }

        public                  void                    Dispose()
        {
            if (_writer != null) {
                _writer.Dispose();
                _writer = null;
            }
        }

        public                  void                    WriteSection(string name, WriterHelper w)
        {
            WriteSection(name, null, w);
        }
        public                  void                    WriteSection(string name, string description, WriterHelper w)
        {
            if (w.hasData) {
                if (_writer != null) {
                    _writeSection(_writer, name, description, w);
                }
                else {
                    using (var writer = _createFile(_filename + name)) {
                        _writeSection(writer, name, description, w);
                    }
                }
            }
        }

        private static          StreamWriter            _createFile(string filename)
        {
            var wr = new StreamWriter(filename, false, System.Text.Encoding.UTF8); 
            try {
                wr.Write("SET NOCOUNT            ON"  + WriterHelper.NewLine);
                wr.Write("SET ANSI_NULLS         ON"  + WriterHelper.NewLine);
                wr.Write("SET ANSI_PADDING       ON"  + WriterHelper.NewLine);
                wr.Write("SET ANSI_WARNINGS      ON"  + WriterHelper.NewLine);
                wr.Write("SET QUOTED_IDENTIFIER  ON"  + WriterHelper.NewLine);
                wr.Write("SET NUMERIC_ROUNDABORT OFF" + WriterHelper.NewLine);
                wr.Write("GO"                         + WriterHelper.NewLine);
                return wr;
            }
            catch {
                wr.Dispose();
                throw;
            }

        }
        private static          void                    _writeSection(StreamWriter writer, string name, string description, WriterHelper w)
        {
            if (w.hasData) {
                writer.Write(WriterHelper.NewLine);
                if (description != null) {
                    writer.Write("--==============================================================================");
                    writer.Write(WriterHelper.NewLine);
                    writer.Write("PRINT '# ");
                    writer.Write(description.Replace("'", "''"));
                    writer.Write("'");
                    writer.Write(WriterHelper.NewLine);
                    writer.Write("GO");
                    writer.Write(WriterHelper.NewLine);
                }
                writer.Write(w.ToString());
            }
        }
    }
}
