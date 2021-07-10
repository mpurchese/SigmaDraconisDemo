namespace SigmaDraconis.AI
{
    using ProtoBuf;
    using Draconis.Shared;
    using Shared;
    using World.PathFinding;
    using WorldInterfaces;

    [ProtoContract]
    public class ActivityRoam : ActivityBase
    {
        // Deserialisation ctor
        protected ActivityRoam() { }

        public ActivityRoam(IColonist colonist, Path path) : base(colonist)
        {
            this.CurrentAction = new ActionWalk(colonist, path, Vector2f.Zero, Direction.None, 0.25f);
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
