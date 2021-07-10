namespace SigmaDraconis.Shared
{
    using System;

    public static class PerfMonitor
    {
        private static int updateFrameCount = 0;
        private static int drawFrameCount = 0;
        private static long drawTickCount = 0;
        private static long updateTickCount = 0;
        private static DateTime lastDrawResetTime;
        private static DateTime lastUpdateResetTime;

        public static int DrawFramesPerSecond { get; private set; } = 60;
        public static int UpdateFramesPerSecond { get; private set; } = 60;
        public static int AverageDrawTicks { get; private set; } = 0;
        public static int AverageUpdateTicks { get; private set; } = 0;
        public static int ShadowTriCount { get; set; } = 0;
        public static int DrawCounter { get; set; } = 0;
        public static int DrawsPerFrame { get; set; } = 0;

        public static void Draw(long ticks)
        {
            ++drawFrameCount;
            drawTickCount += ticks;
            if (drawFrameCount == 100)
            {
                var timeElapsed = (int)(DateTime.UtcNow - lastDrawResetTime).TotalMilliseconds;
                lastDrawResetTime = DateTime.UtcNow;
                DrawFramesPerSecond = (int)Math.Round(100000.0 / timeElapsed);
                AverageDrawTicks = (int)(drawTickCount / 100);
                drawFrameCount = 0;
                drawTickCount = 0;
                DrawsPerFrame = DrawCounter;
                DrawCounter = 0;
            }
        }

        public static void Update(long ticks)
        {
            ++updateFrameCount;
            updateTickCount += ticks;
            if (updateFrameCount == 100)
            {
                var timeElapsed = (int)(DateTime.UtcNow - lastUpdateResetTime).TotalMilliseconds;
                lastUpdateResetTime = DateTime.UtcNow;
                UpdateFramesPerSecond = (int)Math.Round(100000.0 / timeElapsed);
                AverageUpdateTicks = (int)(updateTickCount / 100);
                updateFrameCount = 0;
                updateTickCount = 0;
            }
        }
    }
}
