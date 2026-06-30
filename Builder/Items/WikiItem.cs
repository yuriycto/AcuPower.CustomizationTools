namespace AcuPower.CustomizationTools.Builder.Items
{
    /// <summary>
    /// Placeholder helper for Wiki article items in customization projects.
    /// </summary>
    public static class WikiItem
    {
        /// <summary>
        /// Returns an XML element string for a wiki article entry.
        /// Currently a placeholder for future implementation.
        /// </summary>
        /// <param name="wikiArticleName">Wiki article name.</param>
        public static string ToXml(string wikiArticleName)
        {
            return $"<Wiki ArticleName=\"{EscapeXml(wikiArticleName)}\" />";
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
