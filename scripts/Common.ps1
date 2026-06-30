# Shared helpers for the AcuPower.CustomizationTools PowerShell scripts.
# Dot-sourced by publish.ps1 and build-package.ps1.

function Resolve-AcuPowerLibrary {
    <#
    .SYNOPSIS
        Resolves the path to AcuPower.CustomizationTools.dll, defaulting to the
        Release build output relative to the repository root.
    #>
    [CmdletBinding()]
    param([string]$LibraryPath)

    if ([string]::IsNullOrWhiteSpace($LibraryPath)) {
        $repoRoot = Split-Path -Parent $PSScriptRoot
        $LibraryPath = Join-Path $repoRoot 'bin\Release\netstandard2.0\AcuPower.CustomizationTools.dll'
    }

    if (-not (Test-Path -LiteralPath $LibraryPath)) {
        throw @"
AcuPower.CustomizationTools.dll not found at:
    $LibraryPath
Build it first:
    dotnet build AcuPower.CustomizationTools.csproj -c Release
or pass an explicit -LibraryPath.
"@
    }

    (Resolve-Path -LiteralPath $LibraryPath).Path
}

function Import-AcuPowerAssembly {
    <#
    .SYNOPSIS
        Loads the library assembly (and its Newtonsoft.Json dependency) into the session.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$LibraryPath)

    $libDir = Split-Path -Parent $LibraryPath

    # The REST client depends on Newtonsoft.Json at runtime. Windows PowerShell's
    # assembly resolver does not probe the library's own folder, so load it explicitly.
    $newtonsoft = Join-Path $libDir 'Newtonsoft.Json.dll'
    if (Test-Path -LiteralPath $newtonsoft) {
        Add-Type -Path $newtonsoft
    }
    else {
        Write-Warning "Newtonsoft.Json.dll not found next to the library ('$libDir'). Import/publish (REST) calls may fail. Run 'dotnet build -c Release' to restore it."
    }

    Add-Type -Path $LibraryPath
}

function Write-PublishResult {
    <#
    .SYNOPSIS
        Prints a PublishEndResult log and returns $true when publishing succeeded.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory)]$Result)

    if ($null -eq $Result) {
        Write-Error 'No result returned from publish.'
        return $false
    }

    foreach ($entry in $Result.Log) {
        $line = '[{0}] {1}: {2}' -f $entry.Timestamp, $entry.LogType, $entry.Message
        switch ($entry.LogType) {
            'Error'   { Write-Host $line -ForegroundColor Red }
            'Warning' { Write-Host $line -ForegroundColor Yellow }
            default   { Write-Host $line }
        }
    }

    $ok = $Result.IsCompleted -and -not $Result.IsFailed
    if ($ok) {
        Write-Host 'Publish completed successfully.' -ForegroundColor Green
    }
    else {
        Write-Host ('Publish did not complete (IsCompleted={0}, IsFailed={1}).' -f $Result.IsCompleted, $Result.IsFailed) -ForegroundColor Red
    }
    return $ok
}
