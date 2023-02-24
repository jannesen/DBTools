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
             where s.[user_type_id]  = t.[system_type_id]
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
   and exists (select *
                 from sys.columns c
                      inner join sys.objects o on o.[object_id] = c.[object_id]
                where c.[user_type_id] = t.[user_type_id]
                  and o.[type]         = 'U')
 order by [@name]
   for xml path('type'), type