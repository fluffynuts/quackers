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
        

        bool VerboseSummary { get; set; }
        bool OutputFailuresInline { get; set; }
        bool ShowHelp { get; set; }

        string SummaryStartMarker { get; set; }
        string SummaryCompleteMarker { get; set; }
        string FailureStartMarker { get; set; }
        string SlowStartMarker { get; set; }
        string LogPrefix { get; set; }
        string TestNamePrefix { get; set; }
        string FailureIndexPlaceholder { get; set; }
        string SlowIndexPlaceholder { get; set; }
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