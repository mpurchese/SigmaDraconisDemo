namespace SigmaDraconis.Shared
{
    using System.Collections.Generic;

    public static class Extensions
    {
        /// <summary>
        /// Extension method to add a value to a list only if the list does not already have this value
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <param name="list">The list</param>
        /// <param name="value">The value to add</param>
        /// <returns>True if value was added, false if it exists already</returns>
        public static bool AddIfNew<T>(this ICollection<T> list, T value)
        {
            if (!list.Contains(value))
            {
                list.Add(value);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Extension method to remove a value from a list in an exception safe way
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <param name="list">The list</param>
        /// <param name="value">The value to add</param>
        /// <returns>True if value was removed, false if it did not exist</returns>
        public static bool RemoveIfExists<T>(this ICollection<T> list, T value)
        {
            if (list.Contains(value))
            {
                list.Remove(value);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Extension method to compare to floats with a tolerance
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static bool ApproxEquals(this float left, float right, float tolerance)
        {
            return left - tolerance < right && left + tolerance > right;
        }

        public static int ToFahrenheit(this int centigrade)
        {
            return (centigrade * 9 / 5) + 32;
        }

        public static double ToFahrenheit(this double centigrade)
        {
            return (centigrade * 9 / 5) + 32;
        }

        public static int ToMph(this int metersPerSecond)
        {
            return (int)(metersPerSecond * 2.236936);
        }

        public static int ToKph(this int metersPerSecond)
        {
            return (int)(metersPerSecond * 3.6);
        }
    }
}
