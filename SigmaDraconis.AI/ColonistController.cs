namespace SigmaDraconis.AI
{
    using Draconis.Shared;
    using System.Collections.Generic;
    using System.Linq;
    using Shared;
    using World;
    using WorldInterfaces;

    public static class ColonistController
    {
        private static bool isInitialised = false;

        public static Dictionary<int, ColonistAI> AIs = new Dictionary<int, ColonistAI>();

        public static void Update()
        {
            if (!isInitialised)
            {
                EventManager.Subscribe(EventType.ResourcesForDeconstruction, EventSubType.Added, delegate (object obj) { OnJobAdded(obj); });
                EventManager.Subscribe(EventType.PlantsForHarvest, EventSubType.Added, delegate (object obj) { OnJobAdded(obj); });
                isInitialised = true;
            }

            var colonists = World.GetThings<IColonist>(ThingType.Colonist).ToList();
            foreach (var id in AIs.Keys.ToList())
            {
                if (colonists.All(c => c.Id != id)) AIs.Remove(id);
            }

            foreach (var colonist in colonists)
            {
                if (!AIs.ContainsKey(colonist.Id)) AIs.Add(colonist.Id, new ColonistAI(colonist));
                var ai = AIs[colonist.Id];
                ai.Update();
                colonist.Update();
                if (ai.CurrentActivity?.CurrentAction is ActionWalk w && w.StuckTileIndex.HasValue)
                {
                    // Mechanism for avoiding tiles that we can't get into for some reason
                    if (ai.TilesToAvoidWithTimeouts.ContainsKey(w.StuckTileIndex.Value)) ai.TilesToAvoidWithTimeouts[w.StuckTileIndex.Value] = 60;
                    else ai.TilesToAvoidWithTimeouts.Add(w.StuckTileIndex.Value, 60);
                }
            }
        }

        // Called when there is a new resource to deconstruct or a new plant to harvet
        private static void OnJobAdded(object obj)
        {
            if (!(obj is IThing thing)) return;

            // Look for closest idle colonist, and make them rechoose their activity
            ColonistAI closest = null;
            var closestDistance = 999f;
            foreach (var ai in AIs.Values.Where(a => a.Colonist.ActivityType.In(ColonistActivityType.Roam, ColonistActivityType.None)))
            {
                var distance = (ai.Colonist.MainTile.TerrainPosition - thing.MainTile.TerrainPosition).Length();
                if (distance < closestDistance)
                {
                    closest = ai;
                    closestDistance = distance;
                }
            }

            if (closest != null)
            {
                closest.Rechoose = true;
            }
        }
    }
}
