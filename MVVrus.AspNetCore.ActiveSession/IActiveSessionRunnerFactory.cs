namespace MVVrus.AspNetCore.ActiveSession
{
    public interface IActiveSessionRunnerFactory<in TRequest, TResult>
    {
        IActiveSessionRunner<TResult>? Create(TRequest Request, IServiceProvider Services);
    }
}
