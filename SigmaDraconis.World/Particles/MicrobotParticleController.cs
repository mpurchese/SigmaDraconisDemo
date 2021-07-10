namespace SigmaDraconis.World.Particles
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Xna.Framework;
    using Draconis.Shared;
    using Shared;
    using WorldInterfaces;

    /// <summary>
    /// For colonist building and harvesting
    /// </summary>
    public static class MicrobotParticleController
    {
        private static readonly Dictionary<int, Queue<MicrobotParticle>> particleQueuesByRow = new Dictionary<int, Queue<MicrobotParticle>>();

        public static HashSet<int> RowsForRendererUpdate = new HashSet<int>();
        public static HashSet<int> ActiveColonists = new HashSet<int>();
        public static HashSet<int> ActiveSounds = new HashSet<int>();

        public static void Clear()
        {
            ActiveColonists.Clear();
            ActiveSounds.Clear();
            particleQueuesByRow.Clear();
            RowsForRendererUpdate.Clear();
        }

        public static void AddParticle(ISmallTile tile, float x, float y, float z, float targetX, float targetY, float targetZ, int colonistID, bool incoming, int colourIndex = 0, bool hasSound = true)
        {
            var row = tile.Row;
            if (!particleQueuesByRow.ContainsKey(row))
            {
                particleQueuesByRow.Add(row, new Queue<MicrobotParticle>());
            }

            var particle = new MicrobotParticle(tile.CentrePosition.X + x, tile.CentrePosition.Y + y, z, tile.CentrePosition.X + targetX, tile.CentrePosition.Y + targetY, targetZ, incoming ? 4f : 1.5f, colonistID, incoming, colourIndex, hasSound);
            particleQueuesByRow[row].Enqueue(particle);

            if (!RowsForRendererUpdate.Contains(row)) RowsForRendererUpdate.Add(row);
        }

        public static IEnumerable<MicrobotParticle> GetParticles(int row)
        {
            return particleQueuesByRow.ContainsKey(row) ? particleQueuesByRow[row] as IEnumerable<MicrobotParticle> : new List<MicrobotParticle>();
        }

        public static void Update()
        {
            ActiveColonists.Clear();

            var outgoingCountByColonist = new Dictionary<int, int>();
            var incomingCountByColonist = new Dictionary<int, int>();

            foreach (var row in particleQueuesByRow.Keys)
            {
                var queue = particleQueuesByRow[row];
                if (queue.Count > 0 && !RowsForRendererUpdate.Contains(row)) RowsForRendererUpdate.Add(row);
                while (queue.Count > 0 && queue.Peek().Alpha < 0.001f) queue.Dequeue();

                foreach (var particle in queue)
                {
                    var outVec = new Vector3(particle.TargetX - particle.X, particle.TargetY - particle.Y, particle.TargetZ - particle.Z);
                    var outLength = outVec.Length();
                    if (outLength < 0.1f)
                    {
                        particle.Alpha = 0f;
                    }
                    else
                    {
                        var multiplier = Math.Min(0.5f, outLength * 0.5f);
                        outVec.Normalize();
                        particle.X += outVec.X * multiplier;
                        particle.Y += outVec.Y * multiplier;
                        particle.Z += outVec.Z * multiplier;
                        particle.Size = particle.IsIncoming ? outLength.Clamp(1.5f, 4f) : Math.Min(4f, (particle.Age + 3) * 0.5f);
                        if (!ActiveColonists.Contains(particle.ColonistID))
                        {
                            ActiveColonists.Add(particle.ColonistID);
                            incomingCountByColonist.Add(particle.ColonistID, 0);
                            outgoingCountByColonist.Add(particle.ColonistID, 0);
                            if (particle.HasSound && !ActiveSounds.Contains(particle.ColonistID))
                            {
                                ActiveSounds.Add(particle.ColonistID);
                                EventManager.EnqueueSoundAddEvent(particle.ColonistID, "Construct");
                            }
                        }

                        if (particle.IsIncoming) incomingCountByColonist[particle.ColonistID]++;
                        else outgoingCountByColonist[particle.ColonistID]++;
                    }

                    particle.Age++;
                }
            }

            foreach (var sound in ActiveSounds.ToList())
            {
                if (!ActiveColonists.Contains(sound))
                {
                    ActiveSounds.Remove(sound);
                    EventManager.EnqueueSoundRemoveEvent(sound);
                }
                else
                {
                    var outgoing = outgoingCountByColonist[sound];
                    var incoming = incomingCountByColonist[sound];
                    var volume = 0f;
                    var pitch = 0f;
                    if (outgoing > 0 || incoming > 0)
                    {
                        volume = Mathf.Min(0.25f, 0.002f * (outgoing + incoming));
                        pitch = 0.4f * (incoming / (incoming + outgoing));
                    }

                    if (volume > 0.001f) EventManager.EnqueueSoundUpdateEvent(sound, false, volume, pitch: pitch);
                    else EventManager.EnqueueSoundUpdateEvent(sound, true, 0f);
                }
            }
        }

        // For serialization
        public static Dictionary<int, List<MicrobotParticle>> GetAllParticles()
        {
            var result = new Dictionary<int, List<MicrobotParticle>>();
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
        public static void SetAllParticles(Dictionary<int, List<MicrobotParticle>> particles)
        {
            particleQueuesByRow.Clear();

            if (particles == null) return;

            foreach (var key in particles.Keys)
            {
                var queue = new Queue<MicrobotParticle>();
                foreach (var particle in particles[key])
                {
                    queue.Enqueue(particle);
                }

                particleQueuesByRow.Add(key, queue);
            }
        }
    }
}
