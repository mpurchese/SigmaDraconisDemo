namespace SigmaDraconis.WorldControllers
{
    using ProtoBuf;
    using Shared;

    [ProtoContract]
    public class TileGroundWaterDetail
    {
        [ProtoMember(1)]
        public int CurrentLevel { get; set; }

        [ProtoMember(2)]
        public int MaxLevel { get; set; }

        [ProtoMember(3)]
        public BiomeType OriginalBiome { get; set; }

        // Ctor for deserialization
        public TileGroundWaterDetail()
        {
        }

        public TileGroundWaterDetail(int currentLevel, int maxLevel, BiomeType originalBiome)
        {
            this.CurrentLevel = currentLevel;
            this.MaxLevel = maxLevel;
            this.OriginalBiome = originalBiome;
        }
    }
}
