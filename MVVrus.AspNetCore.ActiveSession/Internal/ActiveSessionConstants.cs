﻿namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal static class ActiveSessionConstants
    {
        public const String CONFIG_SECTION_NAME = "MVVrus.ActiveSessions";
        public const String LOGGING_CATEGORY_NAME = "MVVrus.AspNetCore.ActiveSession";
        public static readonly TimeSpan DEFAULT_MAX_LIFETIME = TimeSpan.FromHours(2);
        public static readonly TimeSpan DEFAULT_RUNNER_IDLE_TIMEOUT = TimeSpan.FromMinutes(2);
        public static readonly String DEFAULT_HOST_NAME = "localhost";
        public static readonly String DEFAULT_SESSION_KEY_PREFIX = "##ActiveSession##";
        public static readonly String UNKNOWN_TRACE_IDENTIFIER = "<unknown>";
        public static readonly String UNKNOWN_SESSION_ID = "<unknown session id>";
        public static readonly String UNKNOWN_TYPE = "<unknown type>";
        public const Int32 DEFAULT_ACTIVESESSIONSIZE = 1;
        public const Int32 DEFAULT_RUNNERSIZE = 1;
        public const String SESSION_TERMINATED = "Terminated";
        public const String SESSION_ACTIVE = "Active";
        public static readonly TimeSpan DEFAULT_PATHREGEXTIMEOUT = TimeSpan.FromSeconds(1);
        public const Int32 ENUM_DEFAULT_ADVANCE = 20;
        public const Int32 ENUM_DEFAULT_QUEUE_SIZE = 1000;
    }
}
