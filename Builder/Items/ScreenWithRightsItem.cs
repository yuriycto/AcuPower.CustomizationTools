using System;

namespace AcuPower.CustomizationTools.Builder.Items
{
    /// <summary>
    /// Helper for generating ScreenWithRights XML in the data-set format used by Acumatica.
    /// </summary>
    public static class ScreenWithRightsItem
    {
        /// <summary>
        /// Returns a complete ScreenWithRights XML fragment in data-set format.
        /// </summary>
        /// <param name="screenId">Screen ID (e.g. "AW501000").</param>
        /// <param name="title">Display title for the screen.</param>
        /// <param name="url">URL path (e.g. "~/Pages/AW/AW501000.aspx").</param>
        /// <param name="graphType">Optional graph type name for the screen.</param>
        /// <param name="mergeRule">Merge rule for access rights (default "GrantAll").</param>
        public static string ToXml(string screenId, string title, string url, string graphType = null, string mergeRule = "GrantAll")
        {
            var nodeId = Guid.NewGuid().ToString("D");
            var graphAttr = !string.IsNullOrEmpty(graphType)
                ? $" GraphType=\"{EscapeXml(graphType)}\""
                : "";

            return "<ScreenWithRights>" +
                   "<data-set>" +
                   "<relations format-version=\"4\" relations-version=\"20240201\" main-table=\"AccessInfo\">" +
                   "<table name=\"AccessInfo\">" +
                   "<link name=\"RolesInCache\" parent-table=\"AccessInfo\" child-table=\"RolesInCache\" />" +
                   "<link name=\"RolesInGraph\" parent-table=\"AccessInfo\" child-table=\"RolesInGraph\" />" +
                   "<link name=\"RolesInMember\" parent-table=\"AccessInfo\" child-table=\"RolesInMember\" />" +
                   "</table>" +
                   "</relations>" +
                   "<layout>" +
                   "<table name=\"AccessInfo\" />" +
                   "<table name=\"RolesInCache\" />" +
                   "<table name=\"RolesInGraph\" />" +
                   "<table name=\"RolesInMember\" />" +
                   "</layout>" +
                   "<data>" +
                   "<AccessInfo>" +
                   $"<row NodeID=\"{{{nodeId}}}\" ScreenID=\"{EscapeXml(screenId)}\" " +
                   $"Title=\"{EscapeXml(title)}\" Url=\"{EscapeXml(url)}\" " +
                   $"MergeRule=\"{EscapeXml(mergeRule)}\"{graphAttr} />" +
                   "</AccessInfo>" +
                   "<RolesInCache />" +
                   "<RolesInGraph />" +
                   "<RolesInMember />" +
                   "</data>" +
                   "</data-set>" +
                   "</ScreenWithRights>";
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
    }
}
