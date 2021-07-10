namespace SigmaDraconis.World.Zones
{
    using Shared;

    public class PathFinderBlock
    {
        public PathFinderBlock(int tileIndex, Direction direction, TileBlockType tileBlockType)
        {
            this.TileIndex = tileIndex;
            this.Direction = direction;
            this.TileBlockType = tileBlockType;
        }

        public int TileIndex { get; set; }
        public Direction Direction { get; set; }
        public TileBlockType TileBlockType { get; set; }
    }
}
