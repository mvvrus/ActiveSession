
namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class NullLocalSession : ILocalSession
    {
        protected const String MESSAGE = "Invalid operation: a LocalSession or ActiveSession is not available";

        public String BaseId =>  "<null session Id>";

        public bool IsAvailable => false;

        public IServiceProvider SessionServices => throw new InvalidOperationException(MESSAGE);

        public IDictionary<String, Object> Properties => throw new NotImplementedException();

    }
}
