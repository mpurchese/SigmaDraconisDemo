namespace SigmaDraconis.World.Zones
{
    using Draconis.Shared;
    using System.Linq;
    using Config;
    using Shared;
    using WorldInterfaces;

    public static class ZoneManager
    {
        public static PathFinderZone GlobalZone { get; set; } = new PathFinderZone(TileBlockType.Colonist);
        public static PathFinderZone HomeZone { get; set; } = new PathFinderZone(TileBlockType.Colonist);
        public static PathFinderZone WaterZone { get; set; } = new PathFinderZone(TileBlockType.Colonist);
        public static PathFinderZone DeepWaterZone { get; set; } = new PathFinderZone(TileBlockType.Colonist);
        public static PathFinderZone AnimalZone { get; set; } = new PathFinderZone(TileBlockType.Animal);

        public static bool Loading { get; set; }

        public static void Init()
        {
            EventManager.Subscribe(EventType.Thing, EventSubType.Added, delegate (object obj) { OnThingAdded(obj); });
            EventManager.Subscribe(EventType.Thing, EventSubType.Removed, delegate (object obj) { OnThingRemoved(obj); });
            EventManager.Subscribe(EventType.ResourceStack, EventSubType.Updated, delegate (object obj) { OnResourceStackUpdated(obj); });
            EventManager.Subscribe(EventType.Door, EventSubType.Updated, delegate (object obj) { OnDoorStateChanged(obj); });
        }

        public static void UpdateNode(int tileIndex)
        {
            GlobalZone.UpdateNode(tileIndex);
            HomeZone.UpdateNode(tileIndex);
            AnimalZone.UpdateNode(tileIndex);
            EventManager.RaiseEvent(EventType.Zone, null);
        }

        public static void BuildGlobalZone()
        {
            GlobalZone.Nodes.Clear();
            AnimalZone.Nodes.Clear();
            foreach (var tile in World.SmallTiles.Where(t => t.TerrainType == TerrainType.Dirt))
            {
                GlobalZone.AddNode(tile.Index);
                AnimalZone.AddNode(tile.Index);
            }

            WaterZone.Nodes.Clear();
            DeepWaterZone.Nodes.Clear();
            foreach (var tile in World.SmallTiles.Where(t => t.TerrainType.In(TerrainType.Water, TerrainType.DeepWaterEdge, TerrainType.DeepWater)))
            {
                WaterZone.AddNode(tile.Index);
                if (tile.TerrainType == TerrainType.DeepWater) DeepWaterZone.AddNode(tile.Index);
            }
        }

        private static void OnThingAdded(object sender)
        {
            if (!(sender is IThing thing)) return;

            if (thing.ThingType.IsFoundation())
            {
                // Update nodes when foundation is added
                for (int i = 0; i < 8; i++)
                {
                    var direction = (Direction)i;
                    var adjacentTile = thing.MainTile.GetTileToDirection(direction);
                    if (adjacentTile != null)
                    {
                        if (GlobalZone.ContainsNode(adjacentTile.Index)) GlobalZone.UpdateNode(adjacentTile.Index);
                        if (HomeZone.ContainsNode(adjacentTile.Index)) HomeZone.UpdateNode(adjacentTile.Index);
                    }
                }
            }
            else if (thing.Definition != null && (thing.TileBlockModel != TileBlockModel.None || !thing.CanWalk))
            {
                var tileBlockModel = thing.TileBlockModel;
                foreach (var tile in thing.AllTiles)
                {
                    if ((tileBlockModel == TileBlockModel.Door || tileBlockModel == TileBlockModel.Wall) && tile != thing.MainTile) continue;

                    if (tileBlockModel == TileBlockModel.Circle || tileBlockModel == TileBlockModel.SmallCircle || tileBlockModel == TileBlockModel.Point || thing.ThingType == ThingType.LaunchPad)
                    {
                        // Point blocks only stop animal movement
                        var blockType = tileBlockModel == TileBlockModel.Point ? TileBlockType.Animal : TileBlockType.All;

                        // Special case: Animals (actually only bugs) can walk through bushes
                        if (thing.ThingType == ThingType.Bush || thing.ThingType == ThingType.SmallPlant4 || thing.ThingType == ThingType.BigSpineBush) blockType = TileBlockType.Colonist;

                        for (int i = 0; i < 8; i++)
                        {
                            PathFinderBlockManager.AddBlock(thing.Id, tile, (Direction)i, blockType, true);
                        }
                    }
                    else if (tileBlockModel == TileBlockModel.Square || tileBlockModel == TileBlockModel.Pod)
                    {
                        var accessDirection = Direction.None;
                        if (tileBlockModel == TileBlockModel.Pod && thing is IRotatableThing r) accessDirection = r.Direction;

                        // Block entry to tile, and prevent corner cutting
                        for (int i = 0; i < 8; i++)
                        {
                            var d = (Direction)i;
                            if (d != accessDirection) PathFinderBlockManager.AddBlock(thing.Id, tile, (Direction)i);
                        }

                        var nw = tile.TileToNW;
                        var ne = tile.TileToNE;
                        var se = tile.TileToSE;
                        var sw = tile.TileToSW;
                        if (nw != null && !thing.AllTiles.Contains(nw))
                        {
                            PathFinderBlockManager.AddBlock(thing.Id, nw, Direction.E);
                            if (GlobalZone.ContainsNode(nw.Index)) GlobalZone.UpdateNode(nw.Index);
                            if (HomeZone.ContainsNode(nw.Index)) HomeZone.UpdateNode(nw.Index);
                            if (AnimalZone.ContainsNode(nw.Index)) AnimalZone.UpdateNode(nw.Index);
                        }

                        if (ne != null && !thing.AllTiles.Contains(ne))
                        {
                            PathFinderBlockManager.AddBlock(thing.Id, ne, Direction.S);
                            if (GlobalZone.ContainsNode(ne.Index)) GlobalZone.UpdateNode(ne.Index);
                            if (HomeZone.ContainsNode(ne.Index)) HomeZone.UpdateNode(ne.Index);
                            if (AnimalZone.ContainsNode(ne.Index)) AnimalZone.UpdateNode(ne.Index);
                        }

                        if (se != null && !thing.AllTiles.Contains(se))
                        {
                            PathFinderBlockManager.AddBlock(thing.Id, se, Direction.W);
                            if (GlobalZone.ContainsNode(se.Index)) GlobalZone.UpdateNode(se.Index);
                            if (HomeZone.ContainsNode(se.Index)) HomeZone.UpdateNode(se.Index);
                            if (AnimalZone.ContainsNode(se.Index)) AnimalZone.UpdateNode(se.Index);
                        }

                        if (sw != null && !thing.AllTiles.Contains(sw))
                        {
                            PathFinderBlockManager.AddBlock(thing.Id, sw, Direction.N);
                            if (GlobalZone.ContainsNode(sw.Index)) GlobalZone.UpdateNode(sw.Index);
                            if (HomeZone.ContainsNode(sw.Index)) HomeZone.UpdateNode(sw.Index);
                            if (AnimalZone.ContainsNode(sw.Index)) AnimalZone.UpdateNode(sw.Index);
                        }
                    }
                    else if (tileBlockModel == TileBlockModel.Wall || tileBlockModel == TileBlockModel.Door)
                    {
                        // Block edge (wall only) and corners in the wall or door direction
                        var wallDirection = (thing as IRotatableThing).Direction;

                        // Only colonists can go through doors
                        PathFinderBlockManager.AddBlock(thing.Id, tile, wallDirection, tileBlockModel == TileBlockModel.Wall ? TileBlockType.All : TileBlockType.Animal);
                        PathFinderBlockManager.AddBlock(thing.Id, tile, DirectionHelper.AntiClockwise45(wallDirection));
                        PathFinderBlockManager.AddBlock(thing.Id, tile, DirectionHelper.Clockwise45(wallDirection));

                        var tileL = tile.GetTileToDirection(DirectionHelper.AntiClockwise90(wallDirection));
                        var tileR = tile.GetTileToDirection(DirectionHelper.Clockwise90(wallDirection));
                        if (tileL != null)
                        {
                            PathFinderBlockManager.AddBlock(thing.Id, tileL, DirectionHelper.Clockwise45(wallDirection));
                            if (GlobalZone.ContainsNode(tileL.Index)) GlobalZone.UpdateNode(tileL.Index);
                            if (HomeZone.ContainsNode(tileL.Index)) HomeZone.UpdateNode(tileL.Index);
                            if (AnimalZone.ContainsNode(tileL.Index)) AnimalZone.UpdateNode(tileL.Index);
                        }

                        if (tileR != null)
                        {
                            PathFinderBlockManager.AddBlock(thing.Id, tileR, DirectionHelper.AntiClockwise45(wallDirection));
                            if (GlobalZone.ContainsNode(tileR.Index)) GlobalZone.UpdateNode(tileR.Index);
                            if (HomeZone.ContainsNode(tileR.Index)) HomeZone.UpdateNode(tileR.Index);
                            if (AnimalZone.ContainsNode(tileR.Index)) AnimalZone.UpdateNode(tileR.Index);
                        }
                    }

                    if (GlobalZone.ContainsNode(tile.Index)) GlobalZone.UpdateNode(tile.Index);
                    if (HomeZone.ContainsNode(tile.Index)) HomeZone.UpdateNode(tile.Index);
                    if (AnimalZone.ContainsNode(tile.Index)) AnimalZone.UpdateNode(tile.Index);
                }
            }

            if (!(sender is IBuildableThing building)) return;

            if (!Loading)
            {
                // Extend the home zone when a building is added
                var minX = building.AllTiles.Select(t => t.TerrainPosition.X - 10).Min();
                var maxX = building.AllTiles.Select(t => t.TerrainPosition.X + 10).Max();
                var minY = building.AllTiles.Select(t => t.TerrainPosition.Y - 10).Min();
                var maxY = building.AllTiles.Select(t => t.TerrainPosition.Y + 10).Max();

                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        var tile = World.GetSmallTile(x, y);
                        if (tile != null && tile.TerrainType == TerrainType.Dirt && !HomeZone.ContainsNode(tile.Index))
                        {
                            var node = HomeZone.AddNode(tile.Index);
                        }
                    }
                }
            }

            EventManager.RaiseEvent(EventType.Zone, null);
        }

        private static void OnThingRemoved(object sender)
        {
            if (!(sender is IThing thing)) return;

            PathFinderBlockManager.RemoveBlocks(thing.Id);

            foreach (var tile in thing.AllTiles)
            {
                if (GlobalZone.ContainsNode(tile.Index)) GlobalZone.UpdateNode(tile.Index);
                if (HomeZone.ContainsNode(tile.Index)) HomeZone.UpdateNode(tile.Index);
            }

            if (thing.ThingType.IsFoundation())
            {
                // Update nodes when foundation is removed
                for (int i = 0; i < 8; i++)
                {
                    var direction = (Direction)i;
                    var adjacentTile = thing.MainTile.GetTileToDirection(direction);
                    if (adjacentTile != null)
                    {
                        if (GlobalZone.ContainsNode(adjacentTile.Index)) GlobalZone.UpdateNode(adjacentTile.Index);
                        if (HomeZone.ContainsNode(adjacentTile.Index)) HomeZone.UpdateNode(adjacentTile.Index);
                    }
                }
            }

            EventManager.RaiseEvent(EventType.Zone, null);
        }

        private static void OnResourceStackUpdated(object sender)
        {
            if (!(sender is IThing thing)) return;

            PathFinderBlockManager.RemoveBlocks(thing.Id);
            OnThingAdded(sender);
        }

        private static void OnDoorStateChanged(object sender)
        {
            if (!(sender is IThing thing)) return;

            PathFinderBlockManager.RemoveBlocks(thing.Id);
            OnThingAdded(sender);
        }
    }
}
