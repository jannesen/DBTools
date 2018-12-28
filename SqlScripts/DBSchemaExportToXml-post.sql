if exists (select * from sys.objects where [object_id] = object_id('dbo.[_jdbtools_sql_module_definition]') and [type] = 'FN')
    drop function dbo.[_jdbtools_sql_module_definition]
go
