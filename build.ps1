$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
Set-StrictMode -Version Latest

$dotnet='C:\Program Files\dotnet\dotnet.exe'

if (-not (Test-Path -Path $dotnet  -PathType Leaf)) { throw "Can't find dotnet"  }

function Cleanup() {
    Write-Host '# cleanup'

    foreach ($proj in (Get-ChildItem -Path . -Recurse -File -Filter "*.csproj")) {
        foreach ($dir in @("obj", "bin")) {
            $path = Join-Path $proj.DirectoryName $dir

            if (Test-Path $path -PathType Container) {
                Write-Output "    delete ${path}"
                Remove-Item -Path $path -Recurse -Force
            }
        }
    }

    if (Test-Path -Path 'output' -PathType Container) {
        Write-Host '    clean output'
        Remove-Item -Path 'output' -Recurse -Force
        New-Item -Path 'output' -ItemType Directory | Out-Null
    }
}

function Cmd-Publish() {
    Cleanup

    Write-Host '# dotnet publish win-x64'
    & $dotnet publish DBTools.slnx --verbosity minimal -c Release -r win-x64 /p:DebugSymbols=false
}

if ($args.Length -eq 0) {
    Write-Host "SYNTAX: builder publish|cleanup"
    Exit 1
}

$argn = 0

Set-Location $PSScriptRoot

Write-Host "# Work in: $(Get-Location)"

while ($argn -lt $args.Length) {
    switch($args[$argn]) {
    "publish" {
            Cmd-Publish
            $argn = $argn + 1
        }

    "cleanup" {
            Cleanup
            $argn = $argn + 1
        }        

    default {
		    Write-Host "Unknown cmd " $args[$argn]
		    Exit 1
	    }
    }
}
