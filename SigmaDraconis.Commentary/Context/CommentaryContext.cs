namespace SigmaDraconis.Commentary.Context
{
    using System.Collections.Generic;
    using System.Linq;
    using Shared;
    using World;
    using WorldControllers;
    using WorldInterfaces;

    internal static class CommentaryContext
    {
        internal static readonly Dictionary<int, int> DiceRolls = new Dictionary<int, int>();

        public static long FrameNumber;
        public static List<ColonistProxy> LiveColonists = new List<ColonistProxy>();
        public static List<ColonistProxy> DeadColonists = new List<ColonistProxy>();
        public static HashSet<int> ColonistsWithComments = new HashSet<int>();
        public static Dictionary<ThingType, List<ThingProxy>> ProxiesByThingType = new Dictionary<ThingType, List<ThingProxy>>();
        public static Dictionary<string, long> Stats = new Dictionary<string, long>();
        public static Dictionary<ItemType, int> ItemTypeCounts = new Dictionary<ItemType, int>();
        public static Dictionary<int, int> AvailableSleepPodsByColonistId = new Dictionary<int, int>();
        public static float WorldLightBrightness;
        public static int WorldTemperature;
        public static float NetworkEnergyTot;
        public static float NetworkEnergyGenNet;
        public static int FoodFreeSpace;
        public static int StorageFreeSpace;
        public static bool AnyPlantsForHarvest;
        public static ClimateType ClimateType;

        // Mothership
        public static int TimeUntilCanWake;
        public static int TimeUntilArrive;
        public static SkillType ArrivingSkill;
        public static string ArrivingName;

        public static void Reset()
        {
            DiceRolls.Clear();
            FrameNumber = 0;
            LiveColonists.Clear();
            DeadColonists.Clear();
            ColonistsWithComments.Clear();
            AvailableSleepPodsByColonistId.Clear();
            TimeUntilCanWake = 0;
            TimeUntilArrive = 0;
            WorldLightBrightness = 1f;
            StorageFreeSpace = 0;
            FoodFreeSpace = 0;
            AnyPlantsForHarvest = false;
        }

        public static void Update()
        {
            DiceRolls.Clear();
            LiveColonists.Clear();
            DeadColonists.Clear();
            foreach (var c in World.GetThings<IColonist>(ThingType.Colonist).Where(c => c.IsArrived && !c.IsDead))
            {
                var proxy = new ColonistProxy(c);
                proxy.AdjacentThings.AddRange(c.MainTile.AdjacentTiles8.SelectMany(t => t.ThingsAll).Distinct().Select(t => new ThingProxy(t)));
                LiveColonists.Add(proxy);
            }

            foreach (var c in World.GetThings<IColonist>(ThingType.Colonist).Where(c => c.IsDead))
            {
                DeadColonists.Add(new ColonistProxy(c));
            }

            foreach (var thingType in ProxiesByThingType.Keys.ToList())
            {
                ProxiesByThingType[thingType] = World.GetThings(thingType).Select(t => new ThingProxy(t)).ToList();
            }

            foreach (var stat in Stats.Keys.ToList())
            {
                Stats[stat] = WorldStats.Get(stat);
            }

            if (World.ResourceNetwork != null)
            {
                foreach (var itemType in ItemTypeCounts.Keys.ToList())
                {
                    ItemTypeCounts[itemType] = World.ResourceNetwork.GetItemTotal(itemType);
                }
            }

            AvailableSleepPodsByColonistId.Clear();
            foreach (var colonistId in World.GetThings<IColonist>(ThingType.Colonist).Select(c => c.Id))
            {
                var count = World.GetThings<ISleepPod>(ThingType.SleepPod).Count(c => c.CanAssignColonist(colonistId));
                AvailableSleepPodsByColonistId.Add(colonistId, count);
            }

            FrameNumber = World.WorldTime.FrameNumber;
            WorldLightBrightness = World.WorldLight.Brightness;
            WorldTemperature = World.Temperature;
            ClimateType = World.ClimateType;
            AnyPlantsForHarvest = World.AnyPlantsForHarvest;

            var network = World.ResourceNetwork;
            if (network != null)
            {
                NetworkEnergyGenNet = (float)(network.EnergyGenTotal.KWh - network.EnergyUseTotal.KWh);
                NetworkEnergyTot = (float)network.EnergyTotal.KWh;
                StorageFreeSpace = network.ResourcesCapacity - network.CountResources;
                FoodFreeSpace = network.FoodCapacity - network.CountFood;
            }

            // Mothership
            TimeUntilCanWake = MothershipController.TimeUntilCanWake;
            TimeUntilArrive = MothershipController.TimeToArrival;
            ArrivingName = MothershipController.ArrivingColonistName;
            ArrivingSkill = MothershipController.ArrivingColonistSkill;
        }

        public static int GetDiceRoll(int id)
        {
            if (!DiceRolls.ContainsKey(id)) DiceRolls.Add(id, Rand.Next(6) + 1);
            return DiceRolls[id];
        }
    }
}
