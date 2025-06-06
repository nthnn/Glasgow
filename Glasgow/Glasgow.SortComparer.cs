/*
 * Copyright 2025 Nathanne Isip
 * This file is part of Glasgow (https://github.com/nthnn/Glasgow)
 * This code is licensed under MIT license (see LICENSE for details)
 */

namespace Glasgow
{
    /// <summary>
    /// Provides a flexible comparison mechanism for sorting heterogeneous objects in either ascending or descending order.
    /// This comparer is primarily used in data structures or algorithms that require ordering—such as sorting collections—
    /// and handles <c>null</c> values gracefully, ensuring deterministic and consistent comparisons.
    /// </summary>
    /// <remarks>
    /// The comparison logic supports objects that implement <see cref="IComparable"/>, and falls back to 
    /// case-insensitive string comparison if the objects do not implement that interface. This makes it suitable 
    /// for generic scenarios where values of various runtime types may need to be sorted.
    /// 
    /// Sorting direction is configurable at construction time via the <c>IsAscending</c> parameter.
    /// </remarks>
    public class SortComparer : IComparer<object>
    {
        /// <summary>
        /// Determines whether the sort order should be ascending (<c>true</c>) or descending (<c>false</c>).
        /// This value is set during instantiation and controls the result inversion of the comparison.
        /// </summary>
        private readonly bool _isAscending;

        /// <summary>
        /// Initializes a new instance of the <see cref="SortComparer"/> class with the specified sort order.
        /// </summary>
        /// <param name="IsAscending">
        /// A boolean flag indicating the desired sort order:
        /// <list type="bullet">
        /// <item><c>true</c> — values will be sorted in ascending order (e.g., A-Z, 1-9).</item>
        /// <item><c>false</c> — values will be sorted in descending order (e.g., Z-A, 9-1).</item>
        /// </list>
        /// </param>
        public SortComparer(bool IsAscending)
        {
            _isAscending = IsAscending;
        }

        /// <summary>
        /// Compares two objects and returns an integer that indicates their relative position in the sort order.
        /// </summary>
        /// <param name="X">The first object to compare. May be <c>null</c>.</param>
        /// <param name="Y">The second object to compare. May be <c>null</c>.</param>
        /// <returns>
        /// An integer that indicates the relative order of the objects:
        /// <list type="bullet">
        /// <item><c>&lt; 0</c>: <paramref name="X"/> precedes <paramref name="Y"/> in the sort order.</item>
        /// <item><c>0</c>: <paramref name="X"/> and <paramref name="Y"/> are considered equal in sort order.</item>
        /// <item><c>&gt; 0</c>: <paramref name="X"/> follows <paramref name="Y"/> in the sort order.</item>
        /// </list>
        /// The sign of the result is inverted if the comparer is configured for descending sort order.
        /// </returns>
        /// <remarks>
        /// Comparison behavior:
        /// <list type="bullet">
        /// <item>If both values are <c>null</c>, they are considered equal.</item>
        /// <item><c>null</c> values are always considered lesser than non-null values.</item>
        /// <item>If both values implement <see cref="IComparable"/>, their native comparison is used.</item>
        /// <item>If native comparison is not available, a case-insensitive string comparison is used as fallback.</item>
        /// </list>
        /// </remarks>
        public int Compare(object? X, object? Y)
        {
            if (X == null && Y == null)
                return 0;

            if (X == null)
                return _isAscending ? -1 : 1;

            if (Y == null)
                return _isAscending ? 1 : -1;

            if (X is IComparable comparableX)
            {
                int comparison = comparableX.CompareTo(Y);
                return _isAscending ? comparison : -comparison;
            }

            int stringComparison = string.Compare(
                X.ToString(),
                Y.ToString(),
                StringComparison.OrdinalIgnoreCase
            );
            return _isAscending ? stringComparison : -stringComparison;
        }
    }
}
