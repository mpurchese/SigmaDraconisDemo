namespace SigmaDraconis.CheckList.Context
{
    using Draconis.Shared;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Config;
    using Shared;
    using World;
    using World.Projects;
    using WorldControllers;
    using WorldInterfaces;

    internal static class CheckListContext
    {
        internal static readonly Dictionary<int, int> DiceRolls = new Dictionary<int, int>();

        public static Dictionary<ThingType, List<ThingProxy>> ProxiesByThingType = new Dictionary<ThingType, List<ThingProxy>>();
        public static Dictionary<ItemType, int> ItemTypeCounts = new Dictionary<ItemType, int>();
        public static Dictionary<ThingType, int> StorageTypeCounts = new Dictionary<ThingType, int>() { { ThingType.Silo, 0 }, { ThingType.FoodStorage, 0 }, { ThingType.ItemsStorage, 0 } } ;
        public static HashSet<int> CompleteItemIds = new HashSet<int>();
        public static HashSet<int> CompleteProjectIds = new HashSet<int>();
        public static MothershipStatus MothershipStatus;
        public static bool IsBotanistWoken;
        public static bool IsGeologistWoken;
        public static bool HaveFoodFromFruit;
        public static bool HaveFoodFromCrops;
        public static ClimateType ClimateType;
        public static bool HaveBotanist;
        public static bool HaveGeologist;
        public static bool HavePumpIndoors;
        public static long FrameNumber;
        public static int RocketsLaunched;
        public static int ArrivedColonistCount;
        public static bool AllColonistsHaveOwnSleepPod;

        public static void Update()
        {
            try
            {
                foreach (var thingType in ProxiesByThingType.Keys.ToList())
                {
                    ProxiesByThingType[thingType] = World.GetThings(thingType).Select(t => new ThingProxy(t)).ToList();
                }

                if (World.ResourceNetwork != null)
                {
                    HaveFoodFromFruit = World.GetFoodCounts().Any(kv => kv.Value > 0 && CropDefinitionManager.GetDefinition(kv.Key).IsWildFruit);
                    HaveFoodFromCrops = World.GetFoodCounts().Any(kv => kv.Value > 0 && CropDefinitionManager.GetDefinition(kv.Key).IsCrop);
                    foreach (var itemType in ItemTypeCounts.Keys.ToList())
                    {
                        ItemTypeCounts[itemType] = World.ResourceNetwork.GetItemTotal(itemType);
                    }

                    StorageTypeCounts[ThingType.Silo] = World.ResourceNetwork.CountResources;
                    StorageTypeCounts[ThingType.FoodStorage] = World.ResourceNetwork.CountFood;
                    StorageTypeCounts[ThingType.ItemsStorage] = World.ResourceNetwork.CountItems;
                }

                var arrivedColonists = World.GetThings<IColonist>(ThingType.Colonist).Where(c => c.IsArrived && !c.IsDead).ToList();

                FrameNumber = World.WorldTime.FrameNumber;
                RocketsLaunched = (int)WorldStats.Get(WorldStatKeys.RocketsLaunched);
                CompleteItemIds = CheckListController.GetCompletedItemIds().ToHashSet();
                CompleteProjectIds = ProjectManager.GetCompletedProjects().ToHashSet();
                MothershipStatus = MothershipController.MothershipStatus;
                IsBotanistWoken = MothershipController.ArrivingColonistSkill == SkillType.Botanist || World.GetThings<IColonist>(ThingType.Colonist).Any(c => c.Skill == SkillType.Botanist);
                IsGeologistWoken = MothershipController.ArrivingColonistSkill == SkillType.Geologist || World.GetThings<IColonist>(ThingType.Colonist).Any(c => c.Skill == SkillType.Geologist);
                ClimateType = World.ClimateType;
                ArrivedColonistCount = arrivedColonists.Count;
                HaveBotanist = arrivedColonists.Any(c => c.Skill == SkillType.Botanist);
                HaveGeologist = arrivedColonists.Any(c => c.Skill == SkillType.Geologist);
                HavePumpIndoors = World.GetThings<IWaterProviderBuilding>(ThingType.WaterPump, ThingType.ShorePump)
                    .Any(p => p.MainTile.ThingsPrimary.Any(t => t.ThingType == ThingType.Roof)
                     || p.MainTile.AdjacentTiles4.SelectMany(t => t.ThingsPrimary).Any(t => t.ThingType == ThingType.DirectionalHeater));
                var sleepPodOwnerIds = World.GetThings<ISleepPod>(ThingType.SleepPod).Where(p => p.IsReady && p.OwnerID.HasValue).Select(p => p.OwnerID.Value).Distinct().ToList();
                AllColonistsHaveOwnSleepPod = arrivedColonists.All(c => sleepPodOwnerIds.Contains(c.Id));
            }
            catch (Exception ex)
            {
                try
                {
                    // Theoretical possibility of threading errors here, probably not important.
                    Logger.Instance.Log("CheckListContext", ex.Message);
                }
                catch { }
            }
        }
    }
}
