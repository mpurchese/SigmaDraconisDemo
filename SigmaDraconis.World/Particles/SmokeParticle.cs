namespace SigmaDraconis.World.Particles
{
    using ProtoBuf;
    using Shared;

    [ProtoContract]
    public class SmokeParticle
    {
        private SmokeParticle() { }  // Deserialization ctor

        public SmokeParticle(SmokeParticleType type, int tileIndex, float x, float y, float z, float vx, float vy, float vz, float size, float alpha)
        {
            this.ParticleType = type;
            this.TileIndex = tileIndex;
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.VelX = vx;
            this.VelY = vy;
            this.VelZ = vz;
            this.StartX = x;
            this.StartY = y;
            this.StartZ = z;
            this.Size = size;
            this.Alpha = alpha;
            this.IsVisible = true;
        }

        [ProtoMember(1)]
        public float X { get; set; }

        [ProtoMember(2)]
        public float Y { get; set; }

        [ProtoMember(3)]
        public float Z { get; set; }

        [ProtoMember(4)]
        public float VelX { get; set; }

        [ProtoMember(5)]
        public float VelY { get; set; }

        [ProtoMember(6)]
        public float VelZ { get; set; }

        [ProtoMember(7)]
        public float Size { get; set; }

        [ProtoMember(8)]
        public float Alpha { get; set; }

        [ProtoMember(9)]
        public float StartX { get; set; }

        [ProtoMember(10)]
        public float StartY { get; set; }

        [ProtoMember(11)]
        public float StartZ { get; set; }

        [ProtoMember(12)]
        public float Temperature { get; set; }

        [ProtoMember(13)]
        public bool IsVisible { get; set; }

        [ProtoMember(14)]
        public float AlphaScale { get; set; }

        [ProtoMember(15)]
        public SmokeParticleType ParticleType { get; set; }

        [ProtoMember(16)]
        public int Row { get; set; }

        [ProtoMember(17)]
        public int TileIndex { get; set; }
    }
}
