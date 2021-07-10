namespace SigmaDraconis.AI
{
    using Draconis.Shared;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using Cards.Interface;
    using Config;
    using Shared;
    using World;
    using World.PathFinding;
    using World.Rooms;
    using World.Zones;
    using WorldControllers;
    using WorldInterfaces;

    [ProtoContract]
    public class ColonistAI
    {
        private static readonly FastRandom random = new FastRandom();

        public IColonist Colonist { get; private set; }

        [ProtoMember(1)]
        private readonly int colonistId;

        [ProtoMember(2)]
        public ActivityBase CurrentActivity { get; private set; }

        [ProtoMember(3)]
        public Dictionary<int, int> TilesToAvoidWithTimeouts { get; private set; }

        [ProtoMember(4)]
        private int frame;

        private int minTempTolerance = -9;
        private static int? bestGeologyTile;
        private bool isLookingForFood;
        private bool isLookingForWater;
        private static readonly Dictionary<int, int> tileContextScoreCache = new Dictionary<int, int>();  // Optimisation

        public bool Rechoose { get; set; } = false;

        // Deserialisation ctor
        protected ColonistAI() { }

        public ColonistAI(IColonist colonist)
        {
            this.Colonist = colonist;
            this.colonistId = colonist.Id;
            this.TilesToAvoidWithTimeouts = new Dictionary<int, int>();
            this.minTempTolerance = -9 - this.Colonist.Cards.GetEffectsSum(CardEffectType.ColdTolerance);
        }

        [ProtoAfterDeserialization]
        public void AfterDeserialization()
        {
            this.Colonist = World.GetThing(this.colonistId) as IColonist;
            this.minTempTolerance = -9 - this.Colonist.Cards.GetEffectsSum(CardEffectType.ColdTolerance);
            if (this.TilesToAvoidWithTimeouts == null) this.TilesToAvoidWithTimeouts = new Dictionary<int, int>();
        }

        public void Update()
        {
            if (this.Colonist.IsDead || World.ResourceNetwork == null) return;

            this.frame++;

            // Disallowed activities
            var priorities = this.Colonist.WorkPriorities;
            if (priorities[ColonistPriority.FarmPlant] == 0 && this.CurrentActivity is ActivityFarmHarvest) this.Rechoose = true;
            else if (priorities[ColonistPriority.FarmHarvest] == 0 && this.CurrentActivity is ActivityHarvestFruit) this.Rechoose = true;
            else if (priorities[ColonistPriority.ResearchBotanist] == 0 && priorities[ColonistPriority.ResearchGeologist] == 0 && priorities[ColonistPriority.ResearchEngineer] == 0 && this.CurrentActivity is ActivityResearch) this.Rechoose = true;
            else if (priorities[ColonistPriority.Deconstruct] == 0 && this.CurrentActivity is ActivityDeconstruct) this.Rechoose = true;
            else if (priorities[ColonistPriority.Construct] == 0 && this.CurrentActivity is ActivityConstruct) this.Rechoose = true;
            else if (priorities[ColonistPriority.Maintain] == 0 && this.CurrentActivity is ActivityRepair) this.Rechoose = true;
            else if (priorities[ColonistPriority.Geology] == 0 && this.CurrentActivity is ActivityGeology) this.Rechoose = true;
            else if (frame % 60 == 0 && (this.Colonist.Body.Hydration < 1 || this.Colonist.Body.Nourishment < 1 || this.Colonist.Body.Temperature >= 30 || this.Colonist.Body.Temperature <= 10))// || this.CurrentActivity?.CurrentAction is ActionWalk))
            {
                this.Rechoose = true;    // Re-evaluate choice every minute if dying
            }
            else if (frame % 300 == 0 && this.CurrentActivity?.CurrentAction is ActionWalk && !(this.CurrentActivity is ActivityRoam))
            {
                this.Rechoose = true;    // Re-evaluate choice every 5 minutes if walking in case anything changed, but not roaming as we don't want to stop this too soon
            }
            else if ((this.CurrentActivity is ActivityRelax || (this.CurrentActivity is ActivityDrinkKek a && a.CurrentAction is ActionRelax)) && frame % 600 == 0)
            {
                this.Rechoose = true;   // Re-evaluate choice every 10 minutes if relaxing
            }
            else if (this.CurrentActivity is ActivityHaulFromStack ah && World.GetThing(ah.TargetID) is IResourceStack rs && (this.Colonist.WorkPriorities[ColonistPriority.Haul] == 0 || rs.HaulPriority == 0))
            {
                this.Rechoose = true;   // Disallowed hauling on target resource stack
            }
            else if (this.Colonist.TargetBuilingID.HasValue)
            {
                if (this.CurrentActivity is ActivityConstruct)
                {
                    if (!World.ConfirmedBlueprints.ContainsKey(this.Colonist.TargetBuilingID.Value)) this.Rechoose = true;  // Blueprint removed
                }
                else if (!(this.CurrentActivity is ActivityDeconstruct))  // For deconstruction we expect the target to be removed.  Anything else, it's a problem.
                {
                    var t = World.GetThing(this.Colonist.TargetBuilingID.Value);
                    if (t == null) this.Rechoose = true;   // Target removed
                    if (t is IBuildableThing b && !b.IsReady) this.Rechoose = true;   // Target not available
                }
            }

            if (!this.Rechoose && this.Colonist.GiveWayRequestTiles?.Any() == true) this.Rechoose = true;

            if ((this.CurrentActivity?.IsFinished != false && !this.Colonist.Body.IsSleeping) || this.Rechoose)
            {
                this.isLookingForWater = false;
                this.isLookingForFood = false;

                this.MakeChoice();

                if (!this.isLookingForFood && this.Colonist.LookingForFoodCounter > 0 && World.ResourceNetwork.CountFood > 0) this.Colonist.LookingForFoodCounter = 0;
                if (!this.isLookingForWater && this.Colonist.LookingForWaterCounter > 0 && World.ResourceNetwork.WaterLevel >= 100) this.Colonist.LookingForWaterCounter = 0;

                if (!(this.CurrentActivity is ActivityRelax) && !(this.CurrentActivity is ActivityDrinkKek)) this.Colonist.IsRelaxing = false;
                if (!(this.CurrentActivity is ActivityDrinkKek)) this.Colonist.DrinkingKekFrame = 0;
                if (!this.Colonist.IsIdle || this.Colonist.StressLevel >= StressLevel.High)
                {
                    WarningsController.Remove(WarningType.Idle, this.Colonist.ShortName);
                    WarningsController.Remove(WarningType.WaitingForCooker, this.Colonist.ShortName);
                }
            }

            if (this.CurrentActivity != null && !this.CurrentActivity.IsFinished) this.CurrentActivity.Update();

            foreach (var v in this.TilesToAvoidWithTimeouts.Keys.ToList())
            {
                this.TilesToAvoidWithTimeouts[v]--;
                if (this.TilesToAvoidWithTimeouts[v] <= 0)
                {
                    this.TilesToAvoidWithTimeouts.Remove(v);
                }
            }

            if (this.Colonist.GiveWayRequestTiles?.Any() == true) this.Colonist.GiveWayRequestTiles.Clear();
        }

        private void MakeChoice()
        {
            this.Rechoose = false;
            this.Colonist.IsIdle = false;
            this.Colonist.IsWaiting = false;

            if (!(this.CurrentActivity?.CurrentAction is ActionWalk))
            {
                this.Colonist.IsMoving = false;
                this.Colonist.CurrentSpeed = 0;
            }

            // Release any tile blocks
            PathFinderBlockManager.RemoveBlocks(this.Colonist.Id);
            ZoneManager.HomeZone.UpdateNode(this.Colonist.MainTileIndex);
            ZoneManager.GlobalZone.UpdateNode(this.Colonist.MainTileIndex);
            ZoneManager.AnimalZone.UpdateNode(this.Colonist.MainTileIndex);
            EventManager.RaiseEvent(EventType.Zone, null);

            if (this.IsInLandingPod())
            {
                if (!(this.CurrentActivity is ActivityLeaveLandingPod)) this.LeaveLandingPod();
                return;
            }

            if (!this.Colonist.IsWorking && !this.Colonist.IsResting && !this.Colonist.IsWaiting
                && this.Colonist.MainTile.ThingsAll.Any(t => t.ThingType != ThingType.SleepPod && t.ThingType != ThingType.LandingPod) && this.IsInClosedTile())
            {
                this.LeaveClosedTile();
                return;
            }

            var thirst = 0;
            if (this.Colonist.Cards.Contains(CardType.Thirst3)) thirst = 3;
            else if (this.Colonist.Cards.Contains(CardType.Thirst2)) thirst = 2;
            else if (this.Colonist.Cards.Contains(CardType.Thirst1)) thirst = 1;

            var hunger = 0;
            if (this.Colonist.Cards.Contains(CardType.Hunger3)) hunger = 3;
            else if (this.Colonist.Cards.Contains(CardType.Hunger2)) hunger = 2;
            else if (this.Colonist.Cards.Contains(CardType.Hunger1)) hunger = 1;

            // Won't walk more than 10 tiles outside if very cold
            HashSet<int> accessibleTiles = null;
            if (World.Temperature <= this.minTempTolerance) accessibleTiles = this.GetAccessibleTiles();

            // Dehydrated?
            if (thirst >= 3 && this.Colonist.Body.Hydration < 60 && this.TrySatisfyThirst(accessibleTiles)) return;

            // Starving?
            if (hunger >= 3 && this.Colonist.Body.Nourishment < 60 && this.TrySatisfyHunger(accessibleTiles)) return;

            // Already sleeping?
            if (this.CurrentActivity is ActivitySleep && !this.CurrentActivity.IsFinished) return;

            // Exhausted?
            if (this.Colonist.Body.Energy < 5 && this.TrySatisfyRestOrSleepAnywhere(true)) return;

            // Very thirsty?
            if (thirst == 2 && this.TrySatisfyThirst(accessibleTiles)) return;

            // Very hungry?
            if (hunger == 2 && this.TrySatisfyHunger(accessibleTiles)) return;

            // Very low health?
            //if ((this.Colonist.HealthLevel < 25.0 && this.Colonist.Body.Hydration >= 25 && this.Colonist.Body.Nourishment >= 25) && this.TrySatisfyRestOrSleepPodOnly(false)) return;

            // Freezing?
            if (this.Colonist.Body.TemperatureForecast <= 10 && this.TrySatisfyGetWarm()) return;
            if (this.CurrentActivity is ActivitySeekSafeTemperature && !this.CurrentActivity.IsFinished) return;

            // Generally don't stop deconstructing
            if (this.CurrentActivity is ActivityDeconstruct && !this.CurrentActivity.IsFinished) return;

            // Have kek?
            if (this.Colonist.CarriedItemTypeArms == ItemType.Kek && this.TryDrinkKek()) return;
            if (this.CurrentActivity is ActivityDrinkKek && !this.CurrentActivity.IsFinished && this.Colonist.DrinkingKekFrame > 0) return;

            // Carrying crops to cook?
            if (this.Colonist.CarriedItemTypeBack == ItemType.Crop && this.TryCook()) return;

            // Urgent work or something to drop off?
            var nextHaulJob = ResourceStackingController.GetJobForColonist(this.Colonist);
            if (this.Colonist.IsWillingToWork(true))
            {
                if (this.TryDoHaulJob(WorkPriority.Urgent, nextHaulJob)) return;
                if (this.TryWork(WorkPriority.Urgent, accessibleTiles)) return;
            }

            // Thirsty?
            if (this.Colonist.Body.Hydration < 60.0 && this.TrySatisfyThirst(accessibleTiles)) return;

            // Hungry?
            if (this.Colonist.Body.Nourishment < 60.0 && this.TrySatisfyHunger(accessibleTiles)) return;

            // Tired?
            if (this.Colonist.Body.Energy < 25 && this.TrySatisfyRestOrSleepPodOnly(true)) return;

            // Already resting?
            if (this.CurrentActivity is ActivityRest && !this.CurrentActivity.IsFinished && RoomManager.GetTileTemperature(this.Colonist.MainTileIndex).Between(0, 30)) return;

            // Already roaming?
            if (this.CurrentActivity is ActivityRoam && !this.CurrentActivity.IsFinished) return;

            // Already drinking kek?
            if (this.CurrentActivity is ActivityPickupKek && !this.CurrentActivity.IsFinished) return;

            // Refusing to work?
            if (this.Colonist.IsWillingToWork(false))
            {
                // High, medium, or low priority work?
                if (this.TryDoHaulJob(WorkPriority.High, nextHaulJob)) return;
                if (this.TryWork(WorkPriority.High, accessibleTiles)) return;
                if (this.TryDoHaulJob(WorkPriority.Normal, nextHaulJob)) return;
                if (this.TryWork(WorkPriority.Normal, accessibleTiles)) return;
                if (this.Colonist.Skill == SkillType.Geologist && this.Colonist.WorkPriorities[ColonistPriority.Geology] > 0 
                    && GeologyController.TilesToSurvey.Any() && this.TrySatisfyDoGeology(accessibleTiles, true, true)) return;
                if (this.TryDoHaulJob(WorkPriority.Low, nextHaulJob)) return;
                if (this.TryWork(WorkPriority.Low, accessibleTiles)) return;
                if (this.Colonist.Skill == SkillType.Geologist && this.Colonist.WorkPriorities[ColonistPriority.Geology] > 0 && this.TrySatisfyDoGeology(accessibleTiles, !GeologyController.TilesToSurvey.Any(), false)) return;
                if (!this.Colonist.IsIdle)
                {
                    this.Colonist.IsIdle = true;
                    if (this.Colonist.StressLevel < StressLevel.High)
                    {
                        if (this.Colonist.CarriedItemTypeBack == ItemType.Crop || this.Colonist.CarriedItemTypeBack == ItemType.Fruit)
                        {
                            WarningsController.Remove(WarningType.Idle, this.Colonist.ShortName);
                            WarningsController.Add(WarningType.WaitingForCooker, this.Colonist.ShortName);
                        }
                        else
                        {
                            WarningsController.Add(WarningType.Idle, this.Colonist.ShortName);
                            WarningsController.Remove(WarningType.WaitingForCooker, this.Colonist.ShortName);
                        }
                    }
                }
            }

            // Go and get some kek?
            if (this.Colonist.KekPolicy != KekPolicy.Never && World.GetThings<IKekDispenser>(ThingType.KekDispenser).Any(t => t.DispenserStatus != DispenserStatus.NoResource))
            {
                if ((this.Colonist.KekPolicy == KekPolicy.AnyTime && this.Colonist.StressDisplay > 0)
                    || (this.Colonist.KekPolicy == KekPolicy.Limited && this.Colonist.StressLevel >= StressLevel.High)
                    || (this.Colonist.KekPolicy == KekPolicy.Normal && this.Colonist.StressLevel >= StressLevel.Moderate))
                {
                    if (this.TrySatisfyGetKek(accessibleTiles)) return;
                }
            }
            
            // Go for a walk?
            if ((!this.Colonist.Cards.Contains(CardType.Roam) || this.Colonist.FramesSinceRoam > 3600 * 4)
                && World.Temperature >= 0 && World.WorldLight.Brightness > 0.1f && this.Colonist.Body.Hydration > 60 && this.Colonist.Body.Nourishment > 60
                && World.WorldTime.FrameNumber > 3600
                && this.TrySatisfyRoam())
            {
                return;
            }

            // Move somewhere suitable to wait?
            if (this.TrySatisfyRelax(accessibleTiles)) return;
            if (this.TrySatisfyReturnHome()) return;
            if (this.TrySatisfyMoveIdle()) return;
            
            this.Colonist.ActivityType = ColonistActivityType.None;
            this.CurrentActivity = new ActivityWait(this.Colonist);

            return;
        }

        private bool TryDoHaulJob(WorkPriority priority, ResourceStackingJob nextHaulJob)
        {
            if (nextHaulJob == null || nextHaulJob.Priority != priority) return false;

            if (this.Colonist.CarriedItemTypeBack == nextHaulJob.ItemType)
            {
                if (nextHaulJob.Target is IResourceStack stack)
                {
                    ResourceStackingController.ClaimJob(this.Colonist, nextHaulJob);
                    if (this.CurrentActivity is ActivityHaulToStack a && a.TargetID == stack.Id && !this.CurrentActivity.IsFinished) return true;
                    this.CurrentActivity = new ActivityHaulToStack(this.Colonist, stack, nextHaulJob.Path);
                    this.Colonist.ActivityType = ColonistActivityType.HaulDropoff;
                    return true;
                }
                else if (nextHaulJob.Target is IResourceProcessor processor)
                {
                    ResourceStackingController.ClaimJob(this.Colonist, nextHaulJob);
                    if (this.CurrentActivity is ActivityHaulToNetwork a && a.TargetID == processor.Id && !this.CurrentActivity.IsFinished) return true;
                    this.CurrentActivity = new ActivityHaulToNetwork(this.Colonist, processor, nextHaulJob.Path);
                    this.Colonist.ActivityType = ColonistActivityType.HaulDropoff;
                    return true;
                }
            }
            if (this.Colonist.CarriedItemTypeBack == ItemType.None)
            {
                if (nextHaulJob.Source is IResourceStack stack)
                {
                    ResourceStackingController.ClaimJob(this.Colonist, nextHaulJob);
                    if (this.CurrentActivity is ActivityHaulFromStack a && a.TargetID == stack.Id && !this.CurrentActivity.IsFinished) return true;
                    this.CurrentActivity = new ActivityHaulFromStack(this.Colonist, stack, nextHaulJob.Path);
                    this.Colonist.ActivityType = ColonistActivityType.HaulPickup;
                    return true;
                }
                else if (nextHaulJob.Source is IResourceProcessor processor)
                {
                    ResourceStackingController.ClaimJob(this.Colonist, nextHaulJob);
                    if (this.CurrentActivity is ActivityHaulFromNetwork a && a.TargetID == processor.Id && a.ItemType == nextHaulJob.ItemType && !this.CurrentActivity.IsFinished) return true;
                    this.CurrentActivity = new ActivityHaulFromNetwork(this.Colonist, processor, nextHaulJob.ItemType, nextHaulJob.Path);
                    this.Colonist.ActivityType = ColonistActivityType.HaulPickup;
                    return true;
                }
            }

            return false;
        }

        private bool TryWork(WorkPriority priority, HashSet<int> accessibleTiles)
        {
            this.Colonist.GetWorkRate();
            if (this.Colonist.WorkRate < 0.05) return false;

            // General working, excluding item drop off and cooking, which take higher priority.
            // We try and find something we can do in the current tile.  Failing this we look for something as nearby as possible.

            // Already working?  Carry on!
            if (this.CurrentActivity is ActivityConstruct && this.CurrentActivity.CurrentAction is ActionConstruct && !this.CurrentActivity.IsFinished) return true;
            if (this.CurrentActivity is ActivityFarmHarvest && this.CurrentActivity.CurrentAction is ActionFarmHarvest && !this.CurrentActivity.IsFinished) return true;
            if (this.CurrentActivity is ActivityFarmPlant && this.CurrentActivity.CurrentAction is ActionFarmPlant && !this.CurrentActivity.IsFinished) return true;
            if (this.CurrentActivity is ActivityHarvestFruit && this.CurrentActivity.CurrentAction is ActionHarvestFruit && !this.CurrentActivity.IsFinished) return true;
            if (this.CurrentActivity is ActivityDeconstruct && this.CurrentActivity.CurrentAction is ActionDeconstruct && !this.CurrentActivity.IsFinished) return true;
            if (this.CurrentActivity is ActivityResearch && this.CurrentActivity.CurrentAction is ActionResearch && !this.CurrentActivity.IsFinished) return true;
            if (this.CurrentActivity is ActivityRepair && this.CurrentActivity.CurrentAction is ActionRepair && !this.CurrentActivity.IsFinished) return true;
            if (this.CurrentActivity is ActivityGeology && this.CurrentActivity.CurrentAction is ActionGeology && !this.CurrentActivity.IsFinished) return true;

            var blueprints = new List<IBlueprint>();
            var plantersForHarvest = new List<IPlanter>();
            var plantersForPlanting = new List<IPlanter>();
            var fruitPlants = new List<IFruitPlant>();
            var mines = new List<IMineInteractive>();
            var labs = new List<ILab>();
            var deconstructs = new List<IColonistInteractive>();
            var repairs = new List<IRepairableThing>();

            // TODO: Find all tiles where we can work.
            // In the case of mines and labs, there may be a maximum distance in case another colonist is already on their way
            // If there is only one then find a path to it in the normal way, otherwise use expanding circle method.

            // Constructing
            if (this.Colonist.WorkPriorities[ColonistPriority.Construct] > 0)
            {
                blueprints = World.ConfirmedBlueprints.Values.OfType<IBlueprint>()
                    .Where(b => ActivityConstruct.CanColonistBuildBlueprint(b as IBlueprint))
                    .Where(d => d != null && d.BuildPriority == priority && d.CanAssignColonist(this.colonistId)).ToList();

                var targets = blueprints.Where(d => d.GetAccessTiles(this.colonistId).Any(t => t.Index == this.Colonist.MainTileIndex)).ToList();
                var target = targets.FirstOrDefault(t => t.ThingType.IsFoundation());
                if (target == null) target = targets.FirstOrDefault(t => t.ThingType != ThingType.Roof);
                if (target == null) target = targets.FirstOrDefault();
                if (target != null)
                {
                    if (this.CurrentActivity is ActivityConstruct a && a.TargetID == target.Id && !this.CurrentActivity.IsFinished) return true;   // Already doing this
                    this.CurrentActivity = new ActivityConstruct(this.Colonist, target);
                    this.Colonist.ActivityType = ColonistActivityType.Construct;
                    return true;
                }
            }

            // Repairing
            if (this.Colonist.WorkPriorities[ColonistPriority.Maintain] > 0)
            {
                var maxLevel = 0.2 * (int)priority;

                repairs = World.GetThings<IRepairableThing>(ThingTypeManager.RepairableThingTypes)
                    .Where(d => d.RepairPriority == priority && d.MaintenanceLevel < maxLevel)
                    .ToList();

                var repair = repairs.FirstOrDefault(d => d.CanAssignColonistForRepair(this.Colonist.Id, this.Colonist.MainTileIndex));
                if (repair != null)
                {
                    if (this.CurrentActivity is ActivityRepair ar && ar.TargetID == repair.Id && !this.CurrentActivity.IsFinished) return true;   // Already doing this

                    this.CurrentActivity = new ActivityRepair(this.Colonist, repair);
                    this.Colonist.ActivityType = ColonistActivityType.Repair;
                    return true;
                }
            }

            // Farming - harvest
            if (this.Colonist.WorkPriorities[ColonistPriority.FarmHarvest] > 0 && this.Colonist.Skill == SkillType.Botanist)
            {
                var isCookerAvailable = World.GetThings<ICooker>(ThingType.Cooker).Any(t => t.IsReadyToCook);
                var isKekFactoryAvailable = World.GetThings<ICooker>(ThingType.KekFactory).Any(t => t.IsReadyToCook);

                plantersForHarvest = World.GetPlanters().Where(d => this.CanHarvest(d, priority, isCookerAvailable, isKekFactoryAvailable)).ToList();

                var planter = plantersForHarvest.FirstOrDefault(d => d.CanAssignColonist(this.Colonist.Id, this.Colonist.MainTileIndex));
                if (planter != null)
                {
                    if (this.CurrentActivity is ActivityFarmHarvest af && af.PlanterId == planter.Id && !this.CurrentActivity.IsFinished) return true;   // Already doing this

                    this.CurrentActivity = new ActivityFarmHarvest(this.Colonist, planter);
                    this.Colonist.ActivityType = ColonistActivityType.Farm;
                    return true;
                }
            }

            // Farming - planting
            if (this.Colonist.WorkPriorities[ColonistPriority.FarmPlant] > 0 && this.Colonist.Skill == SkillType.Botanist)
            {
                plantersForPlanting = World.GetPlanters()
                    .Where(d => !plantersForHarvest.Contains(d) && d.FarmPriority == priority && d.CanAssignColonist(this.Colonist.Id)
                         && ((d.PlanterStatus == PlanterStatus.WaitingForSeeds)
                            || d.PlanterStatus == PlanterStatus.Dead
                            || d.RemoveCrop)).ToList();

                var planter = plantersForPlanting.FirstOrDefault(d => d.CanAssignColonist(this.Colonist.Id, this.Colonist.MainTileIndex));
                if (planter != null)
                {
                    if (this.CurrentActivity is ActivityFarmHarvest af && af.PlanterId == planter.Id && !this.CurrentActivity.IsFinished) return true;   // Already doing this

                    this.CurrentActivity = new ActivityFarmHarvest(this.Colonist, planter);
                    this.Colonist.ActivityType = ColonistActivityType.Farm;
                    return true;
                }
            }

            // Harvesting wild fruit
            if (this.Colonist.WorkPriorities[ColonistPriority.FruitHarvest] > 0 && this.Colonist.Skill == SkillType.Botanist 
                && World.ResourceNetwork.CountFood < World.ResourceNetwork.FoodCapacity)
            {
                var isCookerAvailable = World.GetThings<ICooker>(ThingType.Cooker).Any(t => t.IsReadyToCook);
                fruitPlants = World.GetThings<IFruitPlant>(ThingType.Bush, ThingType.SmallPlant5, ThingType.SmallPlant6, ThingType.SmallPlant9, ThingType.SmallPlant12)
                    .Where(d => d.CountFruitAvailable > 0 && d.HarvestFruitPriority == priority && d.CanAssignColonist(this.Colonist.Id) && isCookerAvailable && this.Colonist.CarriedItemTypeBack == ItemType.None).ToList();

                var plant = fruitPlants.FirstOrDefault(d => d.CanAssignColonist(this.Colonist.Id, this.Colonist.MainTileIndex));
                if (plant != null)
                {
                    if (this.CurrentActivity is ActivityHarvestFruit af && af.PlantId == plant.Id && !this.CurrentActivity.IsFinished) return true;   // Already doing this

                    this.CurrentActivity = new ActivityHarvestFruit(this.Colonist, plant);
                    this.Colonist.ActivityType = ColonistActivityType.Harvest;
                    return true;
                }
            }

            // Deconstructing
            if (this.Colonist.WorkPriorities[ColonistPriority.Deconstruct] > 0)
            {
                deconstructs = World.ResourcesForDeconstruction.Keys.Select(r => World.GetThing(r) as IColonistInteractive)
                    .Where(d => d != null && d.RecyclePriority == priority && d.CanAssignColonist(this.colonistId)).ToList();

                var target = deconstructs.FirstOrDefault(d => d.GetAccessTiles(this.colonistId).Any(t => t.Index == this.Colonist.MainTileIndex));
                if (target != null)
                {
                    if (this.CurrentActivity is ActivityDeconstruct a && a.TargetID == target.Id && !this.CurrentActivity.IsFinished) return true;   // Already doing this
                    this.CurrentActivity = new ActivityDeconstruct(this.Colonist, target);
                    this.Colonist.ActivityType = ColonistActivityType.Deconstruct;
                    return true;
                }
            }

            // Labs
            if (this.Colonist.WorkPriorities[ColonistPriority.ResearchBotanist] > 0)
            {
                labs.AddRange(World.GetThings<ILab>(ThingType.Biolab).Where(d => d.LabPriority == priority && d.LabStatus.In(LabStatus.WaitingForColonist, LabStatus.InProgress) && d.CanAssignColonist(this.Colonist.Id)));
            }

            if (this.Colonist.WorkPriorities[ColonistPriority.ResearchEngineer] > 0)
            {
                labs.AddRange(World.GetThings<ILab>(ThingType.MaterialsLab).Where(d => d.LabPriority == priority && d.LabStatus.In(LabStatus.WaitingForColonist, LabStatus.InProgress) && d.CanAssignColonist(this.Colonist.Id)));
            }

            if (this.Colonist.WorkPriorities[ColonistPriority.ResearchGeologist] > 0)
            {
                labs.AddRange(World.GetThings<ILab>(ThingType.GeologyLab).Where(d => d.LabPriority == priority && d.LabStatus.In(LabStatus.WaitingForColonist, LabStatus.InProgress) && d.CanAssignColonist(this.Colonist.Id)));
            }

            var lab = labs.FirstOrDefault(d => d.CanAssignColonist(this.Colonist.Id, this.Colonist.MainTileIndex));
            if (lab != null)
            {
                if (this.CurrentActivity is ActivityResearch ar && ar.LabId == lab.Id && !this.CurrentActivity.IsFinished) return true;   // Already doing this

                this.CurrentActivity = new ActivityResearch(this.Colonist, lab);
                this.Colonist.ActivityType = ColonistActivityType.Lab;

                return true;
            }

            var candidateTilesAndTargetsDeconstruct = new Dictionary<ISmallTile, List<IColonistInteractive>>();
            var candidateTilesAndTargetsHarvestFruit = new Dictionary<ISmallTile, List<IFruitPlant>>();
            var candidateTilesAndTargetsRepair = new Dictionary<ISmallTile, List<IRepairableThing>>();
            var candidateTilesAndTargetsGeneral = new Dictionary<ISmallTile, List<IColonistInteractive>>();

            foreach (var target in deconstructs)
            {
                foreach (var tile in target.GetAccessTiles(this.colonistId).Where(t => accessibleTiles == null || accessibleTiles.Contains(t.Index)))
                {
                    if (!candidateTilesAndTargetsDeconstruct.ContainsKey(tile)) candidateTilesAndTargetsDeconstruct.Add(tile, new List<IColonistInteractive>());
                    candidateTilesAndTargetsDeconstruct[tile].Add(target);
                }
            }

            foreach (var target in fruitPlants)
            {
                foreach (var tile in target.GetAccessTiles(this.colonistId).Where(t => accessibleTiles == null || accessibleTiles.Contains(t.Index)))
                {
                    if (!tile.AdjacentTiles8.Any(t => t.ThingsAll.Contains(target))) continue;
                    if (!candidateTilesAndTargetsHarvestFruit.ContainsKey(tile)) candidateTilesAndTargetsHarvestFruit.Add(tile, new List<IFruitPlant>());
                    candidateTilesAndTargetsHarvestFruit[tile].Add(target);
                }
            }

            foreach (var target in repairs)
            {
                foreach (var tile in target.GetAccessTilesForRepair(this.colonistId).Where(t => accessibleTiles == null || accessibleTiles.Contains(t.Index)))
                {
                    if (!candidateTilesAndTargetsRepair.ContainsKey(tile)) candidateTilesAndTargetsRepair.Add(tile, new List<IRepairableThing>());
                    candidateTilesAndTargetsRepair[tile].Add(target);
                }
            }

            foreach (var target in plantersForHarvest.OfType<IColonistInteractive>().Concat(plantersForPlanting).Concat(mines).Concat(labs).Concat(blueprints))
            {
                // TODO: Needs review - I think the idea is a closer colonist can "steal" a target from one that is far away
                if (target is IPlanter || target is IFruitPlant || (target is IMineInteractive m && m.AssignedColonistDistance == 0))
                {
                    foreach (var tile in target.GetAccessTiles(this.colonistId).Where(t => accessibleTiles == null || accessibleTiles.Contains(t.Index)))
                    {
                        if (!candidateTilesAndTargetsGeneral.ContainsKey(tile)) candidateTilesAndTargetsGeneral.Add(tile, new List<IColonistInteractive>());
                        candidateTilesAndTargetsGeneral[tile].Add(target);
                    }
                }
                else
                {
                    foreach (var tile in target.GetAllAccessTiles()
                        .Where(t => (accessibleTiles == null || accessibleTiles.Contains(t.Index)) && t.ThingsAll.OfType<IColonist>().All(c => c == this.Colonist || c.IsMoving || c.IsRelaxing)))  // Avoid tile where another colonist is in the way
                    {
                        if (!candidateTilesAndTargetsGeneral.ContainsKey(tile)) candidateTilesAndTargetsGeneral.Add(tile, new List<IColonistInteractive>());
                        candidateTilesAndTargetsGeneral[tile].Add(target);
                    }

                }
            }

            Path pathGeneralHomeZone = null;
            if (candidateTilesAndTargetsGeneral.Any() || candidateTilesAndTargetsRepair.Any())
            {
                // Expanding circle method to find best general work path
                var openNodes = new HashSet<int> { this.Colonist.MainTileIndex };
                var closedNodes = new HashSet<int> { this.Colonist.MainTileIndex };
                ISmallTile targetTile = null;
                var distance = 0;
                while (openNodes.Any() && targetTile == null)
                {
                    var list = openNodes.ToList();
                    openNodes.Clear();
                    foreach (var n in list)
                    {
                        if (!ZoneManager.HomeZone.ContainsNode(n)) continue;
                        var o = ZoneManager.HomeZone.Nodes[n];
                        foreach (var l in o.AllLinks)
                        {
                            if (!closedNodes.Contains(l.Index))
                            {
                                var tile = World.GetSmallTile(l.Index);
                                if (candidateTilesAndTargetsGeneral.ContainsKey(tile))
                                {
                                    if (candidateTilesAndTargetsGeneral[tile].Any(a => a.CanAssignColonist(this.colonistId, tile.Index)
                                        || (a is IMineInteractive m && m.AssignedColonistDistance > distance + 5)
                                        || (a is ILab b && b.AssignedColonistDistance > distance + 5)))
                                    {
                                        targetTile = tile;
                                        break;
                                    }
                                }
                                else if (candidateTilesAndTargetsRepair.ContainsKey(tile))
                                {
                                    if (candidateTilesAndTargetsRepair[tile].Any(a => a.CanAssignColonistForRepair(this.colonistId, tile.Index)
                                        || (a is IMineInteractive m && m.AssignedColonistDistance > distance + 5)
                                        || (a is ILab b && b.AssignedColonistDistance > distance + 5)))
                                    {
                                        targetTile = tile;
                                        break;
                                    }
                                }

                                openNodes.Add(l.Index);
                                closedNodes.Add(l.Index);
                            }
                        }

                        if (targetTile != null) break;
                    }

                    distance++;
                }

                if (targetTile != null) pathGeneralHomeZone = this.FindPath(targetTile.Index);
            }

            var pathDeconstruct = this.FindPath(candidateTilesAndTargetsDeconstruct.Keys.ToList(), true);
            var pathHarvestFruit = this.FindPath(candidateTilesAndTargetsHarvestFruit.Keys.ToList(), true);

            // Preferred order: Haul from network -> Harvest fruit -> Haul from stack -> Deconstruct
            var path = pathHarvestFruit;
            if (path == null || (pathDeconstruct != null && pathDeconstruct.RemainingNodes.Count < path.RemainingNodes.Count * 0.75)) path = pathDeconstruct;

            if (path != null)
            {
                var endTile = World.GetSmallTile(path.EndPosition.X, path.EndPosition.Y);
                if (path == pathDeconstruct)
                {
                    var target = candidateTilesAndTargetsDeconstruct[endTile].First();
                    if (this.CurrentActivity is ActivityDeconstruct a && a.TargetID == target.Id && !this.CurrentActivity.IsFinished) return true;   // Already doing this

                    this.CurrentActivity = new ActivityDeconstruct(this.Colonist, target, path);
                    this.Colonist.ActivityType = ColonistActivityType.Deconstruct;
                }
                else if (path == pathHarvestFruit)
                {
                    var target = candidateTilesAndTargetsHarvestFruit[endTile].First();
                    if (this.CurrentActivity is ActivityHarvestFruit a && a.PlantId == target.Id && !this.CurrentActivity.IsFinished) return true;   // Already doing this

                    this.CurrentActivity = new ActivityHarvestFruit(this.Colonist, target, path);
                    this.Colonist.ActivityType = ColonistActivityType.Harvest;
                }

                return true;
            }

            if (pathGeneralHomeZone != null)
            {
                var endTile = World.GetSmallTile(pathGeneralHomeZone.EndPosition.X, pathGeneralHomeZone.EndPosition.Y);
                if (endTile != null && candidateTilesAndTargetsGeneral.ContainsKey(endTile))
                {
                    var target = candidateTilesAndTargetsGeneral[endTile].First();
                    if (target is IBlueprint blueprint)
                    {
                        if (this.CurrentActivity is ActivityConstruct ac && ac.TargetID == blueprint.Id && !this.CurrentActivity.IsFinished) return true;   // Already doing this

                        this.CurrentActivity = new ActivityConstruct(this.Colonist, blueprint, pathGeneralHomeZone);
                        this.Colonist.ActivityType = ColonistActivityType.Construct;
                        return true;
                    }
                    else if (target is IPlanter planter)
                    {
                        if (plantersForHarvest.Contains(planter))
                        {
                            if (this.CurrentActivity is ActivityFarmHarvest af && af.PlanterId == planter.Id && !this.CurrentActivity.IsFinished) return true;   // Already doing this
                            this.CurrentActivity = new ActivityFarmHarvest(this.Colonist, planter, pathGeneralHomeZone);
                        }
                        else
                        {
                            if (this.CurrentActivity is ActivityFarmPlant af && af.PlanterId == planter.Id && !this.CurrentActivity.IsFinished) return true;   // Already doing this
                            this.CurrentActivity = new ActivityFarmPlant(this.Colonist, planter, pathGeneralHomeZone);
                        }

                        this.Colonist.ActivityType = ColonistActivityType.Farm;
                        return true;
                    }
                    else if (target is ILab l)
                    {
                        if (this.CurrentActivity is ActivityResearch ar && ar.LabId == l.Id && !this.CurrentActivity.IsFinished) return true;   // Already doing this

                        this.CurrentActivity = new ActivityResearch(this.Colonist, l, pathGeneralHomeZone);
                        this.Colonist.ActivityType = ColonistActivityType.Lab;
                        return true;
                    }
                }
                else if (endTile != null && candidateTilesAndTargetsRepair.ContainsKey(endTile))
                {
                    var target = candidateTilesAndTargetsRepair[endTile].First();
                    if (target is IRepairableThing rt && rt.RepairPriority != WorkPriority.Disabled)
                    {
                        if (this.CurrentActivity is ActivityRepair ar && ar.TargetID == target.Id && !this.CurrentActivity.IsFinished) return true;   // Already doing this

                        this.CurrentActivity = new ActivityRepair(this.Colonist, target, pathGeneralHomeZone);
                        this.Colonist.ActivityType = ColonistActivityType.Repair;
                        return true;
                    }
                }
            }

            return false;
        }

        private bool CanHarvest(IPlanter planter, WorkPriority priority, bool isCookerAvailable, bool isKekFactoryAvailable)
        {
            if (planter.FarmPriority != priority || !planter.CanAssignColonist(this.Colonist.Id)) return false;
            if (planter.PlanterStatus == PlanterStatus.Dead || planter.RemoveCrop) return true;
            if (planter.PlanterStatus != PlanterStatus.WaitingToHarvest || this.Colonist.CarriedItemTypeBack != ItemType.None) return false;

            var cropDef = CropDefinitionManager.GetDefinition(planter.CurrentCropTypeId);
            if (cropDef == null) return false;

            if (cropDef.CookerType == ThingType.KekFactory && !isKekFactoryAvailable) return false;

            return isCookerAvailable;
        }

        private bool TrySatisfyHunger(HashSet<int> accessibleTiles)
        {
            var result = this.TrySatisfyHungerInner(accessibleTiles);
            if (result) this.Colonist.LookingForFoodCounter = 0;
            else if (!this.Colonist.Body.IsSleeping)
            {
                this.Colonist.LookingForFoodCounter++;
                this.isLookingForFood = true;
            }

            return result;
        }

        private bool TrySatisfyHungerInner(HashSet<int> accessibleTiles)
        {
            if (this.CurrentActivity?.CurrentAction is ActionEat && !this.CurrentActivity.IsFinished) return true;

            var dispensers = World.GetThings<IFoodDispenser>(ThingType.FoodDispenser).Where(d => d.IsDispenserSwitchedOn && d.DispenserStatus != DispenserStatus.NoResource).ToList();

            // Look for a food dispenser in adjacent tiles that is ready or will be soon
            var dispenser = dispensers.FirstOrDefault(d => d.CanAssignColonist(this.colonistId, this.Colonist.MainTileIndex));
            if (dispenser != null)
            {
                if (this.CurrentActivity is ActivityEat a && a.TargetId == dispenser.Id && !this.CurrentActivity.IsFinished) return true;   // Already doing this
                this.CurrentActivity = new ActivityEat(this.Colonist, dispenser);
                this.Colonist.ActivityType = ColonistActivityType.Eat;
                return true;
            }

            // Look for a food dispenser that is ready somewhere else and try to get to it
            var candidateTiles = dispensers.Where(d => d.DispenserStatus == DispenserStatus.Standby)
                .SelectMany(d => d.GetAccessTiles(this.Colonist.Id)).Distinct().Where(t => accessibleTiles == null || accessibleTiles.Contains(t.Index)).ToList();
            var path = this.FindPath(candidateTiles);
            if (path != null)
            {
                dispenser = dispensers.FirstOrDefault(d => d.CanAssignColonist(this.Colonist.Id, World.GetSmallTile(path.EndPosition).Index));
                if (dispenser == null) return false;   // Something went wrong

                if (this.CurrentActivity is ActivityEat a && a.TargetId == dispenser.Id && !this.CurrentActivity.IsFinished) return true;   // Already doing this
                this.CurrentActivity = new ActivityEat(this.Colonist, dispenser, path);
                this.Colonist.ActivityType = ColonistActivityType.Eat;
                return true;
            }

            // If really hungry then may be able to join a queue somewhere else
            if (this.Colonist.Body.Nourishment < 50)
            {
                candidateTiles = dispensers.Where(d => d.DispenserStatus != DispenserStatus.Standby)
                    .SelectMany(d => d.GetAccessTiles(this.Colonist.Id)).Distinct().ToList();
                path = this.FindPath(candidateTiles);
                if (path?.RemainingNodes?.Any() == true)
                {
                    dispenser = dispensers.FirstOrDefault(d => d.CanAssignColonist(this.Colonist.Id, World.GetSmallTile(path.EndPosition).Index));
                    if (dispenser == null) return false;   // Something went wrong

                    if (this.CurrentActivity is ActivityEat a && a.TargetId == dispenser.Id && !this.CurrentActivity.IsFinished) return true;   // Already doing this
                    this.CurrentActivity = new ActivityEat(this.Colonist, dispenser, path);
                    this.Colonist.ActivityType = ColonistActivityType.Eat;
                    return true;
                }
            }

            return false;
        }

        private bool TrySatisfyThirst(HashSet<int> accessibleTiles)
        {
            var result = this.TrySatisfyThirstInner(accessibleTiles);
            if (result) this.Colonist.LookingForWaterCounter = 0;
            else if (!this.Colonist.Body.IsSleeping && this.Colonist.Body.Hydration < 50)
            {
                this.Colonist.LookingForWaterCounter++;
                this.isLookingForWater = true;
            }

            return result;
        }

        private bool TrySatisfyThirstInner(HashSet<int> accessibleTiles)
        {
            if (this.CurrentActivity?.CurrentAction is ActionDrink && !this.CurrentActivity.IsFinished) return true;

            var dispensers = World.GetThings<IDispenser>(ThingType.WaterDispenser).Where(d => d.IsDispenserSwitchedOn && d.DispenserStatus != DispenserStatus.NoResource).ToList();
            if (!dispensers.Any()) return false;

            // Look for a water dispenser in adjacent tiles that is ready or will be soon
            var dispenser = dispensers.FirstOrDefault(d => d.CanAssignColonist(this.colonistId, this.Colonist.MainTileIndex) && d.IsDispenserSwitchedOn && d.CountColonistAssignments(this.colonistId) == 0);
            if (dispenser != null)
            {
                if (this.CurrentActivity is ActivityDrink a && a.TargetId == dispenser.Id && !this.CurrentActivity.IsFinished) return true;   // Already doing this
                this.CurrentActivity = new ActivityDrink(this.Colonist, dispenser);
                this.Colonist.ActivityType = ColonistActivityType.Drink;
                return true;
            }

            // Look for a water dispenser that is ready somewhere else and try to get to it
            var candidateTiles = dispensers.Where(d => d.CountColonistAssignments(this.colonistId) == 0)
                .SelectMany(d => d.GetAccessTiles(this.Colonist.Id)).Distinct().Where(t => accessibleTiles == null || accessibleTiles.Contains(t.Index)).ToList();
            var path = this.FindPath(candidateTiles);
            if (path != null)
            {
                dispenser = dispensers.FirstOrDefault(d => d.CanAssignColonist(this.Colonist.Id, World.GetSmallTile(path.EndPosition).Index));
                if (dispenser == null) return false;   // Something went wrong

                if (this.CurrentActivity is ActivityDrink a && a.TargetId == dispenser.Id && !this.CurrentActivity.IsFinished) return true;   // Already doing this
                this.CurrentActivity = new ActivityDrink(this.Colonist, dispenser, path);
                this.Colonist.ActivityType = ColonistActivityType.Drink;
                return true;
            }

            // If really thirsty then may be able to join a queue somewhere else
            if (this.Colonist.Body.Hydration < 50)
            {
                candidateTiles = dispensers.Where(d => d.CountColonistAssignments(this.colonistId) > 0)
                    .SelectMany(d => d.GetAccessTiles(this.Colonist.Id)).Distinct().ToList();
                path = this.FindPath(candidateTiles);
                if (path?.RemainingNodes?.Any() == true)
                {
                    dispenser = dispensers.FirstOrDefault(d => d.CanAssignColonist(this.Colonist.Id, World.GetSmallTile(path.EndPosition).Index));
                    if (dispenser == null) return false;   // Something went wrong

                    if (this.CurrentActivity is ActivityDrink a && a.TargetId == dispenser.Id && !this.CurrentActivity.IsFinished) return true;   // Already doing this
                    this.CurrentActivity = new ActivityDrink(this.Colonist, dispenser, path);
                    this.Colonist.ActivityType = ColonistActivityType.Drink;
                    return true;
                }
            }

            return false;
        }

        private bool TrySatisfyGetKek(HashSet<int> accessibleTiles)
        {
            if (this.CurrentActivity?.CurrentAction is ActionPickupKek && !this.CurrentActivity.IsFinished) return true;

            var dispensers = World.GetThings<IDispenser>(ThingType.KekDispenser).Where(d => d.IsDispenserSwitchedOn && d.DispenserStatus != DispenserStatus.NoResource).ToList();
            if (!dispensers.Any()) return false;

            // Colonist will only pickup kek if a table is available and accessible
            var tableTiles = World.GetThings<ITable>(ThingType.TableMetal, ThingType.TableStone).SelectMany(t => t.GetAccessTiles(this.colonistId)).ToList();
            if (!tableTiles.Any()) return false;
            if (!tableTiles.Contains(this.Colonist.MainTile) && this.FindPath(tableTiles) == null) return false;

            // Look for a kek dispenser in adjacent tiles that is ready or will be soon
            var dispenser = dispensers.FirstOrDefault(d => d.CanAssignColonist(this.colonistId, this.Colonist.MainTileIndex) && d.IsDispenserSwitchedOn && d.CountColonistAssignments(this.colonistId) == 0);
            if (dispenser != null)
            {
                if (this.CurrentActivity is ActivityPickupKek a && a.TargetId == dispenser.Id && !this.CurrentActivity.IsFinished) return true;   // Already doing this
                this.CurrentActivity = new ActivityPickupKek(this.Colonist, dispenser);
                this.Colonist.ActivityType = ColonistActivityType.DrinkKek;
                return true;
            }

            // Look for a kek dispenser that is ready somewhere else and try to get to it
            var candidateTiles = dispensers.Where(d => d.CountColonistAssignments(this.colonistId) == 0)
                .SelectMany(d => d.GetAccessTiles(this.Colonist.Id)).Distinct().Where(t => accessibleTiles == null || accessibleTiles.Contains(t.Index)).ToList();
            var path = this.FindPath(candidateTiles);
            if (path != null)
            {
                dispenser = dispensers.FirstOrDefault(d => d.CanAssignColonist(this.Colonist.Id, World.GetSmallTile(path.EndPosition).Index));
                if (dispenser == null) return false;   // Something went wrong

                if (this.CurrentActivity is ActivityPickupKek a && a.TargetId == dispenser.Id && !this.CurrentActivity.IsFinished) return true;   // Already doing this
                this.CurrentActivity = new ActivityPickupKek(this.Colonist, dispenser, path);
                this.Colonist.ActivityType = ColonistActivityType.DrinkKek;
                return true;
            }

            return false;
        }

        private bool TrySatisfyDoGeology(HashSet<int> accessibleTiles, bool firstPass, bool prioritisedOnly)
        {
            if (this.CurrentActivity is ActivityGeology && !this.CurrentActivity.IsFinished) return true;

            // First pass, we look for best tile.  If it's a tile prioiritised by the player, do it now, otherwise save for the second pass.
            if (firstPass)
            {
                var zone = ZoneManager.HomeZone.Nodes.Any() ? ZoneManager.HomeZone : ZoneManager.GlobalZone;
                bestGeologyTile = ZoneManager.GlobalZone.FindBestTile(this.Colonist, 1, GeologyController.TilesToSurvey.Any() ? 200 : 100, 99, this.minTempTolerance, accessibleTiles, GetTileGeologyScore, out int? score);
                if (!bestGeologyTile.HasValue || (prioritisedOnly && !GeologyController.TilesToSurvey.Contains(bestGeologyTile.Value))) return false;
            }
            else if (!bestGeologyTile.HasValue || GeologyController.TilesToSurvey.Contains(bestGeologyTile.Value)) return false;

            if (this.Colonist.MainTileIndex == bestGeologyTile.Value)
            {
                this.CurrentActivity = new ActivityGeology(this.Colonist);
                this.Colonist.ActivityType = ColonistActivityType.Geology;
                return true;
            }
            else
            {
                var path = FindPath(bestGeologyTile.Value, true);
                if (path?.RemainingNodes?.Any() == true)
                {
                    this.CurrentActivity = new ActivityGeology(this.Colonist, path);
                    this.Colonist.ActivityType = ColonistActivityType.Geology;
                    return true;
                }
            }

            return false;
        }

        private bool TryCook()
        {
            if (this.CurrentActivity is ActivityCook && !this.CurrentActivity.IsFinished) return true;

            if (!this.Colonist.CarriedCropType.HasValue) return false;

            var thingType = CropDefinitionManager.GetDefinition(this.Colonist.CarriedCropType.Value)?.CookerType ?? ThingType.Cooker;
            var cookers = World.GetThings<ICooker>(thingType).Where(d => d.IsReadyToCook).ToList();

            // Look for a cooker in adjacent tiles
            var cooker = cookers.FirstOrDefault(d => d.CanAssignColonist(this.Colonist.Id, this.Colonist.MainTileIndex));
            if (cooker != null)
            {
                if (this.CurrentActivity is ActivityCook a && a.CookerId == cooker.Id && !this.CurrentActivity.IsFinished) return true;   // Already doing this
                this.CurrentActivity = new ActivityCook(this.Colonist, cooker);
                this.Colonist.ActivityType = ColonistActivityType.Cook;
                return true;
            }

            // Look for a cooker that is ready somewhere else and try to get to it.  Try cookers that are ready to use first.
            var candidateTiles = cookers.Where(c => c.FactoryStatus == FactoryStatus.Standby).SelectMany(d => d.GetAccessTiles(this.Colonist.Id)).Distinct().ToList();
            var path = this.FindPath(candidateTiles);
            if (path != null)
            {
                cooker = cookers.FirstOrDefault(d => d.CanAssignColonist(this.Colonist.Id, World.GetSmallTile(path.EndPosition).Index));
                if (cooker == null) return false;   // Something went wrong

                if (this.CurrentActivity is ActivityCook a && a.CookerId == cooker.Id && !this.CurrentActivity.IsFinished) return true;   // Already doing this
                this.CurrentActivity = new ActivityCook(this.Colonist, cooker, path);
                this.Colonist.ActivityType = ColonistActivityType.Cook;
                return true;
            }

            candidateTiles = cookers.Where(c => c.FactoryStatus != FactoryStatus.Standby).SelectMany(d => d.GetAccessTiles(this.Colonist.Id)).Distinct().ToList();
            path = this.FindPath(candidateTiles);
            if (path != null)
            {
                cooker = cookers.FirstOrDefault(d => d.CanAssignColonist(this.Colonist.Id, World.GetSmallTile(path.EndPosition).Index));
                if (cooker == null) return false;   // Something went wrong

                if (this.CurrentActivity is ActivityCook a && a.CookerId == cooker.Id && !this.CurrentActivity.IsFinished) return true;   // Already doing this
                this.CurrentActivity = new ActivityCook(this.Colonist, cooker, path);
                this.Colonist.ActivityType = ColonistActivityType.Cook;
                return true;
            }

            return false;
        }

        private bool TryDrinkKek()
        {
            if (this.CurrentActivity is ActivityDrinkKek && !this.CurrentActivity.IsFinished) return true;
            if (this.Colonist.CarriedItemTypeArms != ItemType.Kek) return false;

            var tables = World.GetThings<ITable>(ThingType.TableMetal, ThingType.TableStone).Where(d => d.CanAssignColonist(this.colonistId)).ToList();

            // Look for a table in adjacent tiles
            var table = tables.FirstOrDefault(d => d.CanAssignColonist(this.Colonist.Id, this.Colonist.MainTileIndex));
            if (table != null)
            {
                this.CurrentActivity = new ActivityDrinkKek(this.Colonist, table.Id);
                this.Colonist.ActivityType = ColonistActivityType.DrinkKek;
                return true;
            }

            // Look for a table that is free somewhere else and try to get to it.
            var candidateTiles = tables.SelectMany(d => d.GetAccessTiles(this.Colonist.Id)).Distinct().ToList();
            var path = this.FindPath(candidateTiles);
            if (path != null)
            {
                table = tables.FirstOrDefault(d => d.CanAssignColonist(this.Colonist.Id, World.GetSmallTile(path.EndPosition).Index));
                if (table == null) return false;   // Something went wrong

                this.CurrentActivity = new ActivityDrinkKek(this.Colonist, table.Id, path);
                this.Colonist.ActivityType = ColonistActivityType.DrinkKek;
                return true;
            }

            // No table?  Drink kek anyway
            this.CurrentActivity = new ActivityDrinkKek(this.Colonist, null);
            this.Colonist.ActivityType = ColonistActivityType.DrinkKek;
            return true;
        }

        private bool TrySatisfyRestOrSleepPodOnly(bool isSleep)
        {
            var result = this.TrySatisfyRestOrSleepPodOnlyInner(isSleep);
            if (result) WarningsController.Remove(WarningType.NoSleepPod, this.Colonist.ShortName);
            else if (isSleep) WarningsController.Add(WarningType.NoSleepPod, this.Colonist.ShortName);

            return result;
        }

        private bool TrySatisfyRestOrSleepPodOnlyInner(bool isSleep)
        {
            if (isSleep && this.CurrentActivity is ActivitySleep && !this.CurrentActivity.IsFinished) return true;
            if (!isSleep && this.CurrentActivity is ActivityRest && !this.CurrentActivity.IsFinished) return true;

            var pods = World.GetThings<ISleepPod>(ThingType.SleepPod)
                .Where(d => d.CanAssignColonist(this.Colonist.Id)).ToList();

            // Already in pod?
            var pod = pods.FirstOrDefault(d => d.MainTileIndex == this.Colonist.MainTileIndex);
            if (pod != null)
            {
                this.CurrentActivity = isSleep ? (ActivityBase)new ActivitySleep(this.Colonist, pod) : new ActivityRest(this.Colonist);
                this.Colonist.ActivityType = isSleep ? ColonistActivityType.Sleep : ColonistActivityType.Rest;
                return true;
            }

            // Try to get to our own pod first
            pod = pods.FirstOrDefault(d => d.OwnerID == this.Colonist.Id);
            if (pod != null)
            {
                var p = this.FindPath(pod.MainTileIndex);
                if (p != null)
                {
                    var endTile = World.GetSmallTile(p.EndPosition);
                    pod = pods.FirstOrDefault(d => d.MainTileIndex == endTile.Index);
                    this.CurrentActivity = isSleep ? (ActivityBase)new ActivitySleep(this.Colonist, pod, p) : new ActivityRest(this.Colonist, p);
                    this.Colonist.ActivityType = isSleep ? ColonistActivityType.Sleep : ColonistActivityType.Rest;
                    return true;
                }
            }

            // Look for a pod that is ready somewhere else and try to get to it
            var candidateTiles = pods.Select(d => d.MainTile).ToList();
            var path = this.FindPath(candidateTiles);
            if (path != null)
            {
                var endTile = World.GetSmallTile(path.EndPosition);
                pod = pods.FirstOrDefault(d => d.MainTileIndex == endTile.Index);
                this.CurrentActivity = isSleep ? (ActivityBase)new ActivitySleep(this.Colonist, pod, path) : new ActivityRest(this.Colonist, path);
                this.Colonist.ActivityType = isSleep ? ColonistActivityType.Sleep : ColonistActivityType.Rest;
                return true;
            }

            return false;
        }

        private bool TrySatisfyRestOrSleepAnywhere(bool isSleep)
        {
            var result = this.TrySatisfyRestOrSleepAnywhereInner(isSleep);
            if (result) WarningsController.Remove(WarningType.NoSleepPod, this.Colonist.ShortName);
            else if (isSleep) WarningsController.Add(WarningType.NoSleepPod, this.Colonist.ShortName);

            return result;
        }

        private bool TrySatisfyRestOrSleepAnywhereInner(bool isSleep)
        {
            if (isSleep && this.CurrentActivity is ActivitySleep && !this.CurrentActivity.IsFinished) return true;
            if (!isSleep && this.CurrentActivity is ActivityRest && !this.CurrentActivity.IsFinished) return true;

            var i = ZoneManager.HomeZone.FindBestTile(this.Colonist, 1, 100, 100, this.minTempTolerance, GetTileSleepScore, out int? score);
            if (this.Colonist.MainTileIndex == i)
            {
                // Already in best tile
                this.CurrentActivity = isSleep ? (ActivityBase)new ActivitySleep(this.Colonist, null) : new ActivityRest(this.Colonist);
                this.Colonist.ActivityType = isSleep ? ColonistActivityType.Sleep : ColonistActivityType.Rest;
                return true;
            }
            else if (i.HasValue)
            {
                // Better tile is somewhere else
                var path = PathFinder.FindPath(this.Colonist.MainTileIndex, i.Value, ZoneManager.HomeZone.Nodes);
                if (path?.RemainingNodes?.Any() == true)
                {
                    this.CurrentActivity = isSleep ? (ActivityBase)new ActivitySleep(this.Colonist, null, path) : new ActivityRest(this.Colonist, path);
                    this.Colonist.ActivityType = isSleep ? ColonistActivityType.Sleep : ColonistActivityType.Rest;
                    return true;
                }
            }

            return false;
        }

        private bool TrySatisfyRoam()
        {
            if (this.CurrentActivity is ActivityRoam && !this.CurrentActivity.IsFinished) return true;

            var zone = ZoneManager.HomeZone.Nodes.Any() ? ZoneManager.HomeZone : ZoneManager.GlobalZone;
            var i = zone.FindBestTile(this.Colonist, 0, 37, 16, this.minTempTolerance, GetTileRoamScore, out int? score);
            if (i.HasValue && this.Colonist.MainTileIndex != i)
            {
                var path = FindPath(i.Value);
                if (path?.RemainingNodes?.Any() == true)
                {
                    this.CurrentActivity = new ActivityRoam(this.Colonist, path);
                    this.Colonist.ActivityType = ColonistActivityType.Roam;
                    return true;
                }
            }

            return false;
        }

        private bool TrySatisfyRelax(HashSet<int> accessibleTiles)
        {
            if (this.CurrentActivity is ActivityRelax && !this.CurrentActivity.IsFinished) return true;

            var coldTolerance = this.Colonist.Cards.GetEffectsSum(CardEffectType.ColdTolerance);
            var heatTolerance = this.Colonist.Cards.GetEffectsSum(CardEffectType.HeatTolerance);

            var minTemp = -10 - coldTolerance;
            var maxTemp = 40 + heatTolerance;

            var bestScore = 0;
            ITable bestTable = null;
            ISmallTile bestTile = null;

            foreach (var table in World.GetThings<ITable>(ThingType.TableMetal, ThingType.TableStone))
            {
                var otherColonistCount = table.GetOtherColonists(this.Colonist.Id).Count();
                
                var scoresByTile = new Dictionary<ISmallTile, int>();
                foreach (var tile in table.MainTile.AdjacentTiles4)
                {
                    if (accessibleTiles != null && !accessibleTiles.Contains(tile.Index)) continue;
                    if (!table.CanAssignColonist(this.Colonist.Id, tile.Index)) continue;

                    var score = 50;

                    var temp = (int)RoomManager.GetTileTemperature(tile.Index, false);
                    if (temp < minTemp || temp > maxTemp) continue;

                    if (temp < 6 - coldTolerance) score -= 2 * ((6 - coldTolerance) - temp);
                    else if (temp < 16 - coldTolerance) score -= (16 - coldTolerance) - temp;
                    else if (temp > 34 + heatTolerance) score -= 2 * (temp - (34 + heatTolerance));
                    else if (temp > 24 + heatTolerance) score -= temp - (24 + heatTolerance);

                    var distance = (tile.TerrainPosition - this.Colonist.MainTile.TerrainPosition).Length();
                    score -= (int)(distance * 1.5f);
                    score += otherColonistCount * 10;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestTable = table;
                        bestTile = tile;
                    }
                }
            }

            if (bestTile == null) return false;

            if (bestTile == this.Colonist.MainTile)
            {
                this.CurrentActivity = new ActivityRelax(this.Colonist, bestTable.Id);
                this.Colonist.ActivityType = ColonistActivityType.Relax;
                return true;
            }

            var path = this.FindPath(bestTile.Index);
            if (path != null)
            {
                this.CurrentActivity = new ActivityRelax(this.Colonist, bestTable.Id, path);
                this.Colonist.ActivityType = ColonistActivityType.Relax;
                return true;
            }

            return false;
        }

        private bool TrySatisfyMoveIdle()
        {
            if ((this.CurrentActivity is ActivityIdleWalk || this.CurrentActivity is ActivityRoam || this.CurrentActivity is ActivityRelax) && !this.CurrentActivity.IsFinished) return true;

            var zone = ZoneManager.HomeZone.Nodes.Any() ? ZoneManager.HomeZone : ZoneManager.GlobalZone;
            var i = zone.FindBestTile(this.Colonist, 0, 95, 16, this.minTempTolerance, GetTileIdleScore, out int? score);
            if (i.HasValue && this.Colonist.MainTileIndex != i)
            {
                var path = FindPath(i.Value);
                if (path?.RemainingNodes?.Any() == true)
                {
                    this.CurrentActivity = new ActivityIdleWalk(this.Colonist, path);
                    this.Colonist.ActivityType = ColonistActivityType.Move;
                    return true;
                }
            }
            else if (this.Colonist.PositionOffset.Length() > 0.25f
                && this.Colonist.MainTile.ThingsAll.All(t => t is IColonist || t.TileBlockModel.In(TileBlockModel.None, TileBlockModel.Wall, TileBlockModel.Door)))
            {
                // Move to middle of tile
                var node = new PathNode(this.Colonist.MainTile.X, this.Colonist.MainTile.Y, Direction.None, Direction.None);
                var nodeStack = new Stack<PathNode>();
                nodeStack.Push(node);
                nodeStack.Push(node);
                var nullPath = new Path(this.Colonist.MainTile.TerrainPosition, this.Colonist.MainTile.TerrainPosition, nodeStack);
                this.CurrentActivity = new ActivityIdleWalk(this.Colonist, nullPath);
                return true;
            }

            return false;
        }

        private bool TrySatisfyGetWarm()
        {
            if (this.CurrentActivity is ActivitySeekSafeTemperature && !this.CurrentActivity.IsFinished) return true;

            var zone = ZoneManager.HomeZone.Nodes.Any() ? ZoneManager.HomeZone : ZoneManager.GlobalZone;
            var i = zone.FindBestTile(this.Colonist, 1, 20, 100, this.minTempTolerance, GetTileTemperatureScore, out int? score);
            if (this.Colonist.MainTileIndex == i)
            {
                this.CurrentActivity = new ActivitySeekSafeTemperature(this.Colonist, null);
                this.Colonist.ActivityType = ColonistActivityType.GetWarm;
                return true;
            }
            else if (i.HasValue && this.Colonist.MainTileIndex != i)
            {
                var path = FindPath(i.Value);
                if (path?.RemainingNodes?.Any() == true)
                {
                    this.CurrentActivity = new ActivitySeekSafeTemperature(this.Colonist, path);
                    this.Colonist.ActivityType = ColonistActivityType.GetWarm;
                    return true;
                }
            }

            return false;
        }

        private bool TrySatisfyReturnHome()
        {
            if (this.CurrentActivity is ActivityRoam && !this.CurrentActivity.IsFinished) return true;
            if (ZoneManager.HomeZone.ContainsNode(this.Colonist.MainTileIndex)) return false;

            var zone = ZoneManager.HomeZone.Nodes.Any() ? ZoneManager.HomeZone : ZoneManager.GlobalZone;
            var i = ZoneManager.GlobalZone.FindBestTile(this.Colonist, 1, 8, 999, this.minTempTolerance, GetTileReturnHomeScore, out int? score);
            if (i.HasValue && this.Colonist.MainTileIndex != i)
            {
                var path = FindPath(i.Value);
                if (path?.RemainingNodes?.Any() == true)
                {
                    this.CurrentActivity = new ActivityRoam(this.Colonist, path);
                    this.Colonist.ActivityType = ColonistActivityType.ReturnHome;
                    return true;
                }
            }

            return false;
        }

        private Path FindPath(int tileIndex, bool useGlobalZone = false)
        {
            // If we are on the edge of the home zone, path find using global
            if (!useGlobalZone && this.Colonist.MainTile.AdjacentTiles4.Any(t => !ZoneManager.HomeZone.ContainsNode(t.Index))) useGlobalZone = true;

            return PathFinder.FindPath(this.Colonist.MainTileIndex, tileIndex, useGlobalZone ? ZoneManager.GlobalZone.Nodes : ZoneManager.HomeZone.Nodes);
        }

        private Path FindPath(List<ISmallTile> candidateTiles, bool useGlobalZone = false)
        {
            if (!candidateTiles.Any()) return null;

            // If we are on the edge of the home zone, path find using global
            if (!useGlobalZone && this.Colonist.MainTile.AdjacentTiles4.Any(t => !ZoneManager.HomeZone.ContainsNode(t.Index))) useGlobalZone = true;

            ISmallTile targetTile = null;
            if (candidateTiles.Count > 1 && ZoneManager.GlobalZone.Nodes.ContainsKey(this.Colonist.MainTileIndex))
            {
                // Expanding circle method faster if there are many possible targets
                var openNodes = new HashSet<int> { this.Colonist.MainTileIndex };
                var closedNodes = new HashSet<int> { this.Colonist.MainTileIndex };
                while (openNodes.Any() && targetTile == null)
                {
                    var list = openNodes.ToList();
                    openNodes.Clear();
                    foreach (var n in list)
                    {
                        var o = ZoneManager.GlobalZone.Nodes[n];
                        foreach (var l in o.AllLinks)
                        {
                            if (!closedNodes.Contains(l.Index))
                            {
                                var tile = World.GetSmallTile(l.Index);
                                if (candidateTiles.Contains(tile))
                                {
                                    targetTile = tile;
                                    break;
                                }

                                openNodes.Add(l.Index);
                                closedNodes.Add(l.Index);
                            }
                        }

                        if (targetTile != null) break;
                    }
                }
            }
            else if (candidateTiles.Count == 1) targetTile = candidateTiles[0];

            if (targetTile != null)
            {
                // If we are standing next to a stationary colonist or rover, find a way around it.
                var tilesToAvoid = this.TilesToAvoidWithTimeouts.Keys.ToHashSet();
                var path = PathFinder.FindPath(this.Colonist.MainTileIndex, targetTile.Index, useGlobalZone ? ZoneManager.GlobalZone.Nodes : ZoneManager.HomeZone.Nodes, tilesToAvoid);
                if (path?.RemainingNodes?.Any() == true) return path;
            }

            return null;
        }

        private bool IsInLandingPod()
        {
            return this.Colonist.MainTile.ThingsPrimary.Any(t => t.ThingType == ThingType.LandingPod);
        }

        private void LeaveLandingPod()
        {
            this.Colonist.ActivityType = ColonistActivityType.LeavingLandingPod;
            this.CurrentActivity = new ActivityLeaveLandingPod(this.Colonist);
        }

        // Used to fix problem where colonist ends up in a tile with no links
        private bool IsInClosedTile()
        {
            return this.Colonist.CurrentSpeed < ActionWalk.MaxSpeed / 2f 
                && (!ZoneManager.GlobalZone.ContainsNode(this.Colonist.MainTileIndex) || ZoneManager.GlobalZone.Nodes[this.Colonist.MainTileIndex].AllLinks.Count() == 0);
        }

        private void LeaveClosedTile()
        {
            this.CurrentActivity = new ActivityLeaveClosedTile(this.Colonist);
        }

        // Gets accessible tiles whilst limiting outside walk distance - used to stop colonists trying to walk a long way in the cold
        private HashSet<int> GetAccessibleTiles(int maxDistanceOutside = 10)
        {
            var result = new HashSet<int>();
            var openNodes = new HashSet<int> { this.Colonist.MainTileIndex };
            var closedNodes = new HashSet<int> { this.Colonist.MainTileIndex };
            var distanceOutsideByTile = new Dictionary<int, int> { { this.Colonist.MainTileIndex, 0 } };
            while (openNodes.Any())
            {
                var list = openNodes.ToList();
                openNodes.Clear();
                foreach (var n in list)
                {
                    if (!ZoneManager.HomeZone.ContainsNode(n)) continue;
                    var o = ZoneManager.HomeZone.Nodes[n];
                    var distanceOutside = distanceOutsideByTile[n];

                    foreach (var l in o.AllLinks)
                    {
                        if (!closedNodes.Contains(l.Index))
                        {
                            var newDistance = RoomManager.GetTileTemperature(l.Index) >= this.minTempTolerance ? Math.Max(0, distanceOutside - 1) : distanceOutside + 1;
                            if (newDistance <= maxDistanceOutside)
                            {
                                openNodes.Add(l.Index);
                                result.Add(l.Index);
                                distanceOutsideByTile.Add(l.Index, newDistance);
                            }

                            closedNodes.Add(l.Index);
                        }
                    }
                }
            }

            return result;
        }

        private static int GetTileGeologyScore(IAnimal colonist, ISmallTile tile, int distance, int minTemperature)
        {
            if (tile == null || tile.IsMineResourceVisible || tile.ThingsAll.Any(t => !(t is IWall) && !(t is IMoveableThing) && (t.TileBlockModel != TileBlockModel.None || t.ThingType.IsFoundation()))) return 0;
            if (tile.MineResourceSurveyReservedBy.GetValueOrDefault(colonist.Id) != colonist.Id && tile.MineResourceSurveyReservedAt > World.WorldTime.FrameNumber - 60) return 0;   // Someone else doing this tile
            if (World.Temperature < minTemperature)
            {
                var room = RoomManager.GetRoom(tile.Index);
                var temperature = RoomManager.GetTileTemperature(tile.Index);
                if (temperature < minTemperature) return 0;
            }

            if (World.WorldTime.FrameNumber % 13 == 0) tileContextScoreCache.Clear();

            var score = 20 - distance;

            if (tileContextScoreCache.ContainsKey(tile.Index)) score += tileContextScoreCache[tile.Index];   // Optimisation
            else
            {
                var s = 0;
                foreach (var t in tile.AdjacentTiles8)
                {
                    if (t.IsMineResourceVisible) s += 20;
                    if (World.BuildableTiles.Contains(t.Index)) s += 2;
                }

                score += s;
                tileContextScoreCache.Add(tile.Index, s);
            }

            if (GeologyController.TilesToSurvey.Contains(tile.Index)) score += 200;
            if (World.BuildableTiles.Contains(tile.Index)) score += 20;
            if (tile.OreScannerLstFrame > World.WorldTime.FrameNumber - 60) score -= 80;

            return score;
        }

        private static int GetTileRoamScore(IAnimal colonist, ISmallTile tile, int distance, int minTemperature)
        {
            if (distance < 6) return 0;

            if (tile == null || tile.ThingsAll.Any(t => !(t is IWall) && !(t is IMoveableThing) && t.TileBlockModel != TileBlockModel.None)) return 0;
            var room = RoomManager.GetRoom(tile.Index);
            var temperature = RoomManager.GetTileTemperature(tile.Index);
            if (temperature < minTemperature) return 0;

            var score = room == null ? 30 : 20;    // Prefer to roam outside

            if (tile.IsCorridor) score -= 20;      // Try to avoid tiles where we might be in someone's way
            score += (int)random.NextFloat(8) - Math.Abs(distance - 8);   // Colonists like to walk around 8 tiles at a time

            return score;
        }

        private static int GetTileTemperatureScore(IAnimal colonist, ISmallTile tile, int distance, int minTemperature)
        {
            if (tile == null || tile.ThingsAll.Any(t => !(t is IWall) && !(t is IMoveableThing) && t.TileBlockModel.In(TileBlockModel.None, TileBlockModel.Pod, TileBlockModel.Point) != true)) return 0;

            var light = RoomManager.GetTileLightLevel(tile.Index, false);
            var temperature = RoomManager.GetTileTemperature(tile.Index, false, true);
            if (temperature < minTemperature) return 0;

            var score = light >= 0.49f ? 40 : 36;
            score -= (int)Math.Abs(temperature - 20);

            if (tile.IsCorridor) score -= 10;
            foreach (var thing in tile.ThingsPrimary)
            {
                if (thing.ThingType == ThingType.Colonist && thing != colonist) score -= 10;
                else if (thing is ISleepPod pod && !pod.CanAssignColonist(colonist.Id)) return 0;
            }

            if (score > 0) score = Math.Max(1, score - distance);

            return score;
        }

        private static int GetTileSleepScore(IAnimal colonist, ISmallTile tile, int distance, int minTemperature)
        {
            if (tile == null || tile.ThingsAll.Any(t => !(t is IWall) && t.TileBlockModel != TileBlockModel.None && t.Definition?.TileBlockModel != TileBlockModel.Pod)) return 0;
            var temperature = RoomManager.GetTileTemperature(tile.Index, false, true);

            var pod = tile.ThingsAll.OfType<ISleepPod>().FirstOrDefault();
            if (pod != null && !pod.CanAssignColonist(colonist.Id)) return 0;

            if (temperature < minTemperature && pod == null) return 0;

            if (pod == null && (colonist as IColonist).Body.Energy > Constants.ColonistStartSleepNoPodTiredness) return 0;  // Colonist has to be pretty tired before sleeping outside a pod

            var score = (colonist as IColonist)?.GetTileSleepScore(tile) ?? 0 - distance;
            if (tile.IsCorridor && pod == null) score -= 10;

            return score;
        }

        // Score a tile to wait in if we have nothing else to do
        private static int GetTileIdleScore(IAnimal colonist, ISmallTile tile, int distance, int minTemperature)
        {
            if (tile == null) return 0;
            var temperature = RoomManager.GetTileTemperature(tile.Index, false, true);
            if (temperature < minTemperature) return 0;

            var isPod = false;
            foreach (var t in tile.ThingsAll)
            {
                if (t is IWall) continue;
                if (t is IColonist c && c != colonist && !c.IsMoving) return 0;
                if (t is ISleepPod pod)
                {
                    if (!pod.CanAssignColonist(colonist.Id)) return 0;
                    isPod = true;
                    break;
                }
                if (!(t is IMoveableThing) && t.TileBlockModel != TileBlockModel.None) return 0;
            }

            var score = 100 - distance - (int)Math.Abs(temperature - 20);
            if (isPod) score -= 5;
            else
            {
                var light = RoomManager.GetTileLightLevel(tile.Index, false);
                if (light < 0.49f) score -= 10;
                if (tile.IsCorridor) score -= 15;
                if ((colonist as IColonist).GiveWayRequestTiles?.Contains(tile.Index) == true) score -= 20;
            }

            if (score > 0 && ColonistController.AIs.Values.Any(c => c.Colonist != colonist && c.CurrentActivity?.CurrentAction is ActionWalk aw && aw.Path?.EndPosition == tile.TerrainPosition))
            {
                // Another colonist going here
                score = 0;
            }

            return score;
        }

        private static int GetTileReturnHomeScore(IAnimal colonist, ISmallTile tile, int distance, int minTemperature)
        {
            if (!ZoneManager.HomeZone.ContainsNode(tile.Index)) return 0;
            var node = ZoneManager.HomeZone.Nodes[tile.Index];

            // More links = better (less likely to be cut off)
            return node.AllLinks.Count();
        }
    }
}
