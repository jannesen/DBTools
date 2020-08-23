
set nocount on

select [name]       = db_name(),
       [collate]    = convert(nvarchar(128), databasepropertyex(db_name(), 'Collation')),
       -- role
       (
        select [@name]    = r.[name],
               [@orgname] = (select convert(sysname, [value]) from sys.extended_properties z where z.[class] = 4 and z.[major_id] = r.[principal_id] and z.[name] = 'refactor:orgname'),
               (
                select p.[name]
                  from sys.database_role_members m
                       inner join sys.database_principals p on p.[principal_id] = m.[member_principal_id]
                 where m.[role_principal_id] = r.[principal_id]
                   and p.[type]              in ('R')
                   and p.[principal_id]      <> 0
                   and p.[is_fixed_role]     =  0
              order by p.[name]
                   for xml raw('member'), type
               )
          from sys.database_principals r
         where [type]          in ('R')
           and [principal_id]  <> 0
           and [is_fixed_role] =  0
      order by [name]
           for xml path('role'), type
       ),
       -- default
       (
         select [@name]    = schema_name([schema_id]) + '.' + quotename([name]),
                [@orgname] = (select convert(sysname, [value]) from sys.extended_properties z where z.[class] = 1 and z.[major_id] = o.[object_id] and z.[name] = 'refactor:orgname'),
                [*]        = dbo.[_jannesen_dbtools_sql_module_definition]([object_id], 'CREATE DEFAULT', schema_name([schema_id]), [name])
           from sys.objects o
          where [type]             = 'D'
            and [parent_object_id] = 0
       order by [@name]
            for xml path('default'), type
       ),
       -- rule
       (
         select [@name]    = schema_name([schema_id]) + '.' + quotename([name]),
                [@orgname] = (select convert(sysname, [value]) from sys.extended_properties z where z.[class] = 1 and z.[major_id] = o.[object_id] and z.[name] = 'refactor:orgname'),
                [*]        = dbo.[_jannesen_dbtools_sql_module_definition]([object_id], 'CREATE RULE', schema_name([schema_id]), [name])
          from sys.objects o
         where [type]          in ('R')
           and [is_ms_shipped] = 0
      order by [name]
          for xml path('rule'), type
       ),
       -- udt
       (
        select [@name]      = schema_name([schema_id]) + '.' + quotename([name]),
               [@orgname]   = (select convert(sysname, [value]) from sys.extended_properties z where z.[class] = 6 and z.[major_id] = t.[user_type_id] and z.[name] = 'refactor:orgname'),
               [@collation] = case when t.[collation_name] is not null
                                    and t.[collation_name] <> convert(nvarchar(128), databasepropertyex(db_name(), 'Collation'))
                                   then t.[collation_name]
                              end,
               (
                select case when s.[user_type_id] IN (165,167,173,175,231,239) then s.[name] + '(' + case when t.[max_length] > 0 then convert(varchar(16), t.[max_length]) else 'max' end + ')'
                            when s.[user_type_id] IN (62)                      then s.[name] + '(' + convert(varchar(16), t.[precision]) + ')'
                            when s.[user_type_id] IN (42,43)                   then s.[name] + '(' + convert(varchar(16), t.[scale]) + ')'
                            when s.[user_type_id] IN (106,108)                 then s.[name] + '(' + convert(varchar(16), t.[precision]) + ',' + convert(varchar(16), t.[scale]) + ')'
                                                                               else s.[name]
                       end
                  from sys.types s
                 where s.[user_type_id] = t.[system_type_id]
				   and t.[is_table_type] = 0
               ),
			   (
		         select c.[name],
                        [type]           = case when c.[is_computed] = 0
                                                then case when t.[user_type_id] in (165,167,173,175,231,239) then t.[name] + '(' + case when c.[max_length] > 0 then convert(varchar(16), c.[max_length]) else 'max' end + ')'
                                                          when t.[user_type_id] in (62)                      then t.[name] + '(' + convert(varchar(16), c.[precision]) + ')'
                                                          when t.[user_type_id] in (42,43)                   then t.[name] + '(' + convert(varchar(16), c.[scale]) + ')'
                                                          when t.[user_type_id] in (106,108)                 then t.[name] + '(' + convert(varchar(16), c.[precision]) + ',' + convert(varchar(16), c.[scale]) + ')'
                                                          when t.[user_type_id] <= 256                       then t.[name]
                                                                                                             else schema_name(t.[schema_id]) + '.' + quotename(t.[name])
                                                     end
                                            end,
                        [identity]       = case when c.[is_identity]       <> 0
                                                then convert(varchar, ident_seed(schema_name(tt.[object_schema_id])+'.'+quotename(tt.[object_name]))) + ',' +
                                                     convert(varchar, ident_incr(schema_name(tt.[object_schema_id])+'.'+quotename(tt.[object_name])))
                                            end,
                        [collation]      = case when c.[collation_name] is not null
                                                 and c.collation_name <> convert(nvarchar(128), databasepropertyex(db_name(), 'Collation'))
                                                then c.[collation_name]
                                            end,
                        [is-nullable]    = case when c.[is_nullable]       <> 0
                                                then '1'
                                            end,
                        [is-rowguid]     = case when c.[is_rowguidcol]     <> 0
                                                then '1'
                                            end,
                        [is-filestream]  = case when c.[is_filestream]     <> 0
                                                then '1'
                                            end,
                        [is-xml-document]= case when c.[is_xml_document]   <> 0
                                                then '1'
                                            end,
                        [is-computed]    = case when c.[is_computed]       <> 0
                                                then '1'
                                            end,
                        [default]        = case when c.[default_object_id] <> 0
                                                 and c.[default_object_id] <> t.[default_object_id]
                                                then (
                                                        select case when d.[parent_object_id] = 0
                                                                    then schema_name([schema_id])+'.'+quotename([name])
                                                                    else case when [definition] like '((%))'
                                                                                then substring([definition], 2, len([definition]) - 2)
                                                                                else [definition]
                                                                            end
                                                                end
                                                            from sys.default_constraints d
                                                            where d.[object_id]    = c.[default_object_id]
                                                    )
                                            end,
                        [rule]           = case when c.[rule_object_id]    <> 0 and c.[rule_object_id]    <> t.[rule_object_id]
                                                then (
                                                        select case when r.[parent_object_id] = 0
                                                                    then schema_name([schema_id])+'.'+quotename([name])
                                                                    else isnull((select [definition] from sys.sql_modules m where m.[object_id] = r.[object_id]), '[unknown]')
                                                                end
                                                            from sys.objects r
                                                            where r.[object_id]    = c.[default_object_id]
                                                    )
                                            end
				   from sys.all_columns c
                        left join sys.types t on t.[user_type_id] = c.[user_type_id]
				  where c.[object_id] = tt.[object_id]
               order by c.[column_id]
                    for xml raw('column'), type
			   ),
               -- index
               (
                    select [function]        = case when i.[is_primary_key]       <> 0 then 'primary-key'
                                                    when i.[is_unique_constraint] <> 0 then 'constraint'
                                                    when i.[is_hypothetical]      <> 0 then 'stat'
                                                                                       else 'index'
                                               end,
                           [name],
                           [orgname]        = (select convert(sysname, [value]) from sys.extended_properties z where z.[class] = 7 and z.[major_id] = tt.[object_id] and z.[major_id] = i.[index_id] and z.[name] = 'refactor:orgname'),
                           [type]           = lower(i.[type_desc]),
                           [unique]         = case when i.[is_unique] <> 0 then '1' else '0' end,
                           [fill-factor]    = case when [fill_factor] > 0 then [fill_factor] end,
                           [filter]         = case when i.[has_filter] <> 0 then i.[filter_definition] end,
                           (
                             select c.[name],
                                    [order]     = case when k.[is_descending_key] <> 0 then 'desc' end
                               from sys.index_columns k
                                    inner join sys.columns c on c.[object_id] = k.[object_id]
                                                            and c.[column_id] = k.[column_id]
                              where k.[object_id] = i.[object_id]
                                and k.[index_id]  = i.[index_id]
                           order by k.[key_ordinal]
                                for xml raw('column'), type
                           )
                      from sys.indexes i
                     where i.[object_id] = tt.[object_id]
                       and i.[type]      > 0
                  order by case when i.[is_primary_key]       <> 0 then 0
                                when i.[is_unique_constraint] <> 0 then 2
                                when i.[is_hypothetical]      <> 0 then 3
                                                                   else 1
                           end, [name]
                       for xml raw('index'), type
               )
          from sys.types t
		       left join (
					select tt.[user_type_id],
							  [object_id]        = tt.[type_table_object_id],
							  [object_schema_id] = o.[schema_id],
							  [object_name]      = o.[name]
					  from sys.table_types tt
				           inner join sys.objects o on o.[object_id] = tt.[type_table_object_id]
			   ) tt on tt.[user_type_id] = t.[user_type_id]
         where [is_user_defined]  = 1
           and [is_assembly_type] = 0
      order by [@name]
          for xml path('type'), type
       ),
       -- table
       (
        select [@name]    = schema_name([schema_id]) + '.' + quotename([name]),
               [@orgname] = (select convert(sysname, [value]) from sys.extended_properties z where z.[class] = 1 and z.[major_id] = o.[object_id]  and z.[minor_id] = 0 and z.[name] = 'refactor:orgname'),
               (
                    select c.[name],
                           [orgname]        = (select convert(sysname, [value]) from sys.extended_properties z where z.[class] = 1 and z.[major_id] = o.[object_id] and z.[minor_id] = c.[column_id] and z.[name] = 'refactor:orgname'),
                           [type]           = case when c.[is_computed] = 0
                                                   then case when t.[user_type_id] in (165,167,173,175,231,239) then t.[name] + '(' + case when c.[max_length] > 0 then convert(varchar(16), c.[max_length]) else 'max' end + ')'
                                                             when t.[user_type_id] in (62)                      then t.[name] + '(' + convert(varchar(16), c.[precision]) + ')'
                                                             when t.[user_type_id] in (42,43)                   then t.[name] + '(' + convert(varchar(16), c.[scale]) + ')'
                                                             when t.[user_type_id] in (106,108)                 then t.[name] + '(' + convert(varchar(16), c.[precision]) + ',' + convert(varchar(16), c.[scale]) + ')'
                                                             when t.[user_type_id] <= 256                       then t.[name]
                                                                                                                else schema_name(t.[schema_id]) + '.' + quotename(t.[name])
                                                        end
                                              end,
                           [identity]       = case when c.[is_identity]       <> 0
                                                   then convert(varchar, ident_seed(schema_name(o.[schema_id])+'.'+quotename(o.[name]))) + ',' +
                                                        convert(varchar, ident_incr(schema_name(o.[schema_id])+'.'+quotename(o.[name])))
                                              end,
                           [collation]      = case when c.[collation_name] is not null
                                                    and c.collation_name <> convert(nvarchar(128), databasepropertyex(db_name(), 'Collation'))
                                                   then c.[collation_name]
                                              end,
                           [is-nullable]    = case when c.[is_nullable]       <> 0
                                                   then '1'
                                              end,
                           [is-rowguid]     = case when c.[is_rowguidcol]     <> 0
                                                   then '1'
                                              end,
                           [is-filestream]  = case when c.[is_filestream]     <> 0
                                                   then '1'
                                              end,
                           [is-xml-document]= case when c.[is_xml_document]   <> 0
                                                   then '1'
                                              end,
                           [is-computed]    = case when c.[is_computed]       <> 0
                                                   then '1'
                                              end,
                           [default]        = case when c.[default_object_id] <> 0
                                                    and c.[default_object_id] <> t.[default_object_id]
                                                   then (
                                                            select case when d.[parent_object_id] = 0
                                                                        then schema_name([schema_id])+'.'+quotename([name])
                                                                        else case when [definition] like '((%))'
                                                                                    then substring([definition], 2, len([definition]) - 2)
                                                                                    else [definition]
                                                                             end
                                                                   end
                                                              from sys.default_constraints d
                                                             where d.[object_id]    = c.[default_object_id]
                                                        )
                                              end,
                           [rule]           = case when c.[rule_object_id]    <> 0 and c.[rule_object_id]    <> t.[rule_object_id]
                                                   then (
                                                            select case when r.[parent_object_id] = 0
                                                                        then schema_name([schema_id])+'.'+quotename([name])
                                                                        else isnull((select [definition] from sys.sql_modules m where m.[object_id] = r.[object_id]), '[unknown]')
                                                                   end
                                                              from sys.objects r
                                                             where r.[object_id]    = c.[default_object_id]
                                                        )
                                              end
                      from sys.columns c
                           left join sys.types t on t.[user_type_id] = c.[user_type_id]
                     where c.[object_id] = o.[object_id]
                  order by c.[column_id]
                       for xml raw('column'), type
                ),
                --!! constraint
                (
                    select [@name] = schema_name(c.[schema_id])+'.'+quotename(c.[name]),
                           (
                                select c.[definition]
                           )
                      from sys.check_constraints c
                     where c.[parent_object_id] = o.[object_id]
                       and c.[is_system_named]  = 0
                  order by [@name]
                   for xml path('constraint'), type
                ),
                -- index
                (
                    select [function]        = case when i.[is_primary_key]       <> 0 then 'primary-key'
                                                    when i.[is_unique_constraint] <> 0 then 'constraint'
                                                    when i.[is_hypothetical]      <> 0 then 'stat'
                                                                                       else 'index'
                                               end,
                           [name],
                           [orgname]        = (select convert(sysname, [value]) from sys.extended_properties z where z.[class] = 7 and z.[major_id] = o.[object_id] and z.[major_id] = i.[index_id] and z.[name] = 'refactor:orgname'),
                           [type]           = lower(i.[type_desc]),
                           [unique]         = case when i.[is_unique] <> 0 then '1' else '0' end,
                           [fill-factor]    = case when [fill_factor] > 0 then [fill_factor] end,
                           [filter]         = case when i.[has_filter] <> 0 then i.[filter_definition] end,
                           (
                             select c.[name],
                                    [order]     = case when k.[is_descending_key] <> 0 then 'desc' end
                               from sys.index_columns k
                                    inner join sys.columns c on c.[object_id] = k.[object_id]
                                                            and c.[column_id] = k.[column_id]
                              where k.[object_id] = i.[object_id]
                                and k.[index_id]  = i.[index_id]
                           order by k.[key_ordinal]
                                for xml raw('column'), type
                           )
                      from sys.indexes i
                     where i.[object_id] = o.[object_id]
                       and i.[type]      > 0
                  order by case when i.[is_primary_key]       <> 0 then 0
                                when i.[is_unique_constraint] <> 0 then 2
                                when i.[is_hypothetical]      <> 0 then 3
                                                                   else 1
                           end, [name]
                       for xml raw('index'), type
                ),
                -- reference
                (
                    select [name]          = schema_name(f.[schema_id]) + '.' + quotename(f.[name]),
                           [referenced]    = (select schema_name(x.[schema_id]) + '.' + quotename(x.[name]) from sys.objects x where x.[object_id] = f.[referenced_object_id]),
                           [is-disabled]   = case when f.[is_disabled]               <> 0 then '1' end,
                           [update-action] = case f.[update_referential_action] when 0 then ''
                                                                                when 1 then 'cascade'
                                                                                when 2 then 'set null'
                                                                                when 3 then 'set default'
                                             end,
                           [delete-action] = case f.[delete_referential_action] when 0 then ''
                                                                                when 1 then 'cascade'
                                                                                when 2 then 'set null'
                                                                                when 3 then 'set default'
                                             end,
                           (
                                select [name]       = (select [name] from sys.columns z where z.[object_id] = c.[parent_object_id]     and z.[column_id] = c.[parent_column_id]),
                                       [referenced] = (select [name] from sys.columns z where z.[object_id] = c.[referenced_object_id] and z.[column_id] = c.[referenced_column_id])
                                  from sys.foreign_key_columns c
                                 where c.[constraint_object_id] = f.[object_id]
                                   for xml raw('column'), type
                           )
                      from sys.foreign_keys f
                     where f.[parent_object_id] = o.[object_id]
                       and f.[is_system_named]  = 0
                  order by [name]
                       for xml raw('reference'), type
                ),
                -- permission
                (
                    select [@name]   = (select [name] from sys.database_principals z where z.[principal_id] = p.[grantee_principal_id]),
                           [@grant]  = (
                                            select [data()] = case rtrim(x.[type]) when 'SL' then 'select'
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
                                          order by case rtrim(x.[type]) when 'SL' then 1
                                                                        when 'IN' then 2
                                                                        when 'UP' then 3
                                                                        when 'DL' then 4
                                                                                  else 9
                                                              end
                                               for xml path('')
                                       ),
                           [@deny]   = (
                                            select [data()] = case rtrim(x.[type]) when 'SL' then 'select'
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
                                          order by case rtrim(x.[type]) when 'SL' then 1
                                                                        when 'IN' then 2
                                                                        when 'UP' then 3
                                                                        when 'DL' then 4
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
          where [type] in ('U')
            and [is_ms_shipped] = 0
            and not ([schema_id] = 1 and [name] in ('sysdiagrams')) -- Ignore diagram stuff
       order by [name]
            for xml path('table'), type
          ),
          (
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
          ),
          (
            select [@name]         = schema_name([principal_id]) + '.' + quotename([name]),
                   [@version]      = [version],
                   [*]             = [definition]
              from sysdiagrams
          order by [@name]
               for xml path('diagram'), type
          )
      for xml raw('database'), type
