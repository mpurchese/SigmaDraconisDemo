namespace SigmaDraconis.World.Flora
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Draconis.Shared;
    using ProtoBuf;
    using Rooms;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class CoastGrass : Plant, IRenderOffsettable, IPositionOffsettable, IAnimatedThing, IThingWithRenderLayer, IThingHidesTiles
    {
        [ProtoMember(1)]
        public Direction Direction { get; private set; }

        [ProtoMember(2)]
        public int? FlowerFrame { get; private set; }

        [ProtoMember(3)]
        public Vector2f PositionOffset { get; protected set; }

        [ProtoMember(4)]
        public int RenderLayer { get; set; }

        public override int GrowthStage => Math.Min(8, (this.AnimationFrame + 3) / 4);
        public override int MaxGrowthStage => 8;
        public override bool CanFlower => true;
        public override bool IsFlowering => this.animationFrame >= 29 && this.animationFrame <= 32;
        public override bool HasDeadFrame => true;
        public override bool IsDead => this.animationFrame >= 33;

        public CoastGrass() : base(ThingType.CoastGrass)
        {
        }

        public CoastGrass(ISmallTile tile, Vector2f positionOnTile, float age) : base(ThingType.CoastGrass, tile, 1)
        {
            this.Age = age;

            var direction = Rand.Next(4) + 1;
            this.AnimationFrame = Math.Min(32, 4 * (int)(this.Age * 9f)) + direction;

            float x = positionOnTile.X;
            float y = positionOnTile.Y;

            this.PositionOffset = new Vector2f(x, y);
            this.RenderPositionOffset = new Vector2i((int)((10.6666667 * x) + (10.6666667 * y)), (int)((5.3333333 * y) - (5.3333333 * x)));

            var plants = tile.ThingsPrimary.OfType<CoastGrass>().ToList();

            var i = 0;
            foreach (var p in plants.OrderBy(p => p.RenderPositionOffset.Y))
            {
                p.RenderLayer = i;
                i++;
            }
        }

        public override List<int> UpdateGrowth()
        {
            // Grow only when above freezing and not completely dark
            var temp = RoomManager.GetTileTemperature(this.MainTileIndex);
            var l = RoomManager.GetTileLightLevel(this.MainTileIndex);
            if (temp <= 0 || l <= 0.0) return null;

            // Grow faster when warm and bright
            if (Rand.Next(30) > temp || Rand.NextFloat() > l) return null;

            var prevAge = this.age;
            this.age += 0.01f;

            if (this.age >= 1.0f && this.RecycleProgress == 0)
            {
                World.RemoveThing(this);
            }
            else if (this.RecycleProgress == 0)
            {
                if (this.age >= 0.9f && prevAge < 0.9f)
                {
                    // Create up to 8 new grass seedlings in adjacent tiles
                    foreach (var tile in this.MainTile.AdjacentTiles8)
                    {
                        if (tile?.TerrainType == TerrainType.Dirt && tile.BiomeType == BiomeType.Wet && tile.AdjacentTiles8.Any(t2 => t2.TerrainType == TerrainType.Coast))
                        {
                            var offset = GetPositionOnTileForNewPlant(tile);
                            if (offset != null)
                            {
                                var newPlant = new CoastGrass(tile, offset, 0);
                                World.AddThing(newPlant);
                            }
                        }
                    }
                }

                var direction = this.AnimationFrame % 4;
                if (direction == 0) direction = 4;
                this.AnimationFrame = Math.Min(32, 4 * (int)(this.Age * 9f)) + direction;
            }

            return null;
        }

        public override Dictionary<ItemType, int> GetDeconstructionYield()
        {
            var organics = this.mainTile.ThingsPrimary.OfType<CoastGrass>().Sum(c => DeconstructionYieldSiglePlant());

            var result = new Dictionary<ItemType, int>
            {
                { ItemType.Metal, 0 },
                { ItemType.Biomass, Math.Max(1, organics) },
                { ItemType.IronOre, 0 }
            };

            return result;
        }

        public int DeconstructionYieldSiglePlant()
        {
            if (this.animationFrame <= 4) return 0;
            if (this.animationFrame <= 8) return 1;
            if (this.animationFrame <= 16) return 2;
            if (this.animationFrame >= 33) return 2;
            return 3;
        }

        public IEnumerable<ISmallTile> GetHiddenTiles()
        {
            if (this.AnimationFrame < 12) yield break;

            var tile = this.mainTile.TileToN;
            if (tile == null) yield break;
            yield return tile;

            if (this.AnimationFrame < 16) yield break;

            tile = tile.TileToN;
            if (tile != null) yield return tile;
        }

        public static Vector2f GetPositionOnTileForNewPlant(ISmallTile tile)
        {
            if (tile.ThingsAll.Any(t => t.ThingType != ThingType.CoastGrass) || tile.ThingsAll.Count > 2) return null;

            // 8 attempts to find a free position
            for (int i = 0; i < 8; i++)
            {
                var ok = true;
                var vec = new Vector2f((float)(Rand.NextDouble() * 0.8f) - 0.4f, (float)(Rand.NextDouble() * 0.8f) - 0.4f);
                foreach (var p in tile.ThingsPrimary.OfType<CoastGrass>())
                {
                    if ((p.PositionOffset - vec).Length() < 0.5f)
                    {
                        ok = false;
                        break;
                    }
                }

                if (ok) return vec;
            }

            return null;
        }
    }
}
