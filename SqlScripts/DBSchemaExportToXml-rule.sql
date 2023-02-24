 select [@name]    = schema_name([schema_id]) + '.' + quotename([name]),
        [@orgname] = (select convert(sysname, [value]) from sys.extended_properties z where z.[class] = 1 and z.[major_id] = o.[object_id] and z.[name] = 'refactor:orgname'),
        [*]        = dbo.[_jannesen_dbtools_sql_module_definition]([object_id], 'CREATE RULE', schema_name([schema_id]), [name])
  from sys.objects o
 where [type]          in ('R')
   and [is_ms_shipped] = 0
 order by [name]
   for xml path('rule'), type