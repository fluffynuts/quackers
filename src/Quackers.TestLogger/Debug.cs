using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Quackers.TestLogger
{
    public static class Debug
    {
        public static string DebugLogFile { get; set; }

        public static void Log(string str)
        {
            if (!CanLogToDebugFile)
            {
                return;
            }

            try
            {
                lock (DebugFileLock)
                {
                    File.AppendAllText(
                        DebugLogFile,
                        $"[{Timestamp.Now}] {str}\n"
                    );
                }
            }
            catch
            {
                // disable debug logging if it falls over
                _canLogToDebugFile = false;
            }
        }
        public static void Log(TestResultEventArgs e)
        {
            DumpProps(e.Result);
        }

        public static void DumpProps(object obj)
        {
            if (!CanLogToDebugFile)
            {
                return;
            }

            var lines = new List<string>();
            if (obj is not null)
            {
                var type = obj.GetType();
                var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                lines.Add($"Dumping {props.Length} properties on object of type {type}");
                foreach (var prop in props)
                {
                    lines.Add($"  {prop.Name} = {prop.GetValue(obj)}");
                }
            }

            Log(string.Join("\n", lines));
        }

        public static bool CanLogToDebugFile
            => _canLogToDebugFile ??= DetermineIfCanLogToDebugFile();

        private static bool? _canLogToDebugFile;
        private static readonly object DebugFileLock = new();

        private static bool DetermineIfCanLogToDebugFile()
        {
            if (string.IsNullOrWhiteSpace(DebugLogFile))
            {
                return false;
            }

            try
            {
                var container = Path.GetDirectoryName(DebugLogFile);
                if (container is not null && !Directory.Exists(container))
                {
                    Directory.CreateDirectory(container);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}