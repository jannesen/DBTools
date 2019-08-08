using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Jannesen.Tools.DBTools.Library
{
    class SqlEntityName: IComparable
    {
        public          string                  Fullname            { get; private set; }
        public          string                  Schema              { get; private set; }
        public          string                  Name                { get; private set; }
        private         int?                    _hashcode;

        public                                  SqlEntityName(string schema, string name)
        {
            this.Schema = schema;
            this.Name = name;
            this.Fullname = schema + "." + DBSchema.WriterHelper.QuoteName(name);
        }
        public                                  SqlEntityName(string fullname)
        {
            Fullname = fullname;

            List<string>    parts = _splitSqlName(fullname);

            if (parts.Count != 2)
                throw new ArgumentException("Invalid sql fullname '" + fullname + "'.");

            Schema = parts[0];
            Name   = parts[1];
        }

        public      static      int             Compare(SqlEntityName n1, SqlEntityName n2)
        {
            int     i;

            if (n1 == null)
                return (n1 == null) ? 0 : -1;

            if (n2 == null)
                return 1;

            if ((i = _stringCompare(n1.Schema,   n2.Schema)) != 0)
                return i;

            return _stringCompare(n1.Name,     n2.Name);
        }
        public      static      bool            operator == (SqlEntityName n1, SqlEntityName n2)
        {
            if (n1 == null)
                return (n2 == null);

            if (n2 == null)
                return false;

            if (object.ReferenceEquals(n1, n2))
                return true;

            return _stringCompare(n1.Schema  , n2.Schema  ) == 0 &&
                   _stringCompare(n1.Name    , n2.Name    ) == 0;
        }
        public      static      bool            operator != (SqlEntityName n1, SqlEntityName n2)
        {
            return !(n1 == n2);
        }
        public                  int             CompareTo(object o)
        {
            if (o is SqlEntityName)
                return Compare(this, (SqlEntityName)o);

            return 1;
        }
        public      override    int             GetHashCode()
        {
            if (!_hashcode.HasValue) {
                _hashcode = Name.ToLowerInvariant().GetHashCode() ^
                            (Schema   != null ? Schema.ToLowerInvariant().GetHashCode() : 0);
            }

            return _hashcode.Value;
        }
        public      override    bool            Equals(object obj)
        {
            if (obj is SqlEntityName)
                return this == (SqlEntityName)obj;

            return false;
        }
        public      override    string          ToString()
        {
            return Fullname;
        }

        public      static      List<string>    _splitSqlName(string fullname)
        {
            List<string>    rtn   = new List<string>();
            StringBuilder   s     = new StringBuilder();
            bool            quote = false;

            for (int p = 0 ; p < fullname.Length ; ++p) {
                char c = fullname[p];

                switch(c) {
                case '[':
                    if (quote) {
                        if (p >= fullname.Length - 1)
                            throw new ArgumentException("Invalid sql fullname '" + fullname + "'.");

                        c = fullname[++p];
                        goto add;
                    }

                    quote = true;
                    break;

                case ']':
                    if (!quote)
                        goto add;

                    quote = false;
                    break;

                case '.':
                    if (quote)
                        goto add;

                    rtn.Add(s.ToString());
                    s.Clear();
                    break;

                default:
add:                s.Append(c);
                    break;
                }
            }

            if (quote)
                throw new ArgumentException("Invalid sql fullname '" + fullname + "'.");

            rtn.Add(s.ToString());

            return rtn;
        }
        public      static      int             _stringCompare(string s1, string s2)
        {
            if (s1 != null) {
                if (s2 != null)
                    return string.Compare(s1, s2, true, CultureInfo.InvariantCulture);

                return 1;
            }
            else {
                if (s2 != null)
                    return -1;

                return 0;
            }
        }
    }
}
