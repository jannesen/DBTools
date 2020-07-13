using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design",        "CA1031:Do not catch general exception types")]
[assembly: SuppressMessage("Design",        "CA1032:Implement standard exception constructors")]
[assembly: SuppressMessage("Design",        "CA1034:Nested types should not be visible")]
[assembly: SuppressMessage("Design",        "CA1051:Do not declare visible instance fields")]
[assembly: SuppressMessage("Design",        "CA1062:Validate arguments of public methods")]
[assembly: SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters")]
[assembly: SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
[assembly: SuppressMessage("Performance",   "CA1812:Avoid uninstantiated internal classes")] // False alarm. classes are used!
[assembly: SuppressMessage("Security",      "CA2100:Review SQL queries for security vulnerabilities")]
[assembly: SuppressMessage("Style",         "IDE0044:Add readonly modifier")]
[assembly: SuppressMessage("Style",         "IDE1006:Naming Styles")]
[assembly: SuppressMessage("Usage",         "CA1801:Review unused parameters")]
[assembly: SuppressMessage("Style",         "IDE0060:Remove unused parameter")]
