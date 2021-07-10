namespace SigmaDraconis.World.Buildings
{
    using ProtoBuf;
    using Language;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class WaterStorage : Building, ISilo
    {
        [ProtoMember(1)]
        protected ResourceContainer resourceContainer;

        [ProtoMember(4)]
        public SiloStatus SiloStatus { get; set; }

        [ProtoMember(5)]
        public bool IsSiloSwitchedOn { get; set; }

        public int StorageLevel => this.resourceContainer.StorageLevel;
        public int StorageCapacity => this.resourceContainer.StorageCapacity;

        private WaterStorage() : base(ThingType.ItemsStorage)
        {
            this.canRecycleReasonStringId = StringsForMouseCursor.NotEmpty;
        }

        public WaterStorage(ISmallTile mainTile) : base(ThingType.WaterStorage, mainTile, 1)
        {
            this.canRecycleReasonStringId = StringsForMouseCursor.NotEmpty;
            this.resourceContainer = new ResourceContainer(Constants.WaterStorageCapacity);
        }

        public override void AfterConstructionComplete()
        {
            this.SiloStatus = SiloStatus.Online;
            
            this.IsSiloSwitchedOn = true;
            base.AfterConstructionComplete();
        }

        public override void Update()
        {
            if (this.IsDesignatedForRecycling)
            {
                this.IsSiloSwitchedOn = false;
            }

            if (this.IsSiloSwitchedOn) this.SiloStatus = SiloStatus.Online;
            else this.SiloStatus = this.resourceContainer.StorageLevel > 0 ? SiloStatus.WaitingToDistribute : SiloStatus.Offline;

            if (this.SiloStatus == SiloStatus.WaitingToDistribute)
            {
                if (World.ResourceNetwork?.CanAddItem(ItemType.Water, this) == true)
                {
                    World.ResourceNetwork.AddItem(ItemType.Water, false);
                    this.resourceContainer.TakeItems(ItemType.Water, 1);
                }
            }

            this.AnimationFrame = (int)((this.resourceContainer.StorageLevel + 99) / 100f) + 1;

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
            return itemType == ItemType.Water && this.SiloStatus == SiloStatus.Online && this.resourceContainer.StorageCapacity > this.resourceContainer.StorageLevel;
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
