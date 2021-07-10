namespace SigmaDraconis.World.Flora
{
    using ProtoBuf;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    [ProtoInclude(100, typeof(Bush))]
    [ProtoInclude(101, typeof(SmallPlant5))]
    [ProtoInclude(102, typeof(SmallPlant6))]
    [ProtoInclude(103, typeof(SmallPlant9))]
    [ProtoInclude(104, typeof(SmallPlant12))]
    public abstract class FruitPlant : Plant, IFruitPlant
    {
        private float? harvestJobProgress;

        [ProtoMember(1)]
        public WorkPriority HarvestFruitPriority { get; protected set; }

        [ProtoMember(2)]
        public float? HarvestJobProgress
        {
            get
            {
                return this.harvestJobProgress;
            }
            set
            {
                if (this.harvestJobProgress != value)
                {
                    if (this.mainTile != null) EventManager.EnqueueWorldPropertyChangeEvent(this.Id, nameof(this.HarvestJobProgress), this.harvestJobProgress, value, this.mainTile.Row, this.ThingType);
                    this.harvestJobProgress = value;
                }
            }
        }

        [ProtoMember(3)]
        public int ReservedByAnimalID { get; protected set; }

        public virtual int CountFruitAvailable => 0;

        protected bool seedNextFrame;

        public virtual bool CanFruit => !this.IsDead;
        public virtual bool CanFruitUnripe => false;
        public virtual bool HasFruitUnripe => false;

        public FruitPlant(ThingType thingType, ISmallTile mainTile, int size) : base(thingType, mainTile, size)
        {
        }

        public FruitPlant(ThingType thingType) : base(thingType)
        {
        }

        public FruitPlant() : base(ThingType.None)
        {
        }

        public override void AfterAddedToWorld()
        {
            World.HandleFruitPlantUpdate(this);
            base.AfterAddedToWorld();
        }

        public virtual void SetHarvestFruitPriority(WorkPriority value)
        {
            if (value == this.HarvestFruitPriority) return;

            this.HarvestFruitPriority = value;
            World.HandleFruitPlantUpdate(this);
        }

        public virtual bool DoHarvestJob(double workSpeed)
        {
            if (!this.HarvestJobProgress.HasValue)
            {
                this.HarvestJobProgress = 0;
            }
            else if (this.HarvestJobProgress < 1f)
            {
                if (workSpeed < 0.1) workSpeed = 0.1;
                this.HarvestJobProgress += (float)workSpeed / 600f;
            }
            else
            {
                this.RemoveFruit(Rand.Next(2) == 0);   // Havesting reduces chance of reseeding
                return true;
            }

            return false;
        }

        public virtual void RemoveFruit(bool leaveSeed)
        {
            this.ReservedByAnimalID = 0;
            this.HarvestJobProgress = null;
            this.seedNextFrame = leaveSeed;
        }

        public virtual void Reserve(int animalID)
        {
            this.ReservedByAnimalID = animalID;
        }

        protected override bool HasColonistJob()
        {
            return base.HasColonistJob() || this.HarvestFruitPriority != WorkPriority.Disabled;
        }
    }
}
