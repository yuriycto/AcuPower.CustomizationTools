using System;
using System.Text;

namespace AcuPower.CustomizationTools.Builder.Items
{
    /// <summary>
    /// Helper for generating Report XML elements in the customization project.
    /// </summary>
    public static class ReportItem
    {
        /// <summary>
        /// Returns an XML element string for a report entry with inline RPX XML content.
        /// </summary>
        /// <param name="reportFileName">Report file name (e.g. "AW501000.rpx").</param>
        /// <param name="rpxXmlContent">RPX XML content string.</param>
        public static string ToXml(string reportFileName, string rpxXmlContent)
        {
            return $"<Report Name=\"{EscapeXml(reportFileName)}\">{rpxXmlContent}</Report>";
        }

        /// <summary>
        /// Returns an XML element string for a report entry from raw bytes (UTF-8 decoded).
        /// </summary>
        /// <param name="reportFileName">Report file name (e.g. "AW501000.rpx").</param>
        /// <param name="rpxContent">RPX content as bytes (UTF-8 encoded XML).</param>
        public static string ToXml(string reportFileName, byte[] rpxContent)
        {
            var rpxXml = Encoding.UTF8.GetString(rpxContent);
            return ToXml(reportFileName, rpxXml);
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
