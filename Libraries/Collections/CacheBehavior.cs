namespace KGySoft.Collections
{
    /// <summary>
    /// Possible behaviors of <see cref="Cache{TKey,TValue}"/> when the cache store is full and an element has to be removed.
    /// </summary>
    /// <seealso cref="Cache{TKey,TValue}"/>
    /// <seealso cref="Cache{TKey,TValue}.Touch"/>
    /// <seealso cref="Cache{TKey,TValue}.Capacity"/>
    public enum CacheBehavior
    {
        /// <summary>
        /// Represents an element removal strategy for a <see cref="Cache{TKey,TValue}"/> instance,
        /// where the the oldest (firstly stored) element will be removed when a new element has to be stored and the
        /// cache is full (that is, when <see cref="Cache{TKey,TValue}.Count"/> reaches <see cref="Cache{TKey,TValue}.Capacity"/>).
        /// <br/>This is the suggested behavior when loading a non-cached element is very fast, or when firstly added elements are typically not retrieved again,
        /// or when cache is never full.
        /// <br/>With this strategy element is access is slightly faster than in case of <see cref="RemoveLeastRecentUsedElement"/> because no extra administration is required.
        /// </summary>
        RemoveOldestElement,

        /// <summary>
        /// Represents an element removal strategy for a <see cref="Cache{TKey,TValue}"/> instance,
        /// where the least recent used element will be removed when a new element has to be stored and the
        /// cache is full (that is, when <see cref="Cache{TKey,TValue}.Count"/> reaches <see cref="Cache{TKey,TValue}.Capacity"/>).
        /// <br/>This is the suggested behavior when loading a non-cached element is slow, the cache is often full, and there are elements that are
        /// typically accessed more often than the others. This is the default behavior when a <see cref="Cache{TKey,TValue}"/> instance is instantiated.
        /// <br/>With this strategy element is access is slightly slower than in case of <see cref="RemoveOldestElement"/> because
        /// whenever an element is accessed, it is renewed in the evaluation order. (See also <see cref="Cache{TKey,TValue}.Touch"/>. method)
        /// </summary>
        RemoveLeastRecentUsedElement
    }
}
