#requires -Version 5.1
<#
.SYNOPSIS
    Builds an Acumatica customization .zip package (without publishing).

.DESCRIPTION
    Wraps AcuPower.CustomizationTools.PowerShell.AcumaticaCst. Three modes:
      -DllPath        minimal package containing a single DLL under Bin\ (CreateDllPackage).
      -ProjectXmlPath package from an existing project.xml plus an optional files directory (BuildPackage).
      -SourceDir      package from an exported customization source folder (BuildPackageFromCustomizationSource).

    The resulting ZIP bytes are written to -OutputZip.

.PARAMETER OutputZip
    Path of the .zip file to write.

.PARAMETER DllPath
    Path to a single DLL to package under Bin\ (Dll mode).

.PARAMETER Description
    Project description for the DLL-only package (Dll mode). Default 'Customization'.

.PARAMETER ProjectXmlPath
    Path to an existing project.xml (ProjectXml mode).

.PARAMETER FilesDirectory
    Optional directory of associated files to include (ProjectXml mode).

.PARAMETER SourceDir
    Path to an exported customization source folder containing a _project\ directory (Source mode).

.PARAMETER CustomizationDllPath
    Optional path to the main customization assembly to place under Bin\ (Source mode).

.PARAMETER AdditionalBinFiles
    Optional extra files to place under Bin\ by file name (Source mode).

.PARAMETER ProductVersion
    Acumatica product version stamped into project.xml. Default 25.201.

.PARAMETER LibraryPath
    Optional explicit path to AcuPower.CustomizationTools.dll. Defaults to the repo Release build.

.EXAMPLE
    .\build-package.ps1 -DllPath C:\build\MyExtension.dll -Description "My Extension" -OutputZip .\MyCustomization.zip

.EXAMPLE
    .\build-package.ps1 -SourceDir C:\exports\MyCustomization -ProductVersion 25.201 `
        -CustomizationDllPath C:\build\MyExtension.dll -OutputZip .\MyCustomization.zip

.EXAMPLE
    .\build-package.ps1 -ProjectXmlPath .\project.xml -FilesDirectory .\files -OutputZip .\MyCustomization.zip
#>
[CmdletBinding(DefaultParameterSetName = 'Dll')]
param(
    [Parameter(Mandatory)][string]$OutputZip,

    [Parameter(Mandatory, ParameterSetName = 'Dll')][string]$DllPath,
    [Parameter(ParameterSetName = 'Dll')][string]$Description = 'Customization',

    [Parameter(Mandatory, ParameterSetName = 'ProjectXml')][string]$ProjectXmlPath,
    [Parameter(ParameterSetName = 'ProjectXml')][string]$FilesDirectory,

    [Parameter(Mandatory, ParameterSetName = 'Source')][string]$SourceDir,
    [Parameter(ParameterSetName = 'Source')][string]$CustomizationDllPath,
    [Parameter(ParameterSetName = 'Source')][string[]]$AdditionalBinFiles,

    [string]$ProductVersion = '25.201',
    [string]$LibraryPath
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')

$lib = Resolve-AcuPowerLibrary -LibraryPath $LibraryPath
Import-AcuPowerAssembly -LibraryPath $lib
$cst = [AcuPower.CustomizationTools.PowerShell.AcumaticaCst]

switch ($PSCmdlet.ParameterSetName) {
    'Dll' {
        if (-not (Test-Path -LiteralPath $DllPath)) { throw "DLL not found: '$DllPath'." }
        $DllPath = (Resolve-Path -LiteralPath $DllPath).Path
        Write-Host "Building DLL-only package from '$DllPath' ..."
        $bytes = $cst::CreateDllPackage($Description, $DllPath, $ProductVersion)
    }
    'ProjectXml' {
        if (-not (Test-Path -LiteralPath $ProjectXmlPath)) { throw "project.xml not found: '$ProjectXmlPath'." }
        $ProjectXmlPath = (Resolve-Path -LiteralPath $ProjectXmlPath).Path
        $filesDir = if ($FilesDirectory) { (Resolve-Path -LiteralPath $FilesDirectory).Path } else { $null }
        Write-Host "Building package from '$ProjectXmlPath' ..."
        $bytes = $cst::BuildPackage($ProjectXmlPath, $filesDir)
    }
    'Source' {
        if (-not (Test-Path -LiteralPath $SourceDir)) { throw "Source directory not found: '$SourceDir'." }
        $SourceDir = (Resolve-Path -LiteralPath $SourceDir).Path
        $dll   = if ($CustomizationDllPath) { (Resolve-Path -LiteralPath $CustomizationDllPath).Path } else { $null }
        $extra = if ($AdditionalBinFiles)   { [string[]]$AdditionalBinFiles } else { $null }
        Write-Host "Building package from source '$SourceDir' ..."
        $bytes = $cst::BuildPackageFromCustomizationSource($SourceDir, $ProductVersion, $dll, $extra)
    }
}

if (-not [System.IO.Path]::IsPathRooted($OutputZip)) {
    $OutputZip = Join-Path (Get-Location).Path $OutputZip
}
$OutputZip = [System.IO.Path]::GetFullPath($OutputZip)

$outDir = Split-Path -Parent $OutputZip
if ($outDir -and -not (Test-Path -LiteralPath $outDir)) {
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null
}

[System.IO.File]::WriteAllBytes($OutputZip, $bytes)
Write-Host ("Wrote {0:N0} bytes to '{1}'." -f $bytes.Length, $OutputZip) -ForegroundColor Green
