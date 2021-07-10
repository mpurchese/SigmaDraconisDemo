namespace SigmaDraconis.AI
{
    using ProtoBuf;
    using Shared;
    using World;
    using WorldInterfaces;

    [ProtoContract]
    public class ActionPickupKek : ActionBase
    {
        [ProtoMember(1)]
        private readonly int dispenserID;

        [ProtoMember(2)]
        private int counter = 0;

        // Deserialisation ctor
        protected ActionPickupKek() { }

        public ActionPickupKek(IColonist colonist, int dispenserID) : base(colonist)
        {
            this.dispenserID = dispenserID;
        }

        public override void Update()
        {
            if (!(World.GetThing(this.dispenserID) is IKekDispenser dispenser))
            {
                this.IsFinished = true;
                return;
            }

            this.OpenDoorIfExists();
            this.Colonist.IsWorking = false;

            if (this.counter > 0 && (dispenser.DispenserStatus == DispenserStatus.Standby || dispenser.DispenserStatus == DispenserStatus.NoResource))
            {
                this.Colonist.CarriedItemTypeArms = ItemType.Kek;
                EventManager.EnqueueWorldPropertyChangeEvent(this.Colonist.Id, nameof(IColonist.CarriedItemTypeArms), this.Colonist.MainTile.Row, ThingType.Colonist);
                this.IsFinished = true;
                return;
            }

            if (!dispenser.TakeKek())
            {
                this.IsFinished = true;
                return;
            }

            this.counter++;
            base.Update();
        }
    }
}
