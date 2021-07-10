namespace SigmaDraconis.World.Flora
{
    using System;
    using System.Collections.Generic;
    using ProtoBuf;
    using Rooms;
    using Shared;
    using WorldInterfaces;

    /// <summary>
    /// Roundleaf Plant
    /// </summary>
    [ProtoContract]
    public class SmallPlant4 : Plant, IAnimatedThing, IRotatableThing
    {
        [ProtoMember(1)]
        public Direction Direction { get; }

        [ProtoMember(2)]
        public int Growth { get; private set; }

        [ProtoMember(3, IsRequired = true)]
        public bool Seeded { get; private set; }

        [ProtoMember(4)]
        private int minTemp;

        [ProtoMember(5)]
        private double minLight;

        [ProtoMember(6)]
        private int hoursDead;

        public override int GrowthStage => this.IsDead ? 8 : this.animationFrame;
        public override int MaxGrowthStage => 8;
        public override bool HasDeadFrame => true;
        public override bool IsDead => this.hoursDead > 0;

        public SmallPlant4() : base(ThingType.SmallPlant4)
        {
        }

        public SmallPlant4(ISmallTile tile, bool fromSeed = false) : base(ThingType.SmallPlant4, tile, 1)
        {
            this.minTemp = Rand.Next(8) + 1;
            this.minLight = (Rand.NextDouble() * 0.25) + 0.1;

            this.Direction = (Direction)Rand.Next(4) + 4;
            if (fromSeed) this.AnimationFrame = 1;
            else
            {
                // From world generator.  Plant may be 1 - 4 days old.  If 4 days old then dead.
                var daysOld = Rand.Next(4) + 1;
                if (daysOld == 4)
                {
                    this.Growth = 300;
                    this.hoursDead = 55 + Rand.Next(10);
                }
                else this.Growth = (daysOld * 80) + Rand.Next(10);

                this.AnimationFrame = this.IsDead ? 9 : Math.Min(8, (this.Growth / 30) + 1);
            }
        }

        public override Dictionary<ItemType, int> GetDeconstructionYield()
        {
            var biomass = 1;
            if (this.IsDead) biomass = 2;
            else if (this.AnimationFrame >= 12) biomass = 4;
            else if (this.AnimationFrame >= 8) biomass = 3;
            else if (this.AnimationFrame >= 4) biomass = 2;

            var result = new Dictionary<ItemType, int>
            {
                { ItemType.Metal, 0 },
                { ItemType.Biomass, biomass },
                { ItemType.IronOre, 0 }
            };

            return result;
        }

        // This is called every hour
        public override List<int> UpdateGrowth()
        {
            List<int> seedTiles = null;
            base.UpdateGrowth();

            // For old versions
            if (this.minTemp == 0 && this.minLight == 0)
            {
                this.minTemp = Rand.Next(8) + 1;
                this.minLight = (Rand.NextDouble() * 0.25) + 0.1;
                if (this.Growth > 300)
                {
                    this.Growth = 300;
                    this.hoursDead = World.WorldTime.Hour + Rand.Next(10);
                }
            }

            var light = RoomManager.GetTileLightLevel(this.MainTileIndex);
            var temperature = RoomManager.GetTileTemperature(this.MainTileIndex);

            // Grows when warm and light.  Should be about 80/day.  Seeds at 250 then dies when cold.  Removed from world after dead for one day.
            var grow = this.Growth < 300 && light > this.minLight && temperature >= this.minTemp;
            if (grow)
            {
                this.Growth++;
                if (this.Growth == 300) seedTiles = this.SpreadSeed();
            }

            if (this.hoursDead > 0 || (this.Growth == 300 && temperature < -Rand.Next(6))) this.hoursDead++;
            if (this.hoursDead >= 192 && this.RecycleProgress == 0)
            {
                World.RemoveThing(this);
            }

            var frame = this.IsDead ? 9 : Math.Min(8, (this.Growth / 30) + 1);
            if (this.AnimationFrame != frame)
            {
                EventManager.RaiseEvent(EventType.Plant, EventSubType.Updated, this);
                this.AnimationFrame = frame;
            }

            return seedTiles;
        }

        private List<int> SpreadSeed()
        {
            var seedTiles = new List<int>();
            foreach (var tile in this.MainTile.AdjacentTiles8)
            {
                if (tile.BiomeType == BiomeType.Wet && tile.TerrainType == TerrainType.Dirt && Rand.NextDouble() > 0.8)
                {
                    seedTiles.Add(tile.Index);
                }
            }

            this.Seeded = true;

            return seedTiles;
        }

        public override string GetTextureName(int layer = 1)
        {
            return $"{base.GetTextureName()}_{this.Direction.ToString()}";
        }
    }
}
