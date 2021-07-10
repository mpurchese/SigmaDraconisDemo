namespace SigmaDraconis.World.Flora
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Draconis.Shared;
    using ProtoBuf;
    using Shared;
    using Rooms;
    using WorldInterfaces;

    [ProtoContract]
    public class Swordleaf : Plant, IRenderOffsettable, IPositionOffsettable, IRotatableThing, IPlantWithAnimatedFlower, IAnimatedThing, IThingHidesTiles
    {
        [ProtoMember(1)]
        public Direction Direction { get; private set; }

        [ProtoMember(2)]
        public int? FlowerFrame { get; private set; }

        [ProtoMember(3)]
        public Vector2f PositionOffset { get; protected set; }

        public int? FlowerRenderLayer => FlowerFrame.HasValue ? 0 : (int?)null;

        public override int GrowthStage => Math.Min(6, this.AnimationFrame);
        public override int MaxGrowthStage => 6;
        public override bool CanFlower => true;
        public override bool IsFlowering => this.FlowerFrame == 4;
        public override bool HasDeadFrame => true;
        public override bool IsDead => this.AnimationFrame == 7;

        public Swordleaf() : base(ThingType.Grass)
        {
        }

        public Swordleaf(ISmallTile tile, float age) : base(ThingType.Grass, tile, 1)
        {
            this.Age = age;

            this.UpdateAnimationFrame();

            float x = (float)(Rand.NextDouble() * 0.8f) - 0.4f;
            float y = (float)(Rand.NextDouble() * 0.8f) - 0.4f;

            this.PositionOffset = new Vector2f(x, y);
            this.RenderPositionOffset = new Vector2i((int)((10.6666667 * x) + (10.6666667 * y)), (int)((5.3333333 * y) - (5.3333333 * x)));

            this.Direction = Direction.NE + Rand.Next(4);
        }

        public override void AfterDeserialization()
        {
            if (this.animationFrame > 7)
                this.animationFrame = this.age > 0.95f ? 7 : 6;
            base.AfterDeserialization();
        }

        public ShadowModel ShadowModel { get; protected set; } = new ShadowModel();

        public override List<int> UpdateGrowth()
        {
            // Grow only when above freezing and not completely dark
            var t = RoomManager.GetTileTemperature(this.MainTileIndex);
            var l = RoomManager.GetTileLightLevel(this.MainTileIndex);
            if (t <= 0 || l <= 0.0) return null;

            // Grow faster when warm and bright
            if (Rand.Next(30) > t || Rand.NextFloat() > l) return null;

            var prevAge = this.age;
            this.age += 0.01f;

            if (this.age >= 1.0f && this.RecycleProgress == 0)
            {
                var tile = this.MainTile;
                World.RemoveThing(this);

                // Leave a seedling
                var grass = new Swordleaf(tile, 0);
                World.AddThing(grass);
            }
            else if (this.RecycleProgress == 0)
            {
                if (this.age >= 0.95f && prevAge < 0.95f)
                {
                    // Each time grass dies, create up to 4 new ones in adjacent tiles
                    var adjacentTiles = this.MainTile.AdjacentTiles8;
                    for (int i = 0; i < 4; i++)
                    {
                        var tile = adjacentTiles[Rand.Next(adjacentTiles.Count)];
                        if (tile.TerrainType == TerrainType.Dirt && tile.BiomeType == BiomeType.Grass && !tile.ThingsAll.Any())
                        {
                            var grass = new Swordleaf(tile, 0);
                            World.AddThing(grass);
                        }
                    }
                }

                this.UpdateAnimationFrame();
            }

            return null;
        }

        public override Dictionary<ItemType, int> GetDeconstructionYield()
        {
            var organics = 4;
            if (this.AnimationFrame == 7) organics = 2;
            else if (this.AnimationFrame <= 2) organics = 1;
            else if (this.AnimationFrame <= 4) organics = 2;
            else if (this.AnimationFrame == 5) organics = 3;

            var result = new Dictionary<ItemType, int>
            {
                { ItemType.Metal, 0 },
                { ItemType.Biomass, organics},
                { ItemType.IronOre, 0 }
            };

            return result;
        }

        public IEnumerable<ISmallTile> GetHiddenTiles()
        {
            if (this.AnimationFrame < 3) yield break;

            var tile = this.mainTile.TileToN;
            if (tile == null) yield break;
            yield return tile;

            if (this.AnimationFrame < 5) yield break;

            tile = tile.TileToN;
            if (tile != null) yield return tile;

            if (this.AnimationFrame < 6) yield break;

            if (this.MainTile.TileToNE != null) yield return this.MainTile.TileToNE;
            if (this.MainTile.TileToNW != null) yield return this.MainTile.TileToNW;
            if (this.MainTile.TileToNE?.TileToN != null) yield return this.MainTile.TileToNE.TileToN;
            if (this.MainTile.TileToNW?.TileToN != null) yield return this.MainTile.TileToNW.TileToN;

            tile = tile.TileToN;
            if (tile != null) yield return tile;
        }

        public override string GetTextureName(int layer = 1)
        {
            return layer == 1 ? $"{base.GetTextureName()}_{this.Direction.ToString()}" : $"GrassSpike_{this.FlowerFrame}";
        }

        private bool UpdateAnimationFrame()
        {
            int newFrame;
            var newFlowerFrame = this.FlowerFrame;

            if (this.age < 0.25f)
            {
                // Growing
                newFrame = (int)(this.age * 20) + 1;
            }
            else if (this.age < 0.95f)
            {
                // Mature
                newFrame = 6;
                if (this.age >= 0.85f)
                {
                    // Mature flower
                    newFlowerFrame = 4;
                }
                else if (this.age > 0.76f)
                {
                    // Growing flower
                    newFlowerFrame = 1 + (int)(((this.age - 0.76f) * 100f) / 3);
                }
            }
            else
            {
                // Dead
                newFrame = 7;
                newFlowerFrame = null;
            }

            if (newFrame != this.animationFrame || newFlowerFrame != this.FlowerFrame)
            {
                this.AnimationFrame = newFrame;
                this.FlowerFrame = newFlowerFrame;
                return true;
            }

            return false;
        }
    }
}
