using System;

namespace Quackers.TestLogger
{
    public interface ILoggerProperties
    {
        [Help("The prefix label to show for a passing test, default '✅'")]
        string PassLabel { get; set; }
        [Help("The prefix label to show for a failing test, default '🛑'")]
        string FailLabel { get; set; }
        [Help("The prefix label to show for a 'none' result (eg nunit test marked [Explicit]), default '❓'")]
        string NoneLabel { get; set; }
        [Help("The prefix label to show for a skipped test, default '🚫'")]
        string SkipLabel { get; set; }
        [Help("The prefix label to show for a test that was discovered but later not found, default '🤷'")]
        string NotFoundLabel { get; set; }
        
        [Help("Disable color in outputs (eg for CI), default off, unless the NO_COLOR environment variable is set")]
        bool NoColor { get; set; }

        [Help("Theme for color output, default works well on a dark background")]
        string Theme { get; set; }
        
        [Help("Flag: highlight slow tests in the output")]
        bool HighlightSlowTests { get; set; }
        [Help("Consider a test 'slow' when it exceeds a runtime of this many milliseconds")]
        int SlowTestThresholdMs { get; set; }


        [Help("Path to a file to use to output debug information about Quackers operations")]
        string DebugLogFile { get; set; }

        [Help("Flag: show starting timestamps for tests")]
        bool ShowTimestamps { get; set; }

        string TestNamePrefix { get; set; }
        [Help("Flag: show a more verbose summary of test results")]
        bool ShowTotals { get; set; }
        [Help("Flag: show failures immediately, inline (and summarise again later)")]
        bool OutputFailuresInline { get; set; }
        bool ShowHelp { get; set; }
        [Help("Flag: dump out the overall config Quackers will use before tests start")]
        bool DumpConfig { get; set; }

        [Help("Automated usage marker")]
        string LogPrefix { get; set; }
        [Help("Automated usage marker")]
        string SummaryStartMarker { get; set; }
        [Help("Automated usage marker")]
        string SummaryCompleteMarker { get; set; }
        [Help("Automated usage marker")]
        string FailureStartMarker { get; set; }
        [Help("Automated usage marker")]
        string FailureCompleteMarker { get; set; }
        [Help("Automated usage marker")]
        string SlowSummaryStartMarker { get; set; }
        [Help("Automated usage marker")]
        string SlowSummaryCompleteMarker { get; set; }
        [Help("Automated usage marker")]
        string SummaryTotalsStartMarker { get; set; }
        [Help("Automated usage marker")]
        string SummaryTotalsCompleteMarker { get; set; }
        [Help("Automated usage marker")]
        string FailureIndexPlaceholder { get; set; }
        [Help("Automated usage marker")]
        string SlowIndexPlaceholder { get; set; }
        [Help("Maximum number of slow tests to display in the summary, if found")]
        int MaxSlowTestsToDisplay { get; set; }
    }

    internal class HelpAttribute : Attribute
    {
        public string Help { get; }

        internal HelpAttribute(string help)
        {
            Help = help;
        }
    }
}