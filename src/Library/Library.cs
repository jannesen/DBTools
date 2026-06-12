using System;
using System.Collections.Generic;
using System.Text;

namespace Jannesen.Tools.DBTools.Library;

internal static class Library
{
    public      static  string[]        SplitSqlName(string name)
    {
        var rtn   = new List<string>();
        var s     = new StringBuilder();
        var quote = false;

        for (var p = 0 ; p < name.Length ; ++p) {
            var c = name[p];

            switch(c) {
            case '[':
                if (quote)
                    goto add;

                quote = true;
                break;

            case ']':
                if (!quote)
                    goto add;

                if (p < name.Length - 1) {
                    c = name[++p];
                    goto add;
                }
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

        rtn.Add(s.ToString());

        return rtn.ToArray();
    }
}
