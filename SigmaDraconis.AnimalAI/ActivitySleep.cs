namespace SigmaDraconis.AnimalAI
{
    using ProtoBuf;
    using Draconis.Shared;
    using World.PathFinding;
    using WorldInterfaces;

    [ProtoContract]
    public class ActivitySleep : ActivityBase
    {
        [ProtoMember(1)]
        private readonly int frames;

        // Deserialisation ctor
        protected ActivitySleep() { }

        public ActivitySleep(IAnimal animal, int frames, Path path = null) : base(animal)
        {
            this.frames = frames;

            if (path != null) this.CurrentAction = new ActionWalk(animal, path, Vector2f.Zero);
            else this.CurrentAction = new ActionSleep(animal, frames);
        }

        public override void Update()
        {
            if (this.CurrentAction?.IsFinished != true) this.CurrentAction.Update();
            if (this.CurrentAction?.IsFinished != false)
            {
                if (this.CurrentAction is ActionWalk) this.CurrentAction = new ActionSleep(this.Animal, this.frames);
                else if (this.CurrentAction is ActionSleep) this.CurrentAction = new ActionWait(this.Animal, 60);   // Stop for a moment when waking up
                else this.IsFinished = true;
            }
        }
    }
}
