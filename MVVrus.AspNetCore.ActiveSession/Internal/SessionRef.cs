namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class SessionRef
    {
        public virtual IServiceProvider Services => _session?.Value==null ? _requestServices : _session.Value.SessionServices;
        public virtual Boolean IsFromSession => _session?.Value != null;
        public virtual ISessionServicesHelper? SessionServiceHelper => _session?.Value as ISessionServicesHelper;

        internal /*Just for tests*/ virtual ILocalSession? Session =>_session?.Value;

        readonly Lazy<ILocalSession?> _session;
        readonly IServiceProvider _requestServices;
        ILocalSession? _presetSession = null;
        protected Func<ILocalSession?>? _getSessionFunc=null;

        protected SessionRef(IServiceProvider RequestServices) 
        {
            _requestServices = RequestServices;
            _session = new Lazy<ILocalSession?>(GetAvailableSession);
        }

        protected SessionRef(Func<ILocalSession>? GetSessionFunc, IServiceProvider RequestServices) : this(RequestServices) 
        {
            _getSessionFunc = GetSessionFunc;
        }

        public SessionRef(ILocalSession Session, IServiceProvider RequestServices):this(RequestServices)
        {
            _presetSession = Session;
            _getSessionFunc =  PresetSession;
            _presetSession =_session.Value;  //Call _session initilaization delegate immediately
        }

        ILocalSession? PresetSession() { return _presetSession; }

        ILocalSession? GetAvailableSession()
        {
            if(_getSessionFunc==null) return null;
            else {
                ILocalSession? t = _getSessionFunc(); 
                return t?.IsAvailable??false ? t : null;
            }
        }
    }
}
