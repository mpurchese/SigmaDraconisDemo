namespace SigmaDraconis.AnimalAI
{
    using ProtoBuf;
    using Draconis.Shared;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class ActionSleep : ActionBase
    {
        [ProtoMember(1)]
        private int counter = 0;

        [ProtoMember(2)]
        private readonly int sleepTime;

        // Deserialisation ctor
        protected ActionSleep() { }

        public ActionSleep(IAnimal animal, int sleepTime = 60) : base(animal)
        {
            this.sleepTime = sleepTime;
            this.ApplyTileBlock();
        }

        public override void AfterDeserialization()
        {
            base.AfterDeserialization();
            this.ApplyTileBlock();
        }

        public override void Update()
        {
            if (!this.Animal.IsResting)
            {
                // Rotate to NE, SE, SW or NW before resting
                var targetDirection = Direction.NE;
                if (this.Animal.Rotation >= Mathf.PI * 0.5f && this.Animal.Rotation < Mathf.PI) targetDirection = Direction.SE;
                else if (this.Animal.Rotation >= Mathf.PI && this.Animal.Rotation < 1.5f * Mathf.PI) targetDirection = Direction.SW;
                else if (this.Animal.Rotation >= Mathf.PI * 1.5f) targetDirection = Direction.NW;
                var angle = DirectionHelper.GetAngleFromDirection(targetDirection);
                if (this.RotateToAngle(angle, out _) && !this.Animal.IsResting) this.Animal.BeginResting();
            }

            counter++;
            if (counter >= this.sleepTime)
            {
                if (this.Animal.IsResting) this.Animal.FinishResting();
                this.IsFinished = true;
                this.ReleaseTileBlock();
                return;
            }

            base.Update();
        }
    }
}
