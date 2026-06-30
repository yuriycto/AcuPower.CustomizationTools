# AcuPower.CustomizationTools

A .NET library for programmatically **building** and **deploying** Acumatica ERP customization packages. Use it from C# or PowerShell to automate customization workflows in CI/CD pipelines without relying on the Acumatica UI.

## Features

- **Fluent project builder** — construct `project.xml` and ZIP packages in code
- **Package assembly** — build ZIPs from scratch, from disk, or from exported customization source folders
- **REST client** — import, publish, delete, and query customization projects via Acumatica's CustomizationApi
- **SQL schema conversion** — translate `CREATE TABLE` DDL into Acumatica table schema XML
- **PowerShell helpers** — static wrappers for script-based automation

## Supported Customization Items

| Item | Description |
|------|-------------|
| `File` | File references (commonly compiled DLLs under `Bin\`); content is optional and not restricted to binaries |
| `Page` | ASPX pages with raw-deflate-encoded source |
| `Screen` | Screen references |
| `PerTenantFile` | Modern UI per-tenant files (TypeScript, etc.) |
| `Sql` | Table schemas and custom SQL scripts |
| `SiteMapNode` | Menu entries, including Modern UI workspace integration |
| `ScreenWithRights` | Access rights definitions |
| `Report` | RPX report content |
| `Webhook` | Webhook definitions |
| `Dashboard` | Dashboard placeholder entries |
| `Wiki` | Wiki article placeholder entries |
| `GenericInquiry` | Generic Inquiry placeholder entries |

Default Acumatica product version: **25.201**.

## Requirements

- .NET SDK (to build the library)
- Target framework: **.NET Standard 2.0**
- Runtime dependency: **Newtonsoft.Json 13.0.3** — restored automatically by `dotnet build`. When loading the DLL in PowerShell via `Add-Type`, `Newtonsoft.Json.dll` must be available alongside `AcuPower.CustomizationTools.dll`.
- Acumatica instance with CustomizationApi enabled (for deployment)

## Build

```powershell
dotnet build AcuPower.CustomizationTools.csproj -c Release
```

Output: `bin\Release\netstandard2.0\AcuPower.CustomizationTools.dll`

## Usage

### Build a customization package (C#)

```csharp
using AcuPower.CustomizationTools.Builder;
using System.IO;

var builder = new CustomizationProjectBuilder("My Customization Project");

// Add a compiled extension DLL
builder.AddFile(@"Bin\MyExtension.dll", File.ReadAllBytes(@"C:\build\MyExtension.dll"));

// Register a screen and menu entry
builder.AddScreen("AW501000");
builder.AddSiteMapNode(
    screenId: "AW501000",
    title: "My Screen",
    url: "~/Pages/AW/AW501000.aspx",
    graphType: "MyNamespace.MyGraph");

// Add access rights
builder.AddAccessRights("AW501000", "My Screen", "~/Pages/AW/AW501000.aspx");

byte[] packageZip = builder.BuildPackageZip();
File.WriteAllBytes("MyCustomization.zip", packageZip);
```

### Deploy to an Acumatica instance (C#)

```csharp
using AcuPower.CustomizationTools.Client;
using AcuPower.CustomizationTools.Models;

using var client = new AcumaticaCustomizationClient(
    "https://mysite.acumatica.com/MyInstance",
    ignoreSslErrors: true);

client.Login("admin", "password", company: "MyCompany");

var result = client.ImportAndPublish(
    projectName: "MyProject",
    projectDescription: "My Customization Project",
    projectLevel: 0,
    packageZipContent: packageZip,
    tenantMode: TenantMode.Current);

if (result.IsCompleted && !result.IsFailed)
    Console.WriteLine("Published successfully.");
else
    foreach (var entry in result.Log)
        Console.WriteLine($"{entry.LogType}: {entry.Message}");
```

> The deploy snippet uses `using var` (C# 8). On an older language version, rewrite it as a classic `using (...) { }` block.

### Build from an exported customization source folder

When you export a customization project from Acumatica, the source folder contains a `_project\` directory plus associated files (`Pages\`, `SUContent\`, etc.):

```csharp
using AcuPower.CustomizationTools.Package;

byte[] packageZip = CustomizationPackageBuilder.BuildFromCustomizationSource(
    customizationPackageDirectory: @"C:\exports\MyCustomization",
    productVersion: "25.201",
    customizationDllPath: @"C:\build\MyExtension.dll");
```

### Convert SQL DDL to table schema XML

```csharp
using AcuPower.CustomizationTools.Builder.Schema;

string schemaXml = TableSchemaBuilder.FromSqlDdl(@"
    CREATE TABLE [dbo].[MyTable] (
        [RecordID] int IDENTITY(1,1) NOT NULL,
        [Description] nvarchar(255) NULL,
        CONSTRAINT [PK_MyTable] PRIMARY KEY CLUSTERED ([RecordID])
    )");

var builder = new CustomizationProjectBuilder("Database Extension");
builder.AddSqlTableSchema("MyTable", schemaXml);
```

### PowerShell

Ready-to-run scripts are in [`scripts/`](scripts/) (`build-package.ps1`, `publish.ps1`) — see [scripts/README.md](scripts/README.md). To call the library directly, load the assembly and use the static helper class:

```powershell
Add-Type -Path ".\bin\Release\netstandard2.0\AcuPower.CustomizationTools.dll"

# Build a DLL-only package
$zip = [AcuPower.CustomizationTools.PowerShell.AcumaticaCst]::CreateDllPackage(
    "My Extension", "C:\build\MyExtension.dll")
[System.IO.File]::WriteAllBytes("MyCustomization.zip", $zip)

# Import and publish a package
$result = [AcuPower.CustomizationTools.PowerShell.AcumaticaCst]::ImportAndPublish(
    "https://mysite.acumatica.com/MyInstance",
    "admin",
    "password",
    "MyProject",
    "C:\packages\MyCustomization.zip",
    $true,          # ignoreSsl
    "MyCompany")    # company (optional)

$result.IsCompleted
$result.Log | ForEach-Object { "$($_.LogType): $($_.Message)" }

# Build from exported source and deploy in one step
$result = [AcuPower.CustomizationTools.PowerShell.AcumaticaCst]::ImportAndPublishCustomizationSource(
    "https://mysite.acumatica.com/MyInstance",
    "admin",
    "password",
    "MyProject",
    "C:\exports\MyCustomization",
    "25.201",
    "C:\build\MyExtension.dll")
```

> `Newtonsoft.Json.dll` (13.0.3) must be present next to `AcuPower.CustomizationTools.dll` for the REST/deploy calls above to run.

## API Reference

### `CustomizationProjectBuilder`

Fluent builder for `project.xml` content and ZIP packages.

| Method | Description |
|--------|-------------|
| `AddFile(appRelativePath, content)` | Add a file (content optional). Overload: `AddFile(appRelativePath, diskFilePath)` reads bytes from disk |
| `AddPage(aspxVirtualPath, aspxContent)` | Add an ASPX page |
| `AddScreen(screenId)` | Add a screen reference |
| `AddModernUiFile(relativePath, screenId, content)` | Add a Modern UI per-tenant file |
| `AddSqlTableSchema(tableName, tableSchemaXml)` | Add a table schema |
| `AddSqlCustomScript(tableName, sqlScript)` | Add a custom SQL script |
| `AddSiteMapNode(...)` | Add a SiteMap menu entry |
| `AddAccessRights(...)` | Add screen access rights |
| `AddReport(reportFileName, rpxXmlContent)` | Add an RPX report. Overload accepts `byte[] rpxContent` |
| `AddWebhook(...)` | Add a webhook definition |
| `AddDashboard(dashboardScreenId)` | Add a dashboard placeholder |
| `AddWiki(wikiArticleName)` | Add a wiki placeholder |
| `AddGenericInquiry(giName)` | Add a Generic Inquiry placeholder |
| `BuildProjectXml()` | Returns the `project.xml` string |
| `BuildPackageZip()` | Returns the complete ZIP as `byte[]` |

### `AcumaticaCustomizationClient`

REST client for Acumatica's CustomizationApi endpoints.

| Method | Description |
|--------|-------------|
| `Login(username, password, company?)` | Authenticate via `/entity/auth/login` |
| `Logout()` | End the session |
| `Import(...)` | Import a customization ZIP |
| `PublishBegin(...)` / `PublishEnd()` | Start publishing and poll status |
| `ImportAndPublish(...)` | Import, publish, and poll until complete |
| `GetProject(name)` | Download a project as ZIP bytes |
| `Delete(name)` | Delete a customization project |
| `UnpublishAll(tenantMode)` | Unpublish all projects |
| `GetPublished()` | List currently published projects |

`ImportAndPublish` handles app pool restarts during publishing with automatic retry logic (default: 15-second poll interval, 30-minute timeout).

### `CustomizationPackageBuilder`

Static helpers for assembling the package ZIP.

| Method | Description |
|--------|-------------|
| `Build(projectXml, files)` | Build a ZIP from a `project.xml` string and a path→bytes dictionary (from scratch) |
| `BuildFromDisk(projectXmlPath, filesDirectory)` | Build a ZIP from a `project.xml` file and a directory of files |
| `BuildFromCustomizationSource(customizationPackageDirectory, productVersion?, customizationDllPath?, additionalBinFiles?)` | Build a ZIP from an exported customization source folder |

### `TableSchemaBuilder`

| Method | Description |
|--------|-------------|
| `FromSqlDdl(createTableSql)` | Convert a `CREATE TABLE` statement to Acumatica table schema XML |

### `AcumaticaCst` (PowerShell)

| Method | Description |
|--------|-------------|
| `ImportAndPublish(...)` | Deploy a ZIP package |
| `BuildPackage(projectXmlPath, filesDirectory)` | Build ZIP from `project.xml` + files |
| `BuildPackageFromCustomizationSource(...)` | Build ZIP from exported source folder |
| `ImportAndPublishCustomizationSource(...)` | Build from source and deploy |
| `ConvertSqlToSchema(createTableSql)` | Convert DDL to schema XML |
| `CreateDllPackage(description, dllPath, productVersion?)` | Create a minimal DLL-only package |

## Project Structure

```
AcuPower.CustomizationTools/
├── Builder/
│   ├── CustomizationProjectBuilder.cs   # Fluent builder
│   ├── Items/                           # XML generators per item type
│   └── Schema/
│       └── TableSchemaBuilder.cs        # SQL DDL → schema XML
├── Client/
│   └── AcumaticaCustomizationClient.cs  # CustomizationApi REST client
├── Models/
│   └── LogEntry.cs                      # Import/publish result DTOs + TenantMode enum
├── Package/
│   └── CustomizationPackageBuilder.cs   # ZIP assembly
└── PowerShell/
    └── CmdletHelpers.cs                 # AcumaticaCst static helpers
```

## License

Not specified.
