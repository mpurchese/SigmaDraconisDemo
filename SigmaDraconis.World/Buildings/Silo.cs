namespace SigmaDraconis.World.Buildings
{
    using ProtoBuf;
    using Language;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class Silo : Building, ISilo
    {
        private int animationTimer;

        [ProtoMember(1)]
        protected ResourceContainer resourceContainer;

        [ProtoMember(4)]
        public SiloStatus SiloStatus { get; set; }

        [ProtoMember(5)]
        public bool IsSiloSwitchedOn { get; set; }

        public int StorageLevel => this.resourceContainer.StorageLevel;
        public int StorageCapacity => this.resourceContainer.StorageCapacity;

        private Silo() : base(ThingType.Silo)
        {
            this.canRecycleReasonStringId = StringsForMouseCursor.NotEmpty;
        }

        public Silo(ISmallTile mainTile) : base(ThingType.Silo, mainTile, 1)
        {
            this.canRecycleReasonStringId = StringsForMouseCursor.NotEmpty;
            this.resourceContainer = new ResourceContainer(Constants.SiloCapacity);
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
                if (World.ResourceNetwork != null)
                {
                    foreach (var itemType in this.resourceContainer.GetStoredItemTypes())
                    {
                        if (World.ResourceNetwork.CanAddItem(itemType, this))
                        {
                            World.ResourceNetwork.AddItem(itemType, false);
                            this.resourceContainer.TakeItems(itemType, 1);
                            break;
                        }
                    }
                }
            }

            var targetAnimationFrame = ((this.resourceContainer.StorageLevel + 3) / 4) * 8;
            if (targetAnimationFrame == 0) targetAnimationFrame = 1;
            if (this.AnimationFrame != targetAnimationFrame)
            {
                if (this.animationTimer == 0)
                {
                    this.AnimationFrame = this.AnimationFrame > targetAnimationFrame ? this.AnimationFrame - 1 : this.AnimationFrame + 1;
                    this.animationTimer = 3;
                }
                else this.animationTimer--;
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
            return Constants.StorageTypesByItemType.ContainsKey(itemType) && Constants.StorageTypesByItemType[itemType] == ThingType.Silo
                && this.SiloStatus == SiloStatus.Online && this.resourceContainer.StorageCapacity > this.resourceContainer.StorageLevel;
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
            if (Constants.StorageTypesByItemType[itemTypeToAdd] == ThingType.Silo
                && this.SiloStatus == SiloStatus.Online
                && this.CanTakeItems(itemTypeToRemove))
            {
                this.resourceContainer.TakeItems(itemTypeToRemove, 1);
                this.resourceContainer.AddItems(itemTypeToAdd, 1);
                return true;
            }

            return false;
        }
    }
}
