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