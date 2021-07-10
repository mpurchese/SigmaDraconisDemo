namespace SigmaDraconis.World.Buildings
{
    using Draconis.Shared;
    using System;
    using System.Collections.Generic;
    using ProtoBuf;
    using Config;
    using Shared;
    using Rooms;
    using Smoke;
    using Particles;
    using WorldInterfaces;

    [ProtoContract]
    [ProtoInclude(1, typeof(CharcoalMaker))]
    [ProtoInclude(2, typeof(Cooker))]
    [ProtoInclude(3, typeof(Generator))]
    [ProtoInclude(4, typeof(MushFactory))]
    [ProtoInclude(5, typeof(ResourceProcessor))]
    [ProtoInclude(6, typeof(ElectricFurnace))]
    [ProtoInclude(7, typeof(AlgaePool))]
    [ProtoInclude(8, typeof(StoneFurnace))]
    [ProtoInclude(9, typeof(Mine))]
    [ProtoInclude(10, typeof(FuelFactory))]
    [ProtoInclude(11, typeof(BiomassPower))]
    [ProtoInclude(12, typeof(CoalPower))]
    [ProtoInclude(13, typeof(HydrogenBurner))]
    [ProtoInclude(14, typeof(WaterPump))]
    [ProtoInclude(15, typeof(BatteryCellFactory))]
    [ProtoInclude(16, typeof(CompostFactory))]
    [ProtoInclude(17, typeof(SolarCellFactory))]
    [ProtoInclude(18, typeof(GlassFactory))]
    [ProtoInclude(19, typeof(CompositesFactory))]
    [ProtoInclude(20, typeof(ShorePump))]
    [ProtoInclude(21, typeof(KekFactory))]
    public abstract class FactoryBuilding : Building, IFactoryBuilding
    {
        protected List<SmokeModel> smokeModels;
        protected Energy energyPerHour;
        protected Energy energyPerFrame;
        protected double capacitorSize;
        protected int framesToInitialise;
        protected int framesToProcess;
        protected int framesToPauseResume;
        protected int framesToBreak;
        protected ItemType consumedItemType;
        protected ItemType producedItemType;
        protected int minTemperature;
        protected float soundVolume;
        protected double smokeSoundRate;
        protected float prevSoundVolume;

        [ProtoMember(101)]
        public bool IsSwitchedOn { get; protected set; }

        [ProtoMember(102)]
        public virtual FactoryStatus FactoryStatus { get; set; }

        [ProtoMember(103)]
        public double FactoryProgress { get; set; }

        [ProtoMember(104)]
        protected int pauseResumeFrameCounter;

        /// <summary>
        /// Gets the type of the item waiting to be distributed to the network
        /// </summary>
        [ProtoMember(105)]
        public ItemType OutputItemType { get; protected set; }

        [ProtoMember(106)]
        public int OutputItemCount { get; set; }

        [ProtoMember(107)]
        public ItemType InputItemType { get; protected set; }

        [ProtoMember(110)]
        public Energy EnergyUseRate { get; set; } = 0;

        [ProtoMember(111)]
        public double CapacitorCharge { get; set; } = 0;

        [ProtoMember(120)]
        public double MaintenanceLevel { get; set; }

        [ProtoMember(121)]
        public WorkPriority RepairPriority { get; set; }

        [ProtoMember(122)]
        public Energy EnergyGenRate { get; set; }

        [ProtoMember(130)]
        public double Temperature { get; protected set; }

        [ProtoMember(140)]
        public bool IsAutoRestartEnabled { get; protected set; }

        [ProtoMember(141)]
        public int? InventoryTarget { get; protected set; }

        [ProtoMember(142)]
        public bool InventoryTargetShutdown { get; protected set; }

        [ProtoMember(150)]
        public int WaterUseRate { get; protected set; }

        [ProtoMember(151)]
        private double waterCache;

        public bool GeneratorHasWater => this.waterCache >= 1;

        public int FramesRemaining => (int)((1.0 - this.FactoryProgress) * this.framesToProcess);

        public FactoryBuilding() : base(ThingType.None)
        {
            this.Init();
        }

        public FactoryBuilding(ThingType type, ISmallTile tile, int size) : base(type, tile, size)
        {
            this.Init();
        }

        protected virtual void Init()
        {
        }

        public override void AfterAddedToWorld()
        {
            var definition = ThingTypeManager.GetDefinition(this.ThingType);
            this.framesToBreak = definition.FramesToBreak;
            this.minTemperature = definition.MinTemperature;
            this.smokeModels = SmokeManager.Get(this.ThingType);
            base.AfterAddedToWorld();
        }

        public override void AfterConstructionComplete()
        {
            this.MaintenanceLevel = 1.0;
            this.RepairPriority = WorkPriority.Normal;
            this.animationFrame = 1;
            this.FactoryStatus = FactoryStatus.Initialising;
            this.FactoryProgress = 0;
            this.IsSwitchedOn = true;
            this.IsAutoRestartEnabled = true;
            this.Temperature = RoomManager.GetTileTemperature(this.MainTileIndex);
            base.AfterConstructionComplete();
        }

        public override void Update()
        {
            if (this.IsDesignatedForRecycling) this.IsSwitchedOn = false;
            this.UpdateSound();

            base.Update();
        }

        public virtual void UpdateSound()
        {
            if (this.definitionSoundVolume > 0 && (this.smokeSoundRate > 0 || this.soundVolume > 0))
            {
                this.soundVolume = (float)(this.smokeSoundRate).Clamp(this.soundVolume - this.definitionSoundFade, this.soundVolume + this.definitionSoundFade);
                //if (this.soundVolume != this.prevSoundVolume)
               // {
                    EventManager.EnqueueSoundUpdateEvent(this.id, this.soundVolume < 0.001f, this.soundVolume * this.definitionSoundVolume);
                //    this.prevSoundVolume = this.soundVolume;// Mathf.Clamp(this.soundVolume, this.soundVolume - 0.05f, this.soundVolume + 0.05f);
               // }
            }
        }

        protected virtual double GetGeneratorTargetProcessingRate()
        {
            return this.IsSwitchedOn && this.FactoryStatus.In(FactoryStatus.Starting, FactoryStatus.Resuming, FactoryStatus.InProgress) ? 1.0 : 0.0;
        }

        public virtual Energy UpdateGenerator()
        {
            var energyGenerated = (Energy)0;
            var prevEnergyGenRate = this.EnergyGenRate;
            this.EnergyGenRate = 0;
            this.smokeSoundRate = 0;

            var network = World.ResourceNetwork;
            if (network == null) return 0;

            if ((this is IWaterConsumer) && this.waterCache < 4 && network.CanTakeItems(this, ItemType.Water, 1))
            {
                network.TakeItems(this, ItemType.Water, 1);
                this.waterCache++;
            }

            var noWater = (this is IWaterConsumer) && !this.GeneratorHasWater;

            switch (this.FactoryStatus)
            {
                case FactoryStatus.Initialising:
                    if (!this.IsSwitchedOn) this.FactoryStatus = FactoryStatus.Offline;
                    else if (network.IsEnergyFull == true || noWater) this.FactoryStatus = FactoryStatus.Paused;
                    else
                    {
                        this.FactoryProgress += 1.0 / this.framesToInitialise;
                        if (this.FactoryProgress > 0.999) this.TryStart();
                        if (this.FactoryProgress > 0.999)
                        {
                            this.FactoryProgress = 0.0;
                            this.FactoryStatus = FactoryStatus.Standby;
                        }
                    }
                    break;
                case FactoryStatus.Standby:
                    if (!this.IsSwitchedOn) this.FactoryStatus = FactoryStatus.Offline;
                    else this.TryStart();
                    break;
                case FactoryStatus.InProgress:
                    if (!this.IsSwitchedOn || network.IsEnergyFull == true || noWater) this.FactoryStatus = FactoryStatus.Pausing;
                    energyGenerated = this.DoGeneratorProcess(prevEnergyGenRate);
                    break;
                case FactoryStatus.Pausing:
                case FactoryStatus.Stopping:
                    energyGenerated = this.DoGeneratorProcess(prevEnergyGenRate);
                    if (this.IsSwitchedOn && (this.FactoryStatus == FactoryStatus.Pausing || this.FactoryStatus == FactoryStatus.Stopping) && network.IsEnergyFull == false && !noWater && this.FactoryProgress <= 0.999)
                    {
                        this.FactoryStatus = FactoryStatus.Resuming;
                    }
                    else if (energyGenerated.Joules == 0 && this.FactoryStatus != FactoryStatus.Offline && this.FactoryProgress > 0) this.FactoryStatus = FactoryStatus.Paused;
                    break;
                case FactoryStatus.Paused:
                    if (this.IsSwitchedOn && network.IsEnergyFull == false && !noWater)
                    {
                        if (this.FactoryProgress <= 0.999) this.FactoryStatus = FactoryStatus.Resuming;
                        else
                        {
                            this.FactoryProgress = 0;
                            if (!this.IsAutoRestartEnabled || !this.IsSwitchedOn)
                            {
                                this.FactoryStatus = FactoryStatus.Offline;
                                this.IsSwitchedOn = false;
                            }
                            else if (this.MaintenanceLevel >= 0.0001) this.FactoryStatus = FactoryStatus.Standby;
                            else this.FactoryStatus = FactoryStatus.Broken;
                        }
                    }
                    else if (this.FactoryProgress > 0.999)
                    {
                        this.FactoryProgress = 0;
                        if (this.MaintenanceLevel >= 0.0001) this.FactoryStatus = this.IsSwitchedOn ? FactoryStatus.Standby : FactoryStatus.Offline;
                        else this.FactoryStatus = FactoryStatus.Broken;
                    }
                    break;
                case FactoryStatus.Resuming:
                case FactoryStatus.Starting:
                    if (!this.IsSwitchedOn || network.IsEnergyFull == true || noWater) this.FactoryStatus = FactoryStatus.Pausing;
                    energyGenerated = this.DoGeneratorProcess(prevEnergyGenRate);
                    break;
                case FactoryStatus.Broken:
                    if (this.MaintenanceLevel >= 0.0001) this.FactoryStatus = this.IsSwitchedOn ? FactoryStatus.Standby : FactoryStatus.Offline;
                    break;
            }

            this.UpdateAnimationFrame();

            this.EnergyGenRate = energyGenerated * 3600;

            this.WaterUseRate = this.GetWaterUseRate();
            this.waterCache -= this.WaterUseRate / 3600.0;
            
            return energyGenerated;
        }
        
        protected virtual int GetWaterUseRate()
        {
            return (int)(this.EnergyGenRate.KWh * Constants.GeneratorWaterUsePerKwH);
        }

        private Energy DoGeneratorProcess(Energy prevEnergyGenRate)
        {
            var targetRate = this.GetGeneratorTargetProcessingRate();
            var prevRate = prevEnergyGenRate / 3600;
            var changeRate = this.energyPerFrame / this.framesToPauseResume;
            var energyGenerated = Energy.Clamp(this.energyPerFrame * targetRate, prevRate - changeRate, prevRate + changeRate);
            this.Process(targetRate);

            if (this.FactoryProgress > 0.999)
            {
                this.FactoryProgress = 1.0;
                this.InputItemType = ItemType.None;
                if (energyGenerated > 0)
                {
                    if (this.IsAutoRestartEnabled) this.TryStart();
                    if (this.FactoryProgress > 0.999) this.FactoryStatus = FactoryStatus.Stopping;
                }
                else
                {
                    this.FactoryProgress = 0.0;
                    if (this.IsSwitchedOn && this.IsAutoRestartEnabled) this.FactoryStatus = this.MaintenanceLevel <= 0.0 ? FactoryStatus.Broken : FactoryStatus.Standby;
                    else
                    {
                        this.FactoryStatus = FactoryStatus.Offline;
                        this.IsSwitchedOn = false;
                    }
                }
            }
            else
            {
                this.MaintenanceLevel = Math.Max(0.0, this.MaintenanceLevel - (1.0 / this.framesToBreak));
                if ((this.FactoryStatus == FactoryStatus.Starting || this.FactoryStatus == FactoryStatus.Resuming) && energyGenerated >= this.energyPerFrame * targetRate)
                {
                    this.FactoryStatus = FactoryStatus.InProgress;
                }
            }

            return energyGenerated;
        }
       

        public virtual Energy UpdateFactory()
        {
            var energyUsed = (Energy)0;
            this.EnergyUseRate = 0;
            this.smokeSoundRate = 0;

            if (this.isDesignatedForRecycling) return energyUsed;

            if (this.minTemperature > -99)
            {
                var t = RoomManager.GetTileTemperature(this.MainTileIndex);
                if (t > this.Temperature) this.Temperature += Constants.MushFactoryTemperatureChangePerFrame;
                else if (t < this.Temperature) this.Temperature -= Constants.MushFactoryTemperatureChangePerFrame;
            }

            var network = World.ResourceNetwork;
            if (network == null) return 0;

            switch (this.FactoryStatus)
            {
                case FactoryStatus.NoPower:
                case FactoryStatus.TooCold:
                case FactoryStatus.TooDry:
                    if (!this.CheckTemperatureOK()) this.FactoryStatus = FactoryStatus.TooCold;
                    else if (!this.CheckMoistureOK()) this.FactoryStatus = FactoryStatus.TooDry;
                    else if (this.FactoryStatus == FactoryStatus.TooCold && this.CheckTemperatureOK()) this.FactoryStatus = FactoryStatus.NoPower;
                    else if (this.FactoryStatus == FactoryStatus.TooDry && this.CheckMoistureOK()) this.FactoryStatus = FactoryStatus.NoPower;

                    if (this.FactoryStatus == FactoryStatus.NoPower) this.UpdateFactoryNoPower();
                    break;
                case FactoryStatus.Initialising:
                    if (!this.IsSwitchedOn) this.FactoryStatus = FactoryStatus.Offline;
                    else if (this.capacitorSize == 0)
                    {
                        this.FactoryProgress += 1.0 / this.framesToInitialise;
                        if (this.FactoryProgress > 0.999) this.TryStart();
                    }
                    else if (this.CapacitorCharge >= this.capacitorSize)
                    {
                        this.FactoryProgress = 1.0;
                        this.TryStart();
                    }
                    else if (network.CanTakeEnergy(this.energyPerFrame))
                    {
                        network.TakeEnergy(this.energyPerFrame);
                        energyUsed = this.energyPerFrame;
                        this.CapacitorCharge += this.energyPerFrame.KWh;
                        this.FactoryProgress = this.CapacitorCharge / this.capacitorSize;
                        if (this.CapacitorCharge >= this.capacitorSize) this.TryStart();
                    }
                    else this.FactoryStatus = FactoryStatus.NoPower;
                    break;
                case FactoryStatus.Standby:
                case FactoryStatus.NoResource:
                    if (!this.IsSwitchedOn) this.FactoryStatus = FactoryStatus.Offline;
                    else if (World.WorldTime.FrameNumber % 7 == 0) this.TryStart();
                    break;
                case FactoryStatus.InProgress:
                    if (this.ShouldPause())
                    {
                        this.FactoryStatus = FactoryStatus.Pausing;
                        this.pauseResumeFrameCounter = this.framesToPauseResume;
                        this.CapacitorCharge -= this.energyPerFrame.KWh;
                    }
                    else if (!this.CheckTemperatureOK()) this.FactoryStatus = FactoryStatus.TooCold;
                    else if (!network.CanTakeEnergy(energyPerFrame))
                    {
                        this.FactoryStatus = FactoryStatus.NoPower;
                        this.pauseResumeFrameCounter = this.framesToPauseResume;
                        this.CapacitorCharge -= this.energyPerFrame.KWh;
                    }
                    else
                    {
                        network.TakeEnergy(energyPerFrame);
                        energyUsed = energyPerFrame;
                    }
                    this.Process(1.0);
                    break;
                case FactoryStatus.Pausing:
                    if (!this.ShouldPause()) this.FactoryStatus = FactoryStatus.Resuming;
                    else if (!this.CheckTemperatureOK()) this.FactoryStatus = FactoryStatus.TooCold;
                    else
                    {
                        this.pauseResumeFrameCounter--;
                        if (this.pauseResumeFrameCounter <= 0) this.FactoryStatus = FactoryStatus.Paused;
                        else
                        {
                            var rate = this.pauseResumeFrameCounter / (double)this.framesToPauseResume;
                            if (this.CapacitorCharge >= rate * this.energyPerFrame.KWh)
                            {
                                this.CapacitorCharge -= rate * this.energyPerFrame.KWh;
                                this.Process(rate);
                            }
                            else this.FactoryStatus = FactoryStatus.Paused;
                        }
                    }
                    break;
                case FactoryStatus.Paused:
                    if (!this.CheckTemperatureOK()) this.FactoryStatus = FactoryStatus.TooCold;
                    else if (!this.ShouldPause())
                    {
                        this.pauseResumeFrameCounter = 0;
                        this.FactoryStatus = FactoryStatus.Resuming;
                    }
                    break;
                case FactoryStatus.Resuming:
                    this.DoSmoke(this.pauseResumeFrameCounter / (double)this.framesToPauseResume);
                    if (this.ShouldPause())
                    {
                        this.FactoryStatus = FactoryStatus.Pausing;
                    }
                    else if (!this.CheckTemperatureOK()) this.FactoryStatus = FactoryStatus.TooCold;
                    else if (this.CapacitorCharge >= this.capacitorSize && this.pauseResumeFrameCounter >= this.framesToPauseResume)
                    {
                        this.FactoryStatus = FactoryStatus.InProgress;
                    }
                    else if (this.energyPerFrame == 0 || this.CapacitorCharge >= this.capacitorSize)
                    {
                        this.pauseResumeFrameCounter++;
                        if (this.pauseResumeFrameCounter >= this.framesToPauseResume) this.FactoryStatus = FactoryStatus.InProgress;
                    }
                    else if (network.CanTakeEnergy(energyPerFrame))
                    {
                        network.TakeEnergy(energyPerFrame);
                        energyUsed = energyPerFrame;
                        this.CapacitorCharge += this.energyPerFrame.KWh;
                        this.pauseResumeFrameCounter++;
                        if (this.CapacitorCharge >= this.capacitorSize && this.pauseResumeFrameCounter >= this.framesToPauseResume) this.FactoryStatus = FactoryStatus.InProgress;
                    }
                    else this.FactoryStatus = FactoryStatus.NoPower;
                    break;
                case FactoryStatus.WaitingToDistribute:
                    this.TryDistribute();
                    break;
                case FactoryStatus.Broken:
                    if (this.MaintenanceLevel >= 0.0001) this.FactoryStatus = this.IsSwitchedOn ? FactoryStatus.Standby : FactoryStatus.Offline;
                    break;
            }

            this.UpdateAnimationFrame();

            this.EnergyUseRate = energyUsed * 3600;
            return energyUsed;
        }

        protected virtual bool ShouldPause()
        {
            return !this.IsSwitchedOn || (this.InventoryTarget.HasValue && World.ResourceNetwork?.GetItemTotal(this.producedItemType) >= this.InventoryTarget.Value);
        }

        public virtual void ToggleAutoRestart()
        {
            this.IsAutoRestartEnabled = !this.IsAutoRestartEnabled;
        }

        public virtual void TogglePower()
        {
            this.IsSwitchedOn = !this.IsSwitchedOn;
            if (this.IsSwitchedOn && this.FactoryStatus == FactoryStatus.Offline) this.FactoryStatus = FactoryStatus.Initialising;
            this.UpdateAnimationFrame();
        }

        public virtual void SetInventoryTarget(int? targetValue, bool stopOnComplete)
        {
            this.InventoryTarget = targetValue;
            this.InventoryTargetShutdown = stopOnComplete;
        }

        public virtual bool CanAddInput(ItemType itemType)
        {
            if (World.ResourceNetwork == null) return false;
            if (itemType != this.consumedItemType || !this.IsSwitchedOn || this.FactoryStatus != FactoryStatus.Standby || this.InputItemType != ItemType.None) return false;
            if (this.InventoryTarget.HasValue && World.ResourceNetwork.GetItemTotal(this.producedItemType) >= this.InventoryTarget.Value) return false;
            return true;
        }

        public virtual void AddInput(ItemType itemType)
        {
            this.InputItemType = itemType;
        }

        public virtual bool CanTakeOutput(ItemType itemType)
        {
            return this.FactoryStatus == FactoryStatus.WaitingToDistribute && this.OutputItemType == itemType && this.OutputItemCount > 0;
        }

        public virtual void TakeOutput()
        {
            if (this.OutputItemCount > 0) this.OutputItemCount--;
            if (this.OutputItemCount == 0)
            {
                this.FactoryProgress = 0;
                this.OutputItemType = ItemType.None;
                this.FactoryStatus = this.IsSwitchedOn ? FactoryStatus.Standby : FactoryStatus.Offline;
            }
        }

        public override bool CanRecycle()
        {
            return base.CanRecycle() && this.FactoryStatus.In(FactoryStatus.Offline, FactoryStatus.Standby, FactoryStatus.Paused, FactoryStatus.NoResource, FactoryStatus.TooCold, FactoryStatus.TooDry);
        }

        protected virtual void UpdateFactoryNoPower()
        {
            if (this.pauseResumeFrameCounter > 0 && this.FactoryProgress > 0 && this.CapacitorCharge > 0)
            {
                this.pauseResumeFrameCounter--;
                if (this.pauseResumeFrameCounter <= 0) this.FactoryStatus = FactoryStatus.Paused;
                else
                {
                    var rate = this.pauseResumeFrameCounter / (double)this.framesToPauseResume;
                    this.CapacitorCharge -= rate * this.energyPerFrame.KWh;
                    this.Process(rate);
                }
            }
            else if (World.ResourceNetwork?.CanTakeEnergy(Energy.FromKwH(this.capacitorSize)) == true) this.FactoryStatus = FactoryStatus.Resuming;
        }

        protected virtual void TryStart()
        {
            this.FactoryProgress = 0.0;

            var network = World.ResourceNetwork;
            if (network == null) return;

            if (!this.CheckTemperatureOK()) this.FactoryStatus = FactoryStatus.TooCold;
            else if (!this.CheckMoistureOK()) this.FactoryStatus = FactoryStatus.TooDry;
            else if (this.InventoryTarget.HasValue && network.GetItemTotal(this.producedItemType) >= this.InventoryTarget.Value) this.FactoryStatus = FactoryStatus.Standby;
            else if (this.InputItemType == this.consumedItemType || network.CanTakeItems(this, this.consumedItemType, 1))
            {
                if (this.CapacitorCharge >= this.capacitorSize || network.CanTakeEnergy(Energy.FromKwH(this.capacitorSize - this.CapacitorCharge)))
                {
                    if (this.InputItemType != this.consumedItemType) network.TakeItems(this, this.consumedItemType, 1);
                    this.FactoryStatus = FactoryStatus.InProgress;
                    this.InputItemType = ItemType.None;
                }
                else this.FactoryStatus = FactoryStatus.NoPower;
            }
            else
            {
                this.FactoryStatus = FactoryStatus.Standby;
            }
        }

        protected virtual void Process(double rate)
        {
            this.smokeSoundRate = rate;
            this.DoSmoke(rate);

            this.FactoryProgress += rate / this.framesToProcess;
            if (this.FactoryProgress > 0.999)
            {
                this.CompleteProcessing();
            }
            else if (this.framesToBreak > 0)
            {
                this.MaintenanceLevel = Math.Max(0.0, this.MaintenanceLevel - (1.0 / this.framesToBreak));
            }
        }

        protected virtual bool CheckMoistureOK()
        {
            return true;
        }

        protected virtual bool CheckTemperatureOK()
        {
            return this.minTemperature <= -99 || (int)Math.Round(this.Temperature) >= this.minTemperature;
        }

        protected virtual void CompleteProcessing()
        {
            this.FactoryProgress = 1.0;
            this.pauseResumeFrameCounter = 0;
            this.FactoryStatus = FactoryStatus.WaitingToDistribute;
            this.InputItemType = ItemType.None;
            this.OutputItemType = this.producedItemType;
            this.OutputItemCount = this.OutputItemType == ItemType.None ? 0 : 1;
            this.TryDistribute();
        }

        protected virtual void TryDistribute()
        {
            var network = World.ResourceNetwork;
            if (network == null) return;

            var switchOff = false;
            if (this.OutputItemType == ItemType.None || this.OutputItemCount == 0)
            {
                this.FactoryProgress = 0.0;
                this.OutputItemType = ItemType.None;
                if (this.framesToBreak > 0 && this.MaintenanceLevel < 0.0001) this.FactoryStatus = FactoryStatus.Broken;
                else if (this.IsSwitchedOn && this.IsAutoRestartEnabled) this.TryStart();
                else switchOff = true;
            }
            else if (network.CanAddItem(this.OutputItemType) == true)
            {
                network.AddItem(this.OutputItemType);
                this.OutputItemCount--;
                if (this.OutputItemCount == 0)
                {
                    this.FactoryProgress = 0.0;
                    this.OutputItemType = ItemType.None;
                    if (this.framesToBreak > 0 && this.MaintenanceLevel < 0.0001) this.FactoryStatus = FactoryStatus.Broken;
                    else if (this.IsSwitchedOn && this.IsAutoRestartEnabled) this.TryStart();
                    else switchOff = true;
                }
            }
            else if (this.consumedItemType != ItemType.None && this.IsSwitchedOn && this.IsAutoRestartEnabled && this.OutputItemCount == 1 
                && (this.framesToBreak == 0 || this.MaintenanceLevel >= 0.0001)
                && network.SwapItems(this.OutputItemType, this.consumedItemType) == true)
            {
                this.OutputItemType = ItemType.None;
                this.OutputItemCount = 0;
                this.FactoryProgress = 0.0;
                this.InputItemType = this.consumedItemType;
                this.TryStart();
            }

            if ((this.FactoryStatus == FactoryStatus.Standby || this.FactoryStatus == FactoryStatus.Broken) && this.InventoryTargetShutdown
                && this.producedItemType != ItemType.None && this.InventoryTarget.HasValue && network.GetItemTotal(this.producedItemType) >= this.InventoryTarget.Value)
            {
                switchOff = true;
            }

            if (switchOff)
            {
                this.FactoryStatus = FactoryStatus.Offline;
                this.IsSwitchedOn = false;
            }
        }

        protected virtual void UpdateAnimationFrame()
        {
        }

        protected virtual void DoSmoke(double rate)
        {
            if (this.smokeModels == null) return;

            foreach (var model in this.smokeModels)
            { 
                var adjustedRate = rate * model.ProductionRate;
                if (adjustedRate > 0.99 || Rand.NextDouble() < adjustedRate)
                {
                    SmokeSimulator.AddParticle(this.mainTile, model, (this as IRotatableThing)?.Direction ?? Direction.None, this.allTiles.Count == 4 ? 10.667f : 0, 0);
                }
            }
        }
    }
}
