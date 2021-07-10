namespace Draconis.Shared
{
    using System;

    public class Mathf
    {
        public const float PI = 3.14159265359f;
        public const float twoPI = 6.28318531f;

        public static float Max(float left, float right)
        {
            return left < right ? right : left;
        }

        public static float Min(float left, float right)
        {
            return left > right ? right : left;
        }

        public static float Clamp(float left, float min, float max)
        {
            if (left < min) return min;
            if (left > max) return max;
            return left;
        }

        public static int Clamp(int left, int min, int max)
        {
            if (left < min) return min;
            if (left > max) return max;
            return left;
        }

        public static float Abs(float x)
        {
            return x >= 0 ? x : 0 - x;
        }

        public static float Sin(float a)
        {
            return (float)Math.Sin(a);
        }

        public static float Cos(float a)
        {
            return (float)Math.Cos(a);
        }

        public static float Tan(float a)
        {
            return (float)Math.Tan(a);
        }

        public static float Asin(float a)
        {
            return (float)Math.Asin(a);
        }

        public static float Acos(float a)
        {
            return (float)Math.Acos(a);
        }

        public static float Atan(float a)
        {
            return (float)Math.Atan(a);
        }

        public static float Atan2(float x, float y)
        {
            return (float)Math.Atan2(x, y);
        }

        public static float Log(float a)
        {
            return (float)Math.Log(a);
        }

        public static float Sqrt(float a)
        {
            return (float)Math.Sqrt(a);
        }

        /// <summary>
        /// Returns the angle, in radians, of angle2 to angle1, in the range -PI to +PI
        /// </summary>
        public static float AngleBetween(float angle1, float angle2)
        {
            var delta = (angle2 - angle1) % (Mathf.PI * 2f);
            while (delta < 0 - Mathf.PI) delta += Mathf.PI * 2f;
            while (delta > Mathf.PI) delta -= Mathf.PI * 2f;
            return delta;
        }
    }
}
