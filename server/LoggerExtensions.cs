using System.Runtime.CompilerServices;
using Serilog;

namespace PathApi.Server
{
    internal static class LoggerExtensions
    {
        public static ILogger Here(this ILogger logger,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            int fileNameStart = sourceFilePath.LastIndexOfAny(new[] { '/', '\\' });
            int fileNameEnd = sourceFilePath.LastIndexOf('.');
            return logger
                .ForContext("MemberName", memberName)
                .ForContext("FilePath", sourceFilePath.Substring(fileNameStart, fileNameEnd - fileNameStart))
                .ForContext("LineNumber", sourceLineNumber);
        }
    }
}