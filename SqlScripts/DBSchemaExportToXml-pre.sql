if exists (select * from sys.objects where [object_id] = object_id('dbo.[_jannesen_dbtools_sql_module_definition]') and [type] = 'FN')
    drop function dbo.[_jannesen_dbtools_sql_module_definition]
go
create function dbo.[_jannesen_dbtools_sql_module_definition]
(
    @object_id      int,
    @type           nvarchar(32),
    @schema         nvarchar(128),
    @name           nvarchar(128)
)
returns nvarchar(max)
as
begin
    declare @definition     nvarchar(max),
            @prefix         nvarchar(256)

    select @definition = [definition]
      from sys.sql_modules m
     where m.[object_id] = @object_id

    while charindex(char(10), @definition) > 0  set @definition = replace(@definition, char(10), ' ')
    while charindex(char(13), @definition) > 0  set @definition = replace(@definition, char(13), ' ')
    while charindex(char( 7), @definition) > 0  set @definition = replace(@definition, char( 7), ' ')
    while charindex('  '    , @definition) > 0  set @definition = replace(@definition, '  '    , ' ')

    set @prefix = @type + ' ' + @schema + '.' + quotename(@name) + ' AS '
    if substring(@definition, 1, datalength(@prefix) / 2) = @prefix  return substring(@definition, datalength(@prefix) / 2 + 1, len(@definition))

    set @prefix = @type + ' ' + @schema + '.' + @name + ' AS '
    if substring(@definition, 1, datalength(@prefix) / 2) = @prefix  return substring(@definition, datalength(@prefix) / 2 + 1, len(@definition))

    set @prefix = @type + ' ' + quotename(@name) + ' AS '
    if substring(@definition, 1, datalength(@prefix) / 2) = @prefix  return substring(@definition, datalength(@prefix) / 2 + 1, len(@definition))

    set @prefix = @type + ' ' + @name + ' AS '
    if substring(@definition, 1, datalength(@prefix) / 2) = @prefix  return substring(@definition, datalength(@prefix) / 2 + 1, len(@definition))

    return @definition
end
go
