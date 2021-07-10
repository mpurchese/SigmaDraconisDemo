namespace SigmaDraconis.World.Buildings
{
    using Draconis.Shared;
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using Cards.Interface;
    using Config;
    using Language;
    using Projects;
    using ResourceNetworks;
    using Rooms;
    using WorldInterfaces;
    using Shared;

    [ProtoContract]
    public class Planter : Building, IPlanter, IColonistInteractive
    {
        private float? jobProgress;

        protected CropDefinition cropDefinition;
        protected PlanterStatus planterStatus;

        [ProtoMember(1)]
        public int SelectedCropTypeId { get; protected set; }

        [ProtoMember(2)]
        public double? Progress { get; protected set; }

        [ProtoMember(3)]
        public double? GrowthRate { get; protected set; }

        [ProtoMember(4)]
        public double? Health { get; protected set; }

        [ProtoMember(5)]
        public PlanterStatus PlanterStatus
        {
            get { return this.planterStatus; }
            protected set
            {
                if (this.planterStatus != value)
                {
                    this.planterStatus = value;
                    // Whether the adjacent tiles are reserved as a access corridors depends on current status of the planter
                    if (this.MainTile != null) foreach (var tile in this.MainTile.AdjacentTiles4) tile.UpdateIsCorridor();
                }
            }
        }

        [ProtoMember(6)]
        public int CurrentCropTypeId { get; protected set; }

        [ProtoMember(7)]
        public int RemainingCrop { get; protected set; }

        [ProtoMember(8)]
        public bool IsTooHot { get; protected set; }

        [ProtoMember(9)]
        public bool IsTooCold { get; protected set; }

        [ProtoMember(10)]
        public bool IsTooDark { get; protected set; }

        [ProtoMember(11)]
        protected Dictionary<int, int> colonistsByAccessTile;

        [ProtoMember(12)]
        public float? JobProgress
        {
            get
            {
                return this.jobProgress;
            }
            set
            {
                if (this.jobProgress != value)
                {
                    if (this.mainTile != null) EventManager.EnqueueWorldPropertyChangeEvent(this.Id, nameof(this.JobProgress), this.jobProgress, value, this.mainTile.Row, this.ThingType);
                    this.jobProgress = value;
                }
            }
        }

        [ProtoMember(13)]
        public bool RemoveCrop { get; protected set; }

        [ProtoMember(14)]
        public WorkPriority FarmPriority { get; set; }

        [ProtoMember(15)]
        public Dictionary<string, int> GrowthRateModifiers { get; private set; } = new Dictionary<string, int>();

        [ProtoMember(16)]
        public int WaterUseRate { get; protected set; }

        [ProtoMember(17)]
        protected int framesSinceWaterUse;

        [ProtoMember(18)]
        public bool HasWater { get; protected set; }

        public Dictionary<CardType, int> WorkRateEffects { get; private set; }


        public bool RequiresAccessNow => this.PlanterStatus.In(PlanterStatus.WaitingForSeeds, PlanterStatus.WaitingToHarvest, PlanterStatus.Dead);

        private static int currentLanguageId;
        private static string languageStrLight;
        private static string languageStrTemperature;

        // For deserialization
        public Planter() : base(ThingType.None)
        {
            if (this.colonistsByAccessTile == null) this.colonistsByAccessTile = new Dictionary<int, int>();
            GetLanguageStrings();
        }

        public Planter(ISmallTile tile, ThingType thingType) : base(thingType, tile, 1)
        {
            this.colonistsByAccessTile = new Dictionary<int, int>();
            this.FarmPriority = WorkPriority.High;
            GetLanguageStrings();
        }

        private static void GetLanguageStrings()
        {
            if (!string.IsNullOrEmpty(languageStrLight) && currentLanguageId == LanguageManager.CurrentLanguageId) return;

            currentLanguageId = LanguageManager.CurrentLanguageId;
            languageStrLight = LanguageManager.Get<StringsForThingPanels>(StringsForThingPanels.Light);
            languageStrTemperature = LanguageManager.Get<StringsForThingPanels>(StringsForThingPanels.Temperature);
        }

        public override void AfterDeserialization()
        {
            // Temporary fix for bug in version 0.1
            if ((this.ThingType == ThingType.PlanterHydroponics && this.CurrentCropTypeId == 5) || (this.ThingType == ThingType.PlanterStone && this.CurrentCropTypeId == 4))
            {
                this.CurrentCropTypeId = 0;
                this.SelectedCropTypeId = 0;
                this.Progress = 0;
                this.PlanterStatus = PlanterStatus.SelectCrop;
                this.AnimationFrame = this.GetAnimationFrame();
            }

            base.AfterDeserialization();
        }

        public override void AfterConstructionComplete()
        {
            this.SetCrop(World.DefaultCropId);
            this.PlanterStatus = World.DefaultCropId > 0 ? PlanterStatus.WaitingForSeeds : PlanterStatus.SelectCrop;

            base.AfterConstructionComplete();
            World.UpdateCanFarm();
        }

        public override void AfterAddedToWorld()
        {
            if (this.IsReady)
            { 
                if (this.GrowthRateModifiers == null) this.GrowthRateModifiers = new Dictionary<string, int>();
                this.SetCrop(this.SelectedCropTypeId);
            }

            World.UpdateCanFarm();
            base.AfterAddedToWorld();
        }

        public void SetCrop(int cropTypeId, bool replaceExisting = false)
        {
            if (cropTypeId > 0)
            {
                var definition = CropDefinitionManager.GetDefinition(cropTypeId);
                if (this.ThingType == ThingType.PlanterHydroponics && !definition.CanGrowHydroponics) cropTypeId = 0;
                else if (this.ThingType == ThingType.PlanterStone && !definition.CanGrowSoil) cropTypeId = 0;
            }

            this.RemoveCrop = replaceExisting;
            if (this.jobProgress > 0 && this.PlanterStatus == PlanterStatus.WaitingForSeeds && this.SelectedCropTypeId != cropTypeId) this.JobProgress = 0;
            this.SelectedCropTypeId = cropTypeId;
            EventManager.EnqueueWorldPropertyChangeEvent(this.Id, nameof(this.SelectedCropTypeId), this.MainTile.Row, this.ThingType);
            if (this.PlanterStatus == PlanterStatus.SelectCrop && this.SelectedCropTypeId > 0) this.PlanterStatus = PlanterStatus.WaitingForSeeds;
            else if (this.PlanterStatus == PlanterStatus.WaitingForSeeds && this.SelectedCropTypeId == 0) this.PlanterStatus = PlanterStatus.SelectCrop;
        }

        public virtual bool DoJob(double workSpeed, Dictionary<CardType, int> effects = null)
        {
            this.WorkRateEffects = effects;

            if (!this.JobProgress.HasValue)
            {
                this.JobProgress = 0;
            }
            else if (this.JobProgress < 1f)
            {
                if (workSpeed < 0.1) workSpeed = 0.1;
                this.JobProgress += (float)workSpeed / 600f;
            }
            else if (this.PlanterStatus == PlanterStatus.WaitingForSeeds && this.SelectedCropTypeId > 0)
            {
                this.JobProgress = null;
                this.PlanterStatus = PlanterStatus.InProgress;
                this.Health = 1.0;
                this.Progress = 0.0;
                this.GrowthRate = 0.0;
                this.HasWater = true;
                this.framesSinceWaterUse = 0;
                this.GrowthRateModifiers.Clear();
                this.CurrentCropTypeId = this.SelectedCropTypeId;
                this.RemoveCrop = false;
                this.cropDefinition = this.CurrentCropTypeId > 0 ? CropDefinitionManager.GetDefinition(this.CurrentCropTypeId) : null;
                this.AnimationFrame = this.GetAnimationFrame();
                
                EventManager.EnqueueWorldPropertyChangeEvent(this.Id, nameof(this.CurrentCropTypeId), this.MainTile.Row, this.ThingType);

                return true;
            }
            else if (this.PlanterStatus == PlanterStatus.WaitingToHarvest && this.RemainingCrop == 1)
            {
                this.JobProgress = null;
                this.RemainingCrop = 0;
                this.Health = 0;
                this.Progress = 0;
                this.CurrentCropTypeId = 0;
                this.cropDefinition = null;
                this.AnimationFrame = 1;
                this.RemoveCrop = false;
                EventManager.EnqueueWorldPropertyChangeEvent(this.Id, nameof(this.CurrentCropTypeId), this.MainTile.Row, this.ThingType);
                if (this.SelectedCropTypeId == 0) this.PlanterStatus = PlanterStatus.SelectCrop;
                else this.PlanterStatus = PlanterStatus.WaitingForSeeds;
                return true;
            }
            else if (this.PlanterStatus == PlanterStatus.Dead || this.RemoveCrop)
            {
                this.JobProgress = null;
                this.RemainingCrop = 0;
                this.Health = 0;
                this.Progress = 0;
                this.AnimationFrame = 1;
                this.CurrentCropTypeId = 0;
                this.cropDefinition = null;
                this.RemoveCrop = false;
                EventManager.EnqueueWorldPropertyChangeEvent(this.Id, nameof(this.CurrentCropTypeId), this.MainTile.Row, this.ThingType);
                if (this.SelectedCropTypeId == 0) this.PlanterStatus = PlanterStatus.SelectCrop;
                else this.PlanterStatus = PlanterStatus.WaitingForSeeds;
            }

            return false;
        }

        public virtual void UpdatePlanter()
        {
            if (currentLanguageId != LanguageManager.CurrentLanguageId) GetLanguageStrings();

            if (this.planterStatus == PlanterStatus.InProgress || this.planterStatus == PlanterStatus.WaitingToHarvest)
            {
                if (this.cropDefinition == null && this.CurrentCropTypeId > 0) this.cropDefinition = CropDefinitionManager.GetDefinition(this.CurrentCropTypeId);
                if (this.cropDefinition == null)
                {
                    this.CurrentCropTypeId = 0;
                    this.PlanterStatus = this.SelectedCropTypeId > 0 ? PlanterStatus.WaitingForSeeds : PlanterStatus.SelectCrop;
                    return;
                }

                var light = WorldLight.GetEffectiveLight(RoomManager.GetTileLightLevel(this.MainTileIndex));
                var temperature = RoomManager.GetTileTemperature(this.MainTileIndex);

                var minTemp = (double)this.cropDefinition.MinTemp;
                var minGoodTemp = (double)this.cropDefinition.MinGoodTemp;
                var maxGoodTemp = (double)this.cropDefinition.MaxGoodTemp;
                var maxTemp = (double)this.cropDefinition.MaxTemp;

                this.framesSinceWaterUse++;
                var useRate = this.ThingType == ThingType.PlanterHydroponics ? 40 : 20;
                this.HasWater = this.framesSinceWaterUse < 3600 / useRate;
                if (!this.HasWater && World.ResourceNetwork?.CanTakeItems(this, ItemType.Water, 1) == true)
                {
                    World.ResourceNetwork.TakeItems(this, ItemType.Water, 1);
                    this.framesSinceWaterUse = 0;
                    this.HasWater = true;
                }

                this.WaterUseRate = this.HasWater ? useRate : 0;

                this.IsTooHot = temperature > maxTemp;
                this.IsTooCold = temperature < minTemp;
                this.IsTooDark = light <= 0.0;
                this.GrowthRate = 0;
                this.GrowthRateModifiers.Clear();
                if (this.IsTooHot || this.IsTooCold || !this.HasWater) this.Health -= 1 / 12800.0;

                if (this.Health > 0)
                {
                    if (this.PlanterStatus == PlanterStatus.InProgress && !this.IsTooCold && !this.IsTooHot && !this.IsTooDark && this.HasWater)
                    {
                        var temperatureEffect = 0.0;
                        if (temperature >= maxGoodTemp && temperature < maxTemp) temperatureEffect = (maxTemp - temperature) / (maxTemp - maxGoodTemp);
                        else if (temperature >= minGoodTemp && temperature < maxGoodTemp) temperatureEffect = 1.0;
                        else if (temperature < minGoodTemp && temperature >= minTemp) temperatureEffect = (temperature - minTemp) / (minGoodTemp - minTemp);

                        var projectsEffect = 1.0;

                        this.GrowthRateModifiers.Add(languageStrLight, (int)(light * 100));
                        this.GrowthRateModifiers.Add(languageStrTemperature, (int)(temperatureEffect * 100));

                        if (this.ThingType == ThingType.PlanterHydroponics)
                        {
                            var improvedHydroponicsDef = ProjectManager.GetDefinition(7);
                            if (improvedHydroponicsDef?.IsDone == true)
                            {
                                this.GrowthRateModifiers.Add(improvedHydroponicsDef.DisplayName, 115);
                                projectsEffect = 1.15;
                            }
                        }
                        else //if (this.ThingType == ThingType.PlanterStone)
                        {
                            var improvedCompostDef = ProjectManager.GetDefinition(10);
                            if (improvedCompostDef?.IsDone == true)
                            {
                                this.GrowthRateModifiers.Add(improvedCompostDef.DisplayName, 115);
                                projectsEffect = 1.15;
                            }
                        }

                        this.GrowthRate = light * temperatureEffect * projectsEffect;
                        if (this.GrowthRate > 0.0)
                        {
                            this.Progress += this.GrowthRate / (3600.0 * this.cropDefinition.HoursToGrow);
                            if (this.Progress >= 1.0)
                            {
                                this.Progress = 1.0;
                                this.RemainingCrop = 1;
                                this.PlanterStatus = PlanterStatus.WaitingToHarvest;
                            }
                        }
                    }
                    else if (this.PlanterStatus == PlanterStatus.WaitingToHarvest)
                    {
                        this.Health -= 1 / 43200.0;
                    }
                }
                else
                {
                    this.RemainingCrop = 0;
                    this.Health = 0;
                    this.Progress = 0;
                    this.PlanterStatus = PlanterStatus.Dead;
                }
            }
            else this.WaterUseRate = 0;

            this.AnimationFrame = this.GetAnimationFrame();
            this.CleanupColonistAssignments();
        }

        private int GetAnimationFrame()
        {
            switch (this.PlanterStatus)
            {
                case PlanterStatus.InProgress:
                    return 2 + (int)(this.Progress * 4.0) + ((this.CurrentCropTypeId - 1) * 10);
                case PlanterStatus.Dead:
                    return 7 + (int)(this.Progress * 4.0) + ((this.CurrentCropTypeId - 1) * 10);
                case PlanterStatus.WaitingToHarvest:
                    return 6 + ((this.CurrentCropTypeId - 1) * 10);
            }

            return 1;
        }

        public IEnumerable<ISmallTile> GetAllAccessTiles()
        {
            for (int i = 4; i <= 7; i++)   // NE, SE, SW, NW
            {
                var direction = (Direction)i;
                var tile = this.mainTile.GetTileToDirection(direction);
                // Use CanPickupFromTile not CanWorkInTile, as want to be able to work around lampposts etc.
                if (tile == null || !tile.CanPickupFromTile || this.MainTile.HasWallToDirection(direction)) continue;   // Can't work here
                yield return tile;
            }
        }

        public IEnumerable<ISmallTile> GetAccessTiles(int? colonistId = null)
        {
            if (!this.IsReady || (!this.PlanterStatus.In(PlanterStatus.WaitingForSeeds, PlanterStatus.WaitingToHarvest, PlanterStatus.Dead) && !this.RemoveCrop)) yield break;

            if (this.planterStatus == PlanterStatus.WaitingForSeeds)
            {
                if (this.SelectedCropTypeId == 0) yield break;
                var definition = CropDefinitionManager.GetDefinition(this.SelectedCropTypeId);
                if (definition == null) yield break;
                var temperature = RoomManager.GetTileTemperature(this.MainTileIndex);
                if (temperature < definition.MinTemp || temperature > definition.MaxTemp) yield break;
            }

            var result = new List<ISmallTile>(4);
            for (int i = 4; i <= 7; i++)   // NE, SE, SW, NW
            {
                var direction = (Direction)i;
                var tile = this.mainTile.GetTileToDirection(direction);
                if (tile == null || !tile.CanPickupFromTile || this.MainTile.HasWallToDirection(direction)) continue;   // Can't work here
                if (tile.ThingsPrimary.Any(t => t is IColonist c && (colonistId == null || c.Id != colonistId) && !c.IsMoving && !c.IsRelaxing)) continue;   // Blocked by another colonist
                if (colonistId.HasValue && this.colonistsByAccessTile.ContainsKey(tile.Index) && this.colonistsByAccessTile[tile.Index] != colonistId) continue;  // Assigned to someone else
                yield return tile;
            }
        }

        public bool CanAssignColonist(int colonistId, int? tileIndex = null)
        {
            this.CleanupColonistAssignments();
            if (this.colonistsByAccessTile.Values.Any(v => v != colonistId)) return false;  // Another colonist assigned

            return tileIndex.HasValue
                ? this.GetAccessTiles(colonistId).Any(t => t.Index == tileIndex.Value)
                : this.GetAccessTiles(colonistId).Any();
        }

        public void AssignColonist(int colonistId, int tileIndex)
        {
            if (!this.colonistsByAccessTile.ContainsKey(tileIndex)) this.colonistsByAccessTile.Add(tileIndex, colonistId);
            else this.colonistsByAccessTile[tileIndex] = colonistId;
        }

        protected void CleanupColonistAssignments()
        {
            // Clean up colonist assignments
            var stillWorking = this.planterStatus == PlanterStatus.WaitingForSeeds || this.planterStatus == PlanterStatus.WaitingToHarvest || this.planterStatus == PlanterStatus.Dead;
            foreach (var id in this.colonistsByAccessTile.Keys.ToList())
            {
                if (World.GetThing(this.colonistsByAccessTile[id]) is IColonist c && c.ActivityType == ColonistActivityType.Farm)
                {
                    stillWorking = true;
                    continue;
                }

                this.colonistsByAccessTile.Remove(id);
            }

            if (!stillWorking) this.JobProgress = null;
        }
    }
}
