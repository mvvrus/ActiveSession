/// TODO document this!
public interface IMiddlewareFilterSource
{
    /// TODO document this!
    public IMiddlewareFilter Create(Int32 Order);
    /// TODO document this!
    public Boolean HasSuffix { get; }
    /// TODO document this!
    public Boolean IsGroupable { get; }
    /// TODO document this!
    public Boolean Group(IMiddlewareFilter GroupFilter);
}

