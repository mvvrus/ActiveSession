
namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class ActiveSessionGroup : RefDisposable, IStoreGroupItem
    {
        String _id;
        IServiceScope _scope;
        ConcurentSortedDictionary<String,Object> _properties;
        CancellationToken _token;
        CancellationTokenSource _cts;

        public String Id => _id;

        public Boolean IsAvailable => true;

        public IServiceProvider SessionServices => _scope.ServiceProvider;

        public CancellationToken CompletionToken => _token;

        public IDictionary<String, Object> Properties => _properties; //TODO Implement with concurent access

        public ActiveSessionGroup(String Id, IServiceProvider RootSP)
        {
            _id = Id;
            _properties = new ConcurentSortedDictionary<String, Object>();
            _scope=RootSP.CreateScope();
            _cts = new CancellationTokenSource();
            _token = _cts.Token;
        }

        protected override void Dispose(Boolean Disposing)
        {
            if(Disposing) {
                _scope.Dispose();
                _properties.Dispose();
                _cts.Cancel();
                _cts.Dispose();
            }
            base.Dispose(Disposing);
        }

    }
}
