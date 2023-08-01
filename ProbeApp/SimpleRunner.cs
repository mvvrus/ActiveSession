using Microsoft.Extensions.Primitives;
using MVVrus.AspNetCore.ActiveSession;
using static MVVrus.AspNetCore.ActiveSession.ActiveSessionRunnerState;


namespace ProbeApp
{
    public class SimpleRunner : ActiveSessionRunnerBase, IActiveSessionRunner<int>
    {
        readonly Object _lock=new Object();
        int _immediate, _end, _delay_in_ms;
        Int32 _last_set=-1;
        ActiveSessionRunnerState _state_to_set = Stalled;
        Task? _task_to_continue;

        [ActiveSessionConstructor]
        public SimpleRunner(SimpleRunnerParams Params)
        {
            (_immediate, _end, _delay_in_ms)=Params;
        }

        public override void Abort()
        {
            lock (_lock) {
                SetState(Aborted);
            }
        }

        public ActiveSessionRunnerResult<Int32> GetAvailable(Int32 StartPosition = -1, Int32 Advance = int.MaxValue, String? TraceIdentifier = null)
        {
            if(State!=Progressed) return new ActiveSessionRunnerResult<int>(_last_set, State, Position);
            ActiveSessionRunnerResult<int> result;
            lock (_lock) {
                result = new ActiveSessionRunnerResult<int>(_last_set, State, Position);
                SetState(_state_to_set);
            }
            return result;
        }

        public async ValueTask<ActiveSessionRunnerResult<Int32>> GetMoreAsync(Int32 StartPosition, Int32 Advance, String? TraceIdentifier = null, CancellationToken Token = default)
        {
            if(CompareAndSetStateInterlocked(Stalled, NotStarted)==NotStarted) StartBackground();
            //TODO process Advance==DEFAULT_ADVANCE
            ActiveSessionRunnerResult<int> result=default;
            for (int i=0;i<Advance; i++) {
                ActiveSessionRunnerState state = State;
                result=new ActiveSessionRunnerResult<int>(_last_set, state, Position);
                if (state!=Stalled&&state!=Progressed) break;
                if (state==Stalled) await _task_to_continue!;
                if(State==Progressed) lock (_lock) {
                    result=new ActiveSessionRunnerResult<int>(_last_set, State, Position);
                    SetState(_state_to_set);
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
