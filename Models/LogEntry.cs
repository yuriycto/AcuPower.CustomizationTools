using System.Collections.Generic;

namespace AcuPower.CustomizationTools.Models
{
    public class LogEntry
    {
        public string Timestamp;
        public string LogType;
        public string Message;
    }

    public class ImportResult
    {
        public List<LogEntry> Log;
        public bool IsError;
    }

    public class PublishEndResult
    {
        public bool IsCompleted;
        public bool IsFailed;
        public List<LogEntry> Log;
    }

    public class PublishedInfo
    {
        public string[] ProjectNames;
        public List<LogEntry> Log;
    }

    public enum TenantMode
    {
        Current = 0,
        All = 1,
        List = 2
    }
}
