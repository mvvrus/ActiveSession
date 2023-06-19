namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal static class ActiveSessionConstants
    {
        public const String CONFIG_SECTION_NAME = "MVVrus.ActiveSessions";
        public const String LOGGING_CATEGORY_NAME = "MVVrus.AspNetCore.ActiveSession";
        public static readonly TimeSpan DEFAULT_MAX_LIFETIME = TimeSpan.FromHours(2);
        public static readonly String DEFAULT_HOST_NAME = "localhost";
        public static readonly String DEFAULT_SESSION_KEY_PREFIX = "##ActiveSession##";
        public static readonly String UNKNOWN_TRACE_IDENTIFIER = "<unknown>";
        public static readonly String UNKNOWN_SESSION_KEY = "<unknown session key>";
        public static readonly String UNKNOWN_TYPE = "<unknown type>";

    }
}
