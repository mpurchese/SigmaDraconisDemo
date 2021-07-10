namespace SigmaDraconis.WorldControllers
{
    using System.Collections.Generic;
    using Shared;
    using WorldInterfaces;

    public static class GeologyController
    {
        public static HashSet<int> TilesToSurvey = new HashSet<int>();

        public static void Clear()
        {
            TilesToSurvey.Clear();
        }

        public static void Toggle(ISmallTile tile)
        {
            var tileIndex = tile.Index;
            if (!TilesToSurvey.Contains(tileIndex)) TilesToSurvey.Add(tileIndex);
            else TilesToSurvey.Remove(tileIndex);

            EventManager.EnqueueWorldPropertyChangeEvent(tileIndex, "TilesToSurvey", tile.Row);
        }
    }
}
