#requires -Version 5.1
<#
.SYNOPSIS
    Imports and publishes an Acumatica customization, polling until publishing completes.

.DESCRIPTION
    Wraps AcuPower.CustomizationTools.PowerShell.AcumaticaCst. Two modes:
      -ZipPath    publish an already-built .zip package.
      -SourceDir  build a package from an exported customization source folder, then publish.

    The script loads the compiled library (and Newtonsoft.Json) via Common.ps1, runs the
    import + publish, prints the publish log, and exits 1 if publishing did not complete.

.PARAMETER BaseUrl
    Acumatica instance base URL, e.g. https://mysite.acumatica.com/MyInstance.

.PARAMETER User
    Login user name.

.PARAMETER Password
    Login password. Falls back to $env:ACU_PASSWORD when omitted (handy for CI).

.PARAMETER ProjectName
    Customization project name to import/publish as.

.PARAMETER ZipPath
    Path to an existing .zip package (Zip mode).

.PARAMETER SourceDir
    Path to an exported customization source folder containing a _project\ directory (Source mode).

.PARAMETER ProductVersion
    Acumatica product version stamped into project.xml (Source mode). Default 25.201.

.PARAMETER CustomizationDllPath
    Optional path to the main customization assembly to place under Bin\ (Source mode).

.PARAMETER AdditionalBinFiles
    Optional extra files to place under Bin\ by file name (Source mode).

.PARAMETER Company
    Optional company/tenant name. Required for multi-tenant instances.

.PARAMETER IgnoreSsl
    Ignore SSL certificate errors. Default $true. Pass -IgnoreSsl:$false to enforce validation.

.PARAMETER LibraryPath
    Optional explicit path to AcuPower.CustomizationTools.dll. Defaults to the repo Release build.

.EXAMPLE
    .\publish.ps1 -BaseUrl https://site/Inst -User admin -Password pass -ProjectName MyProj `
        -ZipPath .\MyCustomization.zip -Company MyCompany

.EXAMPLE
    .\publish.ps1 -BaseUrl https://site/Inst -User admin -Password pass -ProjectName MyProj `
        -SourceDir C:\exports\MyCustomization -ProductVersion 25.201 -CustomizationDllPath C:\build\MyExtension.dll
#>
[CmdletBinding(DefaultParameterSetName = 'Zip')]
param(
    [Parameter(Mandatory)][string]$BaseUrl,
    [Parameter(Mandatory)][string]$User,
    [string]$Password = $env:ACU_PASSWORD,
    [Parameter(Mandatory)][string]$ProjectName,

    [Parameter(Mandatory, ParameterSetName = 'Zip')]
    [string]$ZipPath,

    [Parameter(Mandatory, ParameterSetName = 'Source')]
    [string]$SourceDir,
    [Parameter(ParameterSetName = 'Source')][string]$ProductVersion = '25.201',
    [Parameter(ParameterSetName = 'Source')][string]$CustomizationDllPath,
    [Parameter(ParameterSetName = 'Source')][string[]]$AdditionalBinFiles,

    [string]$Company,
    [bool]$IgnoreSsl = $true,
    [string]$LibraryPath
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Common.ps1')

if ([string]::IsNullOrEmpty($Password)) {
    throw 'No password provided. Pass -Password or set $env:ACU_PASSWORD.'
}

$lib = Resolve-AcuPowerLibrary -LibraryPath $LibraryPath
Import-AcuPowerAssembly -LibraryPath $lib
$cst = [AcuPower.CustomizationTools.PowerShell.AcumaticaCst]

if ($PSCmdlet.ParameterSetName -eq 'Zip') {
    if (-not (Test-Path -LiteralPath $ZipPath)) { throw "Zip package not found: '$ZipPath'." }
    $ZipPath = (Resolve-Path -LiteralPath $ZipPath).Path
    Write-Host "Publishing '$ProjectName' from '$ZipPath' to $BaseUrl ..."
    $result = $cst::ImportAndPublish($BaseUrl, $User, $Password, $ProjectName, $ZipPath, $IgnoreSsl, $Company)
}
else {
    if (-not (Test-Path -LiteralPath $SourceDir)) { throw "Source directory not found: '$SourceDir'." }
    $SourceDir = (Resolve-Path -LiteralPath $SourceDir).Path
    $dll   = if ($CustomizationDllPath) { (Resolve-Path -LiteralPath $CustomizationDllPath).Path } else { $null }
    $extra = if ($AdditionalBinFiles)   { [string[]]$AdditionalBinFiles } else { $null }
    Write-Host "Building from source '$SourceDir' and publishing '$ProjectName' to $BaseUrl ..."
    $result = $cst::ImportAndPublishCustomizationSource(
        $BaseUrl, $User, $Password, $ProjectName, $SourceDir,
        $ProductVersion, $dll, $extra, $IgnoreSsl, $Company)
}

$ok = Write-PublishResult -Result $result
if (-not $ok) { exit 1 }
