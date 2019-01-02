using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Jannesen.Tools.DBTools.DBSchema;
using Jannesen.Tools.DBTools.Library;

namespace Jannesen.Tools.DBTools.DBSchema.Item
{
    enum CompareMode
    {
        Update      = 1,
        UpdateWithRefactor,
        Report,
        Code,
        TableCompare
    }
    [Flags]
    enum CompareFlags
    {
        None        = 0,
        Drop        = 0x0001,
        Create      = 0x0002,
        Rebuild     = 0x0003,
        Refactor    = 0x0004,
        Update      = 0x0008
    }

    abstract class CompareItem<TItem,TName>  where TItem:SchemaItem<TItem,TName>
                                             where TName:class
    {
        public              CompareTable                        Table                   { get; set; }
        public              TItem                               Cur                     { get; set; }
        public              TItem                               New                     { get; set; }
        public              CompareFlags                        Flags                   { get; private set; }

        public              void                                SetRebuild()
        {
            if ((Flags & CompareFlags.Drop) == 0 && (Flags & CompareFlags.Create) == 0 && Cur != null && New != null) {
                Flags = (Flags & ~(CompareFlags.Refactor | CompareFlags.Update)) | (CompareFlags.Drop|CompareFlags.Create);
            }
        }
        public              void                                Compare(DBSchemaCompare compare, CompareTable compareTable)
        {
            Flags = (New != null) ? ((Cur != null) ? CompareNewCur(compare, compareTable) : CompareFlags.Create)
                                  : ((Cur != null) ? CompareFlags.Drop                    : CompareFlags.None);

            if (CompareDepended(compare, Flags) && (Flags & (CompareFlags.Update | CompareFlags.Create)) == 0)
                Flags |= CompareFlags.Update;
        }
        public  virtual     CompareFlags                        CompareNewCur(DBSchemaCompare compare, CompareTable compareTable)
        {
            return New.CompareEqual(Cur, compareTable, CompareMode.Update) ? CompareFlags.None : CompareFlags.Rebuild;
        }
        public  virtual     bool                                CompareDepended(DBSchemaCompare dbCompare, CompareFlags status)
        {
            return false;
        }
        public  virtual     void                                ReportUpdate(DBSchemaCompare compare, WriterHelper writer)
        {
            writer.WriteQuoteName(Cur.Name);
            writer.Write(": changed");
            writer.WriteNewLine();

            if (Cur.Name != New.Name) {
                writer.WriteQuoteName(Cur.Name);
                writer.Write(": => ");
                writer.WriteQuoteName(New.Name);
                writer.WriteNewLine();
            }
        }
        public  virtual     void                                Init(WriterHelper writer)
        {
            throw new NotImplementedException(this.GetType().Name + " init not implemented.");
        }
        public  virtual     void                                Refactor(WriterHelper writer)
        {
            throw new NotImplementedException(this.GetType().Name + " refactor not implemented.");
        }
        public  virtual     void                                Process(DBSchemaCompare dbCompare, WriterHelper writer)
        {
            throw new NotImplementedException(this.GetType().Name + " process not implemented.");
        }
        public  virtual     void                                Cleanup(WriterHelper writer)
        {
            throw new NotImplementedException(this.GetType().Name + " cleanup not implemented.");
        }
    }

