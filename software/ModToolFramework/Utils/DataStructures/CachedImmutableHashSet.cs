using System.Collections.Generic;
using System.Collections.Immutable;

namespace ModToolFramework.Utils.DataStructures
{
    /// <summary>
    /// Handles obtaining an ImmutableHashSet in a way which reduces creation of arrays. (Therefore making it acceptable to use in memory-allocation sensitive areas such as render updates.)
    /// </summary>
    /// <typeparam name="TElement">The type of object which the underlying set holds.</typeparam>
    public class CachedImmutableHashSet<TElement> {
        private ImmutableHashSet<TElement> _cachedImmutableSet;
        private bool _cacheInvalid = true;

        /// <summary>
        /// Invalidates the cached immutable set.
        /// </summary>
        public void Invalidate() {
            this._cacheInvalid = true;
            this._cachedImmutableSet = null;
        }

        /// <summary>
        /// Gets the immutable set.
        /// </summary>
        /// <returns>immutableHashSet</returns>
        public ImmutableHashSet<TElement> Get(HashSet<TElement> set) {
            if (this._cacheInvalid || (this._cachedImmutableSet != null && this._cachedImmutableSet.Count != set.Count)) {
                this._cachedImmutableSet = set.Count > 0 ? set.ToImmutableHashSet() : ImmutableHashSet<TElement>.Empty;
                this._cacheInvalid = false;
            }

            return this._cachedImmutableSet;
        }
    }
}