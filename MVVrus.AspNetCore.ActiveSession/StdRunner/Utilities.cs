using static MVVrus.AspNetCore.ActiveSession.IRunner;

namespace MVVrus.AspNetCore.ActiveSession.StdRunner
{
    internal static class Utilities
    {
        public static void ProcessEnumParmeters(
            ref Int32 StartPosition,
            ref Int32 Advance,
            IRunner Caller,
            Int32 DefaultAdvance,
            String MethodName,
            ILogger? Logger=null)
        {
            String classname = Caller.GetType().FullName??"<unknown type>";
            if (StartPosition==CURRENT_POSITION) StartPosition=Caller.Position;
            if (StartPosition!=Caller.Position) {
                //TODO LogError
                throw new InvalidOperationException($"{classname}.{MethodName}: a start position ({StartPosition}) differs from the current one({Caller.Position})");
            }
            if (Advance==DEFAULT_ADVANCE) Advance=DefaultAdvance;
            if (Advance<=0) {
                //TODO LogError
                throw new InvalidOperationException($"{classname}.{MethodName}: Invalid advance value: {Advance}");
            }

        }
    }
}
