namespace SigmaDraconis.AnimalAI
{
    using System.Linq;
    using ProtoBuf;
    using Shared;
    using World;
    using WorldInterfaces;

    [ProtoContract]
    public class ActionEat : ActionBase
    {
        [ProtoMember(1)]
        private int counter = 0;

        [ProtoMember(2)]
        private readonly int eatTime;

        [ProtoMember(3)]
        private readonly int targetId;

        // Deserialisation ctor
        protected ActionEat() { }

        public ActionEat(IAnimal animal, int targetId, int eatTime = 60) : base(animal)
        {
            this.eatTime = eatTime;
            this.targetId = targetId;
            this.ApplyTileBlock();
        }

        public override void AfterDeserialization()
        {
            base.AfterDeserialization();
            this.ApplyTileBlock();
        }

        public override void Update()
        {
            if (!(World.GetThing(this.targetId) is IFruitPlant plant) || plant.CountFruitAvailable == 0)
            {
                if (this.Animal.IsEating) this.Animal.FinishEating();
                this.IsFinished = true;
                this.ReleaseTileBlock();
                return;
            }

            if (!this.Animal.IsEating)
            {
                var tile = this.Animal.MainTile;
                if (tile.ThingsAll.Contains(plant))
                {
                    if (!this.Animal.IsEating) this.Animal.BeginEating();
                }
                else
                {
                    var targetTile = tile.AdjacentTiles4.FirstOrDefault(t => t.ThingsAll.Contains(plant));
                    if (targetTile is null)
                    {
                        if (this.Animal.IsEating) this.Animal.FinishEating();
                        this.IsFinished = true;
                        this.ReleaseTileBlock();
                        return;
                    }

                    // Rotate to NE, SE, SW or NW before eating
                    var targetDirection = DirectionHelper.GetDirectionFromAdjacentPositions(tile.X, tile.Y, targetTile.X, targetTile.Y);
                    var angle = DirectionHelper.GetAngleFromDirection(targetDirection);
                    if (this.RotateToAngle(angle, out _) && !this.Animal.IsEating) this.Animal.BeginEating();
                }
            }

            if (this.Animal.IsEating)
            {
                counter++;
                if (counter >= this.eatTime)
                {
                    if (this.Animal.IsEating) this.Animal.FinishEating();
                    this.IsFinished = true;

                    plant.RemoveFruit(true);

                    this.ReleaseTileBlock();
                    return;
                }
            }

            base.Update();
        }
    }
}
