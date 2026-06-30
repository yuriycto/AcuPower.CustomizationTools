using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace AcuPower.CustomizationTools.Builder.Items
{
    /// <summary>
    /// Helper for generating Page XML elements in the customization project.
    /// Uses raw deflate compression for page source encoding.
    /// </summary>
    public static class PageItem
    {
        /// <summary>
        /// Returns an XML element string for a page entry.
        /// </summary>
        /// <param name="virtualPath">Virtual path (e.g. "~/Pages/AW/AW501000.aspx").</param>
        /// <param name="aspxContent">Raw ASPX content to deflate-encode.</param>
        public static string ToXml(string virtualPath, string aspxContent)
        {
            var encoded = DeflateAndBase64(aspxContent);
            return $"<Page path=\"{EscapeXml(virtualPath)}\" ControlId=\"0\" pageSource=\"{encoded}\" />";
        }

        /// <summary>
        /// Compresses the input string using raw deflate (no zlib header), then base64 encodes it.
        /// </summary>
        public static string DeflateAndBase64(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            using (var outputStream = new MemoryStream())
            {
                using (var deflateStream = new DeflateStream(outputStream, CompressionLevel.Optimal, leaveOpen: true))
                {
                    deflateStream.Write(bytes, 0, bytes.Length);
                }
                return Convert.ToBase64String(outputStream.ToArray());
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
    }
}
