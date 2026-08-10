using System;
using System.Collections.Generic;
using Jannesen.Tools.DBTools.DBSchema;

// DBTools export         TVCN-SERVER-DEV\TAS2 "file:C:\Temp\TAS2-36-schema.xml"
// DBTools compare-report "file:C:\Temp\TAS2-36-schema.xml" "sql:TVCN-SERVER-DEV\TAS2" "C:\Temp\TAS2.36 to 2.37 report.txt"
// DBTools schema-update  "file:C:\Temp\TAS2-36-schema.xml" "sql:TVCN-SERVER-DEV\TAS2" "C:\Temp\TAS2.36 to 2.37 report.sql"
// DBTools code-update    N:\TVCN\TVCN-SERVER\TAS\2.38.01\Schema\TAS2.38.01-schema.xml "sql:TVCN-SERVER-DEV\TAS2" "C:\Temp\T.sql"
namespace Jannesen.Tools.DBTools;

internal sealed class Program
{
    static      void        Main(string[] args)
    {
        try {
            var options = new Options();

            while (args.Length > 1 && args[0].StartsWith('-')) {
                switch(args[0]) {
                case "--refactor":
                    options.Refactor = true;
                    break;

                case "--include-code":
                    options.IncludeCode = true;
                    break;

                default:
                    throw new Exception("Syntax error, unknown option '" + args[0] + "'.");
                }

                args = args[1..];
            }

            if (args.Length < 1) {
                CmdHelp();
                return;
            }

            switch(args[0]) {
            case "help":
                CmdHelp();
                break;

            case "export":
                if (args.Length < 3)
                    throw new Exception("Syntax error, missing argument.");

                CmdExport(options, args[1], args[2]);
                break;

            case "compare-report":
                if (args.Length < 4)
                    throw new Exception("Syntax error, missing argument.");

                CmdCompareReport(options, args[1], args[2], args[3], false);
                break;

            case "compare-diff":
                if (args.Length < 4)
                    throw new Exception("Syntax error, missing argument.");

                CmdCompareReport(options, args[1], args[2], args[3], true);
                break;

            case "schema-create":
                if (args.Length < 3)
                    throw new Exception("Syntax error, missing argument.");

                CmdSchemaCreate(options, args[1], args[2]);
                break;

            case "schema-update":
                if (args.Length < 4)
                    throw new Exception("Syntax error, missing argument.");

                CmdSchemaUpdate(options, args[1], args[2], args[3]);
                break;

            case "code-update":
                if (args.Length < 4)
                    throw new Exception("Syntax error, missing argument.");

                CmdCodeUpdate(options, args[1], args[2], args[3]);
                break;

            case "code-grep":
                if (args.Length < 3)
                    throw new Exception("Syntax error, missing argument.");

                CmdCodeGrep(options, args[1], args[2]);
                break;

            case "data-export":
                if (args.Length < 4)
                    throw new Exception("Syntax error, missing argument.");

                CmdDataExport(options, args[1], args[2], args[3..]);
                break;

            default:
                throw new Exception("Syntax error, unknown command.");
            }
        }
        catch(Exception err) {
            while (err != null) {
                System.Diagnostics.Debug.WriteLine("ERROR: " + err.Message);
                Console.WriteLine("ERROR: " + err.Message);
                err = err.InnerException;
            }
        }
    }

    static      void        CmdHelp()
    {
        Console.WriteLine("DBTools <cmd> <args>...");
        Console.WriteLine("    export sql:<server-name>\\<database-name> <outputfile-name>");
        Console.WriteLine("         Export the database schema and sql-code to xml file.");
        Console.WriteLine("");
        Console.WriteLine("    compare-report <cur-source> <new-source> <outputfile-name>");
        Console.WriteLine("         Compare 2 version and generate a report with the changes.");
        Console.WriteLine("");
        Console.WriteLine("    compare-diff <cur-source> <new-source> <outputfile-name>");
        Console.WriteLine("         Compare 2 version and generate a diff with the changes.");
        Console.WriteLine("");
        Console.WriteLine("    schema-create <cur-source> <outputfile-name>");
        Console.WriteLine("         Create sql script for creating a empty new database.");
        Console.WriteLine("");
        Console.WriteLine("    schema-update <cur-source> <new-source> <outputfile-name>");
        Console.WriteLine("         Compare the current schema with a new schema and generate a sql");
        Console.WriteLine("         script to convert database schema to new schema.");
        Console.WriteLine("         PS: The datacopy code needs manual editing.");
        Console.WriteLine("");
        Console.WriteLine("    code-update <cur-source> <new-source> <outputfile-name>");
        Console.WriteLine("         Compare the current sql-code with the new sql-code and generate");
        Console.WriteLine("         a sql script to update the sql-code");
        Console.WriteLine("");
        Console.WriteLine("    code-grep <source> <regex>");
        Console.WriteLine("         Print sql-code lines that contain a match pattern.");
        Console.WriteLine("");
        Console.WriteLine("    <source>");
        Console.WriteLine("        sql:<server-name>\\<database-name>");
        Console.WriteLine("        file:<filename-name>");
    }
    static      void        CmdExport(Options options, string databaseSource, string outputFileName)
    {
        if (!databaseSource.StartsWith("sql:", StringComparison.Ordinal))
            throw new Exception("Syntax error, invalid database source.");

        DBSchemaDatabase.ExportToFile(options, databaseSource.Substring(4), outputFileName);
    }
    static      void        CmdCompareReport(Options options, string curSchemaName, string newSchemaName, string outputFileName, bool includediff)
    {
        var compare = new DBSchemaCompare(options);

        compare.CurSchema.LoadFrom(curSchemaName);
        compare.NewSchema.LoadFrom(newSchemaName);
        compare.Report(outputFileName, includediff);
    }
    static      void        CmdSchemaCreate(Options options, string schemaName, string outputFileName)
    {
        var compare = new DBSchemaCompare(options);

        compare.NewSchema.LoadFrom(schemaName);
        compare.SchemaUpdate(outputFileName, true);
    }
    static      void        CmdSchemaUpdate(Options options, string curSchemaName, string newSchemaName, string outputFileName)
    {
        var compare = new DBSchemaCompare(options);

        compare.CurSchema.LoadFrom(curSchemaName);
        compare.NewSchema.LoadFrom(newSchemaName);
        compare.SchemaUpdate(outputFileName, false);
    }
    static      void        CmdCodeUpdate(Options options, string curSchemaName, string newSchemaName, string outputFileName)
    {
        options.IncludeCode = true;
        var compare = new DBSchemaCompare(options);

        compare.CurSchema.LoadFrom(curSchemaName);
        compare.NewSchema.LoadFrom(newSchemaName);
        compare.CodeUpdate(outputFileName);
    }
    static      void        CmdCodeGrep(Options options, string schemaName, string regex)
    {
        options.IncludeCode = true;
        var schema = new DBSchemaDatabase(options);

        schema.LoadFrom(schemaName);
        schema.CodeGrep(regex);
    }
    static      void        CmdDataExport(Options options, string datasource, string outputFileName, string[] tableName)
    {
        DataExport.Run(datasource, outputFileName, tableName);
    }
}
