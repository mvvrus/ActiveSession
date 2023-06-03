using static Microsoft.Extensions.Logging.LogLevel;

namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal static partial class LoggingExtensions
    {
        [LoggerMessage(1, Error, "The exception occured while creating the ActiveSession middleware.")]
        public static partial void LogErrorMiddlewareCannotBeCreated(this ILogger Logger, Exception AnException);

        [LoggerMessage(2, Error, "The exception occured while the rest of the pipeline, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogErrorPipelineException(this ILogger Logger, Exception AnException, String TraceIdentifier);


        [LoggerMessage(1000, Warning, "Cannot obtain an ActiveSession store service from the application service container. The ActiveSession middleware cannot be registered. Was the IServiceCollection.AddActiveServices extension method called at least once?")]
        public static partial void LogWarningAbsentFactoryInplementations(this ILogger Logger, Exception AnException);


        [LoggerMessage(3000, Debug, "ActiveSession middleware is registered for addition to a middleware pipeline.")]
        public static partial void LogDebugActiveSessionMiddlewareRegistered(this ILogger Logger);

        [LoggerMessage(3001, Debug, "ActiveSession middleware is added to the middleware pipeline.")]
        public static partial void LogDebugActiveSessionMiddlewareAdded(this ILogger Logger);

        [LoggerMessage(3002, Debug, "ActiveSession middleware is invoked, TraceIdentifier=\"{TraceIdentifier}\", Session.Id=\"{SessionId}\".")]
        public static partial void LogDebugActiveSessionMiddlewareInvoked(this ILogger Logger, String TraceIdentifier, String SessionId);


        [LoggerMessage(4000, Trace, "Enetering ActiveSessionBuilderExtensions.UseActiveSessions.")]
        public static partial void LogTraceUseActiveSessions(this ILogger Logger);

        [LoggerMessage(4001, Trace, "Exiting ActiveSessionBuilderExtensions.UseActiveSessions.")]

        public static partial void LogTraceUseActiveSessionsExit(this ILogger Logger);
        [LoggerMessage(4002, Trace, "Enetering ActiveSessionMiddleware constructor.")]
        public static partial void LogTraceConstructActiveSessionMiddleware(this ILogger Logger);

        [LoggerMessage(4003, Trace, "Exiting ActiveSessionMiddleware constructor.")]
        public static partial void LogTraceConstructActiveSessionMiddlewareExit(this ILogger Logger);

        [LoggerMessage(4010, Trace, "Entering ActiveSessionMiddleware, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceInvokeActiveSessionMiddleware(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4011, Trace, "A feature object is created, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionMiddlewareFeatureCreated(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4012, Trace, "Invocing the rest of the middleware pipeline after the ActiveSession middleware, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionMiddlewareInvokeRest(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4013, Trace, "Control from the rest of the pipeline was returned without exceptions, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionMiddlewareControlReturns(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4014, Trace, "Cleaning up the feature object, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionMiddlewareCleanupStart(this ILogger Logger, String TraceIdentifier);

        [LoggerMessage(4015, Trace, "ActiveSession middleware exited, TraceIdentifier=\"{TraceIdentifier}\".")]
        public static partial void LogTraceActiveSessionMiddlewareExit(this ILogger Logger, String TraceIdentifier);
    }
}
