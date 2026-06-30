using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AcuPower.CustomizationTools.Builder.Items;
using AcuPower.CustomizationTools.Package;

namespace AcuPower.CustomizationTools.Builder
{
    /// <summary>
    /// Fluent builder that constructs an Acumatica customization project.xml and ZIP package.
    /// </summary>
    public class CustomizationProjectBuilder
    {
        private readonly string _description;
        private readonly string _productVersion;

        private readonly List<string> _fileElements = new List<string>();
        private readonly List<string> _pageElements = new List<string>();
        private readonly List<string> _screenElements = new List<string>();
        private readonly List<string> _sqlElements = new List<string>();
        private readonly List<string> _siteMapNodeElements = new List<string>();
        private readonly List<string> _screenWithRightsElements = new List<string>();
        private readonly List<string> _reportElements = new List<string>();
        private readonly List<string> _perTenantFileElements = new List<string>();
        private readonly List<string> _dashboardElements = new List<string>();
        private readonly List<string> _wikiElements = new List<string>();
        private readonly List<string> _genericInquiryElements = new List<string>();
        private readonly List<string> _webhookElements = new List<string>();

        // Binary file contents keyed by their relative path in the ZIP
        private readonly Dictionary<string, byte[]> _fileContents = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Creates a new customization project builder.
        /// </summary>
        /// <param name="description">Project description.</param>
        /// <param name="productVersion">Acumatica product version (default "25.201").</param>
        public CustomizationProjectBuilder(string description, string productVersion = "25.201")
        {
            _description = description ?? string.Empty;
            _productVersion = productVersion ?? "25.201";
        }

        #region AddFile

        /// <summary>
        /// Adds a file entry to the project. If content is provided, it is included in the ZIP.
        /// </summary>
        /// <param name="appRelativePath">App-relative path (e.g. "Bin\MyDll.dll").</param>
        /// <param name="content">Optional file content bytes.</param>
        /// <returns>This builder for fluent chaining.</returns>
        public CustomizationProjectBuilder AddFile(string appRelativePath, byte[] content = null)
        {
            _fileElements.Add(FileItem.ToXml(appRelativePath));
            if (content != null)
            {
                _fileContents[appRelativePath] = content;
            }
            return this;
        }

        /// <summary>
        /// Adds a file entry, reading content from a disk path.
        /// </summary>
        /// <param name="appRelativePath">App-relative path in the project.</param>
        /// <param name="diskFilePath">Path to read file content from on disk.</param>
        /// <returns>This builder for fluent chaining.</returns>
        public CustomizationProjectBuilder AddFile(string appRelativePath, string diskFilePath)
        {
            var content = File.ReadAllBytes(diskFilePath);
            return AddFile(appRelativePath, content);
        }

        #endregion

        #region AddPage

        /// <summary>
        /// Adds an ASPX page to the project with raw-deflate-encoded page source.
        /// </summary>
        /// <param name="aspxVirtualPath">Virtual path (e.g. "~/Pages/AW/AW501000.aspx").</param>
        /// <param name="aspxContent">Raw ASPX content.</param>
        /// <returns>This builder for fluent chaining.</returns>
        public CustomizationProjectBuilder AddPage(string aspxVirtualPath, string aspxContent)
        {
            _pageElements.Add(PageItem.ToXml(aspxVirtualPath, aspxContent));
            return this;
        }

        #endregion

        #region AddScreen

        /// <summary>
        /// Adds a screen reference to the project.
        /// </summary>
        /// <param name="screenId">Screen ID (e.g. "AW501000").</param>
        /// <returns>This builder for fluent chaining.</returns>
        public CustomizationProjectBuilder AddScreen(string screenId)
        {
            _screenElements.Add(ScreenItem.ToXml(screenId));
            return this;
        }

        #endregion

        #region AddModernUiFile

        /// <summary>
        /// Adds a Modern UI per-tenant file to the project.
        /// </summary>
        /// <param name="relativePath">App-relative path (e.g. "screens\AW\AW501000\AW501000.ts").</param>
        /// <param name="screenId">Screen ID (e.g. "AW501000").</param>
        /// <param name="content">File content bytes.</param>
        /// <returns>This builder for fluent chaining.</returns>
        public CustomizationProjectBuilder AddModernUiFile(string relativePath, string screenId, byte[] content)
        {
            _perTenantFileElements.Add(PerTenantFileItem.ToXml(relativePath, screenId));
            if (content != null)
            {
                _fileContents[relativePath] = content;
            }
            return this;
        }

