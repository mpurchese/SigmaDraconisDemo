namespace SigmaDraconis.WorldControllers
{
    using Draconis.Shared;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Shared;
    using World;
    using WorldInterfaces;

    public static class WeatherController
    {
        public static float TreetopSwayAmplitude { get; set; } = 0f;

        public static int MinTemp => World.ClimateType == ClimateType.Normal ? -8 : -22;
        public static int MaxTemp => World.ClimateType == ClimateType.Normal ? 34 : (World.ClimateType == ClimateType.Severe ? 39 : 15);
        public static int MinWind => 0;
        public static int MaxWind => 24;

        private static int timeSinceUpdate = 0;

        public static void Init()
        {
            Update(true);
            EventManager.Subscribe(EventType.Building, EventSubType.Added, delegate (object obj) { OnThingAdded(obj); });
            EventManager.Subscribe(EventType.Plant, EventSubType.Added, delegate (object obj) { OnThingAdded(obj); });
            EventManager.Subscribe(EventType.Building, EventSubType.Removed, delegate (object obj) { OnThingRemoved(obj); });
            EventManager.Subscribe(EventType.Plant, EventSubType.Removed, delegate (object obj) { OnThingRemoved(obj); });
        }

        public static void Update(bool first = false)
        {
            var nextTeetopSwayAmplitude = World.Wind / 20f;
            if (nextTeetopSwayAmplitude > TreetopSwayAmplitude) TreetopSwayAmplitude += 0.01f;
            else if (nextTeetopSwayAmplitude < TreetopSwayAmplitude) TreetopSwayAmplitude -= 0.002f;
            TreetopSwayAmplitude = TreetopSwayAmplitude.Clamp(0f, 1f);

            var h = World.WorldTime.Hour;
            var temp = (-0.0000000000263 * Math.Pow(h, 6)) + (0.0000000181 * Math.Pow(h, 5)) - (0.00000402 * Math.Pow(h, 4)) + (0.000242 * h * h * h) + (0.0147 * h * h) - h - 9;
            if (World.WorldTime.TotalHoursPassed < 24 && World.ClimateType != ClimateType.Normal) temp += (24 - World.WorldTime.TotalHoursPassed) / 8;  // Prevent colonists getting cold warnings at start of game
            if (World.ClimateType == ClimateType.Normal) temp = Math.Max(-8, 5.0 + (temp * 0.75));
            else if (World.ClimateType == ClimateType.Snow && temp > 0) temp /= 2.6;
            World.Temperature = (int)temp;

            timeSinceUpdate++;
            if (!first && GetRandom(900) != 1 && timeSinceUpdate < 1800) return;
            timeSinceUpdate = 0;

            var prevWind = first ? GetRandom(20) : World.Wind;
            if (prevWind <= 3 && GetRandom(100) < 75)
            {
                // This is to cause longer calm periods
                World.Wind = GetRandom(3);
            }
            else if (GetRandom(100) < 25)
            {
                // 25% chance of a completely random wind speed
                World.Wind = GetRandom(25);
            }
            else
            {
                var dayFrac = World.WorldTime.DayFraction;
                var curve = Math.Sin(dayFrac * Math.PI * 2.0);
                curve *= curve;
                var nextWind = (int)GetRandomNormal((int)(curve * 20), 6);
                World.Wind += GetRandom(nextWind - prevWind);
                if (World.Wind < 0)
                {
                    World.Wind = 0;// GetRandom(4);
                }
            }

            World.WindDirection = Rand.NextFloat() * (float)Math.PI * 2f;
        }

        private static int GetRandom(int max)
        {
            bool isNegative = max < 0;
            var rand = Rand.Next(Math.Abs(max));
            return rand * (isNegative ? -1 : 1);
        }

        private static double GetRandomNormal(double mean, double stdDev)
        {
            double u1 = 1.0 - Rand.NextDouble();
            double u2 = 1.0 - Rand.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            double randNormal = mean + stdDev * randStdNormal;
            return randNormal;
        }

        private static void OnThingAdded(object sender)
        {
            var thing = sender as Thing;
            if (thing?.MainTile == null || thing.Definition == null || thing.Definition.WindBlockFactor == 0) return;

            AddWindBlock(thing.AllTiles, thing.Definition.WindBlockFactor);
        }

        private static void OnThingRemoved(object sender)
        {
            var thing = sender as Thing;
            if (thing?.MainTile == null || thing.Definition == null || thing.Definition.WindBlockFactor == 0) return;

            AddWindBlock(thing.AllTiles, -thing.Definition.WindBlockFactor);
        }

        private static void AddWindBlock(IReadOnlyList<ISmallTile> tiles, int amount)
        {
            var minX = tiles.Min(t => t.TerrainPosition.X);
            var maxX = tiles.Max(t => t.TerrainPosition.X);
            var minY = tiles.Min(t => t.TerrainPosition.Y);
            var maxY = tiles.Max(t => t.TerrainPosition.Y);

            for (int x = minX - 4; x <= maxX + 4; x++)
            {
                for (int y = minY - 4; y <= minY + 4; y++)
                {
                    var t = World.GetSmallTile(x, y);
                    if (t != null)
                    {
                        var tileModifier = 0;
                        for (int i = 0; i < 9; i++)
                        {
                            var localAmount = amount;
                            if (!tiles.Contains(t))
                            {
                                var dx = Math.Min(Math.Abs(t.X - minX), Math.Abs(t.X - maxX));
                                var dy = Math.Min(Math.Abs(t.Y - minY), Math.Abs(t.Y - maxY));
                                var d = Math.Sqrt((dx * dx) + (dy * dy));
                                if (d > 0.9)
                                {
                                    localAmount = (int)(amount / (d));
                                }
                            }

                            tileModifier += localAmount;
                        }

                        t.WindModifier += tileModifier / 3;
                    }

                    EventManager.RaiseEvent(EventType.Wind, t);
                }
            }
        }
    }
}
