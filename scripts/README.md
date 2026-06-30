# PowerShell scripts

Ready-to-run wrappers around the `AcumaticaCst` helpers for building and publishing
customization packages from the command line / CI.

| Script | Purpose |
|--------|---------|
| `build-package.ps1` | Build a `.zip` package (DLL-only, from `project.xml`, or from an exported source folder) |
| `publish.ps1` | Import + publish a package to an Acumatica instance (from a `.zip` or from an exported source folder) |
| `Common.ps1` | Shared loader (`Add-Type` the library + Newtonsoft.Json) and result printer — dot-sourced by the others |

## Prerequisites

Build the library first so the scripts can load it (they default to the Release output):

```powershell
dotnet build AcuPower.CustomizationTools.csproj -c Release
```

`Newtonsoft.Json.dll` is copied next to the library by the build and is loaded automatically.
Use a different DLL location by passing `-LibraryPath`.

If script execution is blocked, run them with:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish.ps1 ...
```

## build-package.ps1

```powershell
# Minimal DLL-only package
.\scripts\build-package.ps1 -DllPath C:\build\MyExtension.dll -Description "My Extension" -OutputZip .\MyCustomization.zip

# From an exported customization source folder
.\scripts\build-package.ps1 -SourceDir C:\exports\MyCustomization -ProductVersion 25.201 `
    -CustomizationDllPath C:\build\MyExtension.dll -OutputZip .\MyCustomization.zip

# From a project.xml plus a files directory
.\scripts\build-package.ps1 -ProjectXmlPath .\project.xml -FilesDirectory .\files -OutputZip .\MyCustomization.zip
```

## publish.ps1

```powershell
# Publish an existing .zip
.\scripts\publish.ps1 -BaseUrl https://mysite.acumatica.com/MyInstance -User admin -Password pass `
    -ProjectName MyProject -ZipPath .\MyCustomization.zip -Company MyCompany

# Build from an exported source folder and publish in one step
.\scripts\publish.ps1 -BaseUrl https://mysite.acumatica.com/MyInstance -User admin -Password pass `
    -ProjectName MyProject -SourceDir C:\exports\MyCustomization -CustomizationDllPath C:\build\MyExtension.dll
```

The script prints the publish log and exits with code `1` if publishing did not complete,
so it fails the build step in CI.

### Notes

- **Password:** prefer not to pass `-Password` on the command line. Omit it and set
  `$env:ACU_PASSWORD` instead (the script reads it as a fallback).
- **SSL:** `-IgnoreSsl` defaults to `$true`. Pass `-IgnoreSsl:$false` to enforce certificate validation.
- **Self-signed / dev instances:** keep the default `-IgnoreSsl`.
