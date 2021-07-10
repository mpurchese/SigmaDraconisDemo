namespace SigmaDraconis.Shared
{
    using System;

    public class Rand
    {
        private static Random random = new Random();

        public static int Next(int maxValue)
        {
            return random.Next(maxValue);
        }

        public static float NextFloat()
        {
            return (float)random.NextDouble();
        }

        public static double NextDouble()
        {
            return random.NextDouble();
        }
    }
}
