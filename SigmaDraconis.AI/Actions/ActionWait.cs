namespace SigmaDraconis.AI
{
    using ProtoBuf;
    using WorldInterfaces;

    [ProtoContract]
    public class ActionWait : ActionBase
    {
        [ProtoMember(1)]
        private int counter = 0;

        [ProtoMember(2)]
        private int waitTime;

        // Deserialisation ctor
        protected ActionWait() { }

        public ActionWait(IColonist colonist, int waitTime = 60) : base(colonist)
        {
            this.waitTime = waitTime;
           // this.ApplyTileBlock();
        }

        public override void AfterDeserialization()
        {
            base.AfterDeserialization();
           // this.ApplyTileBlock();
        }

        public override void Update()
        {
            this.Colonist.IsWorking = false;
            this.Colonist.IsWaiting = true;
            counter++;
            if (counter >= this.waitTime)
            {
                this.IsFinished = true;
               // this.ReleaseTileBlock();
                return;
            }

            base.Update();
        }
    }
}
