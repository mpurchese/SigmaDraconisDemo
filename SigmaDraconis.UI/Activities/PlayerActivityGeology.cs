namespace SigmaDraconis.UI
{
    using System.Linq;
    using Language;
    using Shared;
    using WorldControllers;
    using WorldInterfaces;

    public static class PlayerActivityGeology
    {
        private static bool canDoAction;

        public static void HandleLeftClick()
        {
            Update();
            if (canDoAction || (MouseWorldPosition.Tile != null && GeologyController.TilesToSurvey.Contains(MouseWorldPosition.Tile.Index)))
            {
                GeologyController.Toggle(MouseWorldPosition.Tile);
            }
        }

        public static void Update()
        {
            canDoAction = false;
            for (int i = 0; i < 5; i++) MouseCursor.Instance.TextLine[i] = "";
            var tile = MouseWorldPosition.Tile;

            if (tile == null || tile.TerrainType != TerrainType.Dirt) return;

            if (tile.IsMineResourceVisible)
            {
                MouseCursor.Instance.TextLine[0] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.TileAlreadySurveyed);
                MouseCursor.Instance.TextLineColour[0] = UIColour.RedText;
                return;
            }

            if (tile.ThingsAll.Any(t => !(t is IWall) && !(t is IMoveableThing) && t.TileBlockModel != TileBlockModel.None))
            {
                MouseCursor.Instance.TextLine[0] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.TileNotEmpty);
                MouseCursor.Instance.TextLineColour[0] = UIColour.RedText;
                return;
            }

            if (tile.AdjacentTiles8.All(t => !t.CanWalk))
            {
                MouseCursor.Instance.TextLine[1] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.LocationNotAccessible);
                MouseCursor.Instance.TextLineColour[1] = UIColour.OrangeText;
            }

            canDoAction = true;
        }
    }
}