        #endregion

        #region AddSql

        /// <summary>
        /// Adds a SQL table schema entry to the project.
        /// </summary>
        /// <param name="tableName">Database table name.</param>
        /// <param name="tableSchemaXml">Table schema XML content.</param>
        /// <returns>This builder for fluent chaining.</returns>
        public CustomizationProjectBuilder AddSqlTableSchema(string tableName, string tableSchemaXml)
        {
            _sqlElements.Add(SqlItem.ToTableSchemaXml(tableName, tableSchemaXml));
            return this;
        }

        /// <summary>
        /// Adds a SQL custom script entry to the project.
        /// </summary>
        /// <param name="tableName">Database table name.</param>
        /// <param name="sqlScript">Custom SQL script content.</param>
        /// <returns>This builder for fluent chaining.</returns>
        public CustomizationProjectBuilder AddSqlCustomScript(string tableName, string sqlScript)
        {
            _sqlElements.Add(SqlItem.ToCustomScriptXml(tableName, sqlScript));
            return this;
        }

        #endregion

        #region AddSiteMapNode

        /// <summary>
        /// Adds a SiteMapNode entry in data-set format.
        /// </summary>
        /// <param name="screenId">Screen ID (8 chars, no dots).</param>
        /// <param name="title">Display title.</param>
        /// <param name="url">URL path (e.g. "~/Pages/AW/AW501000.aspx").</param>
        /// <param name="graphType">Optional graph type name.</param>
        /// <param name="selectedUI">"T"=Modern only, "E"=Classic only, "D"=Default (both). Default "D".</param>
        /// <param name="workspaceId">GUID of the target MUI workspace. When set, an MUIScreen row is generated for Modern UI menu integration.</param>
        /// <param name="parentId">GUID of the parent SiteMap node. Null for root-level nodes.</param>
        /// <returns>This builder for fluent chaining.</returns>
        public CustomizationProjectBuilder AddSiteMapNode(
            string screenId, string title, string url,
            string graphType = null, string selectedUI = "D",
            string workspaceId = null, string parentId = null)
        {
            _siteMapNodeElements.Add(SiteMapNodeItem.ToXml(
                screenId, title, url, graphType, selectedUI, workspaceId, parentId));
            return this;
        }

        #endregion

        #region AddAccessRights

        /// <summary>
        /// Adds a ScreenWithRights entry in data-set format.
        /// </summary>
        /// <param name="screenId">Screen ID.</param>
        /// <param name="title">Display title.</param>
        /// <param name="url">URL path.</param>
        /// <param name="graphType">Optional graph type.</param>
        /// <param name="mergeRule">Merge rule (default "GrantAll").</param>
        /// <returns>This builder for fluent chaining.</returns>
        public CustomizationProjectBuilder AddAccessRights(string screenId, string title, string url, string graphType = null, string mergeRule = "GrantAll")
        {
            _screenWithRightsElements.Add(ScreenWithRightsItem.ToXml(screenId, title, url, graphType, mergeRule));
            return this;
        }

        #endregion

        #region AddReport

        /// <summary>
        /// Adds a report with inline RPX XML content.
        /// </summary>
        /// <param name="reportFileName">Report file name (e.g. "AW501000.rpx").</param>
        /// <param name="rpxXmlContent">RPX XML content string.</param>
        /// <returns>This builder for fluent chaining.</returns>
        public CustomizationProjectBuilder AddReport(string reportFileName, string rpxXmlContent)
        {
            _reportElements.Add(ReportItem.ToXml(reportFileName, rpxXmlContent));
            return this;
        }

        /// <summary>
        /// Adds a report from raw RPX bytes.
        /// </summary>
        /// <param name="reportFileName">Report file name.</param>
        /// <param name="rpxContent">RPX content as bytes.</param>
        /// <returns>This builder for fluent chaining.</returns>
        public CustomizationProjectBuilder AddReport(string reportFileName, byte[] rpxContent)
        {
            _reportElements.Add(ReportItem.ToXml(reportFileName, rpxContent));
            return this;
        }

