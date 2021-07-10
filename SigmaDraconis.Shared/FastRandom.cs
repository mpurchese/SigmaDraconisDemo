namespace SigmaDraconis.Shared
{
    using System;

    public class FastRandom
    {
        public float[] randoms = new float[10000];
        public int i = 0;

        public FastRandom()
        {
            var random = new Random();
            for (int i = 0; i < 10000; i++)
            {
                this.randoms[i] = (float)random.NextDouble();
            }
        }

        public float NextFloat()
        {
            if (i >= 9998) i = 0;
            return this.randoms[++i];
        }

        public float NextFloat(float max)
        {
            return this.NextFloat() * max;
        }

        public float NextFloat(float min, float max)
        {
            return (this.NextFloat() * (max - min)) + min;
        }
    }
}
