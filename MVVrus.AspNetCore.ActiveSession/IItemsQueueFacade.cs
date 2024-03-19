namespace MVVrus.AspNetCore.ActiveSession
{
    /// <summary>
    ///  TODO
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    public interface IItemsQueueFacade<TItem>
    {
        /// <summary>
        /// TODO
        /// </summary>
        Boolean IsAddingCompleted { get; }
        /// <summary>
        /// TODO
        /// </summary>
        void CompleteAdding();
        /// <summary>
        ///  TODO
        /// </summary>
        /// <param name="Item"></param>
        /// <param name="Timeout"></param>
        /// <param name="Token"></param>
        Boolean TryAdd(TItem Item, Int32 Timeout, CancellationToken Token);
        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="Item"></param>
        /// <returns></returns>
        Boolean TryTake(out TItem Item);
        /// <summary>
        /// TODO
        /// </summary>
        Int32 Count { get; }
    }
}
