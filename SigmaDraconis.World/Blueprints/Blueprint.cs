namespace SigmaDraconis.World.Blueprints
{
    using Microsoft.Xna.Framework;
    using ProtoBuf;
    using Draconis.Shared;
    using Config;
    using Shared;
    using WorldInterfaces;
    using System.Collections.Generic;
    using System.Linq;

    [ProtoContract]
    public class Blueprint : Thing, IBlueprint
    {
        [ProtoMember(101)]
        public float ColourR { get; set; }

        [ProtoMember(102)]
        public float ColourG { get; set; }

        [ProtoMember(103)]
        public float ColourB { get; set; } = 1.0f;

        [ProtoMember(104)]
        public float ColourA { get; set; } = 0.2f;

        [ProtoMember(105)]
        public int AnimationFrame { get; set; } = 1;

        [ProtoMember(106)]
        public ThingType SerializedThingType
        {
            get
            {
                return this.ThingType;
            }
            set
            {
                this.ThingType = value;
            }
        }

        [ProtoMember(107)]
        public int? ThingId { get; set; }

        [ProtoMember(108)]
        public bool CanBuild { get; set; }

        [ProtoMember(109)]
        public string CanBuildReason { get; set; }

        [ProtoMember(110)]
        public Direction Direction { get; set; }

        [ProtoMember(111)]
        public Vector2i RenderPositionOffset { get; set; }

        [ProtoMember(112)]
        protected Dictionary<int, int> colonistsByAccessTile;

        [ProtoMember(113)]
        public WorkPriority BuildPriority { get; set; }

        public bool RequiresAccessNow => World.ConfirmedBlueprints.ContainsKey(this.Id) && this.IsReadyToBuild();

        public Blueprint() : base(ThingType.None)
        {
        }

        public Blueprint(ThingType thingType, ISmallTile mainTile, int size, bool canBuild)
            : base(thingType, mainTile, size, false)
        {
            this.ColourR = 0.5f;
            this.ColourB = 0.0f;
            this.ColourA = canBuild ? 1f : 0.4f;
            this.CanBuild = canBuild;
            this.CanBuildReason = "";
            this.BuildPriority = WorkPriority.High;
            this.colonistsByAccessTile = new Dictionary<int, int>();
        }

        public Blueprint(ThingType thingType, ISmallTile mainTile, float colourR, float colourG, float colourB, int size = 1)
            : base(thingType, mainTile, size, false)
        {
            this.ColourR = colourR;
            this.ColourG = colourG;
            this.ColourB = colourB;
            this.CanBuild = true;
            this.CanBuildReason = "";
            this.BuildPriority = WorkPriority.High;
            this.colonistsByAccessTile = new Dictionary<int, int>();
        }

        public void ChangeType(ThingType thingType)
        {
            this.ThingType = thingType;
            this.definition = ThingTypeManager.GetDefinition(this.ThingType, false);
        }

        public override string GetTextureName(int layer = 1)
        {
            switch (this.ThingType)
            {
                case ThingType.EnvironmentControl when layer == 2: return "EnvironmentControlScreen_1";
                case ThingType.FuelFactory when layer != 1: return $"FuelFactoryPipes_{this.Direction.ToString()}";
                case ThingType.RockSmall: return $"RockSmall_{this.Direction.ToString()}";
                case ThingType.RockLarge: return $"RockLarge_{this.Direction.ToString()}";
                case ThingType.WaterPump: return $"WaterPump_{this.Direction.ToString()}";
                case ThingType.ShorePump: return $"ShorePump_{this.Direction.ToString()}";
                case ThingType.CompostFactory: return $"CompostFactory_{this.Direction.ToString()}";
                case ThingType.KekFactory: return $"KekFactory_{this.Direction.ToString()}";
                case ThingType.SolarCellFactory: return $"SolarCellFactory_{this.Direction.ToString()}";
                case ThingType.CompositesFactory: return $"CompositesFactory_{this.Direction.ToString()}";
                case ThingType.GlassFactory: return $"GlassFactory_{this.Direction.ToString()}";
                case ThingType.FoodStorage: return "FoodStorage";
                case ThingType.ItemsStorage: return "ItemsStorage";
                case ThingType.WaterStorage: return "WaterStorage";
            }

            if (this.Definition?.CanRotate == true || this.ThingType == ThingType.SmallPlant4) return $"{this.ThingType.ToString()}_{this.AnimationFrame}_{this.Direction.ToString()}";
            return $"{this.ThingType.ToString()}_{this.AnimationFrame}";
        }

        public IEnumerable<ISmallTile> GetAccessTiles(int? colonistId = null)
        {
            this.CleanupColonistAssignments();
            if (colonistId.HasValue && this.colonistsByAccessTile.Any(c => c.Value != colonistId)) yield break;
            else if (!colonistId.HasValue && this.colonistsByAccessTile.Any()) yield break;
            if (!this.IsReadyToBuild()) yield break;

            if (this.ThingType == ThingType.Roof)
            {
                yield return this.mainTile;
            }
            else if (this.ThingType.In(ThingType.Wall, ThingType.Door))
            {
                yield return this.mainTile;
                var tile2 = this.mainTile.GetTileToDirection(this.Direction);
                if (this.CanBuildFromTile(tile2, colonistId)) yield return tile2;
                var tile3 = this.mainTile.GetTileToDirection(Direction.S);
                if (this.CanBuildFromTile(tile3, colonistId)) yield return tile3;
                var tile4 = this.mainTile.GetTileToDirection(this.Direction == Direction.SE ? Direction.SW : Direction.SE);
                if (this.CanBuildFromTile(tile4, colonistId)) yield return tile4;
                var tile5 = this.mainTile.GetTileToDirection(this.Direction == Direction.SE ? Direction.E : Direction.W);
                if (this.CanBuildFromTile(tile5, colonistId)) yield return tile5;
                var tile6 = this.mainTile.GetTileToDirection(this.Direction == Direction.SE ? Direction.NE : Direction.NW);
                if (this.CanBuildFromTile(tile6, colonistId)) yield return tile6;
            }
            else
            {
                var done = new HashSet<int>();
                foreach (var tile in this.AllTiles)
                {
                    for (int i = 0; i <= 7; i++)
                    {
                        var direction = (Direction)i;
                        if (tile.HasWallToDirection(direction)) continue;   // Can't work here
                        var t = tile.GetTileToDirection(direction);
                        if (t == null || done.Contains(t.Index) || this.allTiles.Contains(t)) continue;
                        if (!this.CanBuildFromTile(t, colonistId)) continue;
                        if (direction == Direction.N && (this.MainTile.HasWallToDirection(Direction.NW) && this.MainTile.HasWallToDirection(Direction.NE))) continue;
                        if (direction == Direction.E && (this.MainTile.HasWallToDirection(Direction.NE) && this.MainTile.HasWallToDirection(Direction.SE))) continue;
                        if (direction == Direction.S && (this.MainTile.HasWallToDirection(Direction.SE) && this.MainTile.HasWallToDirection(Direction.SW))) continue;
                        done.Add(t.Index);
                        yield return t;
                    }
                }
            }
        }

        private bool CanBuildFromTile(ISmallTile tile, int? colonistId)
        {
            if (tile == null) return false;
            if (tile.ThingsPrimary.Any(a => a is IColonist c && (colonistId == null || c.Id != colonistId) && !c.IsMoving && !c.IsRelaxing)) return false;   // Blocked by another colonist
            if (!tile.CanPickupFromTile) return false;   // Can't work here
            return true;
        }

        public IEnumerable<ISmallTile> GetAllAccessTiles()
        {
            if (this.ThingType == ThingType.Roof)
            {
                yield return this.mainTile;
            }
            else if (this.ThingType.In(ThingType.Wall, ThingType.Door))
            {
                yield return this.mainTile;
                var tile2 = this.mainTile.GetTileToDirection(this.Direction);
                if (this.CanBuildFromTile(tile2, null)) yield return tile2;
                var tile3 = this.mainTile.GetTileToDirection(Direction.S);
                if (this.CanBuildFromTile(tile3, null)) yield return tile3;
                var tile4 = this.mainTile.GetTileToDirection(this.Direction == Direction.SE ? Direction.SW : Direction.SE);
                if (this.CanBuildFromTile(tile4, null)) yield return tile4;
                var tile5 = this.mainTile.GetTileToDirection(this.Direction == Direction.SE ? Direction.E : Direction.W);
                if (this.CanBuildFromTile(tile5, null)) yield return tile5;
                var tile6 = this.mainTile.GetTileToDirection(this.Direction == Direction.SE ? Direction.NE : Direction.NW);
                if (this.CanBuildFromTile(tile6, null)) yield return tile6;
            }
            else
            {
                var done = new HashSet<int>();
                foreach (var t in this.AllTiles)
                {
                    for (int i = 0; i <= 7; i++)
                    {
                        var direction = (Direction)i;
                        var tile = t.GetTileToDirection(direction);
                        if (tile == null || done.Contains(t.Index) || !tile.CanWorkInTile || this.MainTile.HasWallToDirection(direction)) continue;   // Can't work here
                        if (direction == Direction.N && (this.MainTile.HasWallToDirection(Direction.NW) && this.MainTile.HasWallToDirection(Direction.NE))) continue;
                        if (direction == Direction.E && (this.MainTile.HasWallToDirection(Direction.NE) && this.MainTile.HasWallToDirection(Direction.SE))) continue;
                        if (direction == Direction.S && (this.MainTile.HasWallToDirection(Direction.SE) && this.MainTile.HasWallToDirection(Direction.SW))) continue;
                        if (direction == Direction.W && (this.MainTile.HasWallToDirection(Direction.SW) && this.MainTile.HasWallToDirection(Direction.NW))) continue;
                        done.Add(tile.Index);
                        yield return tile;
                    }
                }
            }
        }

        public bool CanAssignColonist(int colonistId, int? tileIndex = null)
        {
            this.CleanupColonistAssignments();
            if (this.colonistsByAccessTile.ContainsValue(colonistId)) return true;
            if (this.colonistsByAccessTile.Any()) return false;
            if (!this.IsReadyToBuild()) return false;

            return tileIndex.HasValue
                ? this.GetAccessTiles(colonistId).Any(t => t.Index == tileIndex.Value)
                : this.GetAccessTiles(colonistId).Any();
        }

        public void AssignColonist(int colonistId, int tileIndex)
        {
            if (!this.colonistsByAccessTile.ContainsKey(tileIndex)) this.colonistsByAccessTile.Add(tileIndex, colonistId);
            else this.colonistsByAccessTile[tileIndex] = colonistId;
        }

        // Ready to build when any required conduits and foundations have been built
        protected virtual bool IsReadyToBuild()
        {
            var tile = this.MainTile;

            if (this.ThingType == ThingType.RocketGantry)
            {
                return tile.ThingsPrimary.OfType<IBuildableThing>().Any(t => t.ThingType == ThingType.LaunchPad && t.IsReady == true);
            }

            if (this.ThingType == ThingType.Rocket)
            {
                return tile.ThingsPrimary.OfType<IBuildableThing>().Any(t => t.ThingType == ThingType.RocketGantry && t.IsReady == true);
            }

            // Make sure we have a conduit node that is complete.
            var hasConduitNode = TileHasAccessToCompletedConduitNode(this.mainTile);
            if (!hasConduitNode && (this.ThingType == ThingType.Wall || this.ThingType == ThingType.Door))
            {
                // Walls/doors can connect from either side.
                hasConduitNode = TileHasAccessToCompletedConduitNode(this.mainTile.GetTileToDirection(this.Direction));
            }
            else if (!hasConduitNode && (this.ThingType == ThingType.FuelFactory))
            {
                // Fuel factory main tile may be coast - check other tiles
                hasConduitNode = this.AllTiles.All(t => t.TerrainType == TerrainType.Coast || TileHasAccessToCompletedConduitNode(t));
            }

            if (!hasConduitNode) return false;

            var definition = ThingTypeManager.GetDefinition(this.ThingType);
            if (tile == null || definition == null) return false;

            // Walls and doors need foundation on one side only
            if (this.ThingType.In(ThingType.Wall, ThingType.Door))
            {
                var tile2 = tile.GetTileToDirection(this.Direction);
                if (tile2 == null) return false;

                var hasFoundation = tile.ThingsAll.Any(t => t.ThingType.IsFoundation() && (t as IBuildableThing)?.IsReady == true)
                    || tile2.ThingsAll.Any(t => t.ThingType.IsFoundation() && (t as IBuildableThing)?.IsReady == true);

                // Egdes of algae pool also count as foundation
                if (!hasFoundation && tile.ThingsAll.FirstOrDefault(t => t.ThingType == ThingType.AlgaePool) is IAlgaePool ap && tile2.ThingsAll.All(t => t.ThingType != ThingType.AlgaePool || t != ap)) hasFoundation = true;
                if (!hasFoundation && tile2.ThingsAll.FirstOrDefault(t => t.ThingType == ThingType.AlgaePool) is IAlgaePool ap2 && tile.ThingsAll.All(t => t.ThingType != ThingType.AlgaePool || t != ap2)) hasFoundation = true;

                return hasFoundation;
            }

            // Don't build roof if any wall blueprints are waiting to be built
            if (this.ThingType == ThingType.Roof)
            {
                // Get connected roofs
                var openNodes = new List<IBlueprint> { this };
                var connectedRoofTileIndexes = new HashSet<int> { this.MainTileIndex };
                var allRoofBlueprints = World.ConfirmedBlueprints.Values.Where(b => b.ThingType == ThingType.Roof).ToList();
                while (openNodes.Any())
                {
                    var list = openNodes.ToList();
                    openNodes.Clear();
                    foreach (var n in list)
                    {
                        foreach (var b in allRoofBlueprints)
                        {
                            if (!connectedRoofTileIndexes.Contains(b.MainTileIndex))
                            {
                                if ((n.MainTile.TileToNE == b.MainTile && b.MainTile.ThingsPrimary.OfType<IWall>().All(w => w.Direction != Direction.SW))
                                    || (n.MainTile.TileToNW == b.MainTile && b.MainTile.ThingsPrimary.OfType<IWall>().All(w => w.Direction != Direction.SE))
                                    || (n.MainTile.TileToSE == b.MainTile && n.MainTile.ThingsPrimary.OfType<IWall>().All(w => w.Direction != Direction.SE))
                                    || (n.MainTile.TileToSW == b.MainTile && n.MainTile.ThingsPrimary.OfType<IWall>().All(w => w.Direction != Direction.SW)))
                                {
                                    openNodes.Add(b);
                                    connectedRoofTileIndexes.Add(b.MainTileIndex);
                                }
                            }
                        }
                    }
                }

                // Return false if any wall blueprints connect to any of our roof tiles
                var allWallBlueprints = World.ConfirmedBlueprints.Values.Where(b => b.ThingType == ThingType.Wall || b.ThingType == ThingType.Door);
                return !allWallBlueprints.Any(b => connectedRoofTileIndexes.Contains(b.MainTileIndex) || connectedRoofTileIndexes.Contains(b.MainTile.GetTileToDirection(b.Direction)?.Index ?? 0));
            }

            // General case, can't build unless we have required conduits and foundations
            var range = (definition.Size.X - 1) / 2;
            var o = (definition.Size.X - 1) % 2;
            var foundations = 0;
            for (int x = tile.X - range; x <= tile.X + range + o; x++)
            {
                for (int y = tile.Y - range; y <= tile.Y + range + o; y++)
                {
                    var t2 = World.GetSmallTile(x, y);
                    if (t2 == null) return false;

                    if (t2.ThingsPrimary.OfType<IBuildableThing>().Any(t => t.IsReady && t.ThingType.IsFoundation()))
                    {
                        foundations++;
                    }
                }
            }

            return foundations >= definition.FoundationsRequired;
        }

        private static bool TileHasAccessToCompletedConduitNode(ISmallTile tile)
        {
            foreach (var t2 in tile.AdjacentTiles8.SelectMany(x => x.AdjacentTiles4).SelectMany(x => x.AdjacentTiles4).Distinct())
            {
                if (t2.ThingsAll.OfType<IConduitNode>().Any(t3 => t3.IsReady && !t3.IsDesignatedForRecycling)) return true;
            }

            return false;
        }

        private void CleanupColonistAssignments()
        {
            if (this.colonistsByAccessTile == null) this.colonistsByAccessTile = new Dictionary<int, int>();
            foreach (var id in this.colonistsByAccessTile.Keys.ToList())
            {
                if (World.GetThing(this.colonistsByAccessTile[id]) is IColonist c && c.ActivityType.In(ColonistActivityType.Construct, ColonistActivityType.Deconstruct) && !c.IsDead) continue;
                this.colonistsByAccessTile.Remove(id);
            }
        }
    }
}
