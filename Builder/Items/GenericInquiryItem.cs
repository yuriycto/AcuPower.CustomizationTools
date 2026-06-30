namespace AcuPower.CustomizationTools.Builder.Items
{
    /// <summary>
    /// Placeholder helper for Generic Inquiry items in customization projects.
    /// </summary>
    public static class GenericInquiryItem
    {
        /// <summary>
        /// Returns an XML element string for a generic inquiry entry.
        /// Currently a placeholder for future implementation.
        /// </summary>
        /// <param name="giName">Generic Inquiry name.</param>
        public static string ToXml(string giName)
        {
            return $"<GenericInquiry Name=\"{EscapeXml(giName)}\" />";
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
