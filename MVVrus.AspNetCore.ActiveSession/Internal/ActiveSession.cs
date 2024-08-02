﻿using System.Collections;
using System.Diagnostics.CodeAnalysis;
using static MVVrus.AspNetCore.ActiveSession.Internal.ActiveSessionConstants;

namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class ActiveSession : IActiveSession, IDisposable
    {
        readonly IActiveSessionStore _store;
        readonly IServiceScope _scope;
        readonly ILogger? _logger;
        readonly String _sessionId;
        readonly String _logSessionId;
        readonly String _baseId;
        Int32 _disposed = 0;
        bool _isFresh = true;
        readonly IRunnerManager _runnerManager;
        readonly CancellationTokenSource _cts;
        readonly ConcurentSortedDictionary<String, Object> _properties;

        //Properties used in tests
        internal IRunnerManager RunnerManager { get { return _runnerManager; } }

        public ActiveSession(
            IRunnerManager RunnerManager
            , IServiceScope SessionScope
            , IActiveSessionStore Store
            , String SessionId
            , ILogger? Logger
            , Int32 Generation
            , Task? CleanupCompletionTask = null
            , String? TraceIdentifier = null
            , String? BaseId=null   //TODO(future) see if this ever will be used
        )
        {
            ArgumentNullException.ThrowIfNull(SessionId, nameof(SessionId));
            _logger=Logger;
            _baseId=BaseId??SessionId;
            _sessionId=SessionId;
            this.Generation=Generation;
            _logSessionId=LoggingExtensions.MakeSessionId(_sessionId,Generation);
            String trace_identifier = TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            _logger?.LogTraceActiveSessionConstructor(_logSessionId, trace_identifier);
            #endif
            _scope=SessionScope??throw new ArgumentNullException(nameof(SessionScope));
            _runnerManager=RunnerManager??throw new ArgumentNullException(nameof(RunnerManager));
            _store=Store??throw new ArgumentNullException(nameof(Store));
            _cts=new CancellationTokenSource();
            CompletionToken=_cts.Token;
            this.CleanupCompletionTask=CleanupCompletionTask??Task.CompletedTask;
            _properties= new ConcurentSortedDictionary<String, Object>();
            #if TRACE
            _logger?.LogTraceActiveSessionConstructorExit(_logSessionId, trace_identifier);
            #endif
        }

        public KeyedRunner<TResult> CreateRunner<TRequest, TResult>(TRequest Request, HttpContext Context)
        {
            CheckDisposed();
            String trace_identifier = Context.TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            _logger?.LogTraceActiveSessionCreateRunner(_logSessionId, trace_identifier);
            #endif
            KeyedRunner<TResult> created = _store.CreateRunner<TRequest, TResult>(Context.Session,
                this,
                _runnerManager,
                Request,
                trace_identifier);
            _isFresh=false;
            #if TRACE
            _logger?.LogTraceCreateActiveSessionCreateRunnerExit(trace_identifier);
            #endif
            return created;
        }

        public IRunner<TResult>? GetRunner<TResult>(int RequestedKey, HttpContext Context)
        {
            CheckDisposed();
            String trace_identifier = Context.TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            _logger?.LogTraceActiveSessionGetRunner(_logSessionId, trace_identifier);
            #endif
            IRunner<TResult>? fetched = _store.GetRunner<TResult>(Context.Session, this, _runnerManager, RequestedKey, trace_identifier);
            if(fetched!=null) _isFresh=false;
            #if TRACE
            _logger?.LogTraceActiveSessionGetRunnerExit(trace_identifier);
            #endif
            return fetched;
        }

        public Task<IRunner<TResult>?> GetRunnerAsync<TResult>(Int32 RequestedKey, HttpContext Context, CancellationToken Token)
        {
            CheckDisposed();
            String trace_identifier = Context.TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            _logger?.LogTraceActiveSessionGetRunnerAsync(_logSessionId, trace_identifier);
            #endif
            Task<IRunner<TResult>?> fetched = _store.GetRunnerAsync<TResult>(Context.Session, this, _runnerManager, RequestedKey, trace_identifier, Token);
            if(_isFresh)
                fetched.ContinueWith((task) => { if(task.Result!=null) _isFresh=false; },
                    TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
            #if TRACE
            _logger?.LogTraceActiveSessionGetRunnerAsyncExit(trace_identifier);
            #endif
            return fetched;
        }

        public Task Terminate(HttpContext Context)
        {
            String trace_identifier = Context.TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            _logger?.LogTraceActiveSessionTerminateCalled(_logSessionId, trace_identifier);
            #endif
            return _store.TerminateSession(Context.Session, this, RunnerManager, trace_identifier);
        }

        public bool IsAvailable { get { return true; } }

        public bool IsFresh => _isFresh;

        public bool IsIdle => _isFresh; //TODO(future) Make real implementation instead of this stub

        public IServiceProvider SessionServices { get { return _scope.ServiceProvider; } }

        public String Id { get => _sessionId; }

        public String BaseId { get => _baseId; } //TODO(future) see if this ever will be used

        public CancellationToken CompletionToken { get; private set; }

        public Task CleanupCompletionTask { get; private set; }

        public IDictionary<String, Object> Properties { get=>_properties; }


        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1)!=0) return;
            #if TRACE
            _logger?.LogTraceActiveSessionDispose(_logSessionId);
            #endif
            _cts.Cancel();
            _cts.Dispose();
            _properties.Dispose();
        }

        private void CheckDisposed()
        {
            if(Volatile.Read(ref _disposed)!=0)  throw new ObjectDisposedException(this.GetType().FullName!);
        }

        //Stuff used just for testing
        internal void SetDisposedForTests()
        {
            _disposed=1;
        }

        //TODO(future) Make real implementation instead of this stub
        public ValueTask<Boolean> WaitUntilIdle(Boolean AbortAll, TimeSpan Timeout, CancellationToken Token = default)
        {
            return new ValueTask<bool>(IsIdle);
        }

        public Task? TrackRunnerCleanup(Int32 RunnerNumber)
        {
            return _runnerManager.GetRunnerCleanupTrackingTask(this, RunnerNumber);
        }

        internal Boolean Disposed { get { return _disposed!=0; }}

        public Int32 Generation { get; init; }

        class ConcurentSortedDictionary<TKey, TValue> : IDisposable, IDictionary<TKey, TValue> where TKey: notnull 
        {
            ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
            SortedDictionary<TKey, TValue> _base = new SortedDictionary<TKey, TValue>();

            public void Dispose()
            {
                _lock.Dispose();
            }

            public TValue this[TKey key] { get
                {
                    _lock.EnterReadLock(); try { return _base[key]; } finally { _lock.ExitReadLock(); }
                } set
                {
                    _lock.EnterWriteLock(); try { _base[key]=value; } finally { _lock.ExitWriteLock(); }
                } 
            }

            public Int32 Count { get { _lock.EnterReadLock(); try { return _base.Count; } finally { _lock.ExitReadLock(); } } }

            public void Add(TKey key, TValue value)
            {
                _lock.EnterWriteLock(); try { _base.Add(key,value); } finally { _lock.ExitWriteLock(); }
            }

            public void Add(KeyValuePair<TKey, TValue> item)
            {
                _lock.EnterWriteLock(); try { ((ICollection<KeyValuePair<TKey,TValue>>)_base).Add(item); } finally { _lock.ExitWriteLock(); }
            }

            public void Clear()
            {
                _lock.EnterWriteLock(); try { _base.Clear(); } finally { _lock.ExitWriteLock(); }
            }

            public Boolean Contains(KeyValuePair<TKey, TValue> item)
            {
                _lock.EnterReadLock(); try { return _base.Contains(item); } finally { _lock.ExitReadLock(); }
            }

            public Boolean ContainsKey(TKey key)
            {
                _lock.EnterReadLock(); try { return _base.ContainsKey(key); } finally { _lock.ExitReadLock(); }
            }

            public void CopyTo(KeyValuePair<TKey, TValue>[] array, Int32 arrayIndex)
            {
                _lock.EnterWriteLock(); try { ((ICollection<KeyValuePair<TKey, TValue>>)_base).CopyTo(array,arrayIndex); } finally { _lock.ExitWriteLock(); }
            }

            public Boolean Remove(TKey key)
            {
                _lock.EnterWriteLock(); try { return _base.Remove(key); } finally { _lock.ExitWriteLock(); }
            }

            public Boolean Remove(KeyValuePair<TKey, TValue> item)
            {
                _lock.EnterWriteLock(); try { return ((ICollection<KeyValuePair<TKey, TValue>>)_base).Remove(item); } finally { _lock.ExitWriteLock(); }
            }

            public Boolean TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
            {
                _lock.EnterReadLock(); try { return _base.TryGetValue(key,out value); } finally { _lock.ExitReadLock(); }
            }

            public ICollection<TKey> Keys => _base.Keys;
            public ICollection<TValue> Values => _base.Values;
            public Boolean IsReadOnly => ((IDictionary<TKey,TValue>)_base).IsReadOnly;
            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
            {
                return _base.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

        }

    }
}
