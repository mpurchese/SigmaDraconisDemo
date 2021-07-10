namespace SigmaDraconis.WorldControllers
{
    using System.Collections.Generic;
    using Config;
    using Language;
    using Shared;
    using World;
    using WorldInterfaces;

    public static class BuildingNameController
    {
        public static Dictionary<ThingType, int> LastNumbers = new Dictionary<ThingType, int>();
        public static Dictionary<int, string> Names = new Dictionary<int, string>();
        private static int currentLanguageId;

        public static void Init()
        {
            EventManager.Subscribe(EventType.Building, EventSubType.Added, delegate (object obj) { OnBuildingAdded(obj); });
        }

        private static void OnBuildingAdded(object obj)
        {
            GetName(obj as IThing);   // Causes name to be initialised
        }

        public static string GetName(int thingID)
        {
            if (LanguageManager.CurrentLanguageId != currentLanguageId)
            {
                Names.Clear();
                currentLanguageId = LanguageManager.CurrentLanguageId;
            }

            if (Names.ContainsKey(thingID)) return Names[thingID];

            var thing = World.GetThing(thingID);
            return GetName(thing);
        }

        public static string GetName(IThing thing)
        {
            if (LanguageManager.CurrentLanguageId != currentLanguageId)
            {
                Names.Clear();
                currentLanguageId = LanguageManager.CurrentLanguageId;
            }

            var def = ThingTypeManager.GetDefinition(thing.ThingType);
            if (def == null) return "";
            if (def.IsNameable)
            {
                if (Names.ContainsKey(thing.Id)) return Names[thing.Id];

                if (!LastNumbers.ContainsKey(thing.ThingType)) LastNumbers.Add(thing.ThingType, 0);
                var number = LastNumbers[thing.ThingType] + 1;
                var name = $"{thing.DisplayName} {number}";
                LastNumbers[thing.ThingType] = number;
                Names.Add(thing.Id, name);
                return name;
            }

            return thing.DisplayName;
        }
    }
}
