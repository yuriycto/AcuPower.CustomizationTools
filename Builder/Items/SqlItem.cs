namespace AcuPower.CustomizationTools.Builder.Items
{
    /// <summary>
    /// Helper for generating Sql XML elements in the customization project.
    /// </summary>
    public static class SqlItem
    {
        /// <summary>
        /// Returns an XML element string for a table schema SQL entry.
        /// </summary>
        /// <param name="tableName">Database table name.</param>
        /// <param name="tableSchemaXml">Table schema XML content.</param>
        public static string ToTableSchemaXml(string tableName, string tableSchemaXml)
        {
            return $"<Sql TableName=\"{EscapeXml(tableName)}\" TableSchemaXml=\"#CDATA\">" +
                   $"<CDATA name=\"TableSchemaXml\"><![CDATA[{tableSchemaXml}]]></CDATA>" +
                   $"</Sql>";
        }

        /// <summary>
        /// Returns an XML element string for a custom SQL script entry.
        /// </summary>
        /// <param name="tableName">Database table name.</param>
        /// <param name="sqlScript">Custom SQL script content.</param>
        public static string ToCustomScriptXml(string tableName, string sqlScript)
        {
            return $"<Sql TableName=\"{EscapeXml(tableName)}\" CustomScript=\"#CDATA\">" +
                   $"<CDATA name=\"CustomScript\"><![CDATA[{sqlScript}]]></CDATA>" +
                   $"</Sql>";
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
