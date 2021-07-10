namespace SigmaDraconis.World.Buildings
{
    using Draconis.Shared;
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using Config;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class Cooker : FactoryBuilding, ICooker, IColonistInteractive, IRepairableThing, IResourceProviderBuilding, IEnergyConsumer
    {
        private static Energy energyStartup;

        private const int AutoCloseDelay = 600;
        private const int FillTime = 60;
        private FactoryStatus cookerStatus;
        private int animationDelay = 0;

        private static readonly Dictionary<int, int> cropAnimationFrames 
            = new Dictionary<int, int> { { 1, 11 }, { 2, 35 }, { 3, 41 }, { 4, 47 }, { 5, 53 }, { 100, 17 }, { 101, 29 }, { 102, 23 }, { 110, 59 }, { 111, 65 } };

        [ProtoMember(1)]
        public override FactoryStatus FactoryStatus
        { 
            get { return this.cookerStatus; }
            set
            {
                if (this.cookerStatus != value)
                {
                    this.cookerStatus = value;
                    // Whether the adjacent tiles are reserved as a access corridors depends on current status of the cooker
                    if (this.MainTile != null)
                    {
                        foreach (var tile in this.MainTile.AdjacentTiles4) tile.UpdateIsCorridor();
                    }
                }
            }
        }

        [ProtoMember(2)]
        public bool HasWater { get; private set; }

        [ProtoMember(3)]
        public int? CropType { get; private set; }

        [ProtoMember(7)]
        public int AutoCloseTimer { get; set; } = 0;

        [ProtoMember(8)]
        public int FillProgress { get; set; } = 0;

        [ProtoMember(9)]
        protected Dictionary<int, int> colonistsByAccessTile;

        public bool RequiresAccessNow => !this.FactoryStatus.In(FactoryStatus.Offline, FactoryStatus.NoPower, FactoryStatus.WaitingToDistribute, FactoryStatus.Broken);

        public bool IsReadyToCook => this.IsSwitchedOn && !FactoryStatus.In(FactoryStatus.NoPower, FactoryStatus.WaitingToDistribute, FactoryStatus.NoResource);

        // For deserialization
        private Cooker() : base()
        {
        }

        public Cooker(ISmallTile tile) : base(ThingType.Cooker, tile, 1)
        {
        }

        protected override void Init()
        {
            this.energyPerHour = Energy.FromKwH(Constants.CookerEnergyUse);
            this.energyPerFrame = this.energyPerHour / Constants.FramesPerHour;
            energyStartup = Energy.FromKwH(Constants.CookerMinStartEnergy);
            this.framesToProcess = Constants.CookerFramesToProcess;
            this.producedItemType = ItemType.Food;
            if (this.colonistsByAccessTile == null) this.colonistsByAccessTile = new Dictionary<int, int>();
            base.Init();
        }

        public override void AfterConstructionComplete()
        {
            base.AfterConstructionComplete();
            World.UpdateCanHarvestFruit();
        }

        public override void AfterDeserialization()
        {
            base.AfterDeserialization();
            World.UpdateCanHarvestFruit();
        }

        public override void Recycle()
        {
            base.Recycle();
            World.UpdateCanHarvestFruit();
        }

        public override void CancelRecycle()
        {
            base.CancelRecycle();
            World.UpdateCanHarvestFruit();
        }

        public override Energy UpdateFactory()
        {
            var network = World.ResourceNetwork;
            if (network == null) return 0;

            this.EnergyUseRate = 0;
            var energyUsed = (Energy)0;
            this.smokeSoundRate = 0;

            switch (this.FactoryStatus)
            {
                case FactoryStatus.Initialising:
                    this.FactoryStatus = this.IsSwitchedOn ? FactoryStatus.Standby : FactoryStatus.Offline;
                    break;
                case FactoryStatus.Offline:
                    if (this.IsSwitchedOn) this.FactoryStatus = this.MaintenanceLevel >= 0.1 ? FactoryStatus.Standby : FactoryStatus.Broken;
                    break;
                case FactoryStatus.Standby:
                    if (!this.IsSwitchedOn) this.FactoryStatus = FactoryStatus.Offline;
                    else if (this.MaintenanceLevel < 0.0001) this.FactoryStatus = FactoryStatus.Broken;
                    break;
                case FactoryStatus.Broken:
                    if (!this.IsSwitchedOn) this.FactoryStatus = FactoryStatus.Offline;
                    else if (this.MaintenanceLevel >= 0.1) this.FactoryStatus = FactoryStatus.Standby;
                    break;
                case FactoryStatus.Paused:
                    if (this.IsSwitchedOn) this.FactoryStatus = FactoryStatus.NoPower;
                    break;
                case FactoryStatus.NoPower:
                    if (!this.IsSwitchedOn) this.FactoryStatus = FactoryStatus.Paused;
                    else if (network.CanTakeEnergy(energyStartup) == true) this.FactoryStatus = this.HasWater ? FactoryStatus.InProgress : FactoryStatus.NoResource;
                    break;
                case FactoryStatus.NoResource:
                    if (!this.IsSwitchedOn) this.FactoryStatus = FactoryStatus.Paused;
                    else if (network.CanTakeItems(this, ItemType.Water, Constants.CookerWaterUse))
                    {
                        network.TakeItems(this, ItemType.Water, Constants.CookerWaterUse);
                        this.FactoryStatus = FactoryStatus.InProgress;
                        this.HasWater = true;
                    }
                    break;
                case FactoryStatus.Opening:
                    if (this.animationDelay == 0)
                    {
                        this.AnimationFrame++;
                        if (this.AnimationFrame == 10)
                        {
                            this.FactoryStatus = FactoryStatus.Open;
                            this.AutoCloseTimer = AutoCloseDelay;
                            this.AnimationFrame = 10;
                        }
                        else this.animationDelay = 2;
                    }
                    else this.animationDelay--;
                    break;
                case FactoryStatus.Open:
                    this.AutoCloseTimer--;
                    if (this.AutoCloseTimer == 0) this.FactoryStatus = FactoryStatus.Closing;
                    break;
                case FactoryStatus.Closing:
                    if (this.animationDelay == 0)
                    {
                        if (this.CropType.GetValueOrDefault() > 0)
                        {
                            var a = cropAnimationFrames[this.CropType.Value];
                            if (this.AnimationFrame == 10)
                            {
                                this.AnimationFrame = a;
                                this.animationDelay = 2;
                            }
                            else if (this.AnimationFrame == a + 5)
                            {
                                this.TryStart();
                            }
                            else
                            {
                                this.AnimationFrame++;
                                this.animationDelay = 2;
                            }
                        }
                        else
                        {
                            this.AnimationFrame--;
                            if (this.AnimationFrame == 5)
                            {
                                this.FactoryStatus = this.IsSwitchedOn ? FactoryStatus.Standby : FactoryStatus.Offline;
                                this.AnimationFrame = this.IsSwitchedOn ? 3 : 1;
                            }
                            else this.animationDelay = 2;
                        }
                    }
                    else this.animationDelay--;
                    break;
                case FactoryStatus.InProgress:
                    if (!this.IsSwitchedOn) this.FactoryStatus = FactoryStatus.Paused;
                    else if (network.CanTakeEnergy(energyPerFrame) == true)
                    {
                        network.TakeEnergy(energyPerFrame);
                        this.Process(1.0);
                        energyUsed = energyPerFrame;
                        this.EnergyUseRate = energyPerHour;
                    }
                    else this.FactoryStatus = FactoryStatus.NoPower;
                    break;
                case FactoryStatus.WaitingToDistribute:
                    this.TryDistribute();
                    break;
            }

            this.UpdateAnimationFrame();
            return energyUsed;
        }

        protected override void TryStart()
        {
            if (World.ResourceNetwork?.CanTakeItems(this, ItemType.Water, Constants.CookerWaterUse) == true)
            {
                World.ResourceNetwork.TakeItems(this, ItemType.Water, Constants.CookerWaterUse);
                this.FactoryStatus = FactoryStatus.InProgress;
                this.HasWater = true;
            }
            else
            {
                this.FactoryStatus = FactoryStatus.NoResource;
            }
        }

        protected override void CompleteProcessing()
        {
            this.FactoryProgress = 1.0;
            this.HasWater = false;
            this.pauseResumeFrameCounter = 0;
            this.FactoryStatus = FactoryStatus.WaitingToDistribute;
            this.InputItemType = ItemType.None;
            this.OutputItemType = ItemType.Food;
            if (this.CropType.GetValueOrDefault() > 0 && CropDefinitionManager.GetDefinition(this.CropType.Value) is CropDefinition def) this.OutputItemCount = def.HarvestYield; 
            else this.OutputItemCount = 1;
            World.AddFood(this.CropType.Value, this.OutputItemCount);
            this.TryDistribute();
        }

        protected override void TryDistribute()
        {
            if (World.ResourceNetwork?.CanAddItem(this.OutputItemType) == true)
            {
                World.ResourceNetwork.AddItem(this.OutputItemType);
                this.OutputItemCount--;
                if (this.OutputItemCount == 0)
                {
                    this.FactoryProgress = 0.0;
                    this.CropType = null;
                    this.OutputItemType = ItemType.None;
                    if (this.framesToBreak > 0 && this.MaintenanceLevel < 0.0001) this.FactoryStatus = FactoryStatus.Broken;
                    else this.FactoryStatus = this.IsSwitchedOn ? FactoryStatus.Standby : FactoryStatus.Offline;
                }
            }
        }

        public void Open()
        {
            this.FactoryStatus = FactoryStatus.Opening;
            this.AnimationFrame = 5;
            this.animationDelay = 2;
        }

        /// <summary>
        /// Colonist will have to call this repeatedly once per frame until it returns true
        /// </summary>
        /// <param name="cropType"></param>
        /// <returns></returns>
        public bool Fill(int? cropType)
        {
            this.FillProgress++;
            if (this.FillProgress == FillTime)
            {
                this.CropType = cropType;
                this.FillProgress = 0;
                this.AutoCloseTimer = 60;
                this.AnimationFrame = cropAnimationFrames.ContainsKey(this.CropType.GetValueOrDefault()) ? cropAnimationFrames[this.CropType.Value] : 10;
                return true;
            }

            return false;
        }

        protected override void UpdateAnimationFrame()
        {
            switch (this.FactoryStatus)
            {
                case FactoryStatus.Offline: this.AnimationFrame = 1; break;
                case FactoryStatus.Standby: this.AnimationFrame = 3; break;
                case FactoryStatus.InProgress: this.AnimationFrame = 4; break;
                case FactoryStatus.NoResource: this.AnimationFrame = 2; break;
                case FactoryStatus.WaitingToDistribute: this.AnimationFrame = 2; break;
                case FactoryStatus.NoPower: this.AnimationFrame = 1; break;
                case FactoryStatus.Broken: this.AnimationFrame = World.WorldTime.Minute % 2 == 0 ? 2 : 1; break;
            }
        }

        public IEnumerable<ISmallTile> GetAllAccessTiles()
        {
            for (int i = 4; i <= 7; i++)   // NE, SE, SW, NW
            {
                var direction = (Direction)i;
                var tile = this.mainTile.GetTileToDirection(direction);
                if (tile == null || !tile.CanWorkInTile || this.MainTile.HasWallToDirection(direction)) continue;   // Can't work here
                yield return tile;
            }
        }

        public IEnumerable<ISmallTile> GetAccessTiles(int? colonistId = null)
        {
            this.CleanupColonistAssignments();
            if (!this.IsReady || this.FactoryStatus.In(FactoryStatus.Offline, FactoryStatus.NoPower, FactoryStatus.WaitingToDistribute, FactoryStatus.Broken)) yield break;

            for (int i = 4; i <= 7; i++)   // NE, SE, SW, NW
            {
                var direction = (Direction)i;
                var tile = this.mainTile.GetTileToDirection(direction);
                if (tile == null || !tile.CanWorkInTile || this.MainTile.HasWallToDirection(direction)) continue;   // Can't work here
                if (tile.ThingsPrimary.Any(t => t is IColonist c && (colonistId == null || c.Id != colonistId) && !c.IsMoving && !c.IsRelaxing)) continue;   // Blocked by another colonist
                if (colonistId.HasValue && this.colonistsByAccessTile.ContainsKey(tile.Index) && this.colonistsByAccessTile[tile.Index] != colonistId) continue;  // Assigned to someone else
                yield return tile;
            }
        }

        public bool CanAssignColonist(int colonistId, int? tileIndex = null)
        {
            return tileIndex.HasValue
                ? this.GetAccessTiles(colonistId).Any(t => t.Index == tileIndex.Value)
                : this.GetAccessTiles(colonistId).Any();
        }

        public void AssignColonist(int colonistId, int tileIndex)
        {
            if (!this.colonistsByAccessTile.ContainsKey(tileIndex)) this.colonistsByAccessTile.Add(tileIndex, colonistId);
            else this.colonistsByAccessTile[tileIndex] = colonistId;
        }

        private void CleanupColonistAssignments()
        {
            if (this.colonistsByAccessTile == null) this.colonistsByAccessTile = new Dictionary<int, int>();
            foreach (var id in this.colonistsByAccessTile.Keys.ToList())
            {
                if (World.GetThing(this.colonistsByAccessTile[id]) is IColonist c && c.TargetBuilingID == this.Id) continue;
                this.colonistsByAccessTile.Remove(id);
            }
        }
    }
}
