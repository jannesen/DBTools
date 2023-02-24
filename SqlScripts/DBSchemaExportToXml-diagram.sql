select [@name]         = schema_name([principal_id]) + '.' + quotename([name]),
       [@version]      = [version],
       [*]             = [definition]
  from sysdiagrams
 order by [@name]
   for xml path('diagram'), type