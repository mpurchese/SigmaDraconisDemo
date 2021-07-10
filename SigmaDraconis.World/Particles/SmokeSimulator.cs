namespace SigmaDraconis.World.Particles
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Shared;
    using Smoke;
    using WorldInterfaces;

    public static class SmokeSimulator
    {
        private static readonly ConcurrentDictionary<int, ConcurrentQueue<SmokeParticle>> particleQueuesByRow = new ConcurrentDictionary<int, ConcurrentQueue<SmokeParticle>>();

        public static float SmokeCoolRate { get; set; } = 0.99f;
        public static ConcurrentDictionary<int, byte> RowsForRendererUpdate = new ConcurrentDictionary<int, byte>();

        private static bool isStarted = false;
        private static CancellationTokenSource cancellationTokenSource;
        private static int updatesPending = 0;

        public static void Start()
        {
            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            Task.Factory.StartNew(UpdateJob, token);
            isStarted = true;
        }

        public static void Stop()
        {
            cancellationTokenSource.Cancel();
        }

        public static void Clear()
        {
            particleQueuesByRow.Clear();
            RowsForRendererUpdate.Clear();
        }

        public static void AddParticle(ISmallTile tile, SmokeModel model, Direction direction, float offsetX = 0, float offsetY = 0)
        {
            if (!model.OriginsByDirection.ContainsKey(direction)) direction = Direction.None;
            var origin = model.OriginsByDirection[direction];
            AddParticle(tile, origin.X + offsetX, origin.Y + offsetY, origin.Z, 0, model.ParticleType, model.Layer);
        }

        public static void AddParticle(ISmallTile tile, float x, float y, float z, float temperature, SmokeParticleType type, int layer)
        {
            var row = tile.Row + layer - 1;
            particleQueuesByRow.TryAdd(row, new ConcurrentQueue<SmokeParticle>());

            var particle = new SmokeParticle(type, tile.Index, tile.CentrePosition.X + x, tile.CentrePosition.Y + y, z
                , (Rand.NextFloat() * 0.02f) - 0.01f, (Rand.NextFloat() * 0.02f) - 0.01f, 0.02f + (Rand.NextFloat() * 0.01f), 1f, 0.1f);
            particleQueuesByRow[row].Enqueue(particle);
            particle.Temperature = temperature;
            particle.VelY = -temperature * 0.00005f;
            RowsForRendererUpdate.TryAdd(row, 0);
        }

        // For serialization
        public static Dictionary<int, List<SmokeParticle>> GetAllParticles()
        {
            var result = new Dictionary<int, List<SmokeParticle>>();
            foreach (var key in particleQueuesByRow.Keys)
            {
                if (particleQueuesByRow[key].Count > 0)
                {
                    result.Add(key, particleQueuesByRow[key].ToList());
                }
            }

            return result;
        }

        // For deserialization
        public static void SetAllParticles(Dictionary<int, List<SmokeParticle>> particles)
        {
            particleQueuesByRow.Clear();

            if (particles == null) return;

            foreach (var key in particles.Keys)
            {
                var queue = new ConcurrentQueue<SmokeParticle>();
                foreach (var particle in particles[key])
                {
                    queue.Enqueue(particle);
                }

                particleQueuesByRow.TryAdd(key, queue);
            }
        }

        public static IEnumerable<SmokeParticle> GetParticles(int row)
        {
            return particleQueuesByRow.ContainsKey(row) ? particleQueuesByRow[row] as IEnumerable<SmokeParticle> : new List<SmokeParticle>();
        }

        public static IEnumerable<SmokeParticle> GetParticles()
        {
            return particleQueuesByRow.Values.SelectMany(r => r.ToList());
        }

        public static void Update()
        {
            if (!isStarted) Start();
            updatesPending++;
        }

        private static void UpdateJob()
        {
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                if (updatesPending > 0)
                {
                    try
                    {
                        updatesPending--;
                        UpdateInternal();
                    }
                    catch (Exception ex)
                    {
                        // Just because I don't trust multithreaded stuff, but errors here are unlikely to be serious
                        Console.WriteLine(ex.Message);
                    }
                }
                else Thread.Sleep(5);
            }
        }

        private static void UpdateInternal()
        {
            foreach (var row in particleQueuesByRow.Keys)
            {
                if (!particleQueuesByRow.TryGetValue(row, out ConcurrentQueue<SmokeParticle> queue)) continue;

                if (queue.Count > 0) RowsForRendererUpdate.TryAdd(row, 0);
                while (queue.Count > 0 && queue.TryPeek(out SmokeParticle q) && q.Alpha < 1 / 255f) queue.TryDequeue(out _);

                // Wind
                var wx = (float)Math.Sin(World.WindDirection) * 0.02f;
                var wy = (float)Math.Cos(World.WindDirection) * 0.02f;

                foreach (var particle in queue.Where(p => p.Alpha > 1 / 255f))
                {
                    particle.Alpha -= 0.0005f;
                    particle.Size += 0.005f;

                    particle.VelX = 0.02f * ((particle.VelX * 49f) - wx);
                    particle.VelY = 0.02f * ((particle.VelY * 49f) - wy);

                    particle.X += particle.VelX;
                    particle.Y += particle.VelY * 0.5f;
                    particle.Z += particle.VelZ;

                    particle.Temperature *= SmokeCoolRate;
                }
            }
        }
    }
}
