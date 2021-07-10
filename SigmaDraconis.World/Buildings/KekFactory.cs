namespace SigmaDraconis.World.Buildings
{
    using Draconis.Shared;
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class KekFactory : FactoryBuilding, ICooker, IColonistInteractive, IRepairableThing, IResourceProviderBuilding, IEnergyConsumer, IRotatableThing
    {
        private static Energy energyStartup;

        private const int AutoCloseDelay = 600;
        private const int FillTime = 60;
        private FactoryStatus factoryStatus;
        private int animationDelay = 0;

        [ProtoMember(1)]
        public override FactoryStatus FactoryStatus
        { 
            get { return this.factoryStatus; }
            set
            {
                if (this.factoryStatus != value)
                {
                    this.factoryStatus = value;
                    // Whether the adjacent tiles are reserved as a access corridors depends on current status of the factory
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
        public bool HasKekke { get; private set; }

        [ProtoMember(4)]
        public Direction Direction { get; private set; }

        [ProtoMember(7)]
        public int AutoCloseTimer { get; set; } = 0;

        [ProtoMember(8)]
        public int FillProgress { get; set; } = 0;

        [ProtoMember(9)]
        protected Dictionary<int, int> colonistsByAccessTile;

        public bool RequiresAccessNow => !this.FactoryStatus.In(FactoryStatus.Offline, FactoryStatus.NoPower, FactoryStatus.WaitingToDistribute, FactoryStatus.Broken);

        public bool IsReadyToCook => this.IsSwitchedOn && !FactoryStatus.In(FactoryStatus.NoPower, FactoryStatus.WaitingToDistribute, FactoryStatus.NoResource);

        // For deserialization
        private KekFactory() : base()
        {
        }

        public KekFactory(ISmallTile tile, Direction direction) : base(ThingType.KekFactory, tile, 1)
        {
            this.Direction = direction;
        }

        protected override void Init()
        {
            this.energyPerHour = Energy.FromKwH(Constants.KekFactoryEnergyUse);
            this.energyPerFrame = this.energyPerHour / Constants.FramesPerHour;
            energyStartup = Energy.FromKwH(Constants.KekFactoryMinStartEnergy);
            this.framesToProcess = Constants.KekFactoryFramesToProcess;
            this.producedItemType = ItemType.Kek;
            if (this.colonistsByAccessTile == null) this.colonistsByAccessTile = new Dictionary<int, int>();
            base.Init();
        }

        public override Energy UpdateFactory()
        {
            this.EnergyUseRate = 0;
            var energyUsed = (Energy)0;
            this.smokeSoundRate = 0;

            var network = World.ResourceNetwork;
            if (network == null) return 0;

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
                    else if (network.CanTakeItems(this, ItemType.Water, Constants.KekFactoryWaterUse))
                    {
                        network.TakeItems(this, ItemType.Water, Constants.KekFactoryWaterUse);
                        this.FactoryStatus = FactoryStatus.InProgress;
                        this.HasWater = true;
                    }
                    break;
                case FactoryStatus.Opening:
                    if (this.animationDelay == 0)
                    {
                        this.AnimationFrame++;
                        if (this.AnimationFrame == 9)
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
                        if (this.HasKekke)
                        {
                            this.AnimationFrame++;
                            if (this.AnimationFrame == 14) this.TryStart();
                            else this.animationDelay = 2;
                        }
                        else
                        {
                            this.AnimationFrame--;
                            if (this.AnimationFrame == 3)
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
            if (World.ResourceNetwork?.CanTakeItems(this, ItemType.Water, Constants.KekFactoryWaterUse) == true)
            {
                World.ResourceNetwork.TakeItems(this, ItemType.Water, Constants.KekFactoryWaterUse);
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
            this.HasWater = false;
            this.HasKekke = false;
            base.CompleteProcessing();
        }

        protected override void TryDistribute()
        {
            if (World.ResourceNetwork?.CanAddItem(this.OutputItemType) == true)
            {
                World.ResourceNetwork.AddItem(this.OutputItemType);
                WorldStats.Increment(WorldStatKeys.KekProduced);
                this.OutputItemCount--;
                if (this.OutputItemCount == 0)
                {
                    this.FactoryProgress = 0.0;
                    this.HasKekke = false;
                    this.OutputItemType = ItemType.None;
                    if (this.framesToBreak > 0 && this.MaintenanceLevel < 0.0001) this.FactoryStatus = FactoryStatus.Broken;
                    else this.FactoryStatus = this.IsSwitchedOn ? FactoryStatus.Standby : FactoryStatus.Offline;
                }
            }
        }

        public void Open()
        {
            this.FactoryStatus = FactoryStatus.Opening;
            this.AnimationFrame = 4;
            this.animationDelay = 2;
        }

        /// <summary>
        /// Colonist will have to call this repeatedly once per frame until it returns true
        /// </summary>
        public bool Fill(int? cropType)
        {
            this.FillProgress++;
            if (this.FillProgress == FillTime)
            {
                this.FillProgress = 0;
                this.HasKekke = true;
                this.AutoCloseTimer = 60;
                this.AnimationFrame = 10;
                return true;
            }

            return false;
        }

        protected override void UpdateAnimationFrame()
        {
            switch (this.FactoryStatus)
            {
                case FactoryStatus.Offline: this.AnimationFrame = 1; break;
                case FactoryStatus.Standby: this.AnimationFrame = 2; break;
                case FactoryStatus.InProgress:
                    if (this.animationDelay == 0)
                    {
                        if (this.AnimationFrame <= 32 && this.AnimationFrame > 23 && this.FactoryProgress > 0.95)
                        {
                            this.AnimationFrame--;
                            if (this.AnimationFrame == 23) this.AnimationFrame = 2;
                        }
                        else if (this.AnimationFrame > 32 || this.FactoryProgress <= 0.95) this.AnimationFrame++;

                        this.animationDelay = 2;
                        if (this.AnimationFrame == 23 && this.FactoryProgress < 0.25) this.AnimationFrame = 14;
                        else if (this.AnimationFrame == 52) this.AnimationFrame = 32;
                    }
                    else this.animationDelay--;
                    break;
                case FactoryStatus.NoResource: this.AnimationFrame = 3; break;
                case FactoryStatus.WaitingToDistribute: this.AnimationFrame = 3; break;
                case FactoryStatus.NoPower: this.AnimationFrame = 1; break;
                case FactoryStatus.Broken: this.AnimationFrame = World.WorldTime.Minute % 2 == 0 ? 3 : 1; break;
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

        public override string GetTextureName(int layer = 1)
        {
            var root = base.GetTextureName();
            switch (this.Direction)
            {
                case Direction.SW:
                case Direction.NE: return $"{root}_SW";
                default: return $"{root}_SE";
            }
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
