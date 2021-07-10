namespace SigmaDraconis.AnimalAI
{
    using ProtoBuf;
    using Draconis.Shared;
    using Shared;
    using World.PathFinding;
    using WorldInterfaces;

    [ProtoContract]
    public class ActivityWalk : ActivityBase
    {
        // Deserialisation ctor
        protected ActivityWalk() { }

        public ActivityWalk(IAnimal animal, Path path) : base(animal)
        {
            this.CurrentAction = new ActionWalk(animal, path, Vector2f.Zero, endOffsetFlexibility: animal.ThingType == ThingType.RedBug ? 0.2f : 0.1f);
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
