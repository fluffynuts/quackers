using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Quackers.TestLogger
{
    public interface ILogger : ILoggerProperties
    {
        void LogInfo(string str);
        void LogError(string str);
        void LogPass(TestResultEventArgs e);
        void LogFail(TestResultEventArgs e);
        void LogNone(TestResultEventArgs e);
        void LogSkipped(TestResultEventArgs e);
        void LogNotFound(TestResultEventArgs e);
        void LogErrorMessage(string str);
        void LogStacktrace(string str);
        void InsertBreak();
        void ShowSummary();
        void Reset();
    }
}