namespace SigmaDraconis.World.Particles
{
    using ProtoBuf;

    [ProtoContract]
    public class MicrobotParticle
    {
        private MicrobotParticle() { }  // Deserialization ctor

        public MicrobotParticle(float x, float y, float z, float targetX, float targetY, float targetZ, float initialSize, int colonistId, bool incoming, int colourIndex, bool hasSound)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.TargetX = targetX;
            this.TargetY = targetY;
            this.TargetZ = targetZ;
            this.Alpha = 1;
            this.Size = initialSize;
            this.ColonistID = colonistId;
            this.IsIncoming = incoming;
            this.ColourIndex = colourIndex;
            this.HasSound = hasSound;
        }

        [ProtoMember(1)]
        public float X { get; set; }

        [ProtoMember(2)]
        public float Y { get; set; }

        [ProtoMember(3)]
        public float Z { get; set; }

        [ProtoMember(4)]
        public float TargetX { get; set; }

        [ProtoMember(5)]
        public float TargetY { get; set; }

        [ProtoMember(6)]
        public float TargetZ { get; set; }

        [ProtoMember(7)]
        public float Alpha { get; set; }

        [ProtoMember(8)]
        public float Size { get; set; }

        [ProtoMember(9)]
        public int ColonistID { get; set; }

        [ProtoMember(10)]
        public bool IsIncoming { get; set; }

        [ProtoMember(11)]
        public int Age { get; set; }

        [ProtoMember(12)]
        public int ColourIndex { get; set; }

        [ProtoMember(14)]
        public bool HasSound { get; set; }
    }
}
