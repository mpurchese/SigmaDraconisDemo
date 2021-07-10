namespace SigmaDraconis.AnimalAI
{
    using ProtoBuf;
    using WorldInterfaces;

    [ProtoContract]
    public class ActivityWait : ActivityBase
    {
        // Deserialisation ctor
        protected ActivityWait() { }

        public ActivityWait(IAnimal animal) : base(animal)
        {
            this.CurrentAction = new ActionWait(animal, 60);
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
