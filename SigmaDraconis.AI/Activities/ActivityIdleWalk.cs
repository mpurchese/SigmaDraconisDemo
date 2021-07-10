namespace SigmaDraconis.AI
{
    using ProtoBuf;
    using Draconis.Shared;
    using World.PathFinding;
    using WorldInterfaces;

    [ProtoContract]
    public class ActivityIdleWalk : ActivityBase
    {
        // Deserialisation ctor
        protected ActivityIdleWalk() { }

        public ActivityIdleWalk(IColonist colonist, Path path) : base(colonist)
        {
            this.CurrentAction = new ActionWalk(colonist, path, Vector2f.Zero);
        }

        public override void Update()
        {
            if (this.CurrentAction?.IsFinished != false)
            {
                this.IsFinished = true;
                return;
            }

            this.CurrentAction.Update();
        }
    }
}
