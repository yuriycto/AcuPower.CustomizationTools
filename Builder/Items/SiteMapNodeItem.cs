using System;

namespace AcuPower.CustomizationTools.Builder.Items
{
    /// <summary>
    /// Helper for generating SiteMapNode XML in the data-set format used by Acumatica.
    /// Supports Acumatica 2025 R2+ / 2026 R1 Modern UI workspace integration.
    /// </summary>
    public static class SiteMapNodeItem
    {
        private const string DefaultCreatedBy = "B5344897-037E-4D58-B5C3-1BDFD0F47BF9";
        private const string DefaultScreenId = "SM209900";
        private const string DefaultTimestamp = "1900-01-01 00:00:00.000";
        private const string DefaultCompanyMask = "0xAA";

        /// <summary>
        /// Returns a complete SiteMapNode XML fragment in data-set format.
        /// </summary>
        /// <param name="screenId">Screen ID (e.g. "AW501000"). Must be 8 chars, no dots.</param>
        /// <param name="title">Display title for the sitemap entry.</param>
        /// <param name="url">URL path (e.g. "~/Pages/AW/AW501000.aspx").</param>
        /// <param name="graphType">Optional graph type name for the screen.</param>
        /// <param name="selectedUI">"T"=Modern only, "E"=Classic only, "D"=Default (both). Default "D".</param>
        /// <param name="workspaceId">GUID of the target MUI workspace. When set, an MUIScreen row is generated.</param>
        /// <param name="parentId">GUID of the parent SiteMap node. Null for root-level nodes.</param>
        public static string ToXml(
            string screenId,
            string title,
            string url,
            string graphType = null,
            string selectedUI = "D",
            string workspaceId = null,
            string parentId = null,
            string nodeId = null)
        {
            // Stable NodeID (when provided) keeps re-publishes updating the same SiteMap row
            // instead of creating duplicates, and lets a matching ScreenWithRights target it.
            nodeId = string.IsNullOrEmpty(nodeId) ? Guid.NewGuid().ToString("D") : nodeId;
            bool hasMui = !string.IsNullOrEmpty(workspaceId);

            // Build optional attributes
            var graphAttr = !string.IsNullOrEmpty(graphType)
                ? $" GraphType=\"{Esc(graphType)}\""
                : "";
            var parentAttr = !string.IsNullOrEmpty(parentId)
                ? $" ParentID=\"{{{Esc(parentId)}}}\""
                : "";
            var urlBackupAttr = selectedUI == "T" && url != null && url.Contains(".aspx")
                ? $" UrlBackup=\"{Esc(url)}\""
                : "";

            // Relations section — full version for MUI support
            string relations;
            string layout;

            if (hasMui)
            {
                relations =
                    "<relations version=\"2\" relations-version=\"2\" main-table=\"SiteMap\">" +
                    "<link from=\"MUIScreen (NodeID)\" to=\"SiteMap (NodeID)\" type=\"Weak\" />" +
                    "<link from=\"MUIWorkspace (WorkspaceID)\" to=\"MUIScreen (WorkspaceID)\" type=\"FromMaster\" linkname=\"workspaceToScreen\" split-location=\"yes\" updateable=\"true\" />" +
                    "<link from=\"MUISubcategory (SubcategoryID)\" to=\"MUIScreen (SubcategoryID)\" type=\"FromMaster\" updateable=\"true\" />" +
                    "<link from=\"MUITile (ScreenID)\" to=\"SiteMap (ScreenID)\" type=\"Weak\" />" +
                    "<link from=\"MUIWorkspace (WorkspaceID)\" to=\"MUITile (WorkspaceID)\" type=\"FromMaster\" linkname=\"workspaceToTile\" split-location=\"yes\" updateable=\"true\" />" +
                    "<link from=\"MUIArea (AreaID)\" to=\"MUIWorkspace (AreaID)\" type=\"FromMaster\" updateable=\"true\" />" +
                    "<link from=\"MUIPinnedScreen (NodeID, WorkspaceID)\" to=\"MUIScreen (NodeID, WorkspaceID)\" type=\"WeakIfEmpty\" isEmpty=\"Username\" />" +
                    "<link from=\"MUIFavoriteWorkspace (WorkspaceID)\" to=\"MUIWorkspace (WorkspaceID)\" type=\"WeakIfEmpty\" isEmpty=\"Username\" />" +
                    "</relations>";

                layout =
                    "<layout>" +
                    "<table name=\"SiteMap\">" +
                    "<table name=\"MUIScreen\" uplink=\"(NodeID) = (NodeID)\">" +
                    "<table name=\"MUIPinnedScreen\" uplink=\"(NodeID, WorkspaceID) = (NodeID, WorkspaceID)\" />" +
                    "</table>" +
                    "<table name=\"MUITile\" uplink=\"(ScreenID) = (ScreenID)\" />" +
                    "</table>" +
                    "</layout>";
            }
            else
            {
                // Simplified format for backward compatibility (no MUI workspace)
                relations = "<relations format-version=\"4\" relations-version=\"20240201\" main-table=\"SiteMap\" />";
                layout = "<layout><table name=\"SiteMap\" /></layout>";
            }

            // SiteMap row
            var siteMapRow =
                $"<row NodeID=\"{{{nodeId}}}\" ScreenID=\"{Esc(screenId)}\" " +
                $"Title=\"{Esc(title)}\" Url=\"{Esc(url)}\"{graphAttr}{parentAttr}" +
                $" SelectedUI=\"{Esc(selectedUI)}\"" +
                urlBackupAttr +
                $" CompanyMask=\"{DefaultCompanyMask}\"" +
                $" CreatedByID=\"{DefaultCreatedBy}\"" +
                $" CreatedByScreenID=\"{DefaultScreenId}\"" +
                $" CreatedDateTime=\"{DefaultTimestamp}\"" +
                $" LastModifiedByID=\"{DefaultCreatedBy}\"" +
                $" LastModifiedByScreenID=\"{DefaultScreenId}\"" +
                $" LastModifiedDateTime=\"{DefaultTimestamp}\"";

            // MUIScreen child row (inside SiteMap row)
            string muiScreenRow = "";
            if (hasMui)
            {
                muiScreenRow =
                    ">" +
                    "<MUIScreen NodeID=\"{" + nodeId + "}\" " +
                    $"WorkspaceID=\"{{{Esc(workspaceId)}}}\" " +
                    "IsPortal=\"false\" " +
                    "Order=\"50.0\" " +
                    $"CompanyMask=\"{DefaultCompanyMask}\" " +
                    $"CreatedByID=\"{DefaultCreatedBy}\" " +
                    $"CreatedByScreenID=\"{DefaultScreenId}\" " +
                    $"CreatedDateTime=\"{DefaultTimestamp}\" " +
                    $"LastModifiedByID=\"{DefaultCreatedBy}\" " +
                    $"LastModifiedByScreenID=\"{DefaultScreenId}\" " +
                    $"LastModifiedDateTime=\"{DefaultTimestamp}\" />" +
                    "</row>";
            }
            else
            {
                muiScreenRow = " />";
            }

            return "<SiteMapNode>" +
                   "<data-set>" +
                   relations +
                   layout +
                   "<data>" +
                   "<SiteMap>" +
                   siteMapRow + muiScreenRow +
                   "</SiteMap>" +
                   "</data>" +
                   "</data-set>" +
                   "</SiteMapNode>";
        }

        private static string Esc(string value)
        {
            if (value == null) return string.Empty;
            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }
    }
}
