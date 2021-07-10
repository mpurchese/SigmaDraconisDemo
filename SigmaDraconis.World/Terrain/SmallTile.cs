namespace SigmaDraconis.World.Terrain
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Draconis.Shared;
    using Shared;
    using Zones;
    using WorldInterfaces;

    public class SmallTile : ISmallTile
    {
        private IMineTileResource mineResource;

        public IBigTile BigTile { get; set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Row { get; private set; }
        public int Index { get; private set; }

        public List<IThing> ThingsPrimary { get; } = new List<IThing>();  // Things for which this is the main tile
        public List<IThing> ThingsAll { get; } = new List<IThing>();

        public BiomeType BiomeType { get; set; }

        // Ground cover
        public int GroundCoverDensity { get; set; }
        public int GroundCoverMaxDensity { get; set; }
        public Direction GroundCoverDirection { get; set; } = (Direction)Rand.Next(4) + 4;

        public ISmallTile TileToN { get; private set; }
        public ISmallTile TileToNE { get; private set; }
        public ISmallTile TileToE { get; private set; }
        public ISmallTile TileToSE { get; private set; }
        public ISmallTile TileToS { get; private set; }
        public ISmallTile TileToSW { get; private set; }
        public ISmallTile TileToW { get; private set; }
        public ISmallTile TileToNW { get; private set; }

        public int WindModifier { get; set; } = 0;

        public Vector2f CentrePosition { get; private set; }

        public TerrainType TerrainType { get; set; }
        public Vector2i TerrainPosition { get { return new Vector2i(this.X, this.Y); } }

        public bool IsCorridor { get; private set; }

        public List<ISmallTile> AdjacentTiles4
        {
            get
            {
                var result = new List<ISmallTile>(4);
                if (this.TileToNE != null) result.Add(this.TileToNE);
                if (this.TileToSE != null) result.Add(this.TileToSE);
                if (this.TileToSW != null) result.Add(this.TileToSW);
                if (this.TileToNW != null) result.Add(this.TileToNW);
                return result;
            }
        }

        public List<ISmallTile> AdjacentTiles8
        {
            get
            {
                var result = new List<ISmallTile>(8);
                if (this.TileToNE != null) result.Add(this.TileToNE);
                if (this.TileToSE != null) result.Add(this.TileToSE);
                if (this.TileToSW != null) result.Add(this.TileToSW);
                if (this.TileToNW != null) result.Add(this.TileToNW);
                if (this.TileToN != null) result.Add(this.TileToN);
                if (this.TileToE != null) result.Add(this.TileToE);
                if (this.TileToS != null) result.Add(this.TileToS);
                if (this.TileToW != null) result.Add(this.TileToW);
                return result;
            }
        }

        public Dictionary<int, HeatOrLightSource> LightSources { get; private set; } = new Dictionary<int, HeatOrLightSource>();
        public Dictionary<int, HeatOrLightSource> HeatSources { get; private set; } = new Dictionary<int, HeatOrLightSource>();

        public bool CanWalk => this.TerrainType == TerrainType.Dirt && this.ThingsAll.All(t => t.CanWalk);
        public bool CanWorkInTile
            => this.TerrainType == TerrainType.Dirt 
            && this.ThingsAll.All(t => t.Definition == null || (t is IResourceStack s && s.ItemCount == 0) || t.Definition.TileBlockModel.In(TileBlockModel.None, TileBlockModel.Door, TileBlockModel.Wall));
        public bool CanPickupFromTile
            => this.TerrainType == TerrainType.Dirt
            && this.ThingsAll.All(t => t.Definition == null || t.ThingType == ThingType.SleepPod || (t is IResourceStack s && s.ItemCount == 0) || t.Definition.TileBlockModel.In(TileBlockModel.None, TileBlockModel.Door, TileBlockModel.Wall, TileBlockModel.Point));

        #region Properties: Resources for Mining

        public bool IsMineResourceVisible => this.mineResource?.IsVisible == true;
        public int MineResourceCount => this.mineResource != null && this.mineResource.Type != ItemType.None ? this.mineResource.Count : 0;
        public MineResourceDensity MineResourceDensity => this.mineResource != null && this.mineResource.Type != ItemType.None ? this.mineResource.Density : MineResourceDensity.None;
        public ItemType MineResourceType => this.mineResource != null ? this.mineResource.Type : ItemType.None;
        public double MineResourceExtrationProgress => this.mineResource != null ? this.mineResource.ExtractionProgress : 0;
        public double MineResourceSurveyProgress => this.mineResource != null ? this.mineResource.SurveyProgress : 0;
        public long OreScannerLstFrame { get; set; }
        public int? MineResourceMineId => this.mineResource?.MineId;
        public int? MineResourceSurveyReservedBy => this.mineResource?.ReservedBy;
        public long MineResourceSurveyReservedAt => this.mineResource?.ReservedAt ?? 0;

        #endregion Properties: Resources for Mining

        public ISmallTile GetTileToDirection(Direction direction)
        {
            if (direction == Direction.N) return this.TileToN;
            if (direction == Direction.NE) return this.TileToNE;
            if (direction == Direction.E) return this.TileToE;
            if (direction == Direction.SE) return this.TileToSE;
            if (direction == Direction.S) return this.TileToS;
            if (direction == Direction.SW) return this.TileToSW;
            if (direction == Direction.W) return this.TileToW;
            if (direction == Direction.NW) return this.TileToNW;
            return null;
        }

        public bool HasWallToDirection(Direction direction)
        {
            switch (direction)
            {
                case Direction.SE:
                case Direction.SW:
                    return this.ThingsPrimary.Any(t => t.ThingType == ThingType.Wall && (t as IRotatableThing)?.Direction == direction);
                case Direction.NE:
                case Direction.NW:
                    var tile = this.GetTileToDirection(direction);
                    var reverse = DirectionHelper.Reverse(direction);
                    return tile?.ThingsPrimary?.Any(t => t.ThingType == ThingType.Wall && (t as IRotatableThing)?.Direction == reverse) == true;
                case Direction.E:
                    var ne = this.GetTileToDirection(Direction.NE);
                    if (ne == null || ne.ThingsPrimary.All(t => t.ThingType != ThingType.Wall)) return false;
                    if (this.HasWallToDirection(Direction.SE)) return true;
                    var e = this.GetTileToDirection(Direction.E);
                    return e.HasWallToDirection(Direction.SW) == true;
                case Direction.S:
                    if (this.HasWallToDirection(Direction.SE))
                    {
                        if (this.HasWallToDirection(Direction.SW)) return true;
                        var sw = this.GetTileToDirection(Direction.SW);
                        if (sw?.HasWallToDirection(Direction.SE) == true) return true;
                    }
                    else if (this.HasWallToDirection(Direction.SW))
                    {
                        var se = this.GetTileToDirection(Direction.SE);
                        if (se?.HasWallToDirection(Direction.SW) == true) return true;
                    }
                    return false;
                case Direction.W:
                    var nw = this.GetTileToDirection(Direction.NW);
                    if (nw == null || nw.ThingsPrimary.All(t => t.ThingType != ThingType.Wall)) return false;
                    if (this.HasWallToDirection(Direction.SW)) return true;
                    var w = this.GetTileToDirection(Direction.W);
                    return w.HasWallToDirection(Direction.SW) == true;
                case Direction.N:
                    var n = this.GetTileToDirection(Direction.N);
                    if (n == null) return false;
                    if (n.HasWallToDirection(Direction.SE))
                    {
                        if (n.HasWallToDirection(Direction.SW)) return true;
                        var nw1 = this.GetTileToDirection(Direction.NW);
                        if (nw1?.HasWallToDirection(Direction.SE) == true) return true;
                    }
                    else if (n.HasWallToDirection(Direction.SW))
                    {
                        var ne1 = this.GetTileToDirection(Direction.NE);
                        if (ne1?.HasWallToDirection(Direction.SW) == true) return true;
                    }
                    return false;
            }

            return false;
        }

        public bool HasWallOrDoorToDirection(Direction direction)
        {
            switch (direction)
            {
                case Direction.SE:
                case Direction.SW:
                    return this.ThingsPrimary.Any(t => t is IWall && (t as IRotatableThing)?.Direction == direction);
                case Direction.NE:
                case Direction.NW:
                    var tile = this.GetTileToDirection(direction);
                    var reverse = DirectionHelper.Reverse(direction);
                    return tile?.ThingsPrimary?.Any(t => t is IWall && (t as IRotatableThing)?.Direction == reverse) == true;
            }

            return false;
        }

        public SmallTile(BigTile parent, int index, int x, int y, int row)
        {
            this.BigTile = parent;
            this.X = x;
            this.Y = y;
            this.Row = row;
            this.Index = index;

            var cx = (10.6666667 * this.X) + (10.6666667 * this.Y);
            var cy = (5.3333333 * this.Y) - (5.3333333 * this.X);
            this.CentrePosition = new Vector2f((float)cx + 10.667f, (float)cy + 16f);

            this.LightSources = new Dictionary<int, HeatOrLightSource>();
            this.HeatSources = new Dictionary<int, HeatOrLightSource>();
            this.mineResource = new MineTileResource();
        }

        public IMineTileResource GetResources()
        {
            return this.mineResource != null && (this.mineResource.Type != ItemType.None || this.mineResource.IsVisible) ? this.mineResource.Clone() : null;
        }

        public void SetResources(IMineTileResource resource)
        {
            this.mineResource = resource;
        }

        public void SetResourceExtractionProgress(double progress)
        {
            this.mineResource.ExtractionProgress = progress;
        }

        public void ReserveForResourceSurvey(int colonistId)
        {
            this.mineResource.ReservedBy = colonistId;
            this.mineResource.ReservedAt = World.WorldTime.FrameNumber;
        }

        public bool InrementResourceSurveyProgress(double progress)
        {
            if (this.mineResource.IsVisible) return true;

            this.mineResource.SurveyProgress += progress;
            EventManager.EnqueueWorldPropertyChangeEvent(this.Index, nameof(this.MineResourceSurveyProgress), this.Row);
            if (this.mineResource.SurveyProgress >= 1.0)
            {
                this.mineResource.SurveyProgress = 1.0;
                this.mineResource.IsVisible = true;
                EventManager.EnqueueWorldPropertyChangeEvent(this.Index, nameof(this.IsMineResourceVisible), this.Row);
                return true;
            }

            return false;
        }

        public void RemoveResource(int count = 1)
        {
            if (this.mineResource.Count > 0)
            {
                this.mineResource.Count -= Math.Min(this.mineResource.Count, count);
                this.mineResource.ExtractionProgress = 0;
                if (this.mineResource.Count == 0)
                {
                    this.mineResource.Density = MineResourceDensity.None;
                    this.mineResource.Type = ItemType.None;
                }

                EventManager.EnqueueWorldPropertyChangeEvent(this.Index, nameof(this.MineResourceCount));
            }
        }

        public void SetIsMineResourceVisible(bool value = true, bool raiseEvent = true)
        {
            if (this.IsMineResourceVisible == value) return;

            if (this.mineResource == null) this.mineResource = new MineTileResource();
            this.mineResource.IsVisible = value;

            if (raiseEvent) EventManager.EnqueueWorldPropertyChangeEvent(this.Index, nameof(this.IsMineResourceVisible));
        }

        public void SetMineResourceMineId(int? mineId)
        {
            if (this.mineResource == null) this.mineResource = new MineTileResource();
            this.mineResource.MineId = mineId;
        }

        public void RemoveThing(IThing thing)
        {
            if (this.ThingsAll.Contains(thing)) this.ThingsAll.Remove(thing);
            if (this.ThingsPrimary.Contains(thing)) this.ThingsPrimary.Remove(thing);

            if (thing.Definition != null && (thing.Definition.TileBlockModel != TileBlockModel.None || !thing.CanWalk)) this.UpdatePathFinderNode();
        }

        public void LinkTiles(ISmallTile n, ISmallTile ne, ISmallTile e, ISmallTile se, ISmallTile s, ISmallTile sw, ISmallTile w, ISmallTile nw)
        {
            this.TileToN = n;
            this.TileToNE = ne;
            this.TileToE = e;
            this.TileToSE = se;
            this.TileToS = s;
            this.TileToSW = sw;
            this.TileToW = w;
            this.TileToNW = nw;
        }

        public void UpdatePathFinderNode()
        {
            ZoneManager.UpdateNode(this.Index);
        }

        public bool UpdateIsCorridor()
        {
            var newValue = false;
            if (ZoneManager.Loading || !ZoneManager.HomeZone.ContainsNode(this.Index) || !this.CanWalk)
            {
                this.IsCorridor = newValue;
                return newValue;
            }

            if (this.ThingsPrimary.Any(t => t.ThingType == ThingType.SleepPod))
            {
                this.IsCorridor = true;
                return true;
            }

            var node = ZoneManager.HomeZone.Nodes[this.Index];
            if (this.AdjacentTiles8.SelectMany(t => t.ThingsAll).Any(t => t is IColonistInteractive c && c.RequiresAccessNow && c.GetAllAccessTiles().Contains(this))) newValue = true;
            else
            {
                // Corridor if not all adjacent nodes are not connected to each other
                var adjacentTiles = new List<int>();
                for (int i = 0; i < 8; i++)
                {
                    var t = this.GetTileToDirection((Direction)i);
                    if (t == null) continue;
                    if (node.GetLink((Direction)i) != null) adjacentTiles.Add(t.Index); 
                }

                if (adjacentTiles.Any() && adjacentTiles.Count < 8)
                {
                    // Find a gap
                    var d1 = Direction.N;
                    for (int i = 0; i < 8; i++)
                    {
                        var t = this.GetTileToDirection(d1);
                        if (t == null || !adjacentTiles.Contains(t.Index)) break;
                        d1 = DirectionHelper.Clockwise45(d1);
                    }

                    // Going clockwise, find the next link
                    d1 = DirectionHelper.Clockwise45(d1);
                    for (int i = 0; i < 8; i++)
                    {
                        var t = this.GetTileToDirection(d1);
                        if (t != null && adjacentTiles.Contains(t.Index)) break;
                        d1 = DirectionHelper.Clockwise45(d1);
                    }

                    // Continue going clockwise until we find another gap
                    d1 = DirectionHelper.Clockwise45(d1);
                    var connections = 1;
                    for (int i = 0; i < 8; i++)
                    {
                        var t = this.GetTileToDirection(d1);
                        if (t == null) break;

                        if (!adjacentTiles.Contains(t.Index))
                        {
                            // Check for corner cuts
                            var t0 = this.GetTileToDirection(DirectionHelper.AntiClockwise45(d1));
                            if (t0 != null && ZoneManager.HomeZone.ContainsNode(t0.Index) && ZoneManager.HomeZone.Nodes[t0.Index].GetLink(DirectionHelper.Clockwise90(d1)) != null)
                            {
                                d1 = DirectionHelper.Clockwise45(d1);
                                continue;
                            }

                            break;
                        }

                        d1 = DirectionHelper.Clockwise45(d1);
                        connections++;
                    }

                    // If we didn't account for all adjacent tiles, then we are in a corridor
                    if (connections < adjacentTiles.Count) newValue = true;
                }
            }

            this.IsCorridor = newValue;
            return newValue;
        }

        public override string ToString()
        {
            return $"Tile {this.Index} at ({this.X}, {this.Y})";
        }
    }
}
