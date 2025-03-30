using System;
using static MVVrus.AspNetCore.ActiveSession.Internal.ActiveSessionConstants;

namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class ActiveSessionFeature : IActiveSessionFeatureImpl
    {
        //These properties with internal access specifier are just for tetsting purposes
        internal IActiveSessionStore Store { get { return _store; } }
        internal ISession? Session { get { return _session; } }
        internal ILogger? Logger { get { return _logger; } }
        internal String TraceIdentifier { get { return _traceIdentifier; } }
        internal IActiveSession RawActiveSession { get { return _activeSession; } }

        public String? Suffix { get { return _suffix; } } //Currently is used for tests only

        readonly IActiveSessionStore _store;
        ISession? _session;
        readonly ILogger? _logger;
        String _traceIdentifier;
        IStoreActiveSessionItem _activeSession;
        bool _isLoaded;
        String? _suffix;


        public ActiveSessionFeature(IActiveSessionStore Store, ISession? Session, ILogger? Logger, String? TraceIdentifier, String? Suffix)
        {
            _logger=Logger;
            _traceIdentifier=TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            _logger?.LogTraceActiveSessionFeatureConstructor(_traceIdentifier);
            #endif
            _store= Store;
            _session = Session;
            _activeSession = DummySession;
            _suffix=Suffix;
            #if TRACE
            _logger?.LogTraceActiveSessionFeatureConstructorExit(_traceIdentifier);
            #endif
        }

        public IActiveSession ActiveSession { get { Load(); return _activeSession; } }

        public ILocalSession LocalSession { 
            get {return (ILocalSession?)(_activeSession.IsAvailable ? _activeSession.BaseGroup : null)??DummyLocalSession; }
        }

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
                if(_activeSession.IsAvailable) _store.DetachSession(_session!, _activeSession, _traceIdentifier);
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
                            _activeSession=_store.FetchOrCreateSession(_session, _traceIdentifier, _suffix)??DummySession;
                        }
                    }
                }
                catch(Exception exception)
                {
                    _logger?.LogWarningActiveSessionLoad(exception, _traceIdentifier);
                    _activeSession=DummySession;
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
                    _activeSession= _store.FetchOrCreateSession(_session, _traceIdentifier, _suffix)??DummySession;
                }
            }
            catch(Exception exception)
            {
                _logger?.LogWarningActiveSessionLoad(exception, _traceIdentifier);
                _activeSession=DummySession;
            }
            finally
            {
                _isLoaded = true;
            }
            #if TRACE
            _logger?.LogTraceActiveSessionFeatureLoadExit(_traceIdentifier);
            #endif
        }

        public Boolean RefreshActiveSession()
        {
            //TODO Add LogTrace
            if(!_isLoaded) return false;
            if(_activeSession.IsAvailable) {
                if(_session==null) 
                    throw new InvalidOperationException("Internal error: null environment reference for an available active session.");
                IStoreActiveSessionItem old_active_session = _activeSession;
                _activeSession = _store.FetchOrCreateSession(_session, _traceIdentifier, _suffix)??DummySession;
                _store.DetachSession(_session, old_active_session, _traceIdentifier);
                if(old_active_session==_activeSession) return false;
                return true;
            }
            else return false;
        }

        //(future) Implement LocalSession

        static readonly internal NullActiveSession DummySession = new NullActiveSession();
        static readonly internal NullLocalSession DummyLocalSession = new NullLocalSession();
    }
}
