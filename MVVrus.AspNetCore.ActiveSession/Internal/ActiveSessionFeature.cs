namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class ActiveSessionFeature : IActiveSessionFeature
    {
        //TODO Implement logging
        readonly IActiveSessionStore _store;
        ISession? _session;
        IActiveSession _activeSession;
        bool _isLoaded;

        public ActiveSessionFeature(IActiveSessionStore Store, ISession? Session)
        {
            _store = Store;
            _session = Session;
            _activeSession = DummySession;
        }

        public IActiveSession ActiveSession { get { Load(); return _activeSession; } }

        public async Task CommitAsync(CancellationToken Token = default)
        {
            if (_isLoaded && _activeSession != null)
                await _activeSession!.CommitAsync(Token);
            if (Token.CanBeCanceled) Token.ThrowIfCancellationRequested();
            if (_isLoaded && _session != null && _session.IsAvailable)
                await _session.CommitAsync(Token);
        }

        public void Clear()
        {
            if (_isLoaded)
            {
                _activeSession = DummySession;
                _isLoaded = false;
            }
            _session = null;
        }

        public bool IsLoaded { get { return _isLoaded; } }

        //TODO Check _activeSession
        public bool IsSessionAvailable => _session != null && _session.IsAvailable;

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
                        _activeSession = _store.FetchOrCreate(_session);
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
                    _activeSession = _store.FetchOrCreate(_session);
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
