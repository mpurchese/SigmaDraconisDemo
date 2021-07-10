namespace SigmaDraconis.Shared
{
    public class TileHighlight
    {
        public TileHighlight(int tile, bool isPulsing, int alpha = 255)
        {
            this.Tile = tile;
            this.Alpha = alpha;
            this.IsPulsing = isPulsing;
        }

        public int Tile { get; set; }
        
        public int Alpha { get; set; }
        public bool IsPulsing { get; set; }
    }
}
