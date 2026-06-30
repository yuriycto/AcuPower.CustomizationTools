namespace AcuPower.CustomizationTools.Builder.Items
{
    /// <summary>
    /// Helper for generating Screen XML elements in the customization project.
    /// </summary>
    public static class ScreenItem
    {
        /// <summary>
        /// Returns an XML element string for a screen entry.
        /// </summary>
        /// <param name="screenId">Screen identifier (e.g. "AW501000").</param>
        public static string ToXml(string screenId)
        {
            return $"<Screen ID=\"{EscapeXml(screenId)}\" />";
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
