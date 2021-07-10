namespace SigmaDraconis.AI
{
    using ProtoBuf;
    using WorldInterfaces;

    [ProtoContract]
    public class ActionRest : ActionBase
    {
        [ProtoMember(1)]
        private int counter = 0;

        // Deserialisation ctor
        protected ActionRest() { }

        // Currently not used
        public ActionRest(IColonist colonist) : base(colonist)
        {
            //if (colonist.HealthLevel < 100) colonist.IsResting = true;
            //else this.IsFinished = true;
            this.ApplyTileBlock();
        }

        public override void AfterDeserialization()
        {
            base.AfterDeserialization();
            this.ApplyTileBlock();
        }

        public override void Update()
        {
            this.Colonist.IsWorking = false;
            this.StopMovement();

            this.counter++;
            if (!this.Colonist.IsResting || this.counter > 120)
            {
                this.IsFinished = true;
                this.ReleaseTileBlock();
                return;
            }

            base.Update();
        }
    }
}
