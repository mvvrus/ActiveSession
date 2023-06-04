using static MVVrus.AspNetCore.ActiveSession.Internal.ActiveSessionConstants;

namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class ActiveSessionFeature : IActiveSessionFeature
    {
        //TODO Implement logging
        readonly IActiveSessionStore _store;
        ISession? _session;
        IActiveSession _activeSession;
        bool _isLoaded;
        String _trace_identifier;

        public ActiveSessionFeature(IActiveSessionStore Store, ISession? Session, String? TraceIdentifier)
        {
            _store = Store;
            _session = Session;
            _activeSession = DummySession;
            _trace_identifier=TraceIdentifier??UNKNOWN_TRACE_IDENTIFIER;
        }

        public IActiveSession ActiveSession { get { Load(); return _activeSession; } }

        public async Task CommitAsync(CancellationToken Token = default)
        {
            if (_isLoaded)
                await _activeSession.CommitAsync(Token,_trace_identifier);
        }

        public void Clear()
        {
            if (_isLoaded)
            {
                _activeSession = DummySession;
                _isLoaded = false;
            }
            _session = null;
            _trace_identifier=UNKNOWN_TRACE_IDENTIFIER;
        }

        public bool IsLoaded { get { return _isLoaded; } }

        public async Task LoadAsync(CancellationToken Token = default)
        {
            //TODO perform logging
            if (!_isLoaded)
            {
                try
                {
                    if (_session != null)
                    {
                        await _session!.LoadAsync(Token);
                        if (_session!.IsAvailable) _activeSession= _store.FetchOrCreateSession(_session, _trace_identifier);
                    }
                }
                catch
                {
                    //TODO Log error
                }
                finally
                {
                    _isLoaded = true;
                }
            }
            return;
        }

        void Load()
        {
            //TODO perform logging
            if (_isLoaded) return;
            try
            {
                if (_session != null)
                {
                    if(_session!.IsAvailable) _activeSession = _store.FetchOrCreateSession(_session, _trace_identifier);
                }
            }
            catch
            {
                //TODO Log error
            }
            finally
            {
                _isLoaded = true;
            }
        }

        static readonly NullActiveSession DummySession = new NullActiveSession();
    }
}
