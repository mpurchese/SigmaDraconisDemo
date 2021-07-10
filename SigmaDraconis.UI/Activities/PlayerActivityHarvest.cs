namespace SigmaDraconis.UI
{
    using System.Linq;
    using Language;
    using Shared;
    using World;
    using WorldInterfaces;

    public static class PlayerActivityHarvest
    {
        private static bool canDoAction;

        public static void HandleLeftClick()
        {
            Update();
            var thing = GameScreen.Instance.HighlightedThing;
            if (!(thing is IFruitPlant plant) || plant.CountFruitAvailable == 0 || !canDoAction) return;
            if (plant.HarvestFruitPriority == WorkPriority.Disabled)
            {
                plant.SetHarvestFruitPriority(WorkPriority.Normal);
                if (plant.RecyclePriority != WorkPriority.Disabled)
                {
                    plant.RecyclePriority = WorkPriority.Disabled;
                    if (World.ResourcesForDeconstruction.ContainsKey(plant.Id)) World.ResourcesForDeconstruction.Remove(plant.Id);
                    EventManager.RaiseEvent(EventType.ResourcesForDeconstruction, EventSubType.Removed, plant);  // For AI
                }
            }
            else plant.SetHarvestFruitPriority(WorkPriority.Disabled);
        }

        public static void Update()
        {
            canDoAction = false;
            for (int i = 0; i < 5; i++) MouseCursor.Instance.TextLine[i] = "";

            if (!(GameScreen.Instance.HighlightedThing is IFruitPlant plant)) return;

            var fruitCount = plant.CountFruitAvailable;
            if (fruitCount == 0) return;

            if (!World.CanHarvestFruit)
            {
                MouseCursor.Instance.TextLine[0] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.HarvestRequiresBotanistAndCooker);
                MouseCursor.Instance.TextLineColour[0] = UIColour.RedText;
                return;
            }

            canDoAction = true;
            MouseCursor.Instance.TextLine[0] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.HarvestYieldFruit, fruitCount);
            MouseCursor.Instance.TextLineColour[0] = World.ClimateType == ClimateType.Snow ? UIColour.DarkGreenText : UIColour.GreenText;

            if (!plant.GetAllAccessTiles().Any())
            {
                MouseCursor.Instance.TextLine[1] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.LocationNotAccessible);
                MouseCursor.Instance.TextLineColour[1] = UIColour.OrangeText;
            }
        }
    }
}
