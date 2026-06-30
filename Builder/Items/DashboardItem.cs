namespace AcuPower.CustomizationTools.Builder.Items
{
    /// <summary>
    /// Placeholder helper for Dashboard items in customization projects.
    /// </summary>
    public static class DashboardItem
    {
        /// <summary>
        /// Returns an XML element string for a dashboard entry.
        /// Currently a placeholder for future implementation.
        /// </summary>
        /// <param name="dashboardScreenId">Dashboard screen ID.</param>
        public static string ToXml(string dashboardScreenId)
        {
            return $"<Dashboard ScreenID=\"{EscapeXml(dashboardScreenId)}\" />";
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
