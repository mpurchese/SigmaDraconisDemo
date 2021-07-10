namespace SigmaDraconis.AI
{
    using ProtoBuf;
    using Shared;
    using World;
    using WorldInterfaces;
    using WorldControllers;

    [ProtoContract]
    public class ActionPickupFromNetwork : ActionBase
    {
        [ProtoMember(1)]
        private int counter = 0;

        public IColonistInteractive Thing { get; private set; }

        [ProtoMember(2)]
        private readonly int thingId;

        [ProtoMember(3)]
        private readonly ItemType itemType;

        // Deserialisation ctor
        protected ActionPickupFromNetwork() { }

        public ActionPickupFromNetwork(IColonist colonist, IColonistInteractive thing, ItemType itemType) : base(colonist)
        {
            this.Thing = thing;
            this.thingId = thing.Id;
            this.itemType = itemType;
        }

        public override void AfterDeserialization()
        {
            this.Thing = World.GetThing(this.thingId) as IColonistInteractive;
            base.AfterDeserialization();
        }

        public override void Update()
        {
            if (!(this.Thing is IBuildableThing building))
            {
                // For some reason building can be missing after load
                this.IsFailed = true;
                this.IsFinished = true;
                return;
            }

            counter++;
            this.Colonist.IsWorking = true;
            this.OpenDoorIfExists();

            if (this.Thing is IResourceProcessor processor)
            {
                if (this.Colonist.CarriedItemTypeBack == ItemType.None && processor.RequestUnprocessedResource(this.itemType))
                {
                    this.Colonist.CarriedItemTypeBack = this.itemType;
                    ResourceStackingController.JobInTransit(this.Colonist);
                    EventManager.EnqueueWorldPropertyChangeEvent(this.Colonist.Id, nameof(IColonist.CarriedItemTypeBack), this.Colonist.MainTile.Row, ThingType.Colonist);
                    this.Colonist.IsWorking = false;
                    this.IsFinished = true;
                }
            }
            else if (counter >= 60)
            {
                this.Colonist.IsWorking = false;
                if (this.Colonist.RaisedArmsFrame == 0) this.IsFinished = true;
            }
            else if (counter == 30)
            {
                // Take the resource
                if (World.ResourceNetwork?.TakeItems(building, this.itemType, 1) > 0)
                {
                    this.Colonist.CarriedItemTypeBack = this.itemType;
                    ResourceStackingController.JobInTransit(this.Colonist);
                    EventManager.EnqueueWorldPropertyChangeEvent(this.Colonist.Id, nameof(IColonist.CarriedItemTypeBack), this.Colonist.MainTile.Row, ThingType.Colonist);
                }
            }

            base.Update();
        }
    }
}
