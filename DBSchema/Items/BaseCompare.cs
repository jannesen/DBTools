﻿using System;
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
        public              CompareFlags                        Flags                   { get; set; }

        public              void                                SetRebuild()
        {
            if ((Flags & CompareFlags.Drop) == 0 && (Flags & CompareFlags.Create) == 0 && Cur != null && New != null) {
                Flags = (Flags & ~(CompareFlags.Refactor | CompareFlags.Update)) | (CompareFlags.Drop|CompareFlags.Create);
            }
        }
        public  virtual     void                                InitDepended(DBSchemaCompare compare)
        {
        }
        public              void                                Compare(DBSchemaCompare compare, ICompareTable compareTable)
        {
            InitDepended(compare);

            Flags = (New != null) ? ((Cur != null) ? CompareNewCur(compare, compareTable) : (CompareNeededCreate(compare) ? CompareFlags.Create : CompareFlags.None))
                                  : ((Cur != null) ? CompareFlags.Drop                    : CompareFlags.None);

            if (CompareDepended(compare, Flags) && (Flags & (CompareFlags.Update | CompareFlags.Create)) == 0)
                Flags |= CompareFlags.Update;
        }
        public  virtual     CompareFlags                        CompareNewCur(DBSchemaCompare compare, ICompareTable compareTable)
        {
            return New.CompareEqual(Cur, compare, compareTable, CompareMode.Update) ? CompareFlags.None : CompareFlags.Rebuild;
        }
        public  virtual     bool                                CompareNeededCreate(DBSchemaCompare compare)
        {
            return true;
        }
        public  virtual     bool                                CompareDepended(DBSchemaCompare dbCompare, CompareFlags status)
        {
            return false;
        }
        public  virtual     void                                ReportUpdate(DBSchemaCompare compare, WriterHelper writer)
        {
            if (!Cur.CompareEqual(New, compare, null, CompareMode.Report)) {
                writer.WriteQuoteName(Cur.Name);
                writer.Write(": " + Cur.ToReportString() + " => " + New.ToReportString());
                writer.WriteNewLine();
            }

            if (!Cur.Name.Equals(New.Name)) {
                writer.WriteQuoteName(Cur.Name);
                writer.Write(" => ");
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

    enum SortReason
    {
        Init        = 1
    }

    abstract class CompareItemCollection<TCompare, TItem, TName>
                                                            where TCompare:CompareItem<TItem, TName>, new()
                                                            where TItem:SchemaItem<TItem, TName>
                                                            where TName:class,IComparable
    {
        private readonly    List<TCompare>                      _items;
        private readonly    Dictionary<TName, TCompare>         _curDictionary;
        private readonly    Dictionary<TName, TCompare>         _newDictionary;

        public              IReadOnlyList<TCompare>             Items
        {
            get {
                return _items;
            }
        }

        public                                                  CompareItemCollection(DBSchemaCompare compare, IReadOnlyList<TItem> curSchema, IReadOnlyList<TItem> newSchema): this(compare, null, curSchema, newSchema)
        {
        }
        public                                                  CompareItemCollection(DBSchemaCompare compare, CompareTable table, IReadOnlyList<TItem> curSchema, IReadOnlyList<TItem> newSchema)
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
                    if (!_curDictionary.TryGetValue(item.GetOrgName(compare), out var cur)) {
                        if (table != null) {
                            foreach (TCompare i in _items) {
                                if (i.Cur != null && i.New == null &&
                                    i.Cur.CompareEqual(item, compare, table, CompareMode.UpdateWithRefactor)) {
                                    cur = i;
                                    break;
                                }
                            }
                        }

                        if (cur == null) {
                            _items.Add(cur = new TCompare() { Table = table });
                        }
                    }

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

        public              void                                Report(DBSchemaCompare compare, WriterHelper writer, string sectionname)
        {
            using (WriterHelper     wr = new WriterHelper()) {
                foreach(TCompare cmp in Items) {
                    if (cmp.Cur != null && cmp.New != null) {
                        cmp.ReportUpdate(compare, wr);
                    }
                }

                writer.WriteReportSection("changed " + sectionname, wr);
            }

            using (WriterHelper     wr = new WriterHelper()) {
                foreach(TCompare cmp in Items) {
                    if (cmp.Cur == null && cmp.New != null) {
                        wr.Write(WriterHelper.QuoteName(cmp.New.Name));
                        wr.WriteNewLine();
                    }
                }

                writer.WriteReportSection("new " + sectionname, wr);
            }

            using (WriterHelper     wr = new WriterHelper()) {
                foreach(TCompare cmp in Items) {
                    if (cmp.Cur != null && cmp.New == null) {
                        wr.Write(WriterHelper.QuoteName(cmp.Cur.Name));
                        wr.WriteNewLine();
                    }
                }

                writer.WriteReportSection("deleted " + sectionname, wr);
            }
        }
        public              bool                                ReportDepended(WriterHelper writer, DBSchemaCompare compare, ICompareTable compareTable, string dependedname)
        {
            bool rtn = false;
            foreach(TCompare cmp in Items) {
                if (cmp.Cur != null && cmp.New != null && !cmp.Cur.CompareEqual(cmp.New, compare, compareTable, CompareMode.Report)) {
                    writer.WriteWidth(dependedname + WriterHelper.QuoteName(cmp.Cur.Name), 68);
                    writer.Write(": changed");
                    writer.WriteNewLine();
                    rtn = true;
                }
            }

            foreach(TCompare cmp in Items) {
                if (cmp.Cur == null && cmp.New != null) {
                    writer.WriteWidth(dependedname + WriterHelper.QuoteName(cmp.New.Name), 68);
                    writer.Write(": new");
                    writer.WriteNewLine();
                    rtn = true;
                }
            }

            foreach(TCompare cmp in Items) {
                if (cmp.Cur != null && cmp.New == null) {
                    writer.WriteWidth(dependedname + WriterHelper.QuoteName(cmp.Cur.Name), 68);
                    writer.Write(": delete");
                    writer.WriteNewLine();
                    rtn = true;
                }
            }

            return rtn;
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
        public  virtual     void                                Process(DBSchemaCompare dbCompare, WriterHelper writer)
        {
            foreach(TCompare cmp in Items)
                cmp.Process(dbCompare, writer);
        }
        public  virtual     void                                Cleanup(WriterHelper writer)
        {
            foreach(TCompare cmp in Items)
                cmp.Cleanup(writer);
        }

        public              void                                SetRebuild()
        {
            foreach(TCompare cmp in Items)
                cmp.SetRebuild();
        }
        public              void                                Compare(DBSchemaCompare compare, ICompareTable compareTable)
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
