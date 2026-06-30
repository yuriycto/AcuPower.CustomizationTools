using System;

namespace AcuPower.CustomizationTools.Builder.Items
{
    /// <summary>
    /// Helper for generating Webhook XML in the data-set format used by Acumatica.
    /// </summary>
    public static class WebhookItem
    {
        /// <summary>
        /// Returns a complete Webhook XML fragment in data-set format.
        /// </summary>
        /// <param name="name">Webhook display name.</param>
        /// <param name="handler">Fully qualified webhook handler type name.</param>
        /// <param name="webHookId">Optional webhook identifier. A new GUID is generated when omitted.</param>
        /// <param name="isActive">Whether the webhook is active.</param>
        /// <param name="isSystem">Whether the webhook is marked as system-owned.</param>
        /// <param name="requestLogLevel">Request log level value used by Acumatica.</param>
        /// <param name="requestRetainCount">How many webhook requests to retain.</param>
        /// <param name="noteId">Optional note identifier. A new GUID is generated when omitted.</param>
        public static string ToXml(
            string name,
            string handler,
            string webHookId = null,
            bool isActive = true,
            bool isSystem = false,
            int requestLogLevel = 2,
            int requestRetainCount = 100,
            string noteId = null)
        {
            var resolvedWebHookId = ResolveGuid(webHookId);
            var resolvedNoteId = ResolveGuid(noteId);

            return "<Webhook>" +
                   "<data-set>" +
                   "<relations format-version=\"3\" relations-version=\"20160101\" main-table=\"WebHook\" />" +
                   "<layout><table name=\"WebHook\" /></layout>" +
                   "<data>" +
                   "<WebHook>" +
                   $"<row WebHookID=\"{Esc(resolvedWebHookId)}\" " +
                   $"Name=\"{Esc(name)}\" " +
                   $"Handler=\"{Esc(handler)}\" " +
                   $"IsActive=\"{(isActive ? "1" : "0")}\" " +
                   $"IsSystem=\"{(isSystem ? "1" : "0")}\" " +
                   $"RequestLogLevel=\"{requestLogLevel}\" " +
                   $"RequestRetainCount=\"{requestRetainCount}\" " +
                   $"NoteID=\"{Esc(resolvedNoteId)}\" />" +
                   "</WebHook>" +
                   "</data>" +
                   "</data-set>" +
                   "</Webhook>";
        }

        private static string ResolveGuid(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? Guid.NewGuid().ToString("D")
                : Guid.Parse(value).ToString("D");
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
