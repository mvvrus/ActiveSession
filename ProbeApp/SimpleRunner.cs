using Microsoft.Extensions.Primitives;
using MVVrus.AspNetCore.ActiveSession;
using static MVVrus.AspNetCore.ActiveSession.IRunner;
using static MVVrus.AspNetCore.ActiveSession.RunnerState;


namespace ProbeApp
{
    public class SimpleRunner : RunnerBase, IRunner<int>
    {
        readonly Object _lock=new Object();
        int _immediate, _end, _delay_in_ms;
        Int32 _last_set=-1;
        RunnerState _state_to_set = Stalled;
        Task? _task_to_continue;
        ILogger? _logger;

        [ActiveSessionConstructor]
        public SimpleRunner(SimpleRunnerParams Params, ILoggerFactory LoggerFactory)
        {
            (_immediate, _end, _delay_in_ms)=Params;
            _logger=LoggerFactory.CreateLogger<SimpleRunner>();
            _logger?.LogDebug($"Parameters: {Params}");
        }

        public RunnerResult<Int32> GetAvailable(Int32 StartPosition = -1, Int32 Advance = int.MaxValue, String? TraceIdentifier = null)
        {
            if(State!=Progressed) return new RunnerResult<int>(_last_set, State, Position);
            RunnerResult<int> result;
            lock (_lock) {
                SetState(_state_to_set);
                result= new RunnerResult<int>(_last_set, State, Position);
            }
            return result;
        }

        public async ValueTask<RunnerResult<Int32>> GetRequiredAsync(Int32 StartPosition, Int32 Advance, String? TraceIdentifier = null, CancellationToken Token = default)
        {
            if(StartRunning()) StartBackground();
            if (Advance<=0) Advance=1;
            RunnerResult<int> result=default;
            for (int i=0;i<Advance; i++) {
                RunnerState state = State;
                result=new RunnerResult<int>(_last_set, state, Position);
                if (state!=Stalled&&state!=Progressed) break;
                if (state==Stalled) await _task_to_continue!;
                if(State==Progressed) lock (_lock) {
                        SetState(_state_to_set);
                        result=new RunnerResult<int>(_last_set, State, Position);
                }
            }
            return result;
        }

        void StartBackground()
        {
            _task_to_continue=Task.Run(() => BackgroundTaskBody());
        }

        void BackgroundTaskBody(Task? ignored=null)
        {
            Thread.Sleep(0);
            lock (_lock) {
                if((State==Stalled || State==Progressed) && _state_to_set!=Complete) {
                    Position++;
                    _last_set=Position;
                    if(Position>=_end) {
                        _state_to_set=Complete;
                    }
                    else {
                        _state_to_set=Stalled;
                        SetState(Progressed);
                        _task_to_continue=Position>=_immediate ? Task.Delay(_delay_in_ms).ContinueWith(BackgroundTaskBody) :
                            Task.Run(() => BackgroundTaskBody());
                    }
                }
            }
        }
    }
}
