namespace SigmaDraconis.AI
{
    using ProtoBuf;
    using WorldInterfaces;

    [ProtoContract]
    public class ActionSleep : ActionBase
    {
        // Deserialisation ctor
        protected ActionSleep() { }

        public ActionSleep(IColonist colonist) : base(colonist)
        {
            if (colonist.Body.Energy < 100) colonist.BeginSleeping();
            else this.IsFinished = true;
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
            if (!this.Colonist.Body.IsSleeping)
            {
                this.IsFinished = true;
                this.ReleaseTileBlock();
                return;
            }

            base.Update();
        }
    }
}
