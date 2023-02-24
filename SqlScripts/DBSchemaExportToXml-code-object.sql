select [@type]                  = [type_desc],
       [@name]                  = schema_name([schema_id]) + '.' + quotename([name]),
       [@table-name]            = case when [parent_object_id] <> 0 then (select schema_name(z.[schema_id]) + '.' + quotename(z.[name]) from sys.objects z where z.[object_id] = o.[parent_object_id]) end,
       [@opt-ansi_nulls]        = objectpropertyex(o.[object_id], N'ExecIsAnsiNullsOn'),
       [@opt-quoted_identifier] = objectpropertyex(o.[object_id], N'ExecIsQuotedIdentOn'),
       [code]                   = (select [definition] from sys.sql_modules m where m.[object_id] = o.[object_id]),
       (
            select [@name]   = (select [name] from sys.database_principals z where z.[principal_id] = p.[grantee_principal_id]),
                   [@grant]  = (
                                    select [data()] = case rtrim(x.[type]) when 'EX' then 'execute'
                                                                           when 'SL' then 'select'
                                                                           when 'IN' then 'insert'
                                                                           when 'UP' then 'update'
                                                                           when 'DL' then 'delete'
                                                                                        else rtrim(x.[type])
                                                        end
                                      from sys.database_permissions x
                                     where x.[class]                = 1
                                       and x.[major_id]             = o.[object_id]
                                       and x.[minor_id]             = 0
                                       and x.[grantee_principal_id] = p.[grantee_principal_id]
                                       and x.[grantor_principal_id] = 1
                                       and x.[state]                = 'G'
                                  order by case rtrim(x.[type]) when 'EX' then 1
                                                                when 'SL' then 2
                                                                when 'IN' then 3
                                                                when 'UP' then 4
                                                                when 'DL' then 5
                                                                          else 9
                                           end
                                       for xml path('')
                              ),
                  [@deny]  = (
                                    select [data()] = case rtrim(x.[type]) when 'EX' then 'execute'
                                                                           when 'SL' then 'select'
                                                                           when 'IN' then 'insert'
                                                                           when 'UP' then 'update'
                                                                           when 'DL' then 'delete'
                                                                                        else rtrim(x.[type])
                                                        end
                                      from sys.database_permissions x
                                     where x.[class]                = 1
                                       and x.[major_id]             = o.[object_id]
                                       and x.[minor_id]             = 0
                                       and x.[grantee_principal_id] = p.[grantee_principal_id]
                                       and x.[grantor_principal_id] = 1
                                       and x.[state]                = 'D'
                                  order by case rtrim(x.[type]) when 'EX' then 1
                                                                when 'SL' then 2
                                                                when 'IN' then 3
                                                                when 'UP' then 4
                                                                when 'DL' then 5
                                                                          else 9
                                           end
                                       for xml path('')
                              )
              from (
                        select distinct [grantee_principal_id]
                          from sys.database_permissions p
                         where p.[class]                = 1
                           and p.[major_id]             = o.[object_id]
                           and p.[minor_id]             = 0
                           and p.[grantor_principal_id] = 1
                   ) p
          order by [@name]
               for xml path('permission'), type
        )
  from sys.objects o
 where [type]          in ('FN', 'IF', 'TF', 'P', 'TR', 'V')
   and [is_ms_shipped] = 0
   and not ([schema_id] = 1 and [name] in ('_jannesen_dbtools_sql_module_definition')
          or exists (select * from sys.extended_properties z where z.[major_id] = o.[object_id] and z.[minor_id] = 0 and z.[class] = 1 and z.[name] = N'microsoft_database_tools_support'))
 order by case [type] when 'V'  then 1
                      when 'FN' then 2
                      when 'IF' then 3
                      when 'TF' then 4
                      when 'P'  then 5
                      when 'TR' then 6
       end,
       [name]
   for xml path('code-object'), type