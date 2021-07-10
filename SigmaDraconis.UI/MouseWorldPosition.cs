namespace SigmaDraconis.UI
{
    using Draconis.Shared;
    using Draconis.UI;
    using Shared;
    using World;
    using WorldInterfaces;

    public static class MouseWorldPosition
    {
        public static ISmallTile Tile { get; set; } = null;
        public static Direction ClosestEdge { get; set; } = Direction.None;
        public static bool IsEdge { get; set; } = false;
        public static Vector2f TerrainPosition { get; set; } = Vector2f.Zero;

        /// <summary>
        /// Returns true if position changed
        /// </summary>
        public static bool Update(Vector2f scrollPosition, float zoom)
        {
            var worldPos = CoordinateHelper.GetWorldPosition(UIStatics.Graphics, scrollPosition, zoom, UIStatics.CurrentMouseState.X, UIStatics.CurrentMouseState.Y);
            var mx = (int)worldPos.X;
            var my = (int)worldPos.Y;

            var x = (mx + 32 - (2 * my)) / 64f;
            var y = (mx / 32f) - x;

            var tx = 3f * x;
            var ty = 3f * y;

            var xmod = tx % 1;
            var ymod = ty % 1;
            var direction = Direction.SE;
            if (xmod > ymod) direction = xmod > 1 - ymod ? Direction.NE : Direction.NW;
            else if (xmod < 1 - ymod) direction = Direction.SW;

            IsEdge = xmod < 0.2f || ymod < 0.2f || xmod > 0.8f || ymod > 0.8f;

            TerrainPosition = new Vector2f(tx - 0.5f, ty - 0.5f);

            var newTile = World.GetSmallTile((int)tx, (int)ty);
            if (newTile != Tile || direction != ClosestEdge)// || corner != Corner)
            {
                Tile = newTile;
                ClosestEdge = direction;
                return true;
            }

            return false;
        }
    }
}
