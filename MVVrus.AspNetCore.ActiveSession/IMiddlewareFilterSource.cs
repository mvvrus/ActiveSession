/// TODO document this!
public interface IMiddlewareFilterSource
{
    /// TODO document this!
    public IMiddlewareFilter Create(Int32 Order);
    /// TODO document this!
    public Boolean HasSuffix { get; }
    /// TODO document this!
    public String GetPrettyName() { return "<unspecified filter>"; }
}

