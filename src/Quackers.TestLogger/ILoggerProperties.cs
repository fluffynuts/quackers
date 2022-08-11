namespace Quackers.TestLogger
{
    public interface ILoggerProperties
    {
        string PassLabel { get; set; }
        string FailLabel { get; set; }
        string NoneLabel { get; set; }
        string SkipLabel { get; set; }
        string NotFoundLabel { get; set; }
        bool NoColor { get; set; }
        bool VerboseSummary { get; set; }
        string SummaryStartMarker { get; set; }
        string SummaryCompleteMarker { get; set; }
        string FailureStartMarker { get; set; }
        string LogPrefix { get; set; }
        bool OutputFailuresInline { get; set; }
        string TestNamePrefix { get; set; }
    }
}