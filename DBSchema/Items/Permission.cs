using System;
using System.Collections.Generic;
using System.Xml;
using Jannesen.Tools.DBTools.Library;

namespace Jannesen.Tools.DBTools.DBSchema.Item
{
    [Flags]
    enum SqlPermissions
    {
        None        = 0x0000,
        Select      = 0x0001,
        Execute     = 0x0002,
        Insert      = 0x0010,
        Update      = 0x0020,
        Delete      = 0x0040,
    }
    class SchemaPermission: SchemaItemName<SchemaPermission>
    {
        public          SqlPermissions                          Grant               { get; private set; }
        public          SqlPermissions                          Deny                { get; private set; }

        public                                                  SchemaPermission(XmlReader xmlReader): base(xmlReader)
        {
            try {
                Grant = ParsePermissions(xmlReader.GetValueStringNullable("grant"));
                Deny = ParsePermissions(xmlReader.GetValueStringNullable("deny"));

                xmlReader.NoChildElements();
            }
            catch(Exception err) {
                throw new DBSchemaException("Reading of permission '" + Name + "' failed.", err);
            }
        }

        public  override    bool                                CompareEqual(SchemaPermission other, DBSchemaCompare compare, CompareTable compareTable, CompareMode mode)
        {
            if (this.Name  != other.Name     ||
                this.Grant != other.Grant    ||
                this.Deny  != other.Deny)
                return false;

            return true;
        }

        public  static      SqlPermissions                      ParsePermissions(string sperm)
        {
            SqlPermissions      rtn = SqlPermissions.None;

            if (sperm != null) {
                foreach (string sp in sperm.Split(' ')) {
                    string s = sp.Trim().ToLower();

                    switch(s) {
                    case "select":      rtn |= SqlPermissions.Select;       break;
                    case "execute":     rtn |= SqlPermissions.Execute;      break;
                    case "insert":      rtn |= SqlPermissions.Insert;       break;
                    case "update":      rtn |= SqlPermissions.Update;       break;
                    case "delete":      rtn |= SqlPermissions.Delete;       break;
                    default:            throw new Exception("Unknown permission '" + s + "'.");
                    }
                }
            }

            return rtn;
        }
        public  static      string                              ToSqlPermissions(SqlPermissions p)
        {
            string  rtn = "";

            if ((p & SqlPermissions.Select)  == SqlPermissions.Select)      rtn = (rtn.Length > 0) ? rtn + ",SELECT"  : "SELECT";
            if ((p & SqlPermissions.Execute) == SqlPermissions.Execute)     rtn = (rtn.Length > 0) ? rtn + ",EXECUTE" : "EXECUTE";
            if ((p & SqlPermissions.Insert)  == SqlPermissions.Insert)      rtn = (rtn.Length > 0) ? rtn + ",INSERT"  : "INSERT";
            if ((p & SqlPermissions.Update)  == SqlPermissions.Update)      rtn = (rtn.Length > 0) ? rtn + ",UPDATE"  : "UPDATE";
            if ((p & SqlPermissions.Delete)  == SqlPermissions.Delete)      rtn = (rtn.Length > 0) ? rtn + ",DELETE"  : "DELETE";

            return rtn;
        }
    }

    class SchemaPermissionCollection: SchemaItemList<SchemaPermission,string>
    {
    }

    class ComparePermission: CompareItem<SchemaPermission,string>
    {
    }

    class ComparePermissionCollection: CompareItemCollection<ComparePermission,SchemaPermission,string>
    {
        public                                                  ComparePermissionCollection(DBSchemaCompare compare, CompareTable table, IReadOnlyList<SchemaPermission> curSchema, IReadOnlyList<SchemaPermission> newSchema): base(compare, table, curSchema, newSchema)
        {
        }

        public              void                                WriteGrantRevoke(WriterHelper writer, CompareFlags status, SqlEntityName name, int width)
        {
            bool        f = false;

            foreach(ComparePermission cmpPermission in this.Items) {
                SqlPermissions      curGrant = (cmpPermission.Cur != null && status != CompareFlags.Rebuild ? cmpPermission.Cur.Grant : SqlPermissions.None);
                SqlPermissions      newGrant = (cmpPermission.New != null                                   ? cmpPermission.New.Grant : SqlPermissions.None);
                SqlPermissions      curDeny  = (cmpPermission.Cur != null && status != CompareFlags.Rebuild ? cmpPermission.Cur.Deny  : SqlPermissions.None);
                SqlPermissions      newDeny  = (cmpPermission.New != null                                   ? cmpPermission.New.Deny  : SqlPermissions.None);
                SqlPermissions      revoke   = (curGrant & (~newGrant)) | (curDeny & (~newDeny));
                SqlPermissions      grant    = newGrant & (~(curGrant | revoke));
                SqlPermissions      deny     = newDeny  & (~(curDeny  | revoke));

                if (revoke != SqlPermissions.None) {
                    writer.Write("REVOKE ");
                    writer.Write(SchemaPermission.ToSqlPermissions(revoke));
                    writer.Write(" ON ");
                    writer.Write(name);
                    writer.Write(" FROM ");
                    writer.WriteQuoteName(cmpPermission.Cur.Name);
                    writer.WriteNewLine();
                    f = true;
                }

                if (grant != SqlPermissions.None) {
                    writer.Write("GRANT ");
                    writer.WriteWidth(SchemaPermission.ToSqlPermissions(grant), width);
                    writer.Write("ON ");
                    writer.Write(name);
                    writer.Write(" TO ");
                    writer.WriteQuoteName(cmpPermission.New.Name);
                    writer.WriteNewLine();
                    f = true;
                }

                if (deny != SqlPermissions.None) {
                    writer.Write("DENY ");
                    writer.WriteWidth(SchemaPermission.ToSqlPermissions(deny), width);
                    writer.Write("ON ");
                    writer.Write(name);
                    writer.Write(" TO ");
                    writer.WriteQuoteName(cmpPermission.New.Name);
                    writer.WriteNewLine();
                    f = true;
                }
            }

            if (f)
                writer.WriteSqlGo();
        }
    }
}
