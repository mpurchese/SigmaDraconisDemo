namespace SigmaDraconis.World.Zones
{
    using System.Collections.Generic;
    using System.Linq;
    using Draconis.Shared;
    using Shared;
    using WorldInterfaces;

    public static class PathFinderBlockManager
    {
        private static readonly Dictionary<int, List<PathFinderBlock>> blocksByThing = new Dictionary<int, List<PathFinderBlock>>();
        private static readonly TwoKeyDictionary<int, Direction, int> colonistBlockCountsByTileDirection = new TwoKeyDictionary<int, Direction, int>();
        private static readonly TwoKeyDictionary<int, Direction, int> animalBlockCountsByTileDirection = new TwoKeyDictionary<int, Direction, int>();

        public static void Reset()
        {
            blocksByThing.Clear();
            colonistBlockCountsByTileDirection.Clear();
            animalBlockCountsByTileDirection.Clear();
        }

        public static void AddBlock(int thingId, ISmallTile tile, Direction direction, TileBlockType tileBlockType = TileBlockType.All, bool inboundOnly = false)
        {
            if (!blocksByThing.ContainsKey(thingId)) blocksByThing.Add(thingId, new List<PathFinderBlock>());

            if (!inboundOnly)
            {
                blocksByThing[thingId].Add(new PathFinderBlock(tile.Index, direction, tileBlockType));
                if (tileBlockType == TileBlockType.All || tileBlockType == TileBlockType.Colonist)
                {
                    if (colonistBlockCountsByTileDirection.ContainsKey(tile.Index, direction)) colonistBlockCountsByTileDirection[tile.Index, direction]++;
                    else colonistBlockCountsByTileDirection.Add(tile.Index, direction, 1);
                }

                if (tileBlockType == TileBlockType.All || tileBlockType == TileBlockType.Animal)
                {
                    if (animalBlockCountsByTileDirection.ContainsKey(tile.Index, direction)) animalBlockCountsByTileDirection[tile.Index, direction]++;
                    else animalBlockCountsByTileDirection.Add(tile.Index, direction, 1);
                }
            }

            var nextTile = tile.GetTileToDirection(direction);
            if (nextTile != null)
            {
                var reverse = DirectionHelper.Reverse(direction);
                blocksByThing[thingId].Add(new PathFinderBlock(nextTile.Index, reverse, tileBlockType));

                if (tileBlockType == TileBlockType.All || tileBlockType == TileBlockType.Colonist)
                {
                    if (colonistBlockCountsByTileDirection.ContainsKey(nextTile.Index, reverse)) colonistBlockCountsByTileDirection[nextTile.Index, reverse]++;
                    else colonistBlockCountsByTileDirection.Add(nextTile.Index, reverse, 1);
                }

                if (tileBlockType == TileBlockType.All || tileBlockType == TileBlockType.Animal)
                {
                    if (animalBlockCountsByTileDirection.ContainsKey(nextTile.Index, reverse)) animalBlockCountsByTileDirection[nextTile.Index, reverse]++;
                    else animalBlockCountsByTileDirection.Add(nextTile.Index, reverse, 1);
                }
            }
        }

        public static void RemoveBlocks(int thingId)
        {
            if (!blocksByThing.ContainsKey(thingId)) return;

            foreach (var block in blocksByThing[thingId])
            {
                if ((block.TileBlockType == TileBlockType.All || block.TileBlockType == TileBlockType.Colonist) && colonistBlockCountsByTileDirection.ContainsKey(block.TileIndex, block.Direction))
                {
                    colonistBlockCountsByTileDirection[block.TileIndex, block.Direction]--;
                }

                if ((block.TileBlockType == TileBlockType.All || block.TileBlockType == TileBlockType.Animal) && animalBlockCountsByTileDirection.ContainsKey(block.TileIndex, block.Direction))
                {
                    animalBlockCountsByTileDirection[block.TileIndex, block.Direction]--;
                }
            }

            blocksByThing.Remove(thingId);
        }

        public static void RemoveBlocks(int thingId, params Direction[] directions)
        {
            if (!blocksByThing.ContainsKey(thingId)) return;

            foreach (var block in blocksByThing[thingId].ToList())
            {
                if (directions.Contains(block.Direction) && (block.TileBlockType == TileBlockType.All || block.TileBlockType == TileBlockType.Colonist) && colonistBlockCountsByTileDirection.ContainsKey(block.TileIndex, block.Direction))
                {
                    colonistBlockCountsByTileDirection[block.TileIndex, block.Direction]--;
                    blocksByThing[thingId].Remove(block);
                }

                if (directions.Contains(block.Direction) && (block.TileBlockType == TileBlockType.All || block.TileBlockType == TileBlockType.Animal) && animalBlockCountsByTileDirection.ContainsKey(block.TileIndex, block.Direction))
                {
                    animalBlockCountsByTileDirection[block.TileIndex, block.Direction]--;
                    blocksByThing[thingId].Remove(block);
                }
            }
        }

        public static bool IsBlocked(int tileIndex, Direction direction, TileBlockType tileBlockType)
        {
            if (tileBlockType == TileBlockType.Colonist)
            {
                return colonistBlockCountsByTileDirection.ContainsKey(tileIndex, direction) && colonistBlockCountsByTileDirection[tileIndex, direction] > 0;
            }
            else if (tileBlockType == TileBlockType.Animal)
            {
                return animalBlockCountsByTileDirection.ContainsKey(tileIndex, direction) && animalBlockCountsByTileDirection[tileIndex, direction] > 0;
            }

            return false;
        }
    }
}
