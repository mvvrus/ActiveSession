using System;
using static MVVrus.AspNetCore.ActiveSession.Internal.ActiveSessionConstants;

namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class ActiveSessionFeature : IActiveSessionFeature
    {
        //These fields have internal access specifier just for tetstin purposes
        readonly internal IActiveSessionStore _store;
        internal ISession? _session;
        readonly internal ILogger? _logger;
        internal String _trace_identifier;
        internal IActiveSession _activeSession;

        bool _isLoaded;


        public ActiveSessionFeature(IActiveSessionStore Store, ISession? Session, ILogger? Logger, String? TraceIdentifier)
        {
            _logger=Logger;
            _trace_identifier=TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
            #if TRACE
            _logger?.LogTraceActiveSessionFeatureConstructor(_trace_identifier);
            #endif
            _store= Store;
            _session = Session;
            _activeSession = DummySession;
            #if TRACE
            _logger?.LogTraceActiveSessionFeatureConstructorExit(_trace_identifier);
            #endif
        }

        public IActiveSession ActiveSession { get { Load(); return _activeSession; } }

        public async Task CommitAsync(CancellationToken Token = default)
        {
            #if TRACE
            _logger?.LogTraceActiveSessionFeatureCommitAsync(_trace_identifier);
            #endif
            if (_isLoaded && _session!=null) {
                #if TRACE
                _logger?.LogTraceActiveSessionFeatureCommitAsyncActiveSessionWait(_trace_identifier);
                #endif
                await _session!.CommitAsync(Token);
            }
            #if TRACE
            _logger?.LogTraceActiveSessionFeatureCommitAsyncExit(_trace_identifier);
            #endif
        }

        public void Clear()
        {
            #if TRACE
            _logger?.LogTraceActiveSessionFeatureClear(_trace_identifier);
            #endif
            if (_isLoaded)
            {
                #if TRACE
                _logger?.LogTraceActiveSessionFeaturePerformClear(_trace_identifier);
                #endif
                _activeSession= DummySession;
                _isLoaded = false;
            }
            _session = null;
            #if TRACE
            _logger?.LogTraceActiveSessionFeatureClearExit(_trace_identifier);
            #endif
            _trace_identifier=UNKNOWN_TRACE_IDENTIFIER;
        }

        public bool IsLoaded { get { return _isLoaded; } }

        public async Task LoadAsync(CancellationToken Token = default)
        {
            #if TRACE
            _logger?.LogTraceActiveSessionFeatureLoadAsync(_trace_identifier);
            #endif
            if (!_isLoaded)
            {
                #if TRACE
                _logger?.LogTraceActiveSessionFeatureLoadAsyncLoading(_trace_identifier);
                #endif
                try {
                    if (_session != null)
                    {
                        #if TRACE
                        _logger?.LogTraceActiveSessionFeatureLoadAsyncSessionWait(_trace_identifier);
                        #endif
                        await _session!.LoadAsync(Token);
                        #if TRACE
                        _logger?.LogTraceActiveSessionFeatureLoadAsyncSessionWaitEnded(_trace_identifier);
                        #endif
                        if (_session!.IsAvailable) {
                            #if TRACE
                            _logger?.LogTraceActiveSessionFeatureLoadAsyncGetActiveSession(_trace_identifier);
                            #endif
                            _activeSession=_store.FetchOrCreateSession(_session, _trace_identifier);
                        }
                    }
                }
                catch(Exception exception)
                {
                    _logger?.LogWarningActiveSessionLoad(exception, _trace_identifier);
                }
                finally
                {
                    _isLoaded = true;
                }
            }
            #if TRACE
            _logger?.LogTraceActiveSessionFeatureLoadAsyncExit(_trace_identifier);
            #endif
            return;
        }

        void Load()
        {
            if (_isLoaded) return;
            #if TRACE
            _logger?.LogTraceActiveSessionFeatureLoad(_trace_identifier);
            #endif
            try {
                if (_session != null &&_session!.IsAvailable)
                {
                    #if TRACE
                    _logger?.LogTraceActiveSessionFeatureLoadGetActiveSession(_trace_identifier);
                    #endif
                    _activeSession= _store.FetchOrCreateSession(_session, _trace_identifier);
                }
            }
            catch(Exception exception)
            {
                _logger?.LogWarningActiveSessionLoad(exception, _trace_identifier);
            }
            finally
            {
                _isLoaded = true;
            }
            #if TRACE
            _logger?.LogTraceActiveSessionFeatureLoadExit(_trace_identifier);
            #endif
        }

        static readonly internal NullActiveSession DummySession = new NullActiveSession();
    }
}
