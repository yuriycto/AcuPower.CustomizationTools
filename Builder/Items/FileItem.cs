namespace AcuPower.CustomizationTools.Builder.Items
{
    /// <summary>
    /// Helper for generating File XML elements in the customization project.
    /// </summary>
    public static class FileItem
    {
        /// <summary>
        /// Returns an XML element string for a file entry.
        /// </summary>
        /// <param name="appRelativePath">Application-relative path (e.g. "Bin\MyDll.dll").</param>
        public static string ToXml(string appRelativePath)
        {
            return $"<File AppRelativePath=\"{EscapeXml(appRelativePath)}\" />";
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
