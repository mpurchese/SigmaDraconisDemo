namespace SigmaDraconis.UI
{
    using Draconis.Shared;

    public static class ScreenShaker
    {
        private static float vx = 0;
        private static float vy = 0;
        private static int frames = 0;
        public static Vector2f CurrentOffset = new Vector2f();
        public static bool startShake;
        
        public static void Shake()
        {
            // Do it this way to ensure that nothing happens if the game is paused
            startShake = true;
            frames = 120;
        }

        public static void Update()
        {
            if (frames == 0) return;

            if (startShake)
            {
                CurrentOffset.X = 1.5f;
                vy = -0.1f;
                startShake = false;
            }

            for (int i = 0; i < 4; ++i)
            {
                vx -= CurrentOffset.X * 0.1f;
                vx *= 0.995f;
                CurrentOffset.X += vx;

                vy -= CurrentOffset.Y * 0.1f;
                vy *= 0.995f;
                CurrentOffset.Y += vy;
            }

            frames--;
            if (frames == 0)
            {
                CurrentOffset = new Vector2f();
            }
        }
    }
}
