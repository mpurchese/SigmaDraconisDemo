namespace SigmaDraconis.World.Flora
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using Draconis.Shared;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class Tree : Plant, ITree
    {
        public Tree() : base(ThingType.Tree)
        {
        }

        public Tree(ISmallTile mainTile, float age) : base(ThingType.Tree, mainTile, 1)
        {
            this.Age = age;
            this.TextureSize = new Vector2i(25, 400);
            this.UpdateHeight();
        }

        [ProtoMember(1)]
        public float Height { get; set; }

        [ProtoMember(2)]
        public float TreeTopWarpPhase { get; private set; }

        public override int GrowthStage => Math.Min(16, (int)Math.Floor(this.Age) + 1);
        public override int MaxGrowthStage => 16;

        // Needed because TreeTrunkShadowRenderer inherits ShadowRenderer
        public ShadowModel ShadowModel { get; protected set; } = new ShadowModel();

        public override void AfterDeserialization()
        {
            this.TextureSize = new Vector2i(25, 400);
            this.RenderSize = new Vector2i((int)Math.Min(0.195 * (this.Height + 32), 25.0) / 3, (int)(this.Height * 0.975));
            if (this.TreeTopWarpPhase == 0f) this.TreeTopWarpPhase = Rand.NextFloat();
            base.AfterDeserialization();
        }

        public override void AfterAddedToWorld()
        {
            // No shadow model, but still has a shadow using special renderer
            EventManager.RaiseEvent(EventType.Shadow, EventSubType.Updated, this);

            base.AfterAddedToWorld();
        }

        public override List<int> UpdateGrowth()
        {
            // Grow only when warm and light
            if (World.Temperature <= 0 || World.WorldLight.Brightness < 0.25) return null;

            this.age += .05f;

            if (this.age > 40 && this.RecycleProgress == 0)
            {
                // Die ...
                World.RemoveThing(this);

                // ... but create a new tree somewhere else.  Up to 50 attempts to find a suitable location.
                var tileCount = World.SmallTiles.Count;
                for (int i = 0; i < 50; i++)
                {
                    var tile = World.SmallTiles[Rand.Next(tileCount)];
                    if (!tile.ThingsAll.Any() && tile.TerrainType == TerrainType.Dirt && tile.BiomeType == BiomeType.Forest && tile.AdjacentTiles8.SelectMany(t => t.ThingsPrimary).All(t => t.ThingType != ThingType.Tree))
                    {
                        var tree = new Tree(tile, 0);
                        World.AddThing(tree);
                        break;
                    }
                }
            }
            else this.UpdateHeight();

            return null;
        }

        public override Dictionary<ItemType, int> GetDeconstructionYield()
        {
            var result = new Dictionary<ItemType, int>
            {
                { ItemType.Metal, 0 },
                { ItemType.Biomass, (int)(this.Height * 20f / 98f) },
                { ItemType.IronOre, 0 }
            };

            return result;
        }

        public IEnumerable<ISmallTile> GetHiddenTiles()
        {
            var h = (int)(this.Height + 0.1f);

            var tile = this.mainTile.TileToN;
            if (tile == null) yield break;
            yield return tile;

            if (h < 24) yield break;

            tile = tile.TileToN;
            if (tile == null) yield break;
            yield return tile;

            if (h == 24)
            {
                if (tile.TileToSE != null) yield return tile.TileToSE;
                if (tile.TileToSW != null) yield return tile.TileToSW;
                yield break;
            }

            if (h <= 32)
            {
                if (tile.TileToN != null) yield return tile.TileToN;
                if (tile.TileToNE != null) yield return tile.TileToNE;
                if (tile.TileToNW != null) yield return tile.TileToNW;
                yield break;
            }

            for (int i = 40; i <= 80; i += 10)
            {
                tile = tile.TileToN;
                if (tile == null) yield break;
                yield return tile;

                if (h <= i + 5)
                {
                    if (tile.TileToN != null) yield return tile.TileToN;
                    if (tile.TileToNE != null) yield return tile.TileToNE;
                    if (tile.TileToNW != null) yield return tile.TileToNW;
                    if (tile.TileToE != null) yield return tile.TileToE;
                    if (tile.TileToW != null) yield return tile.TileToW;
                    if (tile.TileToSE != null) yield return tile.TileToSE;
                    if (tile.TileToSW != null) yield return tile.TileToSW;
                    if (h > i && tile.TileToN != null)
                    {
                        if (tile.TileToN.TileToE != null) yield return tile.TileToN.TileToE;
                        if (tile.TileToN.TileToW != null) yield return tile.TileToN.TileToW;
                        if (tile.TileToN.TileToNE != null) yield return tile.TileToN.TileToNE;
                        if (tile.TileToN.TileToNW != null) yield return tile.TileToN.TileToNW;
                        if (h > 80)
                        {
                            if (tile.TileToNE?.TileToE != null) yield return tile.TileToNE.TileToE;
                            if (tile.TileToNW?.TileToW != null) yield return tile.TileToNW.TileToW;
                        }
                    }
                    yield break;
                }
            }

            if (tile.TileToE != null) yield return tile.TileToE;
            if (tile.TileToW != null) yield return tile.TileToW;
            if (tile.TileToSE != null) yield return tile.TileToSE;
            if (tile.TileToSW != null) yield return tile.TileToSW;
            if (tile.TileToE?.TileToNE != null) yield return tile.TileToE.TileToNE;
            if (tile.TileToW?.TileToNW != null) yield return tile.TileToW.TileToNW;

            tile = tile.TileToN;
            if (tile == null) yield break;
            yield return tile;

            if (tile.TileToN != null) yield return tile.TileToN;
            if (tile.TileToNE != null) yield return tile.TileToNE;
            if (tile.TileToNW != null) yield return tile.TileToNW;
            if (tile.TileToE != null) yield return tile.TileToE;
            if (tile.TileToW != null) yield return tile.TileToW;
            if (tile.TileToSE != null) yield return tile.TileToSE;
            if (tile.TileToSW != null) yield return tile.TileToSW;

            if (h >= 95)
            {
                if (tile.TileToNE?.TileToN != null) yield return tile.TileToNE.TileToN;
                if (tile.TileToNE?.TileToNE != null) yield return tile.TileToNE.TileToNE;
                if (tile.TileToNE?.TileToE != null) yield return tile.TileToNE.TileToE;
                if (tile.TileToNW?.TileToN != null) yield return tile.TileToNW.TileToN;
                if (tile.TileToNW?.TileToNW != null) yield return tile.TileToNW.TileToNW;
                if (tile.TileToNW?.TileToW != null) yield return tile.TileToNW.TileToW;
                if (h >= 98 && tile.TileToN?.TileToN != null) yield return tile.TileToN.TileToN;
            }
        }

        private void UpdateHeight()
        {
            var prevHeight = this.Height;

            var ageInt = (int)Math.Floor(this.Age);
            if (ageInt == 0) this.Height = 32;
            else if (ageInt == 1) this.Height = 48;
            else if (ageInt == 2) this.Height = 64;
            else if (ageInt == 3) this.Height = 80;
            else if (ageInt < 15) this.Height = 80 + (10 * (ageInt - 3));
            else this.Height = 196;

            this.Height /= 2;

            this.RenderSize = new Vector2i((int)Math.Min(0.195 * (this.Height + 32), 25.0) / 3, (int)(this.Height * 0.975));
            this.RenderPositionOffset = new Vector2i(0 - ((this.RenderSize.X) / 2), 0 - this.RenderSize.Y);

            // Events
            if (prevHeight != this.Height)
            {
                EventManager.EnqueueWorldPropertyChangeEvent(this.Id, nameof(this.Height), prevHeight, this.Height, this.mainTile.Row, this.ThingType);
                EventManager.RaiseEvent(EventType.Shadow, EventSubType.Updated, this);
            }
        }
    }
}
