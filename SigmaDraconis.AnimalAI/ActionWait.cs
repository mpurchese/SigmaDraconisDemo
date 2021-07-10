namespace SigmaDraconis.AnimalAI
{
    using ProtoBuf;
    using WorldInterfaces;

    [ProtoContract]
    public class ActionWait : ActionBase
    {
        [ProtoMember(1)]
        private int counter = 0;

        [ProtoMember(2)]
        private readonly int waitTime;

        // Deserialisation ctor
        protected ActionWait() { }

        public ActionWait(IAnimal animal, int waitTime = 60) : base(animal)
        {
            this.waitTime = waitTime;
        }

        public override void AfterDeserialization()
        {
            base.AfterDeserialization();
        }

        public override void Update()
        {
            counter++;
            if (counter >= this.waitTime)
            {
                this.IsFinished = true;
                return;
            }

            base.Update();
        }
    }
}
