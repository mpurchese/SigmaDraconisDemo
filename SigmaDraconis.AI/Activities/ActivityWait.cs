namespace SigmaDraconis.AI
{
    using ProtoBuf;
    using WorldInterfaces;

    [ProtoContract]
    public class ActivityWait : ActivityBase
    {
        // Deserialisation ctor
        protected ActivityWait() { }

        public ActivityWait(IColonist colonist) : base(colonist)
        {
            this.CurrentAction = new ActionWait(colonist, 60);
        }

        public override void Update()
        {
            if (this.CurrentAction?.IsFinished != true) this.CurrentAction.Update();
            if (this.CurrentAction?.IsFinished != false)
            {
                this.IsFinished = true;
                return;
            }
        }
    }
}
