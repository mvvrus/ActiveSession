namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    /// This static class is a container for extension methods for <see cref="ILocalSession"/> interface.
    /// </summary>
    public static class LocalSessionExtensions
    {

        /// <summary>
        /// Pass responsibility of disposing the disposable object passed to an object implementing the <see cref="ILocalSession"/> interface.
        /// </summary>
        /// <param name="Session">The <see cref="ILocalSession"/> implementation to which the responsibility is to be passed.</param>
        /// <param name="Disposable">The disposable object passed.</param>
        /// <returns>The registration object. Dispose it to dispose the object passed and to recall the responsibility.</returns>
        /// <exception cref="InvalidOperationException">Raised if an attempt to pass reponsibility to an unavalable session was detected.</exception>
        public static IDisposable TakeOwnership(this ILocalSession Session, IDisposable Disposable)
        {
            if(Session==null || !Session.IsAvailable) throw new InvalidOperationException("Active session or group is not available.");
            return new Registration(
                Session.CompletionToken, Disposable);
        }

        class Registration: IDisposable
        {
            CancellationToken _token;
            CancellationTokenRegistration _registration;
            IDisposable? _disposable;

            public Registration(CancellationToken Token, IDisposable Disposable)
            {
                _disposable = Disposable??throw new ArgumentNullException(nameof(Disposable));
                _token = Token;
                _registration = Token.Register(static r => { Registration.DisposeRegistration((Registration)r!); }, this);
            }

            public void Dispose()
            {
                DisposeRegistration(this);
            }

            static void DisposeRegistration(Registration Registration)
            {
                IDisposable? disposable = Interlocked.Exchange(ref Registration._disposable, null);
                if(disposable != null) Registration._registration.Dispose();
                disposable?.Dispose();
            }
        }
    }
}
