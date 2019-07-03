# DBTools
Tool for comparing and updating MS-SQL Database schema.

Command:

* `export <database-sourcename> <output>`
* `compare-report <src1> <src2> <output>`
* `compare-diff <src1> <src2> <output>`
* `schema-create <src> <output>`
* `schema-update <src1> <src2> <output>`
* `code-update <src1> <src2> <output>`
* `code-grep <src1> <src2> <output>`

`database-sourcename: sql:<database-server>.<database-name>`  
`src[12]: <database-sourcename>|<filename>`
`output: <filename>`

## cmd: export 
Export database schema to xml file that can be used as input for other commands.

## cmd: compare-report
compare 2 schema and generate report what is changed.

## cmd: compare-diff
compare 2 schema and generate diff-report what is changed.

## cmd: schema-create 
Create a sql file to create a new empty database.

## cmd: schema-update
Create a sql file to update the database schema from src1 schema to src2 schema


## cmd: code-update
Create a sql file to patch the database code from src1 to src2 

## cmd: code-grep
Execute a regex grep over the code in the database.

