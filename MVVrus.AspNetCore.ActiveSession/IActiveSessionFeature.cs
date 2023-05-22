using Microsoft.AspNetCore.Routing.Template;

namespace MVVrus.AspNetCore.ActiveSession
{
    public interface IActiveSessionFeature
    {
        IActiveSession ActiveSession { get; }
        public Boolean IsLoaded { get; }
        public Task LoadAsync(CancellationToken Token=default);
        public Task CommitAsync(CancellationToken Token = default);
        public void Clear();
    }
}
