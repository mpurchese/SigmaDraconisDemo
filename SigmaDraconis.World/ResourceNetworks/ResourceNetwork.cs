namespace SigmaDraconis.World.ResourceNetworks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Shared;
    using WorldInterfaces;
    using Zones;

    public class ResourceNetwork
    {
        #region Fields

        private static int nextId = 0;

        private bool isZoneInvalidated = true;
        private bool isConnectionsInvalidated = true;

        private readonly ResourceNetworkZone zone = new ResourceNetworkZone();

        private Energy energyCache = 0;         // Distributes to batteries if unused at end of frame

        private readonly List<IBattery> batteries = new List<IBattery>();
        private readonly List<IDispenser> dispensers = new List<IDispenser>();
        private readonly List<IEnergyGenerator> generators = new List<IEnergyGenerator>();
        private readonly List<IEnergyConsumer> energyConsumers = new List<IEnergyConsumer>();
        private readonly List<ISilo> silos = new List<ISilo>();
        private readonly List<IResourceConsumerBuilding> resourceConsumers = new List<IResourceConsumerBuilding>();
        private readonly List<IResourceProviderBuilding> resourceProviders = new List<IResourceProviderBuilding>();
        private readonly List<IWaterProviderBuilding> waterProviders = new List<IWaterProviderBuilding>();
        private readonly List<IEnvironmentControl> roomControls = new List<IEnvironmentControl>();
        private readonly List<ISleepPod> sleepPods = new List<ISleepPod>();
        private readonly List<IPlanter> planters = new List<IPlanter>();
        private readonly List<IWaterConsumer> waterConsumers = new List<IWaterConsumer>();
        private readonly List<ILab> labs = new List<ILab>();
        private readonly List<ILamp> lamps = new List<ILamp>();
        private readonly List<IHeater> heaters = new List<IHeater>();
        private readonly List<IOreScanner> oreScanners = new List<IOreScanner>();

        private readonly List<SmoothedWaterConsumer> smoothedWaterConsumers = new List<SmoothedWaterConsumer>();

        private readonly Dictionary<IBuildableThing, List<ISilo>> buildingSiloConnections = new Dictionary<IBuildableThing, List<ISilo>>();

        private readonly Dictionary<ItemType, int> itemTotals = new Dictionary<ItemType, int>();

        #endregion Fields
        #region Public Properties

        public int Id { get; }

        public Energy EnergyTotal { get; set; }
        public Energy EnergyCapacity { get; set; }
        public Energy EnergyGenTotal { get; private set; }
        public Energy EnergyUseTotal { get; private set; }
        public bool IsEnergyFull { get; private set; }

        public int ResourcesCapacity { get; private set; }
        public int CountResources { get; private set; }
        public int ItemsCapacity { get; private set; }
        public int CountItems { get; private set; }
        public int FoodCapacity { get; private set; }
        public int CountFood { get; private set; }
        public int WaterCapacity { get; private set; }
        public int WaterLevel { get; private set; }
        public int WaterLevelForDisplay => this.WaterLevel + this.smoothedWaterConsumers.Sum(kv => kv.RemainingAmount);
        public int HydrogenCapacity { get; private set; }
        public int CountHydrogen { get; private set; }

        public int ResourceChangeCounter { get; private set; }  // Used by ResourceStackingController to monitor for changes

        public int WaterGenTotal { get; private set; }
        public int WaterUseTotal => this.waterConsumers.Sum(p => p.WaterUseRate) + this.smoothedWaterConsumers.Sum(s => s.AmountPerFrame) * 3600;

        public List<IEnergyConsumer> EnergyConsumers => this.energyConsumers.ToList();
        public List<IEnergyGenerator> EnergyGenerators => this.generators.ToList();

        public List<IBuildableThing> WaterConsumers => this.waterConsumers.Concat(this.smoothedWaterConsumers.Select(kv => kv.Building)).ToList();
        public List<IWaterProviderBuilding> WaterGenerators => this.waterProviders.ToList();

        #endregion Public Properties
        #region Ctor

        public ResourceNetwork()
        {
            this.Id = nextId++;

            foreach (var itemType in Enum.GetValues(typeof(ItemType)).Cast<ItemType>().Where(i => i != ItemType.None))
            {
                this.itemTotals.Add(itemType, 0);
            }

            EventManager.Subscribe(EventType.Game, EventSubType.Loaded, delegate (object obj) { this.OnGameLoaded(obj); });
            EventManager.Subscribe(EventType.Building, EventSubType.Added, delegate (object obj) { this.OnBuildingAdded(obj); });
            EventManager.Subscribe(EventType.Building, EventSubType.Ready, delegate (object obj) { this.OnBuildingReady(obj); });
            EventManager.Subscribe(EventType.Building, EventSubType.Recycling, delegate (object obj) { this.OnBuildingRecycling(obj); });
            EventManager.Subscribe(EventType.BuildableArea, delegate (object obj) { this.OnBuildableAreaUpdated(); });
        }

        #endregion Ctor

        #region Public Methods

        public int GetItemTotal(ItemType itemType)
        {
            return this.itemTotals.ContainsKey(itemType) ? this.itemTotals[itemType] : 0;
        }

        public void UpdateStartOfFrame(bool isPaused)
        {
            if (this.isZoneInvalidated)
            {
                this.RebuildZone();
            }

            if (this.isConnectionsInvalidated)
            {
                this.RebuildConnections();
            }

            if (World.InitialGameVersion.Major == 0 && World.InitialGameVersion.Minor < 4)
            {
                this.MoveItemsToNewStorageTypeV04(ItemType.Food);
                this.MoveItemsToNewStorageTypeV04(ItemType.Mush);
               // this.MoveItemsToNewStorageTypeV04(ItemType.Metal);
            }

            this.UpdateSilos();         // Updates item totals etc.
            this.UpdateBatteries();     // Update energy totals

            if (!isPaused)
            {
                this.EnergyGenTotal = 0;
                this.EnergyUseTotal = 0;
                this.UpdateGenerators();
                this.UpdateDispensers();
                this.UpdateRoomControls();
                this.UpdatePlanters();
                this.UpdateLabs();
                this.UpdateLamps();
                this.UpdateHeaters();
                this.UpdateSleepPods();
                this.UpdateOreScanners();

                this.WaterGenTotal = this.resourceProviders.OfType<IWaterProviderBuilding>().Sum(p => p.WaterGenRate);

                // "Smoothed" water consumption for display e.g. for dispensers
                foreach (var consumer in smoothedWaterConsumers.ToList())
                {
                    consumer.RemainingAmount -= consumer.AmountPerFrame;
                    if (consumer.RemainingAmount <= 0) smoothedWaterConsumers.Remove(consumer);
                }
            }

            this.UpdateResourceProviders(isPaused);

            if (!isPaused)
            {
                WorldStats.Increment(WorldStatKeys.EnergyGenerated, this.EnergyGenTotal.Joules);
                WorldStats.Increment(WorldStatKeys.EnergyUsed, this.EnergyUseTotal.Joules);
            }
        }

        public void UpdateEndOfFrame()
        {
            var energyToDistribute = this.energyCache;
            this.energyCache = 0;

            // Distribute energy cache
            var activeBatteries = this.batteries.Where(b => b.ChargeCapacity > b.ChargeLevel).ToList();
            if (!activeBatteries.Any()) return;

            var energyPerBattery = energyToDistribute / activeBatteries.Count;
            var nearlyFullBattery = activeBatteries.FirstOrDefault(b => (b.ChargeCapacity - b.ChargeLevel) < energyPerBattery);
            while (nearlyFullBattery != null)
            {
                energyToDistribute -= nearlyFullBattery.ChargeCapacity - nearlyFullBattery.ChargeLevel;
                nearlyFullBattery.ChargeLevel = nearlyFullBattery.ChargeCapacity;
                if (activeBatteries.Count == 1) return;
                activeBatteries.Remove(nearlyFullBattery);
                energyPerBattery = energyToDistribute / activeBatteries.Count;
                nearlyFullBattery = activeBatteries.FirstOrDefault(b => (b.ChargeCapacity - b.ChargeLevel) < energyPerBattery);
            }

            if (!activeBatteries.Any() || energyToDistribute == 0) return;

            energyPerBattery = energyToDistribute / activeBatteries.Count;
            foreach (var battery in activeBatteries)
            {
                battery.ChargeLevel += energyPerBattery;
            }
        }

        public void AddEnergy(Energy amount)
        {
            // Energy cache will be redistributed at the end of the frame
            this.energyCache += amount;
        }

        public bool CanTakeEnergy(Energy amountRequested)
        {
            return this.energyCache + this.batteries.Sum(b => b.ChargeLevel) >= amountRequested;
        }

        // Client should call CanTakeEnergy first
        public void TakeEnergy(Energy amountRequested)
        {
            // Take from cache first
            if (this.energyCache >= amountRequested)
            {
                this.energyCache -= amountRequested;
                return;
            }

            Energy amountProvided = this.energyCache;
            this.energyCache = 0;

            // Take energy from batteries where only a small amount is available
            foreach (var battery in this.batteries.Where(b => b.ChargeLevel < (amountRequested - amountProvided) / this.batteries.Count).ToList())
            {
                amountProvided += battery.ChargeLevel;
                battery.ChargeLevel = 0;
            }

            // Take energy from remaining batteries equally
            var nonEmptyBatteries = this.batteries.Where(b => b.ChargeLevel > 0).ToList();
            foreach (var battery in nonEmptyBatteries)
            {
                battery.ChargeLevel -= (amountRequested - amountProvided) / nonEmptyBatteries.Count;
                if (battery.ChargeLevel < 0) battery.ChargeLevel = 0;
            }
        }

        public bool CanAddItem(ItemType itemType, IThing excludedThing = null)
        {
            foreach (var consumer in this.resourceConsumers)
            {
                if (consumer != excludedThing && consumer.CanAddInput(itemType)) return true;
            }

            return this.silos.Any(s => s != excludedThing && s.SiloStatus == SiloStatus.Online && s.CanAddItem(itemType));
        }

        public bool AddItem(ItemType itemType, bool updateTotals = true)
        {
            if (Constants.ResourceStackTypes.ContainsKey(itemType)) this.ResourceChangeCounter++;

            var consumer = this.resourceConsumers.FirstOrDefault(c => c.CanAddInput(itemType));
            if (consumer != null)
            {
                consumer.AddInput(itemType);
                return true;
            }

            var silo = this.silos.FirstOrDefault(s => s.SiloStatus == SiloStatus.Online && s.CanAddItem(itemType));
            if (silo != null)
            {
                silo.AddItem(itemType);
                if (updateTotals)
                {
                    if (itemType == ItemType.LiquidFuel) this.CountHydrogen++;
                    else if (Constants.StorageTypesByItemType[itemType] == ThingType.FoodStorage) this.CountFood++;
                    else if (itemType == ItemType.Water) this.WaterLevel++;
                    else if (Constants.StorageTypesByItemType[itemType] == ThingType.ItemsStorage) this.CountItems++;
                    else this.CountResources++;
                    this.itemTotals[itemType]++;
                }
                return true;
            }

            return false;
        }

        public bool SwapItems(ItemType itemToAdd, ItemType itemToTake)
        {
            if (Constants.ResourceStackTypes.ContainsKey(itemToAdd) || Constants.ResourceStackTypes.ContainsKey(itemToTake)) this.ResourceChangeCounter++;

            foreach (var silo in this.silos)
            {
                if (silo.SwapItem(itemToTake, itemToAdd)) return true;
            }

            return false;
        }

        // Client should check there are enough items if requesting more than one
        public bool CanTakeItems(IBuildableThing building, ItemType itemType, int amountRequested)
        {
            int amountProvided = 0;

            if (this.buildingSiloConnections.ContainsKey(building))
            {
                foreach (var provider in this.resourceProviders.Where(s => s.CanTakeOutput(itemType)))
                {
                    amountProvided += provider.OutputItemCount;
                    if (amountProvided >= amountRequested) return true;
                }

                // Silos
                var remaining = amountRequested - amountProvided;
                foreach (var silo in this.buildingSiloConnections[building])
                {
                    amountProvided += silo.CountItems(itemType);
                    if (amountProvided >= amountRequested) return true;
                }
            }

            return false;
        }

        // Client should check there are enough items if requesting more than one
        // Use SilosOnly if we need to ensure that there is space in the network to swap out items
        public int TakeItems(IBuildableThing building, ItemType itemType, int amountRequested)
        {
            int amountProvided = 0;

            if (Constants.ResourceStackTypes.ContainsKey(itemType)) this.ResourceChangeCounter++;

            // Smoothed water values for display
            if (itemType == ItemType.Water && !(building is IWaterConsumer))
            {
                smoothedWaterConsumers.Add(new SmoothedWaterConsumer { Building = building, RemainingAmount = amountRequested, AmountPerFrame = amountRequested / 100 });
            }

            //  Resource providers first
            foreach (var provider in this.resourceProviders.Where(s => s.CanTakeOutput(itemType)))
            {
                while (provider.OutputItemCount > 0)
                {
                    amountProvided++;
                    this.itemTotals[itemType]--;
                    provider.TakeOutput();
                    if (amountProvided == amountRequested) return amountProvided;
                }
            }

            if (this.buildingSiloConnections.ContainsKey(building))
            {
                // Silos should be in order of distance
                var remaining = amountRequested - amountProvided;
                foreach (var silo in this.buildingSiloConnections[building])
                {
                    if (silo.CountItems(itemType) > 0)
                    {
                        var a = silo.TakeItems(itemType, remaining);
                        if (itemType == ItemType.Water) this.WaterLevel -= a;
                        else if (Constants.StorageTypesByItemType[itemType] == ThingType.FoodStorage) this.CountFood -= a;
                        else if (Constants.StorageTypesByItemType[itemType] == ThingType.ItemsStorage) this.CountItems -= a;
                        else this.CountResources -= a;
                        amountProvided += a;
                        remaining -= a;
                        this.itemTotals[itemType] -= a;
                        if (amountProvided >= amountRequested) return amountProvided;
                    }
                }
            }
            else
            {
                throw new Exception("Building not registered on network");
            }

            return amountProvided;
        }

        public int TakeFood(IBuildableThing building, int foodType)
        {
            if (World.GetFoodCount(foodType) == 0) return 0;

            if (this.TakeItems(building, ItemType.Food, 1) == 1)
            {
                World.TakeFood(foodType);
                return 1;
            }

            return 0;
        }

        public bool ContainsNode(int tileIndex)
        {
            return zone.ContainsNode(tileIndex);
        }

        // Everything that makes or uses stored resources, not including power and water
        public static bool DoesBuildingNeedSiloConnection(IBuildableThing building)
        {
            return building is IDispenser || building is IFactoryBuilding || building is IPlanter || building is ISilo || building.ThingType == ThingType.LaunchPad;
        }

        // Everything that interacts with the network in any way
        public static bool IsNetworkBuilding(IBuildableThing building)
        {
            return DoesBuildingNeedSiloConnection(building) || building is IEnergyGenerator || building is IEnergyConsumer || building is IBattery;
        }

        public List<IResourceProviderBuilding> GetResourceProviders()
        {
            return this.resourceProviders.ToList();
        }

        #endregion Public Methods
        #region Private Methods

        private void UpdateSilos()
        {
            this.CountResources = 0;
            this.ResourcesCapacity = 0;

            this.CountItems = 0;
            this.ItemsCapacity = 0;

            this.CountHydrogen = 0;
            this.HydrogenCapacity = 0;

            this.CountFood = 0;
            this.FoodCapacity = 0;

            this.WaterLevel = 0;
            this.WaterCapacity = 0;

            foreach (var itemType in this.itemTotals.Keys.ToList()) this.itemTotals[itemType] = 0;

            foreach (var silo in this.silos)
            {
                if (silo.IsSiloSwitchedOn)
                {
                    if (silo.ThingType == ThingType.HydrogenStorage) this.HydrogenCapacity += silo.StorageCapacity;
                    else if (silo.ThingType == ThingType.WaterPump || silo.ThingType == ThingType.WaterStorage || silo.ThingType == ThingType.ShorePump) this.WaterCapacity += silo.StorageCapacity;
                    else if (silo.ThingType == ThingType.FoodStorage) this.FoodCapacity += silo.StorageCapacity;
                    else if (silo.ThingType == ThingType.ItemsStorage) this.ItemsCapacity += silo.StorageCapacity;
                    else this.ResourcesCapacity += silo.StorageCapacity;
                }

                if (silo.ThingType == ThingType.HydrogenStorage) this.CountHydrogen += silo.StorageLevel;
                else if (silo.ThingType == ThingType.WaterPump || silo.ThingType == ThingType.ShorePump || silo.ThingType == ThingType.WaterStorage) this.WaterLevel += silo.StorageLevel;
                else if (silo.ThingType == ThingType.FoodStorage) this.CountFood += silo.StorageLevel;
                else if (silo.ThingType == ThingType.ItemsStorage) this.CountItems += silo.StorageLevel;
                else
                {
                    this.CountResources += silo.StorageLevel;

                    // For compatibility pre-v0.4
                    this.CountFood += silo.CountItems(ItemType.Food);
                    this.CountFood += silo.CountItems(ItemType.Mush);
                    this.CountFood += silo.CountItems(ItemType.Kek);
                }

                if (silo is ILander lander)
                {
                    this.FoodCapacity += lander.FoodContainer.StorageCapacity;
                    this.ItemsCapacity += lander.ItemsContainer.StorageCapacity;
                    this.CountItems += lander.ItemsContainer.StorageLevel;
                }

                foreach (var t in this.itemTotals.Keys.ToList())
                {
                    this.itemTotals[t] += silo.CountItems(t);
                }
            }
        }

        private void UpdateBatteries()
        {
            this.EnergyTotal = 0;
            this.EnergyCapacity = 0;

            foreach (var battery in this.batteries)
            {
                this.EnergyTotal += battery.ChargeLevel;
                this.EnergyCapacity += battery.ChargeCapacity;
            }

            // Stop generators if energy > 99%
            this.IsEnergyFull = this.EnergyTotal.KWh / this.EnergyCapacity.KWh > 0.99;
        }

        private void UpdateGenerators()
        {
            foreach (var generator in this.generators)
            {
                var energy = generator.UpdateGenerator();
                this.energyCache += energy;
                this.EnergyGenTotal += energy;
            }
        }

        private void UpdateDispensers()
        {
            foreach (var dispenser in this.dispensers)
            {
                dispenser.UpdateDispenser();
            }
        }

        private void UpdateResourceProviders(bool isPaused)
        {
            // Resource totals include silos and factory output slots, except algae pools.
            foreach (var factory in this.resourceProviders)
            {
                if (!isPaused)
                {
                    var energyUsed = factory.UpdateFactory();
                    this.EnergyUseTotal += energyUsed;
                }

                if (factory.ThingType != ThingType.AlgaePool && factory.CanTakeOutput(factory.OutputItemType))
                {
                    this.itemTotals[factory.OutputItemType] += factory.OutputItemCount;
                    switch(Constants.StorageTypesByItemType[factory.OutputItemType])
                    {
                        case ThingType.Silo: this.CountResources += factory.OutputItemCount; break;
                        case ThingType.ItemsStorage: this.CountItems += factory.OutputItemCount; break;
                        case ThingType.FoodStorage: this.CountFood += factory.OutputItemCount; break;
                        case ThingType.HydrogenStorage: this.CountHydrogen += factory.OutputItemCount; break;
                    }
                }
            }
        }

        private void UpdateLamps()
        {
            foreach (var l in this.lamps)
            {
                l.UpdateLamp(out Energy energyUsed);
                this.EnergyUseTotal += energyUsed;
            }
        }

        private void UpdateHeaters()
        {
            foreach (var h in this.heaters)
            {
                h.UpdateHeater(out Energy energyUsed);
                this.EnergyUseTotal += energyUsed;
            }
        }

        private void UpdateOreScanners()
        {
            foreach (var h in this.oreScanners)
            {
                h.UpdateScanner(out Energy energyUsed);
                this.EnergyUseTotal += energyUsed;
            }
        }

        private void UpdateRoomControls()
        {
            foreach (var rc in this.roomControls)
            {
                rc.UpdateRoomControl(out Energy energyUsed);
                this.EnergyUseTotal += energyUsed;
            }
        }

        private void UpdateSleepPods()
        {
            foreach (var sp in this.sleepPods)
            {
                this.EnergyUseTotal += sp.EnergyUseRate / 3600f;
            }
        }

        private void UpdatePlanters()
        {
            foreach (var planter in this.planters) planter.UpdatePlanter();
        }

        private void UpdateLabs()
        {
            foreach (var lab in this.labs)
            {
                this.EnergyUseTotal += lab.UpdateLab();
            }
        }

        private void RebuildZone()
        {
            this.isZoneInvalidated = false;
            this.zone.Clear();

            foreach (var tileIndex in World.BuildableTiles)
            {
                this.zone.AddNode(tileIndex);
            }
        }

        private void RebuildConnections()
        {
            this.isConnectionsInvalidated = false;
            this.batteries.Clear();
            this.dispensers.Clear();
            this.generators.Clear();
            this.energyConsumers.Clear();
            this.roomControls.Clear();
            this.lamps.Clear();
            this.heaters.Clear();
            this.oreScanners.Clear();
            this.silos.Clear();
            this.sleepPods.Clear();
            this.planters.Clear();
            this.labs.Clear();
            this.resourceConsumers.Clear();
            this.resourceProviders.Clear();
            this.waterConsumers.Clear();
            this.waterProviders.Clear();
            this.buildingSiloConnections.Clear();

            var allBuildings = new HashSet<int>();

            foreach (var node in this.zone.Nodes.Values)
            {
                var building = node.ResourceBuilding;
                if (building is null || allBuildings.Contains(building.Id) || !building.IsReady) continue;

                allBuildings.Add(building.Id);

                if (building is IDispenser dispenser) this.dispensers.Add(dispenser);
                if (building is IEnergyConsumer consumer) this.energyConsumers.Add(consumer);
                if (building is IWaterConsumer wc) this.waterConsumers.Add(wc);
                if (building is IPlanter p) this.planters.Add(p);

                if (DoesBuildingNeedSiloConnection(building))
                {
                    this.buildingSiloConnections.Add(building, this.GetConnections<ISilo>(node));

                    if (building is IEnergyGenerator generator) this.generators.Add(generator);
                    if (building is IResourceConsumerBuilding rcb) this.resourceConsumers.Add(rcb);
                    if (building is IResourceProviderBuilding rpb) this.resourceProviders.Add(rpb);
                    if (building is IWaterProviderBuilding wpb) this.waterProviders.Add(wpb);
                    if (building is ISilo silo) this.silos.Add(silo);
                    if (building is IBattery battery) this.batteries.Add(battery);
                }
                else if (building is IEnergyGenerator energyGenerator) this.generators.Add(energyGenerator);
                else if (building is IBattery battery) this.batteries.Add(battery);
                else if (building is IEnvironmentControl rc) this.roomControls.Add(rc);
                else if (building is ILab lab) this.labs.Add(lab);
                else if (building is ILamp lamp) this.lamps.Add(lamp);
                else if (building is IHeater heater) this.heaters.Add(heater);
                else if (building is ISleepPod pod) this.sleepPods.Add(pod);
                else if (building is IOreScanner scanner) this.oreScanners.Add(scanner);
            }
        }

        // This will get connections in order of distance
        private List<T> GetConnections<T>(ResourceNetworkNode startNode) where T : class
        {
            var result = new List<T>();

            var openNodes = new Queue<ResourceNetworkNode>();
            var closedNodes = new HashSet<int>();

            foreach (var node in startNode.AllLinks)
            {
                openNodes.Enqueue(node);
                closedNodes.Add(node.TileIndex);
            }

            while (openNodes.Any())
            {
                var currentNode = openNodes.Dequeue();

                if (currentNode.ResourceBuilding is T && !result.Contains(currentNode.ResourceBuilding as T))
                {
                    result.Add(currentNode.ResourceBuilding as T);
                }

                foreach (var t in currentNode.AllLinks.Where(u => !closedNodes.Contains(u.TileIndex)))
                {
                    openNodes.Enqueue(t);
                    closedNodes.Add(t.TileIndex);
                }
            }

            return result;
        }

        private void MoveItemsToNewStorageTypeV04(ItemType itemType)
        {
            foreach (var silo in this.silos.Where(s => (s.ThingType == ThingType.Lander || s.ThingType == ThingType.Silo) && s.CanTakeItems(itemType, 1)))
            {
                var target = this.silos.FirstOrDefault(s => s.CanAddItem(itemType));
                while (target != null)
                {
                    silo.TakeItems(itemType, 1);
                    target.AddItem(itemType);
                    target = silo.CanTakeItems(itemType, 1) ? this.silos.FirstOrDefault(s => s.CanAddItem(itemType)) : null;
                }
            }
        }

        #endregion Private Methods
        #region Event Handlers

        private void OnGameLoaded(object _)
        {
            this.isZoneInvalidated = true;
            this.isConnectionsInvalidated = true;
        }

        private void OnBuildingAdded(object obj)
        {
            if (!(obj is IBuildableThing building) || !building.IsReady) return;

            foreach (var tile in building.AllTiles)
            {
                if (!zone.ContainsNode(tile.Index))
                {
                    this.isZoneInvalidated = true;
                    if (IsNetworkBuilding(building)) this.isConnectionsInvalidated = true;
                }
            }
        }

        private void OnBuildingReady(object obj)
        {
            if (!(obj is IBuildableThing building)) return;
            if (IsNetworkBuilding(building)) this.isConnectionsInvalidated = true;
        }

        private void OnBuildingRecycling(object obj)
        {
            if (!(obj is IBuildableThing building)) return;

            foreach (var tile in building.AllTiles)
            {
                if (!zone.ContainsNode(tile.Index))
                {
                    this.isZoneInvalidated = true;
                    if (IsNetworkBuilding(building)) this.isConnectionsInvalidated = true;
                }
            }
        }


        private void OnBuildableAreaUpdated()
        {
            this.isZoneInvalidated = true;
        }

        #endregion
    }
}
