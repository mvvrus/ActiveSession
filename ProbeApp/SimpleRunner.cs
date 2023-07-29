using Microsoft.Extensions.Primitives;
using MVVrus.AspNetCore.ActiveSession;

namespace ProbeApp
{
    public class SimpleRunner : IActiveSessionRunner<int>
    {
        int _position, _immediate, _end, _delay_in_ms;

        [ActiveSessionConstructor]
        public SimpleRunner(SimpleRunnerParams Params)
        {
            _position=0;
            (_immediate, _end, _delay_in_ms)=Params;
        }

        public ActiveSessionRunnerState State => throw new NotImplementedException();

        public Int32 Position => _position;

        public void Abort()
        {
            throw new NotImplementedException();
        }

        public ActiveSessionRunnerResult<Int32> GetAvailable(Int32 StartPosition = -1, Int32 Advance = int.MaxValue, String? TraceIdentifier = null)
        {
            throw new NotImplementedException();
        }

        public IChangeToken GetCompletionToken()
        {
            throw new NotImplementedException();
        }

        public ValueTask<ActiveSessionRunnerResult<Int32>> GetMoreAsync(Int32 StartPosition, Int32 Advance, String? TraceIdentifier = null, CancellationToken Token = default)
        {
            throw new NotImplementedException();
        }
    }
}
