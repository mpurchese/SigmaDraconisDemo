namespace Draconis.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class ExtensionMethods
    {
        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }

        public static bool In<T>(this T x, params T[] set)
        {
            return set.Contains(x);
        }

        public static bool Between<T>(this T x, T min, T max) where T : IComparable<T>
        {
            return x.CompareTo(min) >= 0 && x.CompareTo(max) <= 0;
        }

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer = null)
        {
            return new HashSet<T>(source, comparer);
        }
    }
}
