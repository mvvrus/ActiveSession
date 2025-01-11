using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using static MVVrus.AspNetCore.ActiveSession.Internal.ActiveSessionConstants;

namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class ActiveSession : IActiveSession, IActiveSessionInternal, IDisposable
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
        readonly ConcurrentDictionary<Type, SemaphoreSlim> _serviceLocks = new ConcurrentDictionary<Type, SemaphoreSlim>();


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
            , String? BaseId=null
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

        public IRunner<TResult>? GetRunner<TResult>(int RunnerNumber, HttpContext Context)
        {
            CheckDisposed();
            String trace_identifier = Context.TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            _logger?.LogTraceActiveSessionGetRunner(_logSessionId, RunnerNumber, trace_identifier);
            #endif
            IRunner? base_result = GetNonTypedRunner(RunnerNumber, Context);
            IRunner<TResult>? result = base_result as IRunner<TResult>;
            if (result==null && base_result!=null) 
                _logger?.LogWarningNoExpectedRunnerInCache(new RunnerId(this,RunnerNumber), trace_identifier);
            #if TRACE
            _logger?.LogTraceActiveSessionGetRunnerExit(_logSessionId, RunnerNumber, trace_identifier);
            #endif
            return result;
        }

        public async ValueTask<IRunner<TResult>?> GetRunnerAsync<TResult>(Int32 RunnerNumber, HttpContext Context, CancellationToken Token)
        {
            CheckDisposed();
            String trace_identifier = Context.TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            _logger?.LogTraceActiveSessionGetRunnerAsync(_logSessionId, RunnerNumber, trace_identifier);
            #endif
            IRunner? base_result = await GetNonTypedRunnerAsync(RunnerNumber, Context, Token);
            IRunner<TResult>? result = base_result as IRunner<TResult>;
            if(result==null && base_result!=null)
                _logger?.LogWarningNoExpectedRunnerInCache(new RunnerId(this, RunnerNumber), trace_identifier);
            #if TRACE
            _logger?.LogTraceActiveSessionGetRunnerAsyncExit(_logSessionId, RunnerNumber, trace_identifier);
            #endif
            return result;
        }

        public IRunner? GetNonTypedRunner(Int32 RunnerNumber, HttpContext Context)
        {
            CheckDisposed();
            String trace_identifier = Context.TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            _logger?.LogTraceActiveSessionGetNonTypedRunner(_logSessionId, RunnerNumber, trace_identifier);
            #endif
            IRunner? fetched = _store.GetRunner(Context.Session, this, _runnerManager, RunnerNumber, trace_identifier);
            if(fetched!=null) _isFresh=false;
            #if TRACE
            _logger?.LogTraceActiveSessionGetNonTypedRunnerExit(_logSessionId, RunnerNumber, trace_identifier);
            #endif
            return fetched;
        }

        public async ValueTask<IRunner?> GetNonTypedRunnerAsync(Int32 RunnerNumber, HttpContext Context, CancellationToken Token = default)
        {
            CheckDisposed();
            String trace_identifier = Context.TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            _logger?.LogTraceActiveSessionGetNonTypedRunnerAsync(_logSessionId, RunnerNumber, trace_identifier);
            #endif
            IRunner? fetched =  await _store.GetRunnerAsync(Context.Session, this, _runnerManager, RunnerNumber, trace_identifier, Token);
            _store.GetRunner(Context.Session, this, _runnerManager, RunnerNumber, trace_identifier);
            if(fetched!=null) _isFresh=false;
            #if TRACE
            _logger?.LogTraceActiveSessionGetNonTypedRunnerAsyncExit(_logSessionId, RunnerNumber, trace_identifier);
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

        public IServiceProvider SessionServices { get { return _scope.ServiceProvider; } }

        public String Id { get => _sessionId; }

        public String BaseId { get => _baseId; }

        public CancellationToken CompletionToken { get; private set; }

        public Task CleanupCompletionTask { get; private set; }

        public IDictionary<String, Object> Properties { get=>_properties; }


        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1)!=0) return;
            #if TRACE
            _logger?.LogTraceActiveSessionDispose(_logSessionId);
            #endif
            try {
                _cts.Cancel();
            }
            catch(Exception e) 
            {
                _logger?.LogWarningExceptionWhileActiveSessionDispose(e, _logSessionId);
            }
            _cts.Dispose();
            _properties.Dispose();
            foreach(SemaphoreSlim semaphore in _serviceLocks.Values)
                lock(semaphore) {
                    semaphore.Release(); 
                    semaphore.Dispose();
                }
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

        public Task? TrackRunnerCleanup(Int32 RunnerNumber)
        {
            return _runnerManager.GetRunnerCleanupTrackingTask(this, RunnerNumber);
        }

        public async Task<Boolean> WaitForServiceAsync(Type ServiceType, TimeSpan Timeout, CancellationToken Token)
        {
            CheckDisposed();
            //Set a semaphore MaxCount below to 2, not 1, to avoid SemaphoreFullException in Dispose()
            SemaphoreSlim semaphore = _serviceLocks.GetOrAdd(ServiceType, (_) => new SemaphoreSlim(1,2));
            #if TRACE
            _logger?.LogTraceActiveSessionRequestServiceLock(ServiceType.FullName??UNKNOWN_TYPE, _logSessionId);
            #endif
            Boolean wait_result = await semaphore.WaitAsync(Timeout, Token);
            if(wait_result)
                lock(semaphore) if(semaphore.Wait(0)) semaphore.Release();  //This is a check if the semaphore has been disposed
            #if TRACE
            _logger?.LogTraceActiveSessionAcquireServiceLock(ServiceType.FullName??UNKNOWN_TYPE, _logSessionId, wait_result);
            #endif
            return wait_result;
        }

        public void ReleaseService(Type ServiceType)
        {
            if(_disposed!=0) return;
            SemaphoreSlim? semaphore;
            if(_serviceLocks.TryGetValue(ServiceType, out semaphore)) {
                if(semaphore.CurrentCount>0 && Volatile.Read(ref _disposed)==0)
                    throw new InvalidOperationException();
                #if TRACE
                _logger?.LogTraceActiveSessionReleaseServiceLock(ServiceType.FullName??UNKNOWN_TYPE, _logSessionId);
                #endif
                semaphore.Release();
            }
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
