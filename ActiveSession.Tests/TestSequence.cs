using Microsoft.VisualStudio.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveSession.Tests
{
    internal class TestSequence
    {
        Boolean _busy;
        Boolean _released = false;
        Int32 _current=-1;
        Int32? _nextStop;
        StopAction _nextStopAction;
        AsyncManualResetEvent _resumeEvent = new AsyncManualResetEvent(false);
        AsyncManualResetEvent _nextStopEvent = new AsyncManualResetEvent(true);
        Boolean _done=false;
        CancellationToken _token = default;

        public Int32? NextStop { get { return _nextStop; } }
        public StopAction NextStopAction {  get { return _nextStopAction; } }
        public Int32 Current { get { return _current; } }


        public TestSequence() { }


        void Reset()
        {
            _busy = false;
            _current = -1;
            _nextStop=null;
            _nextStopAction=StopAction.Unknown;
            _nextStopEvent.Set();
            _resumeEvent.Set();
            Task.Yield().GetAwaiter().GetResult();
            _resumeEvent.Reset();
            _done=false;
            _token=default;
            _released=false;
        }

        async Task<Boolean> MoveNext()
        {
            if(_done || _released) {
                _nextStopEvent.Set();
                return false;
            }
            _current++;
            Int32 next_stop = _nextStop ?? throw new InvalidOperationException("No next stop point specified.");
            Boolean result=true;
            if(_current>=next_stop) {
                _resumeEvent.Reset();
                _nextStopEvent.Set();
                try {
                    switch(_nextStopAction) {
                        case StopAction.Wait:
                            _nextStopAction=StopAction.Unknown;
                            await _resumeEvent.WaitAsync(_token);
                            result=!_released;
                            break;
                        case StopAction.Complete:
                            _done=true;
                            result=false;
                            break;
                        case StopAction.Fail:
                            throw new TestException();
                        default:
                            throw new InvalidOperationException("Bad or no stop action was specifed.");
                    }
                }
                catch {
                    _done=true;
                    throw;
                }
            }
            return result;
        }

        void CheckBusy()
        {
            if(_busy) throw new InvalidOperationException("Cannot get two active enumerables simultaneously. \n" 
                +"Dispose the the active (Async)Enumerable before getting a new one.");
        }

        public Task Resume(Int32 OffsetToNextStop, StopAction NextStopAction=StopAction.Wait)

        {
            if(OffsetToNextStop<0 || OffsetToNextStop==0 && _current>=0) throw new ArgumentOutOfRangeException(nameof(OffsetToNextStop));
            if(NextStopAction!=StopAction.Wait && NextStopAction!=StopAction.Complete && NextStopAction!=StopAction.Fail)
                throw new ArgumentOutOfRangeException(nameof(NextStopAction));
            if(_nextStopAction!=StopAction.Unknown)
                throw new InvalidOperationException("Cannot resume in this state");
            _nextStop = (_nextStop??0)+OffsetToNextStop;
            _nextStopAction = NextStopAction;
            _nextStopEvent.Reset();
            _resumeEvent.Set();
            return _nextStopEvent.WaitAsync();
        }

        public IEnumerable<Int32> GetEnumerable()
        {
            CheckBusy();
            _token=default;
            return new SyncEnumeration(this);
        }

        public IAsyncEnumerable<Int32> GetAsyncEnumerable(CancellationToken Token=default)
        {
            CheckBusy();
            _token=Token;
            return new AsyncEnumeration(this);
        }

        public void ReleaseEnumerable()
        {
            _resumeEvent.Set();
        }

        public enum StopAction { Unknown, Wait, Complete, Fail}

        public class TestException : Exception
        {
            public TestException() : base("Test exception thrown.") { }
        }

        class AsyncEnumeration : IAsyncEnumerable<Int32>, IAsyncEnumerator<Int32>, IDisposable
        {
            TestSequence _owner;

            public AsyncEnumeration(TestSequence Owner)
            {
                _owner=Owner;
            }

            public IAsyncEnumerator<Int32> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                return this;
            }

            public Int32 Current => _owner._current;

            public async ValueTask DisposeAsync()
            {
                Dispose();
                await Task.Yield();
            }

            public ValueTask<Boolean> MoveNextAsync()
            {
                return new ValueTask<Boolean>(_owner.MoveNext());
            }

            public void Dispose()
            {
                _owner.Reset();
            }
        }

        class SyncEnumeration : IEnumerable<Int32>, IEnumerator<Int32>
        {
            TestSequence _owner;
            public SyncEnumeration(TestSequence Owner)
            {
                _owner=Owner;
            }

            public IEnumerator<Int32> GetEnumerator()
            {
                return this;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public Int32 Current => _owner._current;

            Object IEnumerator.Current => Current;

            public void Dispose()
            {
                _owner.Reset();
            }

            public Boolean MoveNext()
            {
                return _owner.MoveNext().GetAwaiter().GetResult();
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

        }
    }
}
