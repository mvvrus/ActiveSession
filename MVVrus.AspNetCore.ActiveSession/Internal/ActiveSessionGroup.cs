
namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class ActiveSessionGroup : RefDisposable, IStoreGroupItem
    {
        String _id;
        IServiceScope _scope;
        IDictionary<String,Object> _properties;

        public String Id => _id;

        public Boolean IsAvailable => true;

        public IServiceProvider SessionServices => _scope.ServiceProvider;

        public IDictionary<String, Object> Properties => _properties;

        public ActiveSessionGroup(String Id, IServiceProvider RootSP)
        {
            _id = Id;
            _properties = new Dictionary<String, Object>();
            _scope=RootSP.CreateScope();
        }

        protected override void Dispose(Boolean Disposing)
        {
            if(Disposing) _scope.Dispose();
            base.Dispose(Disposing);
        }

    }
}