    abstract class CompareItemCollection<TCompare, TItem, TName>
                                                            where TCompare:CompareItem<TItem, TName>, new()
                                                            where TItem:SchemaItem<TItem, TName>
                                                            where TName:class,IComparable
    {
        private             List<TCompare>                      _items;
        private             Dictionary<TName, TCompare>         _curDictionary;
        private             Dictionary<TName, TCompare>         _newDictionary;

        public              IReadOnlyList<TCompare>             Items
        {
            get {
                return _items;
            }
        }

        public                                                  CompareItemCollection(CompareTable table, IReadOnlyList<TItem> curSchema, IReadOnlyList<TItem> newSchema)
        {
            _items          = new List<TCompare>();
            _curDictionary = new Dictionary<TName, TCompare>();
            _newDictionary = new Dictionary<TName, TCompare>();

            if (curSchema != null) {
                foreach (var item in curSchema) {
                    var n = new TCompare() { Table = table, Cur = item };

                    _items.Add(n);
                    _curDictionary.Add(item.Name, n);
                }
            }

            if (newSchema != null) {
                foreach (var item in newSchema) {
                    if (!_curDictionary.TryGetValue(item.OrgName ?? item.Name, out var cur)) {
                        cur = new TCompare() { Table = table, New = item };
                        _items.Add(cur);
                    }
                    else
                        cur.New = item;

                    _newDictionary.Add(item.Name, cur);
                }
            }

            _items.Sort((i1,i2) => (i1.New ?? i1.Cur).Name.CompareTo((i2.New ?? i2.Cur).Name));
        }

        public              TCompare                            FindByCurName(TName name)
        {
            return _curDictionary[name];
        }
        public              bool                                FindByCurName(TName name, out TCompare found)
        {
            return _curDictionary.TryGetValue(name, out found);
        }
        public              TCompare                            FindByNewName(TName name)
        {
            return _newDictionary[name];
        }

        public              void                                Report(DBSchemaCompare compare, CompareTable compareTable, WriterHelper writer, string sectionname)
        {
            using (WriterHelper     wr = new WriterHelper())
            {
                foreach(TCompare cmp in Items) {
                    if (cmp is CompareTable)
                        compareTable = (CompareTable)(object)cmp;

                    if (cmp.Cur != null && cmp.New != null && !cmp.New.CompareEqual(cmp.Cur, compareTable, CompareMode.Report)) {
                        cmp.ReportUpdate(compare, wr);
                    }
                }

                writer.WriteReportSection("changed " + sectionname, wr);
            }

            using (WriterHelper     wr = new WriterHelper())
            {
                foreach(TCompare cmp in Items) {
                    if (cmp.Cur == null && cmp.New != null) {
                        wr.Write(WriterHelper.QuoteName(cmp.New.Name));
                        wr.WriteNewLine();
                    }
                }

                writer.WriteReportSection("new " + sectionname, wr);
            }

            using (WriterHelper     wr = new WriterHelper())
            {
                foreach(TCompare cmp in Items) {
                    if (cmp.Cur != null && cmp.New == null) {
                        wr.Write(WriterHelper.QuoteName(cmp.Cur.Name));
                        wr.WriteNewLine();
                    }
                }

                writer.WriteReportSection("deleted " + sectionname, wr);
            }
        }
        public              void                                ReportDepended(WriterHelper writer, CompareTable compareTable, string dependedname)
        {
            foreach(TCompare cmp in Items) {
                if (cmp.Cur != null && cmp.New != null && !cmp.New.CompareEqual(cmp.Cur, compareTable, CompareMode.Report)) {
                    writer.WriteWidth(dependedname + WriterHelper.QuoteName(cmp.Cur.Name), 68);
                    writer.Write(": changed");
                    writer.WriteNewLine();
                }
            }

            foreach(TCompare cmp in Items) {
                if (cmp.Cur == null && cmp.New != null) {
                    writer.WriteWidth(dependedname + WriterHelper.QuoteName(cmp.New.Name), 68);
                    writer.Write(": new");
                    writer.WriteNewLine();
                }
            }

            foreach(TCompare cmp in Items) {
                if (cmp.Cur != null && cmp.New == null) {
                    writer.WriteWidth(dependedname + WriterHelper.QuoteName(cmp.Cur.Name), 68);
                    writer.Write(": delete");
                    writer.WriteNewLine();
                }
            }
        }
        public              void                                ReportPermissions(WriterHelper writer)
        {
            using (WriterHelper     wr = new WriterHelper())
            {
            }
        }
        public              void                                Init(WriterHelper writer)
        {
            foreach(TCompare cmp in Items)
                cmp.Init(writer);
        }
        public              void                                Refactor(WriterHelper writer)
        {
            foreach(var item in _items)
                item.Refactor(writer);
        }
        public              void                                Process(DBSchemaCompare dbCompare, WriterHelper writer)
        {
            foreach(TCompare cmp in Items)
                cmp.Process(dbCompare, writer);
        }
        public              void                                Cleanup(WriterHelper writer)
        {
            foreach(TCompare cmp in Items)
                cmp.Cleanup(writer);
        }

        public              void                                SetRebuild()
        {
            foreach(TCompare cmp in Items)
                cmp.SetRebuild();
        }
        public              void                                Compare(DBSchemaCompare compare, CompareTable compareTable)
        {
            foreach(TCompare cmp in Items)
                cmp.Compare(compare, compareTable);
        }
        public              bool                                hasChange()
        {
            foreach(TCompare cmp in _items) {
                if ((cmp.Flags & (CompareFlags.Create | CompareFlags.Refactor | CompareFlags.Update)) != 0)
                    return true;
            }

            return false;
        }
    }
}
