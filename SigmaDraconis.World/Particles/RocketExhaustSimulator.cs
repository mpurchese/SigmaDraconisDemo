namespace SigmaDraconis.World.Particles
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Shared;
    using WorldInterfaces;

    public static class RocketExhaustSimulator
    {
        private static readonly ConcurrentQueue<SmokeParticle> particleQueue = new ConcurrentQueue<SmokeParticle>();
        private static readonly object lockObj = new object();
        private static bool isStarted = false;
        private static readonly FastRandom random = new FastRandom();
        private static readonly Dictionary<int, List<SmokeParticle>> listsForRendererLayer1 = new Dictionary<int, List<SmokeParticle>>();
        private static readonly Dictionary<int, List<SmokeParticle>> listsForRendererLayer2 = new Dictionary<int, List<SmokeParticle>>();
        private static List<SmokeParticle> listForGroundRenderer = new List<SmokeParticle>();
        private static CancellationTokenSource cancellationTokenSource;
        private static int updatesPending = 0;

        public static float ExhaustInitialSize { get; set; } = 2f;
        public static float ExhaustInitialAlpha { get; set; } = 0.2f;
        public static float ExhaustFadeRateLinear { get; set; } = 0.0000f;
        public static float ExhaustFadeRateCurve { get; set; } = 0.99f;
        public static float ExhaustInitialSpeedVariation { get; set; } = 0.5f;
        public static float ExhaustInitialTemp { get; set; } = 1000f;
        public static float ExhaustCoolRate { get; set; } = 0.99f;
        public static float ExhaustBaseExpansion { get; set; } = 0.01f;
        public static float ExhaustTempExpansion { get; set; } = 0f;
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

        public static bool IsActive => !particleQueue.IsEmpty;

        public static void Clear()
        {
            listsForRendererLayer1.Clear();
            listsForRendererLayer2.Clear();
            listForGroundRenderer.Clear();
            while (particleQueue.TryDequeue(out _));
            RowsForRendererUpdate.Clear();
        }

        public static void AddParticle(ISmallTile tile, float x, float y, float z, float vz, float alphaScale = 1f)
        {
            var r = 1f - random.NextFloat() * ExhaustInitialSpeedVariation;
            var particle = new SmokeParticle(SmokeParticleType.Exhaust, tile.Index, tile.CentrePosition.X + x, tile.CentrePosition.Y + y, z, random.NextFloat(-0.05f, 0.05f), random.NextFloat(-0.05f, 0.05f), vz * r, ExhaustInitialSize, random.NextFloat(ExhaustInitialAlpha))
            {
                Temperature = ExhaustInitialTemp,
                AlphaScale = alphaScale,
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

        public static SmokeParticle[] GetParticlesForGroundRenderer()
        {
            SmokeParticle[] result = null;
            lock (lockObj)
            {
                result = listForGroundRenderer.ToArray();
                // result = particleQueuesByRow.Values.SelectMany(r => r.Where(p => p.Z <= 2.5f && p.IsVisible && p.Alpha > 0.004f).ToList()).ToArray();
            }

            return result;
        }

        public static SmokeParticle[] GetParticlesForSahdowRenderer()
        {
            SmokeParticle[] result = null;
            lock (lockObj)
            {
                IEnumerable<SmokeParticle> enumerable = new List<SmokeParticle>();
                foreach (var l in listsForRendererLayer1.Values) enumerable = enumerable.Concat(l);
                foreach (var l in listsForRendererLayer2.Values) enumerable = enumerable.Concat(l);
                result = enumerable.ToArray();
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
            var particlesForGroundRenderer = new List<SmokeParticle>();

            var count = particleQueue.Count;
            for (int i = 0; i < count; i++)
            {
                particleQueue.TryDequeue(out SmokeParticle particle);
                if (particle == null) break;

                if (particle.Alpha < 0.004f)
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

                if (dH > 8 && dH < 9 && particle.Z < 3)
                {
                    // Redirect exhaust smoke so it comes out of the holes in the launch pad
                    RedirectParticleInLaunchPad(particle, speedH);
                }

                if (particle.VelZ < 0 && particle.VelZ < 2 - particle.Z)
                {
                    // Particle has hit the ground, so needs to go sideways
                    particle.VelZ = 2 - particle.Z;
                    if (speedH > 0.03)
                    {
                        var speed = (float)Math.Sqrt((particle.VelX * particle.VelX) + (particle.VelY * particle.VelY) + (particle.VelZ * particle.VelZ));
                        particle.VelX = particle.VelX * speed / speedH;
                        particle.VelY = particle.VelY * speed / speedH;
                        particle.Size = 4f;
                    }
                }
                else
                {
                    // Drag and smoke rising
                    var targetRiseRate = ExhaustBaseSpeedZ + (ExhaustTempSpeedZ * particle.Temperature);
                    var vz = targetRiseRate - particle.VelZ;
                    var s = (float)Math.Sqrt((particle.VelX * particle.VelX) + (particle.VelY * particle.VelY) + (vz * vz));
                    var drag = Math.Min(1, s * ExhaustDrag);
                    particle.VelX -= particle.VelX * drag;
                    particle.VelY -= particle.VelY * drag;
                    particle.VelZ -= (particle.VelZ - targetRiseRate) * drag;

                    var randMotion = ExhaustRandomMotionBase + (particle.Temperature * ExhaustRandomMotionTemp);
                    particle.VelX += (random.NextFloat() - 0.5f) * randMotion;
                    particle.VelY += (random.NextFloat() - 0.5f) * randMotion;
                    particle.VelZ += (random.NextFloat() - 0.5f) * randMotion;
                }

                if (particle.Z < 2.2f && dH < 26)
                {
                    // Particle is inside launch pad, so can't rise, fade or expand
                    if (dH > 6 || speedH >= 0.04f) particle.VelZ = 0;

                    // Only fast moving smoke can escape the pad
                    if (speedH < 0.04f && dH > 6) particle.Alpha = 0f;
                }
                else
                {
                    particle.Alpha = (particle.Alpha * ExhaustFadeRateCurve) - ExhaustFadeRateLinear;
                    particle.Size += ExhaustBaseExpansion;
                }

                particle.X += particle.VelX;
                particle.Y += particle.VelY * 0.5f;
                particle.Z += particle.VelZ;

                if (particle.IsVisible) particleQueue.Enqueue(particle);

                if (particle.IsVisible && particle.Alpha > 0 && (particle.Z > 2.2f || dH < 8 || dH > 24))
                {
                    if (particle.Z <= 2.5f)
                    {
                        particlesForGroundRenderer.Add(particle);
                    }
                    else if(particle.Y < particle.StartY)
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

                listForGroundRenderer = particlesForGroundRenderer.ToList();
            }
        }

        private static void RedirectParticleInLaunchPad(SmokeParticle particle, float speedH)
        {
            particle.Size = 3f;
            var offset = (random.NextFloat() - 0.5f) * 10f;
            if (particle.VelX > 0 && particle.VelY > 0)
            {
                particle.VelX = speedH + ((random.NextFloat() - 0.5f) * 0.1f);
                particle.VelY = speedH + ((random.NextFloat() - 0.5f) * 0.1f);
                particle.X = particle.StartX + 8f + offset;
                particle.Y = particle.StartY + 4f - (0.5f * offset);
            }
            else if (particle.VelX > 0 && particle.VelY < 0)
            {
                particle.VelX = speedH + ((random.NextFloat() - 0.5f) * 0.1f);
                particle.VelY = -speedH + ((random.NextFloat() - 0.5f) * 0.1f);
                particle.X = particle.StartX + 8f + offset;
                particle.Y = particle.StartY - 4f + (0.5f * offset);
            }
            else if (particle.VelX < 0 && particle.VelY > 0)
            {
                particle.VelX = -speedH + ((random.NextFloat() - 0.5f) * 0.1f);
                particle.VelY = speedH + ((random.NextFloat() - 0.5f) * 0.1f);
                particle.X = particle.StartX - 8f - offset;
                particle.Y = particle.StartY + 4f - (0.5f * offset);
            }
            else if (particle.VelX < 0 && particle.VelY < 0)
            {
                particle.VelX = -speedH + ((random.NextFloat() - 0.5f) * 0.1f);
                particle.VelY = -speedH + ((random.NextFloat() - 0.5f) * 0.1f);
                particle.X = particle.StartX - 8f - offset;
                particle.Y = particle.StartY - 4f + (0.5f * offset);
            }
        }
    }
}
