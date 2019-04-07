namespace PathApi.Server
{
    using Serilog;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Static class providing some extensions to make working with Serilog easier.
    /// </summary>
    internal static class LoggerExtensions
    {
        /// <summary>
        /// Adds context about the callsite to this <see cref="ILogger"/> instance.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> instance in question.</param>
        /// <param name="memberName">Automatically populated with the caller's member name.</param>
        /// <param name="sourceFilePath">Automatically populated with the caller's file path.</param>
        /// <param name="sourceLineNumber">Automatically populated with the caller's line number.</param>
        /// <returns>A <see cref="ILogger"/> instance with the relevant context set.</returns>
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