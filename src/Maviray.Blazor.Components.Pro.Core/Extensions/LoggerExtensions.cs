using Maviray.Blazor.Components.Pro.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace Maviray.Blazor.Components.Pro.Core.Extensions;

public static class LoggerExtensions
{
    extension(ILogger logger)
    {
        public void Information(string message,
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "[{MemberName}:{LineNumber}] {Message}",
                    memberName,
                    lineNumber,
                    message);
            }
        }

        public void Warning(string message,
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int lineNumber = 0)
        {

            if (!logger.IsEnabled(LogLevel.Warning))
            {
                return;
            }

            logger.LogWarning(
                "[{MemberName}:{LineNumber}] {Message}",
                memberName,
                lineNumber,
                message);
        }

        public void Error(Exception? exception,
            string message,
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (!logger.IsEnabled(LogLevel.Error))
            {
                return;
            }

            logger.LogError(
                exception,
                "[{MemberName}:{LineNumber}] {Message}",
                memberName,
                lineNumber,
                message);
        }

        public void Error(string message,
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (!logger.IsEnabled(LogLevel.Error))
            {
                return;
            }

            logger.LogError(
                "[{MemberName}:{LineNumber}] {Message}",
                memberName,
                lineNumber,
                message);
        }

        /// <summary>
        /// Logs a message if the configured log level allows it as per IMaviComponentOptions
        /// </summary>
        /// <param name="componentOptions">Component options for log level filtering (optional)</param>
        /// <param name="callerType">The type of the calling class</param>
        /// <param name="level">The log level</param>
        /// <param name="id">id of the executing component</param>
        /// <param name="message">The log message</param>
        /// <param name="memberName">caller member name</param>
        /// <param name="sourceLineNumber">source line number</param>
        public void LogIfEnabled(IMaviComponentProOptions? componentOptions,
            string? id,
            Type callerType,
            LogLevel level,
            string message,
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {

            if (componentOptions == null) return;

            // If component options are provided, respect the configured log level
            if (level < componentOptions.ComponentLogLevel) return;

            logger.LogInformation(
                "[{Component}.{Member}][{Line}]-[{Id}]: {Message}",
                callerType.Name,
                memberName,
                sourceLineNumber,
                id, message);
        }

        /// <summary>
        /// Logs a message if the configured log level allows it as per IMaviComponentOptions
        /// </summary>
        /// <param name="callerType">The type of the calling class</param>
        /// <param name="id">id of the executing component</param>
        /// <param name="memberName">caller member name</param>
        /// <param name="sourceLineNumber">source line number</param>
        public void LogDebugLifeCycle(string? id, Type callerType, [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            // Format: $"[{callerType.Name}.{memberName}][{sourceLineNumber}]-[{id}];

            logger.Log(LogLevel.Information, "[{Component}.{Member}][{Line}]-[{Id}]",
                callerType.Name,
                memberName,
                sourceLineNumber,
                id);
        }

        /// <summary>
        /// Logs a message if the configured log level allows it as per IMaviComponentOptions
        /// </summary>
        /// <param name="callerType">The type of the calling class</param>
        /// <param name="id">id of the executing component</param>
        /// <param name="message">The log message</param>
        /// <param name="memberName">caller member name</param>
        /// <param name="sourceLineNumber">source line number</param>
        public void LogDebugLifeCycle(Type callerType, string? id, string message, [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            // Format: $"[{callerType.Name}.{memberName}][{sourceLineNumber}]-[{id}]: {message}";

            logger.Log(LogLevel.Information, "[{Component}.{Member}][{Line}]-[{Id}]: {Message}",
                callerType.Name,
                memberName,
                sourceLineNumber,
                id,
                message);
        }
    }
}