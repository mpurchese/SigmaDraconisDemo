namespace SigmaDraconis.World.Flora
{
    using System;
    using System.Collections.Generic;
    using ProtoBuf;
    using Draconis.Shared;
    using Shared;
    using Rooms;
    using WorldInterfaces;

    /// <summary>
    /// Pin-rodax
    /// </summary>
    [ProtoContract]
    public class SmallPlant8 : Plant, IAnimatedThing, IRenderOffsettable, IPositionOffsettable
    {
        [ProtoMember(2)]
        public int Growth { get; private set; }

        [ProtoMember(3)]
        public Vector2f PositionOffset { get; protected set; }

        [ProtoMember(4)]
        private readonly int minTemp;

        [ProtoMember(5)]
        private readonly double minLight;

        [ProtoMember(6)]
        private int hoursDead;

        public override int GrowthStage => Math.Min(5, (this.AnimationFrame + 3) / 4);
        public override int MaxGrowthStage => 5;
        public override bool HasDeadFrame => true;
        public override bool IsDead => this.hoursDead > 0;

        public SmallPlant8() : base(ThingType.SmallPlant8)
        {
        }

        public SmallPlant8(ISmallTile tile, bool fromSeed = false) : base(ThingType.SmallPlant8, tile, 1)
        {
            this.minTemp = Rand.Next(8) + 1;
            this.minLight = (Rand.NextDouble() * 0.25) + 0.1;

            var direction = Rand.Next(4) + 1;

            if (fromSeed) this.AnimationFrame = direction;
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

                this.UpdateAnimationFrame();
            }

            var offsetX = (float)(Rand.NextDouble() * 0.3f) - 0.15f;
            var offsetY = (float)(Rand.NextDouble() * 0.3f) - 0.15f;
            this.PositionOffset = new Vector2f(offsetX, offsetY);
            this.RenderPositionOffset = new Vector2i((int)((10.6666667 * offsetX) + (10.6666667 * offsetY)), (int)((5.3333333 * offsetY) - (5.3333333 * offsetX)));
        }

        public override Dictionary<ItemType, int> GetDeconstructionYield()
        {
            var organics = 4;
            if (this.AnimationFrame > 20) organics = 2;
            else if (this.AnimationFrame <= 4) organics = 1;
            else if (this.AnimationFrame <= 8) organics = 2;
            else if (this.AnimationFrame <= 12) organics = 3;

            return new Dictionary<ItemType, int>
            {
                { ItemType.Metal, 0 },
                { ItemType.Biomass, organics },
                { ItemType.IronOre, 0 }
            };
        }

        // This is called every hour
        public override List<int> UpdateGrowth()
        {
            // Day 1: Grows to ~80
            // Day 2: Grows to ~160
            // Day 3: Grows to ~240
            // Day 4: Grows to 300 then dies at night
            // Day 5: Dead

            List<int> seedTiles = null;
            base.UpdateGrowth();

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

            this.UpdateAnimationFrame();

            return seedTiles;
        }

        private void UpdateAnimationFrame()
        {
            var direction = this.AnimationFrame % 4;
            if (direction == 0) direction = 4;

            var m = 0;
            if (this.IsDead) m = 5;
            else if (this.Growth > 260) m = 4;
            else if (this.Growth > 240) m = 3;
            else if (this.Growth > 160) m = 2;
            else if (this.Growth > 80) m = 1;

            var frame = (m * 4) + direction;
            if (this.AnimationFrame != frame)
            {
                EventManager.RaiseEvent(EventType.Plant, EventSubType.Updated, this);
                this.AnimationFrame = frame;
            }
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

            return seedTiles;
        }
    }
}
