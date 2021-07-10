namespace SigmaDraconis.World.Buildings
{
    using ProtoBuf;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class StoneFurnace : FactoryBuilding, IResourceProviderBuilding, IResourceConsumerBuilding, IRotatableThing, IRepairableThing
    {
        [ProtoMember(1, IsRequired = true)]
        public bool HasCoal { get; protected set; }

        [ProtoMember(2, IsRequired = true)]
        public bool HasOre { get; protected set; }

        [ProtoMember(3)]
        public Direction Direction { get; private set; }

        public StoneFurnace() : base()
        {
        }

        public StoneFurnace(ISmallTile tile, Direction direction) : base(ThingType.StoneFurnace, tile, 1)
        {
            this.Direction = direction;
        }

        protected override void Init()
        {
            this.framesToInitialise = Constants.StoneFurnaceFramesToInitialise;
            this.framesToProcess = Constants.StoneFurnaceFramesToProcess;
            this.framesToPauseResume = Constants.StoneFurnaceFramesToPauseResume;
            this.producedItemType = ItemType.Metal;
            base.Init();
        }

        protected override void UpdateAnimationFrame()
        {
            this.AnimationFrame = this.IsSwitchedOn && (this.FactoryStatus == FactoryStatus.InProgress || this.FactoryStatus == FactoryStatus.Pausing || this.FactoryStatus == FactoryStatus.Resuming) ? 2 : 1;
        }

        public override bool CanAddInput(ItemType itemType)
        {
            var network = World.ResourceNetwork;
            if (network == null) return false;

            if ((itemType != ItemType.Coal && itemType != ItemType.IronOre) || !this.IsSwitchedOn || this.FactoryStatus != FactoryStatus.Standby) return false;
            if (this.InventoryTarget.HasValue && network.GetItemTotal(this.producedItemType) >= this.InventoryTarget.Value) return false;

            if (itemType == ItemType.Coal) return !this.HasCoal && (this.HasOre || network.CanTakeItems(this, ItemType.IronOre, 1));

            return !this.HasOre && (this.HasCoal || network.CanTakeItems(this, ItemType.Coal, 1));
        }

        public override void AddInput(ItemType itemType)
        {
            if (itemType == ItemType.Coal) this.HasCoal = true;
            else if (itemType == ItemType.IronOre) this.HasOre = true;
        }

        public override string GetTextureName(int layer = 1)
        {
            return $"{base.GetTextureName()}_{this.Direction.ToString()}";
        }

        protected override void TryStart()
        {
            var network = World.ResourceNetwork;
            if (network == null) return;

            this.FactoryProgress = 0.0;

            if (this.InventoryTarget.HasValue && network.GetItemTotal(this.producedItemType) >= this.InventoryTarget.Value)
            {
                this.FactoryStatus = FactoryStatus.Standby;
                return;
            }

            var hasOre = this.HasOre || network.CanTakeItems(this, ItemType.IronOre, 1);
            var hasCoal = this.HasCoal || network.CanTakeItems(this, ItemType.Coal, 1);
            if (hasOre && hasCoal)
            {
                if (!this.HasOre) network.TakeItems(this, ItemType.IronOre, 1);
                if (!this.HasCoal) network.TakeItems(this, ItemType.Coal, 1);
                this.FactoryStatus = FactoryStatus.InProgress;
                this.HasCoal = false;
                this.HasOre = false;
            }
            else
            {
                this.FactoryStatus = FactoryStatus.Standby;
            }
        }

        protected override void TryDistribute()
        {
            var network = World.ResourceNetwork;
            if (network == null) return;

            var itemCount = this.OutputItemCount;
            base.TryDistribute();
            if (this.OutputItemCount < itemCount) WorldStats.Increment(WorldStatKeys.MetalSmeltedStoneFurnace, itemCount - this.OutputItemCount);

            if (this.FactoryStatus == FactoryStatus.WaitingToDistribute && this.IsSwitchedOn && this.IsAutoRestartEnabled && this.MaintenanceLevel >= 0.0001)
            {
                if (network.CanTakeItems(this, ItemType.Coal, 1) && network.SwapItems(ItemType.Metal, ItemType.IronOre) == true)
                {
                    network.TakeItems(this, ItemType.Coal, 1);
                    this.HasCoal = true;
                    this.HasOre = true;
                    this.OutputItemType = ItemType.None;
                    this.OutputItemCount = 0;
                    this.FactoryProgress = 0.0;
                    this.TryStart();
                }
                else if (network.CanTakeItems(this, ItemType.IronOre, 1) && network.SwapItems( ItemType.Metal, ItemType.Coal) == true)
                {
                    network.TakeItems(this, ItemType.IronOre, 1);
                    this.HasCoal = true;
                    this.HasOre = true;
                    this.OutputItemType = ItemType.None;
                    this.OutputItemCount = 0;
                    this.FactoryProgress = 0.0;
                    this.TryStart();
                }
            }

        }
    }
}