        #endregion

        #region AddWebhook

        /// <summary>
        /// Adds a webhook definition to the customization project.
        /// </summary>
        /// <param name="name">Webhook display name.</param>
        /// <param name="handler">Fully qualified webhook handler type name.</param>
        /// <param name="webHookId">Optional webhook identifier. A new GUID is generated when omitted.</param>
        /// <param name="isActive">Whether the webhook is active.</param>
        /// <param name="isSystem">Whether the webhook is marked as system-owned.</param>
        /// <param name="requestLogLevel">Request log level value used by Acumatica.</param>
        /// <param name="requestRetainCount">How many webhook requests to retain.</param>
        /// <param name="noteId">Optional note identifier. A new GUID is generated when omitted.</param>
        /// <returns>This builder for fluent chaining.</returns>
        public CustomizationProjectBuilder AddWebhook(
            string name,
            string handler,
            string webHookId = null,
            bool isActive = true,
            bool isSystem = false,
            int requestLogLevel = 2,
            int requestRetainCount = 100,
            string noteId = null)
        {
            _webhookElements.Add(WebhookItem.ToXml(
                name,
                handler,
                webHookId,
                isActive,
                isSystem,
                requestLogLevel,
                requestRetainCount,
                noteId));
            return this;
        }

        #endregion

        #region Stubs

        /// <summary>
        /// Adds a dashboard placeholder entry.
        /// </summary>
        /// <param name="dashboardScreenId">Dashboard screen ID.</param>
        /// <returns>This builder for fluent chaining.</returns>
        public CustomizationProjectBuilder AddDashboard(string dashboardScreenId)
        {
            _dashboardElements.Add(DashboardItem.ToXml(dashboardScreenId));
            return this;
        }

        /// <summary>
        /// Adds a wiki article placeholder entry.
        /// </summary>
        /// <param name="wikiArticleName">Wiki article name.</param>
        /// <returns>This builder for fluent chaining.</returns>
        public CustomizationProjectBuilder AddWiki(string wikiArticleName)
        {
            _wikiElements.Add(WikiItem.ToXml(wikiArticleName));
            return this;
        }

        /// <summary>
        /// Adds a Generic Inquiry placeholder entry.
        /// </summary>
        /// <param name="giName">Generic Inquiry name.</param>
        /// <returns>This builder for fluent chaining.</returns>
        public CustomizationProjectBuilder AddGenericInquiry(string giName)
        {
            _genericInquiryElements.Add(GenericInquiryItem.ToXml(giName));
            return this;
        }

        #endregion

        #region Build

        /// <summary>
        /// Builds the project.xml content string.
        /// </summary>
        /// <returns>Complete project.xml XML content.</returns>
        public string BuildProjectXml()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine($"<project description=\"{EscapeXml(_description)}\" productVersion=\"{EscapeXml(_productVersion)}\">");

            AppendSection(sb, _fileElements);
            AppendSection(sb, _pageElements);
            AppendSection(sb, _screenElements);
            AppendSection(sb, _perTenantFileElements);
            AppendSection(sb, _sqlElements);
            AppendSection(sb, _siteMapNodeElements);
            AppendSection(sb, _screenWithRightsElements);
            AppendSection(sb, _reportElements);
            AppendSection(sb, _dashboardElements);
            AppendSection(sb, _wikiElements);
            AppendSection(sb, _genericInquiryElements);
            AppendSection(sb, _webhookElements);

            sb.AppendLine("</project>");
            return sb.ToString();
        }

        /// <summary>
        /// Builds the complete customization package ZIP containing project.xml and all binary files.
        /// </summary>
        /// <returns>ZIP file bytes.</returns>
        public byte[] BuildPackageZip()
        {
            var projectXml = BuildProjectXml();
            return CustomizationPackageBuilder.Build(projectXml, _fileContents);
        }

        #endregion

        #region Private Helpers

        private static void AppendSection(StringBuilder sb, List<string> elements)
        {
            foreach (var element in elements)
            {
                sb.Append("  ");
                sb.AppendLine(element);
            }
        }

        private static string EscapeXml(string value)
        {
            if (value == null) return string.Empty;
            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        #endregion
    }
}
