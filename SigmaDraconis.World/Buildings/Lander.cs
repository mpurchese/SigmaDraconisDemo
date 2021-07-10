namespace SigmaDraconis.World.Buildings
{
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using Draconis.Shared;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class Lander : Building, ILander
    {
        [ProtoMember(1)]
        private readonly HashSet<int> connectedConduits;

        public IEnumerable<IBuildableThing> ConnectedConduits => this.connectedConduits.Select(c => World.GetThing(c)).OfType<IBuildableThing>();

        [ProtoMember(3)]
        public float ConstructionBotProgress { get; protected set; }

        [ProtoMember(4)]
        public Energy EnergyGenRate { get; set; }

        [ProtoMember(7)]
        public int NoPowerTimer { get; set; }

        protected int timerCount = 0;

        #region IEnergyGenerator implementation

        [ProtoMember(12)]
        public FactoryStatus FactoryStatus { get; set; }

        [ProtoMember(13)]
        public double FactoryProgress { get; protected set; }

        [ProtoMember(14)]
        public bool IsGeneratorSwitchedOn { get; set; }

        #endregion

        #region IBattery implementation

        [ProtoMember(20)]
        public Energy ChargeLevel { get; set; }

        public Energy ChargeCapacity { get { return Energy.FromKwH(Constants.LanderEnergyStorage); } }

        #endregion

        #region ISilo implementation

        [ProtoMember(22)]
        protected ResourceContainer resourceContainer;

        [ProtoMember(23)]
        public SiloStatus SiloStatus { get; set; } = SiloStatus.Online;

        [ProtoMember(24)]
        public bool IsSiloSwitchedOn { get; set; } = true;

        #endregion

        [ProtoMember(25)]
        protected Dictionary<int, int> colonistsByAccessTile;

        [ProtoMember(26)]
        public ResourceContainer FoodContainer { get; private set; }

        [ProtoMember(27)]
        public ResourceContainer ItemsContainer { get; private set; }

        public int StorageLevel => this.resourceContainer.StorageLevel;
        public int StorageCapacity => this.resourceContainer.StorageCapacity;

        public Lander() : base(ThingType.Lander)
        {
            if (this.colonistsByAccessTile == null) this.colonistsByAccessTile = new Dictionary<int, int>();
            if (this.connectedConduits == null) this.connectedConduits = new HashSet<int>();
        }

        public Lander(ISmallTile mainTile) : base(ThingType.Lander, mainTile, 1)
        {
            if (this.colonistsByAccessTile == null) this.colonistsByAccessTile = new Dictionary<int, int>();
            if (this.connectedConduits == null) this.connectedConduits = new HashSet<int>();
            this.resourceContainer = new ResourceContainer(Constants.LanderResourceCapacity);
            this.FoodContainer = new ResourceContainer(Constants.LanderFoodCapacity);
            this.ItemsContainer = new ResourceContainer(Constants.LanderItemsCapacity);
        }

        public override void AfterDeserialization()
        {
            base.AfterDeserialization();

            // Added for v0.4
            if (this.FoodContainer == null) this.FoodContainer = new ResourceContainer(Constants.LanderFoodCapacity);
            if (this.ItemsContainer == null) this.ItemsContainer = new ResourceContainer(Constants.LanderItemsCapacity);
        }

        public void ConnectConduit(IBuildableThing conduit)
        {
            if (!this.connectedConduits.Contains(conduit.Id)) this.connectedConduits.Add(conduit.Id);
        }

        public void DisconnectConduit(IBuildableThing conduit)
        {
            if (this.connectedConduits.Contains(conduit.Id)) this.connectedConduits.Remove(conduit.Id);
        }

        public override void AfterAddedToWorld()
        {
            World.ExpandBuildableAreaAroundTile(this.mainTile);
            base.AfterAddedToWorld();
        }

        public Energy UpdateGenerator()
        {
            var dayFraction = World.WorldTime.DayFraction;
            if (dayFraction <= 0.25 || dayFraction >= 0.75) return 0;
            this.EnergyGenRate = Energy.FromKwH(Constants.LanderSolarPanelEnergyProduction * Mathf.Sin((dayFraction - 0.25f) * 2 * Mathf.PI));
            return this.EnergyGenRate / 3600f;
        }

        public int CountItems(ItemType itemType)
        {
            if (!Constants.StorageTypesByItemType.ContainsKey(itemType)) return 0;

            var storageType = Constants.StorageTypesByItemType[itemType];
            switch (storageType)
            {
                case ThingType.Silo: return this.resourceContainer.GetItemTotal(itemType);
                case ThingType.ItemsStorage: return this.ItemsContainer.GetItemTotal(itemType);
                case ThingType.FoodStorage: return this.FoodContainer.GetItemTotal(itemType);
            }

            return 0;
        }

        public bool CanAddItem(ItemType itemType)
        {
            if (!Constants.StorageTypesByItemType.ContainsKey(itemType)) return false;

            var storageType = Constants.StorageTypesByItemType[itemType];
            switch (storageType)
            {
                case ThingType.Silo: return this.resourceContainer.StorageCapacity > this.resourceContainer.StorageLevel;
                case ThingType.ItemsStorage: return this.ItemsContainer.StorageCapacity > this.ItemsContainer.StorageLevel;
                case ThingType.FoodStorage: return this.FoodContainer.StorageCapacity > this.FoodContainer.StorageLevel;
            }

            return false;
        }

        public void AddItem(ItemType itemType)
        {
            var storageType = Constants.StorageTypesByItemType[itemType];
            switch(storageType)
            {
                case ThingType.Silo: this.resourceContainer.AddItems(itemType, 1); break;
                case ThingType.ItemsStorage: this.ItemsContainer.AddItems(itemType, 1); break;
                case ThingType.FoodStorage: this.FoodContainer.AddItems(itemType, 1); break;
            }
        }

        public bool CanTakeItems(ItemType itemType, int count = 1)
        {
            if (!Constants.StorageTypesByItemType.ContainsKey(itemType)) return false;

            var storageType = Constants.StorageTypesByItemType[itemType];
            switch (storageType)
            {
                case ThingType.Silo: return this.resourceContainer.CanTakeItems(itemType, count);
                case ThingType.ItemsStorage: return this.ItemsContainer.CanTakeItems(itemType, count);
                case ThingType.FoodStorage: return this.FoodContainer.CanTakeItems(itemType, count);
            }

            return false;
        }

        public int TakeItems(ItemType itemType, int count = 1)
        {
            var storageType = Constants.StorageTypesByItemType[itemType];
            switch (storageType)
            {
                case ThingType.Silo: return this.resourceContainer.TakeItems(itemType, count);
                case ThingType.ItemsStorage: return this.ItemsContainer.TakeItems(itemType, count);
                case ThingType.FoodStorage: return this.FoodContainer.TakeItems(itemType, count);
            }

            return 0;
        }

        public bool SwapItem(ItemType itemTypeToRemove, ItemType itemTypeToAdd)
        {
            // Can only swap items with the same silo type
            if (this.SiloStatus != SiloStatus.Online || !this.CanTakeItems(itemTypeToRemove)
                || Constants.StorageTypesByItemType[itemTypeToAdd] != Constants.StorageTypesByItemType[itemTypeToRemove])
            {
                return false;
            }

            this.TakeItems(itemTypeToRemove);
            this.AddItem(itemTypeToAdd);
            return true;
        }

        public bool IsConnectedToLander(int? excludedNode)
        {
            return true;
        }
    }
}
