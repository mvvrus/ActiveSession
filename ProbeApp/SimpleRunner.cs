using Microsoft.Extensions.Primitives;
using MVVrus.AspNetCore.ActiveSession;
using static MVVrus.AspNetCore.ActiveSession.IRunner;
using static MVVrus.AspNetCore.ActiveSession.RunnerStatus;


namespace ProbeApp
{
    public class SimpleRunner : RunnerBase, IRunner<int>
    {
        readonly Object _lock=new Object();
        int _immediate, _end, _delay_in_ms;
        Int32 _last_set=-1;
        RunnerStatus _state_to_set = Stalled;
        Task? _task_to_continue;

        [ActiveSessionConstructor]
        public SimpleRunner(SimpleRunnerParams Params, ILoggerFactory LoggerFactory):base(null,true, default, LoggerFactory.CreateLogger<SimpleRunner>())
        {
            (_immediate, _end, _delay_in_ms)=Params;
            Logger?.LogDebug($"Parameters: {Params}");
        }

        public RunnerResult<Int32> GetAvailable(Int32 Advance = int.MaxValue, Int32 StartPosition = -1, String? TraceIdentifier = null)
        {
            if(Status!=Progressed) return new RunnerResult<int>(_last_set, Status, Position);
            RunnerResult<int> result;
            lock (_lock) {
                SetStatus(_state_to_set);
                result= new RunnerResult<int>(_last_set, Status, Position);
            }
            return result;
        }

        public async ValueTask<RunnerResult<Int32>> GetRequiredAsync(
            Int32 Advance = IRunner.DEFAULT_ADVANCE,
            CancellationToken Token = default,
            Int32 StartPosition = IRunner.CURRENT_POSITION,
            String? TraceIdentifier = null)
        {
            if(StartRunning()) StartBackgroundExecution();
            if (Advance<=0) Advance=1;
            RunnerResult<int> result=default;
            for (int i=0;i<Advance; i++) {
                RunnerStatus state = Status;
                result=new RunnerResult<int>(_last_set, state, Position);
                if (state!=Stalled&&state!=Progressed) break;
                if (state==Stalled) await _task_to_continue!;
                if(Status==Progressed) lock (_lock) {
                        SetStatus(_state_to_set);
                        result=new RunnerResult<int>(_last_set, Status, Position);
                }
            }
            return result;
        }

        protected override void StartBackgroundExecution()
        {
            _task_to_continue=Task.Run(() => BackgroundTaskBody());
        }

        void BackgroundTaskBody(Task? ignored=null)
        {
            Thread.Sleep(0);
            lock (_lock) {
                if((Status==Stalled || Status==Progressed) && _state_to_set!=Complete) {
                    Position++;
                    _last_set=Position;
                    if(Position>=_end) {
                        _state_to_set=Complete;
                    }
                    else {
                        _state_to_set=Stalled;
                        SetStatus(Progressed);
                        _task_to_continue=Position>=_immediate ? Task.Delay(_delay_in_ms).ContinueWith(BackgroundTaskBody) :
                            Task.Run(() => BackgroundTaskBody());
                    }
                }
            }
        }
    }
}
