/// <summary>
/// TODO document this!
/// </summary>
public interface IMiddlewareFilter
{
    /// TODO document this!
    public Int32 MinOrder { get; }

    /// TODO document this!
    public (Boolean WasMapped, String? SessionSuffix, Int32 Order) Apply(HttpContext Context);

    /// TODO document this!
    public String GetPrettyName() { return "<unspecified filter>"; } 
}

