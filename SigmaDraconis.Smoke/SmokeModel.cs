namespace SigmaDraconis.Smoke
{
    using System.Collections.Generic;
    using Microsoft.Xna.Framework;
    using Shared;

    public class SmokeModel
    {
        public Dictionary<Direction, Vector3> OriginsByDirection { get; } = new Dictionary<Direction, Vector3>();
        public SmokeParticleType ParticleType { get; set; } = SmokeParticleType.Normal;
        public float ProductionRate { get; set; } = 1f;
        public int Layer { get; set; } = 1;
    }
}
