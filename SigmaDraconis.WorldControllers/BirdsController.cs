namespace SigmaDraconis.WorldControllers
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Draconis.Shared;
    using Draconis.UI;
    using Shared;
    using World;
    using World.Fauna;
    using World.Zones;
    using WorldInterfaces;

    public static class BirdsController
    {
        private static int updatesPending = 0;
        private static int updatesTotal = 0;
        private static readonly int targetBird1Count = 8;
        private static readonly int targetBird2Count = 8;
        private static CancellationTokenSource cancellationTokenSource;
        private static int worldBird1Count;
        private static int worldBird2Count;
        private static bool isStarted;

        private static Vector2f screenScrollPos = new Vector2f();
        private static float screenZoom;
        private static float screenWidth;
        private static float screenHeight;

        private static readonly ConcurrentQueue<int> birdsToAddQueue1 = new ConcurrentQueue<int>();
        private static readonly ConcurrentQueue<int> birdsToAddQueue2 = new ConcurrentQueue<int>();
        private static readonly ConcurrentDictionary<int, Bird1Proxy> birdProxies1 = new ConcurrentDictionary<int, Bird1Proxy>();
        private static readonly ConcurrentDictionary<int, Bird2Proxy> birdProxies2 = new ConcurrentDictionary<int, Bird2Proxy>();

        public static readonly List<int> EdgeNodes = new List<int>();

        public static void Update(bool isLastUpdateInFrame, Vector2f scrollPos, float zoom, float width, float height)
        {
            screenScrollPos = scrollPos;
            screenZoom = zoom;
            screenWidth = width;
            screenHeight = height;

            if (!isStarted) Start();

            updatesPending++;
            while (birdsToAddQueue1.TryDequeue(out int tileIndex))
            {
                var tile = World.GetSmallTile(tileIndex);
                if (tile == null) continue;

                var direction = (Direction)Rand.Next(8);
                if (tile.X < 4 && tile.Y < 4) direction = Direction.E;
                else if (tile.X < 4 && tile.Y + 4 >= World.Width * 3) direction = Direction.N;
                else if (tile.X + 4 >= World.Width * 3 && tile.Y < 4) direction = Direction.S;
                else if (tile.X + 4 >= World.Width * 3 && tile.Y + 4 >= World.Width * 3) direction = Direction.W;
                else if (tile.X < 4) direction = Direction.NE;
                else if (tile.X + 4 >= World.Width * 3) direction = Direction.SW;
                else if (tile.Y < 4) direction = Direction.SE;
                else if (tile.Y + 4 >= World.Width * 3) direction = Direction.NW;

                var bird = new Bird1(tile) { FacingDirection = direction, IsFadingIn = true };
                bird.Init();
                World.AddThing(bird);
                birdProxies1.TryAdd(bird.Id, new Bird1Proxy(0, true, bird.Angle, bird.Height, bird.AnimationFrame, bird.Position.Clone(), bird.FacingDirection, bird.Turning));
            }

            while (birdsToAddQueue2.TryDequeue(out int tileIndex))
            {
                var tile = World.GetSmallTile(tileIndex);
                if (tile == null) continue;

                // var direction = tile.X + tile.Y > World.Width * 3 ? Direction.W : Direction.E;
                var direction = Direction.W;

                var bird = new Bird2(tile) { FacingDirection = direction, IsFadingIn = true };
                bird.Init();
                World.AddThing(bird);
                birdProxies2.TryAdd(bird.Id, new Bird2Proxy(0, true, bird.Angle, bird.AnimationFrame, bird.Position.Clone(), bird.FacingDirection));

                var flock = new List<Bird2> { bird };
                var maxFlockSize = Rand.Next(3 + 6);
                for (int i = 1; i < maxFlockSize; i++)
                {
                    var x = bird.Position.X + (((Rand.NextFloat() * 2f) + 0.4f) * (Rand.Next(2) == 1 ? 1 : -1));
                    var y = bird.Position.Y + ((Rand.NextFloat() + 0.2f) * (Rand.Next(2) == 1 ? 1 : -1));
                    var ok = true;
                    foreach (var b in flock)
                    {
                        var dist = Mathf.Sqrt(((x - b.Position.X) * (x - b.Position.X)) + ((y - b.Position.Y) * (y - b.Position.Y) / 2f));
                        ok &= dist > 0.8f;
                        if (!ok) break;
                    }

                    if (ok)
                    {
                        var bird2 = new Bird2(tile) { FacingDirection = direction, IsFadingIn = true };
                        bird2.Init();
                        bird2.Position.X = x;
                        bird2.Position.Y = y;
                        bird2.AnimationFrame = Rand.Next(20) + (direction == Direction.W ? 1 : 21);
                        World.AddThing(bird2);
                        birdProxies2.TryAdd(bird2.Id, new Bird2Proxy(0, true, bird2.Angle, bird2.AnimationFrame, bird2.Position.Clone(), bird2.FacingDirection));
                        flock.Add(bird2);
                    }
                }
            }

            worldBird1Count = World.GetThings(ThingType.Bird1).Count();
            worldBird2Count = World.GetThings(ThingType.Bird2).Count();

            if (!isLastUpdateInFrame) return;

            foreach (var kv in birdProxies1)
            {
                if (World.GetThing(kv.Key) is IBird bird)
                {
                    if (kv.Value.Alpha > 0.01f || kv.Value.IsFadingIn)
                    {
                        bird.AnimationFrame = kv.Value.AnimationFrame;
                        bird.Angle = kv.Value.Angle;
                        bird.Height = kv.Value.Height;
                        bird.RenderAlpha = kv.Value.Alpha;
                        bird.Position = kv.Value.Position.Clone();
                        bird.FacingDirection = kv.Value.Direction;
                        bird.Turning = kv.Value.Turning;
                        bird.UpdateRenderPos();
                    }
                    else
                    {
                        World.RemoveThing(bird);
                        birdProxies1.TryRemove(kv.Key, out Bird1Proxy _);
                    }
                }
                else birdProxies1.TryRemove(kv.Key, out Bird1Proxy _);
            }

            foreach (var kv in birdProxies2)
            {
                if (World.GetThing(kv.Key) is IBird bird)
                {
                    if (kv.Value.Alpha > 0.01f || kv.Value.IsFadingIn)
                    {
                        bird.AnimationFrame = kv.Value.AnimationFrame;
                        bird.Angle = kv.Value.Angle;
                        bird.Height = 200f;  // No height variation as it messes up flocks
                        bird.RenderAlpha = kv.Value.Alpha;
                        bird.Position = kv.Value.Position.Clone();
                        bird.FacingDirection = kv.Value.Direction;
                        bird.UpdateRenderPos();
                    }
                    else
                    {
                        World.RemoveThing(bird);
                        birdProxies2.TryRemove(kv.Key, out Bird2Proxy _);
                    }
                }
                else birdProxies2.TryRemove(kv.Key, out Bird2Proxy _);
            }
        }

        public static void Start()
        {
            isStarted = true;
            updatesTotal = 0;
            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            worldBird1Count = 0;
            worldBird2Count = 0;

            EdgeNodes.Clear();
            foreach (int tileIndex in ZoneManager.DeepWaterZone.Nodes.Keys)
            {
                var tile = World.GetSmallTile(tileIndex);
                if (tile?.AdjacentTiles4?.Count < 4) EdgeNodes.Add(tileIndex);
            }

            birdProxies1.Clear();
            foreach (var bird in World.GetThings<IBird>(ThingType.Bird1))
            {
                worldBird1Count++;
                birdProxies1.TryAdd(bird.Id, new Bird1Proxy(bird.RenderAlpha, false, bird.Angle, bird.Height, bird.AnimationFrame, bird.Position.Clone(), bird.FacingDirection, bird.Turning));
            }

            birdProxies2.Clear();
            foreach (var bird in World.GetThings<IBird>(ThingType.Bird2))
            {
                worldBird2Count++;
                birdProxies2.TryAdd(bird.Id, new Bird2Proxy(bird.RenderAlpha, false, bird.Angle, bird.AnimationFrame, bird.Position.Clone(), bird.FacingDirection));
            }

            Task.Factory.StartNew(UpdateJob, token);
        }

        public static void Stop()
        {
            isStarted = false;
            if (cancellationTokenSource != null) cancellationTokenSource.Cancel();
            while (birdsToAddQueue1.TryDequeue(out _));
            while (birdsToAddQueue2.TryDequeue(out _));
            updatesPending = 0;
        }

        public static void UpdateJob()
        {
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                if (updatesPending > 0)
                {
                    updatesPending--;
                    updatesTotal++;

                    foreach (var proxy in birdProxies1) proxy.Value.Update();
                    foreach (var proxy in birdProxies2) proxy.Value.Update();

                    if (!EdgeNodes.Any() || World.Temperature < 0) continue;

                    if (updatesTotal % 61 == 0)
                    {
                        if (worldBird1Count < targetBird1Count && World.Temperature >= 0)
                        {
                            var tileIndex = EdgeNodes[Rand.Next(EdgeNodes.Count)];
                            if (!IsTileOnScreen(tileIndex)) birdsToAddQueue1.Enqueue(tileIndex);
                        }

                        if (worldBird2Count < targetBird2Count && World.Temperature >= 5 && World.WorldLight.Brightness > 0.8f)
                        {
                            // Fly W when warm
                            var tileIndex = EdgeNodes[Rand.Next(EdgeNodes.Count)];
                            var tile = World.GetSmallTile(tileIndex);
                            if (tile != null && tile.X + tile.Y > (World.Width * 3) + 20 && !IsTileOnScreen(tile)) birdsToAddQueue2.Enqueue(tileIndex);
                        }

                        //if (worldBird2Count < targetBird2Count && World.Temperature < 0)
                        //{
                        //    // Fly E when cold
                        //    var tileIndex = EdgeNodes[Rand.Next(EdgeNodes.Count)];
                        //    var tile = World.GetSmallTile(tileIndex);
                        //    if (tile != null && tile.X + tile.Y < (World.Width * 3) - 20 && !IsTileOnScreen(tile)) birdsToAddQueue2.Enqueue(tileIndex);
                        //}
                    }
                }
                else Thread.Sleep(5);
            }

            while (birdsToAddQueue1.TryDequeue(out _)) ;
            while (birdsToAddQueue2.TryDequeue(out _)) ;
        }

        private static bool IsTileOnScreen(int tileIndex)
        {
            var tile = World.GetSmallTile(tileIndex);
            if (tile == null) return false;
            return IsTileOnScreen(tile);
        }

        private static bool IsTileOnScreen(ISmallTile tile)
        {
            var x = tile.X;
            var y = tile.Y - 4;
            var screenPos = CoordinateHelper.GetScreenPosition(UIStatics.Graphics, screenScrollPos, screenZoom, x, y);
            var fracX = screenPos.X / screenWidth;
            var fracY = screenPos.Y / screenHeight;

            return fracX >= -0.1f && fracX < 1.1f && fracY > -0.1f && fracY < 1.1f;
        }
    }
}
