using System;

namespace Quackers.TestLogger
{
    public static class Timestamp
    {
        public const string DEFAULT_TIMESTAMP_FORMAT = "yyyy-MM-dd HH:mm:ss.fff";
        public static string TimestampFormat
        {
            get;
            set;
        } = DEFAULT_TIMESTAMP_FORMAT;

        public static string Now
        {
            get
            {
                var now = DateTime.Now;
                return now.ToString(TimestampFormat);
            }
        }
    }
}