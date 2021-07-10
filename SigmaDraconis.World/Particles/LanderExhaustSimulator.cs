namespace SigmaDraconis.World.Particles
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Draconis.Shared;
    using Shared;
    using WorldInterfaces;

    public static class LanderExhaustSimulator
    {
        private static readonly ConcurrentQueue<SmokeParticle> particleQueue = new ConcurrentQueue<SmokeParticle>();
        private static readonly object lockObj = new object();
        private static bool isStarted = false;
        private static readonly FastRandom random = new FastRandom();
        private static readonly Dictionary<int, List<SmokeParticle>> listsForRendererLayer1 = new Dictionary<int, List<SmokeParticle>>();
        private static readonly Dictionary<int, List<SmokeParticle>> listsForRendererLayer2 = new Dictionary<int, List<SmokeParticle>>();
        private static CancellationTokenSource cancellationTokenSource;
        private static int updatesPending = 0;
        public static float ExhaustInitialSize { get; set; } = 2f;
        public static float ExhaustInitialAlpha { get; set; } = 0.2f;
        public static float ExhaustInitialSpeedVariation { get; set; } = 0.5f;
        public static float ExhaustInitialTemp { get; set; } = 1000f;
        public static float ExhaustCoolRate { get; set; } = 0.99f;
        public static float ExhaustBaseExpansion { get; set; } = 0.02f;
        public static float ExhaustBaseSpeedZ { get; set; } = 0.05f;
        public static float ExhaustTempSpeedZ { get; set; } = 0.0001f;
        public static float ExhaustDrag { get; set; } = 0.1f;
        public static float ExhaustRandomMotionBase { get; set; } = 0.001f;
        public static float ExhaustRandomMotionTemp { get; set; } = 0.00001f;

        private static readonly HashSet<int> rowsForRendererUpdate = new HashSet<int>();
        public static List<int> RowsForRendererUpdate
        {
            get
            {
                lock (lockObj)
                {
                    return rowsForRendererUpdate.ToList();
                }
            }
        }

        public static void ClearRowsForRendererUpdate()
        {
            lock (lockObj)
            {
                rowsForRendererUpdate.Clear();
            }
        }

        public static bool IsActive => !particleQueue.IsEmpty;

        public static void Clear()
        {
            listsForRendererLayer1.Clear();
            listsForRendererLayer2.Clear();
            while (particleQueue.TryDequeue(out _));
            RowsForRendererUpdate.Clear();
        }

        public static void AddParticle(ISmallTile tile, float x, float y, float z, float vz, float alphaScale = 1f)
        {
            var r = 1f - random.NextFloat() * ExhaustInitialSpeedVariation;
            var particle = new SmokeParticle(SmokeParticleType.LanderExhaust, tile.Index, tile.CentrePosition.X + x, tile.CentrePosition.Y + y, z, random.NextFloat(-0.05f, 0.05f), random.NextFloat(-0.05f, 0.05f), vz * r, ExhaustInitialSize, random.NextFloat(ExhaustInitialAlpha))
            {
                Temperature = ExhaustInitialTemp,
                AlphaScale = alphaScale,
                StartY = tile.CentrePosition.Y,
                Row = tile.Row
            };

            particleQueue.Enqueue(particle);
            if (!rowsForRendererUpdate.Contains(particle.Row)) rowsForRendererUpdate.Add(particle.Row);
        }

        // For serialization
        public static List<SmokeParticle> GetAllParticles()
        {
            var result = new List<SmokeParticle>();
            lock (lockObj)
            {
                result = particleQueue.ToList();
            }

            return result;
        }

        // For deserialization
        public static void SetAllParticles(List<SmokeParticle> particles)
        {
            while (particleQueue.TryDequeue(out _)) ;

            if (particles == null) return;
            foreach (var particle in particles)
            {
                particleQueue.Enqueue(particle);
            }
        }

        public static SmokeParticle[] GetParticlesForRenderer(int row, int layer)
        {
            SmokeParticle[] result = null;
            while (updatesPending > 8) Thread.Sleep(5);
            lock (lockObj)
            {
                if (layer == 1 && listsForRendererLayer1.ContainsKey(row))
                {
                    result = listsForRendererLayer1[row].ToArray();
                }
                else if (layer == 2 && listsForRendererLayer2.ContainsKey(row))
                {
                    result = listsForRendererLayer2[row].ToArray();
                }
            }

            return result;
        }

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
                        if (particleQueue.Any()) UpdateInternal();
                    }
                    catch (Exception)
                    {
                        // Just because I don't trust multithreaded stuff, but errors here are unlikely to be serious
                    }
                }
                else Thread.Sleep(5);
            }
        }

        private static void UpdateInternal()
        {
            var particlesForRendererLayer1 = new Dictionary<int, List<SmokeParticle>>();
            var particlesForRendererLayer2 = new Dictionary<int, List<SmokeParticle>>();

            var count = particleQueue.Count;
            for (int i = 0; i < count; i++)
            {
                particleQueue.TryDequeue(out SmokeParticle particle);
                if (particle == null) break;

                if (particle.Alpha < 0.01f)
                {
                    particle.Alpha = 0.0f;
                    particle.IsVisible = false;
                    continue;
                }

                var dH = 0f;
                if (particle.Z < 3)
                {
                    var dx = particle.X - particle.StartX;
                    var dy = particle.Y - particle.StartY;
                    dH = (float)Math.Sqrt((dx * dx) + (dy * dy * 4));
                }

                particle.Temperature *= ExhaustCoolRate;

                // We only calculate horizontal speed if we are likely to use it
                var speedH = particle.Z + particle.VelZ < 3 ? (float)Math.Sqrt((particle.VelX * particle.VelX) + (particle.VelY * particle.VelY)) : 0;

                if (particle.VelZ < 0 && particle.VelZ < 2 - particle.Z)
                {
                    // Particle has hit the ground, so needs to go sideways.  Convert to dust.
                    particle.VelZ = 2 - particle.Z;
                    if (particle.ParticleType == SmokeParticleType.LanderExhaust)
                    {
                        particle.AlphaScale *= 0.5f;
                        particle.ParticleType = SmokeParticleType.Dust;
                        particle.Size = 5f;
                        particle.Temperature = 20f;
                        var direction = random.NextFloat() * Mathf.PI * 2f;
                        var m = 0.1f + (random.NextFloat() * 4f);
                        particle.VelX = m * Mathf.Sin(direction);
                        particle.VelY = m * Mathf.Cos(direction);
                    }
                }
                else
                {
                    // Drag and smoke rising
                    var targetRiseRate = ExhaustBaseSpeedZ + (ExhaustTempSpeedZ * particle.Temperature);
                    var vz = targetRiseRate - particle.VelZ;
                    var s = (float)Math.Sqrt((particle.VelX * particle.VelX) + (particle.VelY * particle.VelY) + (vz * vz));
                    var drag = Math.Min(1, s * ExhaustDrag) * (particle.ParticleType == SmokeParticleType.Dust ? 1.8f : 1f);
                    particle.VelX -= particle.VelX * drag;
                    particle.VelY -= particle.VelY * drag;
                    particle.VelZ -= (particle.VelZ - targetRiseRate) * drag;

                    var randMotion = ExhaustRandomMotionBase + (particle.Temperature * ExhaustRandomMotionTemp);
                    particle.VelX += (random.NextFloat() - 0.5f) * randMotion;
                    particle.VelY += (random.NextFloat() - 0.5f) * randMotion;
                    particle.VelZ += (random.NextFloat() - 0.5f) * randMotion;
                }

                if (particle.Z < 2.2f && dH < 10)
                {
                    // Don't allow particle to rise right away
                    if (speedH >= 0.04f) particle.VelZ = 0;
                }
                else
                {
                    particle.Alpha *= (particle.ParticleType == SmokeParticleType.Dust ? 0.99f : 0.9f);
                    particle.Size += ExhaustBaseExpansion * (particle.ParticleType == SmokeParticleType.Dust ? 3f : 1f);
                }

                particle.IsVisible = particle.Alpha > 0;

                particle.X += particle.VelX;
                particle.Y += particle.VelY * 0.5f;
                particle.Z += particle.VelZ;
                particle.Row = (int)(particle.Y / 5.33333) + 188;   // May break if world size changes

                if (particle.IsVisible)
                {
                    particleQueue.Enqueue(particle);
                    if (particle.Y < particle.StartY)
                    {
                        if (!particlesForRendererLayer1.ContainsKey(particle.Row)) particlesForRendererLayer1.Add(particle.Row, new List<SmokeParticle>());
                        particlesForRendererLayer1[particle.Row].Add(particle);
                    }
                    else
                    {
                        if (!particlesForRendererLayer2.ContainsKey(particle.Row)) particlesForRendererLayer2.Add(particle.Row, new List<SmokeParticle>());
                        particlesForRendererLayer2[particle.Row].Add(particle);
                    }
                }
            }

            // Synchronise
            lock (lockObj)
            {
                foreach (var row in particlesForRendererLayer1.Keys.ToList())
                {
                    if (!rowsForRendererUpdate.Contains(row)) rowsForRendererUpdate.Add(row);
                    if (listsForRendererLayer1.ContainsKey(row)) listsForRendererLayer1[row] = particlesForRendererLayer1[row].ToList();
                    else listsForRendererLayer1.Add(row, particlesForRendererLayer1[row].ToList());
                }

                foreach (var row in particlesForRendererLayer2.Keys.ToList())
                {
                    if (!rowsForRendererUpdate.Contains(row)) rowsForRendererUpdate.Add(row);
                    if (listsForRendererLayer2.ContainsKey(row)) listsForRendererLayer2[row] = particlesForRendererLayer2[row].ToList();
                    else listsForRendererLayer2.Add(row, particlesForRendererLayer2[row].ToList());
                }
            }
        }
    }
}
