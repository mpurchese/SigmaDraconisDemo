namespace SigmaDraconis.World.Terrain
{
    using ProtoBuf;
    using SigmaDraconis.Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class MineTileResource : IMineTileResource
    {
        [ProtoMember(1)]
        public ItemType Type { get; set; }
        [ProtoMember(2)]
        public int Count { get; set; }
        [ProtoMember(3)]
        public MineResourceDensity Density { get; set; }
        [ProtoMember(4, IsRequired = true)]
        public bool IsVisible { get; set; }
        [ProtoMember(5)]
        public double ExtractionProgress { get; set; }
        [ProtoMember(6)]
        public int? MineId { get; set; }
        [ProtoMember(7)]
        public double SurveyProgress { get; set; }
        [ProtoMember(8)]
        public int? ReservedBy { get; set; }
        [ProtoMember(9)]
        public long ReservedAt { get; set; }

        public IMineTileResource Clone()
        {
            return new MineTileResource
            {
                IsVisible = this.IsVisible,
                Count = this.Count,
                Density = this.Density,
                Type = this.Type,
                ExtractionProgress = this.ExtractionProgress,
                MineId = this.MineId,
                SurveyProgress = this.SurveyProgress,
                ReservedBy = this.ReservedBy,
                ReservedAt = this.ReservedAt
            };
        }
    }
}
