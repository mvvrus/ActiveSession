using System;
using static MVVrus.AspNetCore.ActiveSession.Internal.ActiveSessionConstants;

namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class ActiveSessionFeature : IActiveSessionFeature, IActiveSessionFeatureControl
    {
        //These properties with internal access specifier are just for tetsting purposes
        internal IActiveSessionStore Store { get { return _store; } }
        internal ISession? Session { get { return _session; } }
        internal ILogger? Logger { get { return _logger; } }
        internal String TraceIdentifier { get { return _traceIdentifier; } }
        internal IActiveSession RawActiveSession { get { return _activeSession; } }

        readonly IActiveSessionStore _store;
        ISession? _session;
        readonly ILogger? _logger;
        String _traceIdentifier;
        IActiveSession _activeSession;
        bool _isLoaded;


        public ActiveSessionFeature(IActiveSessionStore Store, ISession? Session, ILogger? Logger, String? TraceIdentifier)
        {
            _logger=Logger;
            _traceIdentifier=TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            _logger?.LogTraceActiveSessionFeatureConstructor(_traceIdentifier);
            #endif
            _store= Store;
            _session = Session;
            _activeSession = DummySession;
            #if TRACE
            _logger?.LogTraceActiveSessionFeatureConstructorExit(_traceIdentifier);
            #endif
        }

        public IActiveSession ActiveSession { get { Load(); return _activeSession; } }

        public async Task CommitAsync(CancellationToken Token = default)
        {
            #if TRACE
            _logger?.LogTraceActiveSessionFeatureCommitAsync(_traceIdentifier);
            #endif
            if (_isLoaded && _session!=null && _session.IsAvailable) {
                #if TRACE
                _logger?.LogTraceActiveSessionFeatureCommitAsyncActiveSessionWait(_traceIdentifier);
                #endif
                await _session!.CommitAsync(Token);
            }
            #if TRACE
            _logger?.LogTraceActiveSessionFeatureCommitAsyncExit(_traceIdentifier);
            #endif
        }

        public void Clear()
        {
            #if TRACE
            _logger?.LogTraceActiveSessionFeatureClear(_traceIdentifier);
            #endif
            if (_isLoaded)
            {
                #if TRACE
                _logger?.LogTraceActiveSessionFeaturePerformClear(_traceIdentifier);
                #endif
                _activeSession= DummySession;
                _isLoaded = false;
            }
            _session = null;
            #if TRACE
            _logger?.LogTraceActiveSessionFeatureClearExit(_traceIdentifier);
            #endif
            _traceIdentifier=UNKNOWN_TRACE_IDENTIFIER;
        }

        public bool IsLoaded { get { return _isLoaded; } } 

        public async Task LoadAsync(CancellationToken Token = default)
        {
            #if TRACE
            _logger?.LogTraceActiveSessionFeatureLoadAsync(_traceIdentifier);
            #endif
            if (!_isLoaded)
            {
                #if TRACE
                _logger?.LogTraceActiveSessionFeatureLoadAsyncLoading(_traceIdentifier);
                #endif
                try {
                    if (_session != null)
                    {
                        #if TRACE
                        _logger?.LogTraceActiveSessionFeatureLoadAsyncSessionWait(_traceIdentifier);
                        #endif
                        await _session!.LoadAsync(Token);
                        #if TRACE
                        _logger?.LogTraceActiveSessionFeatureLoadAsyncSessionWaitEnded(_traceIdentifier);
                        #endif
                        if (_session!.IsAvailable) {
                            #if TRACE
                            _logger?.LogTraceActiveSessionFeatureLoadAsyncGetActiveSession(_traceIdentifier);
                            #endif
                            _activeSession=_store.FetchOrCreateSession(_session, _traceIdentifier)??DummySession;
                        }
                    }
                }
                catch(Exception exception)
                {
                    _logger?.LogWarningActiveSessionLoad(exception, _traceIdentifier);
                }
                finally
                {
                    _isLoaded = true;
                }
            }
            #if TRACE
            _logger?.LogTraceActiveSessionFeatureLoadAsyncExit(_traceIdentifier);
            #endif
            return;
        }

        public ActiveSessionStoreStats? GetCurrentStoreStatistics()
        {
            return _store.GetCurrentStatistics();
        }

        internal void Load()
        {
            if (_isLoaded) return;
            #if TRACE
            _logger?.LogTraceActiveSessionFeatureLoad(_traceIdentifier);
            #endif
            try {
                if (_session != null &&_session!.IsAvailable)
                {
                    #if TRACE
                    _logger?.LogTraceActiveSessionFeatureLoadGetActiveSession(_traceIdentifier);
                    #endif
                    _activeSession= _store.FetchOrCreateSession(_session, _traceIdentifier)??DummySession;
                }
            }
            catch(Exception exception)
            {
                _logger?.LogWarningActiveSessionLoad(exception, _traceIdentifier);
            }
            finally
            {
                _isLoaded = true;
            }
            #if TRACE
            _logger?.LogTraceActiveSessionFeatureLoadExit(_traceIdentifier);
            #endif
        }

        public void SetSession(ISession? Session, String? TraceIdentifier)
        {
            if (_isLoaded) Clear(); //Safety precaution
            #if TRACE
            _logger?.LogTraceActiveSessionFeatureSetSession(_traceIdentifier);
            #endif
            _traceIdentifier=TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            _session=Session;
            #if TRACE
            _logger?.LogTraceActiveSessionFeatureSetSessionExit(_traceIdentifier);
            #endif
        }

        static readonly internal NullActiveSession DummySession = new NullActiveSession();
        static readonly internal NullLocalSession DummyLocalSession = new NullLocalSession();
    }
}
