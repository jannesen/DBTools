@ echo off

cd /d %~dp0

SourceCleaner --optimize=true --encoding=utf8-bom --eol=nl **\\*.csproj **\\*.cs  !**\\bin\\**\\* !**\\obj\\**\\* !**\\.vs\\**\\* !**\\.git\\**\\* !**\\output\\**\\*
SourceCleaner --optimize=true --encoding=utf8     --eol=nl **\\*.txt **\\*.md     !**\\bin\\**\\* !**\\obj\\**\\* !**\\.vs\\**\\* !**\\.git\\**\\* !**\\output\\**\\*
SourceCleaner --optimize=true --encoding=utf8     --eol=nl **\\*.sql              !**\\bin\\**\\* !**\\obj\\**\\* !**\\.vs\\**\\* !**\\.git\\**\\* !**\\output\\**\\*
SourceCleaner --optimize=true --encoding=ascii    --eol=nl **\\.editorconfig **\\.gitattributes **\\.gitignore

pause