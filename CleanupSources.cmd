@ echo off

cd /d %~dp0

SourceCleaner --optimize=true --encoding=utf8-bom --eol=nl **\*.csproj **\*.cs                                         !.vs\**\* !**\bin\**\* !**\obj\**\* !output\**\* !contrib\jannesen\typescript\**\* !contrib\node_modules\**\*
SourceCleaner --optimize=true --encoding=utf8     --eol=nl **\*.ts **\*.tsx **\*.txt **\*.md **\*.json **\*.html       !.vs\**\* !**\bin\**\* !**\obj\**\* !output\**\* !contrib\jannesen\typescript\**\* !contrib\node_modules\**\*
SourceCleaner --optimize=true --encoding=ascii    --eol=nl **\*.scss **\.editorconfig **\.gitattributes **\.gitignore  !.vs\**\* !**\bin\**\* !**\obj\**\* !output\**\* !contrib\jannesen\typescript\**\* !contrib\node_modules\**\*

pause