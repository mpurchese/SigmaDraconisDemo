namespace SigmaDraconis.World.Buildings
{
    using ProtoBuf;
    using Language;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class HydrogenStorage : Building, ISilo
    {
        [ProtoMember(1)]
        protected ResourceContainer resourceContainer;

        [ProtoMember(2)]
        public SiloStatus SiloStatus { get; set; }

        [ProtoMember(3)]
        public bool IsSiloSwitchedOn { get; set; }

        public int StorageLevel => this.resourceContainer.StorageLevel;
        public int StorageCapacity => this.resourceContainer.StorageCapacity;

        private HydrogenStorage() : base(ThingType.HydrogenStorage)
        {
            this.canRecycleReasonStringId = StringsForMouseCursor.NotEmpty;
        }

        public HydrogenStorage(ISmallTile mainTile) : base(ThingType.HydrogenStorage, mainTile, 1)
        {
            this.canRecycleReasonStringId = StringsForMouseCursor.NotEmpty;
            this.resourceContainer = new ResourceContainer(Constants.HydrogenStorageCapacity);
        }

        public override void AfterConstructionComplete()
        {
            this.SiloStatus = SiloStatus.Online;
            this.IsSiloSwitchedOn = true;
            
            base.AfterConstructionComplete();
        }

        public override void Update()
        {
            if (this.IsDesignatedForRecycling) this.IsSiloSwitchedOn = false;

            if (this.resourceContainer.StorageLevel > this.resourceContainer.StorageCapacity * 2 / 3) this.AnimationFrame = 4;
            else if (this.resourceContainer.StorageLevel > this.resourceContainer.StorageCapacity / 2) this.AnimationFrame = 3;
            else if (this.resourceContainer.StorageLevel > 0) this.AnimationFrame = 2;
            else this.AnimationFrame = 1;

            if (this.IsSiloSwitchedOn) this.SiloStatus = SiloStatus.Online;
            else this.SiloStatus = this.resourceContainer.StorageLevel > 0 ? SiloStatus.WaitingToDistribute : SiloStatus.Offline;

            if (this.SiloStatus == SiloStatus.WaitingToDistribute)
            {
                var network = World.ResourceNetwork;
                if (network != null)
                {
                    foreach (var itemType in this.resourceContainer.GetStoredItemTypes())
                    {
                        if (network.CanAddItem(itemType, this))
                        {
                            network.AddItem(itemType, false);
                            this.resourceContainer.TakeItems(itemType, 1);
                            break;
                        }
                    }
                }
            }

            base.Update();
        }

        public override bool CanRecycle()
        {
            return base.CanRecycle() && this.resourceContainer.StorageLevel == 0;
        }

        public int CountItems(ItemType itemType)
        {
            return this.resourceContainer.GetItemTotal(itemType);
        }

        public bool CanAddItem(ItemType itemType)
        {
            return itemType == ItemType.LiquidFuel && this.SiloStatus == SiloStatus.Online && this.resourceContainer.StorageCapacity > this.resourceContainer.StorageLevel;
        }

        public void AddItem(ItemType itemType)
        {
            this.resourceContainer.AddItems(itemType, 1);
        }

        public bool CanTakeItems(ItemType itemType, int count = 1)
        {
            return this.resourceContainer.CanTakeItems(itemType, count);
        }

        public int TakeItems(ItemType itemType, int count = 1)
        {
            return this.resourceContainer.TakeItems(itemType, count);
        }

        public bool SwapItem(ItemType itemTypeToRemove, ItemType itemTypeToAdd)
        {
            return false;
        }
    }
}
