namespace SigmaDraconis.AI
{
    using ProtoBuf;
    using Config;
    using Shared;
    using World;
    using WorldInterfaces;

    [ProtoContract]
    public class ActionCook : ActionBase
    {
        public ICooker Cooker { get; private set; }

        [ProtoMember(2)]
        private readonly int cookerId;

        // Deserialisation ctor
        protected ActionCook() { }

        public ActionCook(IColonist colonist, ICooker cooker) : base(colonist)
        {
            this.Cooker = cooker;
            this.cookerId = cooker.Id;
        }

        public override void AfterDeserialization()
        {
            this.Cooker = World.GetThing(this.cookerId) as ICooker;
            base.AfterDeserialization();
        }

        public override void Update()
        {
            this.Colonist.IsWorking = true;
            this.OpenDoorIfExists();

            if (this.Cooker?.IsReady != true)
            {
                this.Colonist.IsWorking = false;
                this.IsFinished = true;
            }
            else
            {
                switch (this.Cooker?.FactoryStatus)
                {
                    case FactoryStatus.Standby: this.Cooker.Open(); break;
                    case FactoryStatus.Open:
                        // We have to call Fill once per frame until it returns true.
                        if (this.Cooker.Fill(this.Colonist.CarriedCropType.Value))
                        {
                            this.Colonist.CarriedItemTypeBack = ItemType.None;
                            this.Colonist.CarriedCropType = null;
                            this.Colonist.IsWorking = false;
                            this.IsFinished = true;
                            EventManager.EnqueueWorldPropertyChangeEvent(this.Colonist.Id, nameof(IColonist.CarriedItemTypeBack), null, null, this.Colonist.MainTile.Row, ThingType.Colonist);
                        }

                        break;
                    case FactoryStatus.NoPower:
                    case FactoryStatus.Offline:
                    case FactoryStatus.WaitingToDistribute:
                    case FactoryStatus.Broken:
                        this.Colonist.IsWorking = false;
                        this.IsFinished = true;
                        break;
                }
            }

            base.Update();
        }
    }
}
