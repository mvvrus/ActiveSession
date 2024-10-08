﻿using System.Text.RegularExpressions;
using static MVVrus.AspNetCore.ActiveSession.IRunner;

namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal static class Utilities
    {
        static Regex _genericNamePattern = new Regex(@"^(.+)`\d+.*");

        public static string MakeClassCategoryName(Type Class)
        {
            string full_name = Class.FullName ?? ActiveSessionConstants.UNKNOWN_TYPE;
            MatchCollection matches = _genericNamePattern.Matches(full_name);
            return matches.Count > 0 && matches[0].Groups.Count > 1 ? matches[0].Groups[1].Value : full_name;
        }

        public record TimeoutLoggerContext(Task CleanupTask, Action EntranceTraceLogger, Action<Boolean> TimeoutCheckLogger);

        public static void TimeoutLoggerBody(Task<Task> AntecedentTask, Object? State)
        {
            TimeoutLoggerContext state = (TimeoutLoggerContext)State!;
            state.EntranceTraceLogger();
            state.TimeoutCheckLogger(state.CleanupTask.Id==AntecedentTask.Result.Id);
        }


    }
}
