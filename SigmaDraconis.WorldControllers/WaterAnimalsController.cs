namespace SigmaDraconis.WorldControllers
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Shared;
    using World;
    using World.Fauna;
    using World.Zones;

    public static class WaterAnimalsController
    {
        private static int updatesPending = 0;
        private static int updatesTotal = 0;
        private static readonly int targetCount = 200;
        private static CancellationTokenSource cancellationTokenSource;
        private static int worldFishCount;
        private static bool isStarted;

        private static readonly ConcurrentQueue<int> fishToAddQueue = new ConcurrentQueue<int>();
        private static readonly ConcurrentDictionary<int, FishProxy> fishProxies = new ConcurrentDictionary<int, FishProxy>();

        public static readonly List<int> WaterNodes = new List<int>();
        public static readonly List<int> DeepWaterNodes = new List<int>();

        public static void Update(bool isLastUpdateInFrame)
        {
            if (!isStarted) Start();

            updatesPending++;
            while (fishToAddQueue.TryDequeue(out int tileIndex))
            {
                var fish = new Fish(World.GetSmallTile(tileIndex));
                fish.Init();
                fish.IsFadingIn = true;
                World.AddThing(fish);
                fishProxies.TryAdd(fish.Id, new FishProxy { Alpha = 0, IsFadingIn = true, Angle = fish.Angle, AnimationFrame = fish.AnimationFrame, Position = fish.Position.Clone() });
            }

            worldFishCount = World.GetThings<Fish>(ThingType.Fish).Count();

            if (!isLastUpdateInFrame) return;

            foreach (var kv in fishProxies)
            {
                if (World.GetThing(kv.Key) is Fish fish)
                {
                    if (kv.Value.Alpha > 0.01f || kv.Value.IsFadingIn)
                    {
                        fish.AnimationFrame = kv.Value.AnimationFrame;
                        fish.Angle = kv.Value.Angle;
                        fish.RenderAlpha = kv.Value.Alpha;
                        fish.Position = kv.Value.Position.Clone();
                        fish.UpdateRenderPos();
                    }
                    else
                    {
                        World.RemoveThing(fish);
                        fishProxies.TryRemove(kv.Key, out FishProxy _);
                    }
                }
                else fishProxies.TryRemove(kv.Key, out FishProxy _);
            }
        }

        public static void Start()
        {
            isStarted = true;
            updatesTotal = 0;
            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            worldFishCount = World.GetThings<Fish>(ThingType.Fish).Count();

            WaterNodes.Clear();
            WaterNodes.AddRange(ZoneManager.WaterZone.Nodes.Keys.ToList());

            DeepWaterNodes.Clear();
            DeepWaterNodes.AddRange(ZoneManager.DeepWaterZone.Nodes.Keys.ToList());

            fishProxies.Clear();
            foreach (var fish in World.GetThings<Fish>(ThingType.Fish))
            {
                fishProxies.TryAdd(fish.Id, new FishProxy { Alpha = fish.RenderAlpha, Angle = fish.Angle, AnimationFrame = fish.AnimationFrame, Position = fish.Position.Clone() });
            }

            Task.Factory.StartNew(UpdateJob, token);
        }

        public static void Stop()
        {
            isStarted = false;
            if (cancellationTokenSource != null) cancellationTokenSource.Cancel();
            while (fishToAddQueue.TryDequeue(out _));
        }

        public static void UpdateJob()
        {
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                if (updatesPending > 0)
                {
                    updatesPending--;
                    updatesTotal++;
                    if (!DeepWaterNodes.Any()) return;

                    foreach (var proxy in fishProxies) proxy.Value.Update();

                    if (updatesTotal % 61 == 0)
                    {
                        var count = worldFishCount;
                        while (count < targetCount)
                        {
                            var tileIndex = DeepWaterNodes[Rand.Next(DeepWaterNodes.Count)];
                            fishToAddQueue.Enqueue(tileIndex);
                            count++;
                        }
                    }
                }
                else Thread.Sleep(5);
            }
        }
    }
}
