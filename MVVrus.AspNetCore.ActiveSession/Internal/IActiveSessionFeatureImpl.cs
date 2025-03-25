namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal interface IActiveSessionFeatureImpl: IActiveSessionFeature
    {
        void Clear();
        String? Suffix { get; }
    }
}
