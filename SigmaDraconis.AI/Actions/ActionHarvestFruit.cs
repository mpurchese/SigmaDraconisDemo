namespace SigmaDraconis.AI
{
    using ProtoBuf;
    using System.Collections.Generic;
    using System.Linq;
    using Shared;
    using World;
    using World.Particles;
    using WorldInterfaces;

    [ProtoContract]
    public class ActionHarvestFruit : ActionBase
    {
        public IFruitPlant Plant { get; private set; }

        [ProtoMember(1)]
        private readonly int plantId;

        [ProtoMember(2)]
        private int counter = 0;

        private static readonly Dictionary<ThingType, int> colourIndexes 
            = new Dictionary<ThingType, int> { { ThingType.SmallPlant5, 1 }, { ThingType.Bush, 6 }, { ThingType.SmallPlant6, 7 }, { ThingType.SmallPlant9, 3 }, { ThingType.SmallPlant12, 8 } };

        // Deserialisation ctor
        protected ActionHarvestFruit() { }

        public ActionHarvestFruit(IColonist colonist, IFruitPlant plant) : base(colonist)
        {
            this.Plant = plant;
            this.plantId = plant.Id;
            this.ApplyTileBlock();
        }

        public override void AfterDeserialization()
        {
            this.Plant = World.GetThing(this.plantId) as IFruitPlant;
            base.AfterDeserialization();
            this.ApplyTileBlock();
        }

        public override void Update()
        {
            this.counter++;
            this.Colonist.IsWorking = true;
            this.OpenDoorIfExists();

            if (this.counter < 30 && this.Colonist.RaisedArmsFrame < 18) return;

            if (this.Colonist.CarriedItemTypeBack == ItemType.Crop)
            {
                if (!MicrobotParticleController.ActiveColonists.Contains(this.Colonist.Id))
                {
                    this.Colonist.IsWorking = false;
                    if (this.Colonist.RaisedArmsFrame == 0)
                    {
                        this.IsFinished = true;
                        this.ReleaseTileBlock();
                    }
                }

                return;
            }
            else if (this.Plant?.CountFruitAvailable > 0
                && this.Colonist.CarriedItemTypeBack == ItemType.None
                && (this.Plant.HarvestJobProgress > 0 || World.GetThings<ICooker>(ThingType.Cooker).Any(t => t.IsReadyToCook)))
            {
                if (this.Plant.HarvestJobProgress < 0.99f) this.DoParticles();

                // Only harvest if there is a cooker available
                var cropType = this.Plant.Definition.CropDefinitionId;
                if (cropType.HasValue && this.counter > 30 && this.Plant.DoHarvestJob(this.Colonist.GetWorkRate()))
                {
                    this.Colonist.CarriedItemTypeBack = ItemType.Crop;
                    this.Colonist.CarriedCropType = cropType;
                    switch (cropType)
                    {
                        case 100: WorldStats.Increment(WorldStatKeys.FruitHarvested1); break;
                        case 101: WorldStats.Increment(WorldStatKeys.FruitHarvested2); break;
                        case 102: WorldStats.Increment(WorldStatKeys.FruitHarvested3); break;
                        case 110: WorldStats.Increment(WorldStatKeys.FruitHarvested4); break;
                        case 111: WorldStats.Increment(WorldStatKeys.FruitHarvested5); break;
                    }

                    WorldStats.Increment(WorldStatKeys.FruitHarvested);
                    EventManager.EnqueueWorldPropertyChangeEvent(this.Colonist.Id, nameof(IColonist.CarriedItemTypeBack), null, cropType, this.Colonist.MainTile.Row, ThingType.Colonist);
                }
            }
            else
            { 
                this.Colonist.IsWorking = false;
                this.IsFinished = true;
                this.ReleaseTileBlock();
                return;
            }

            base.Update();
        }

        private void DoParticles()
        {
            var renderTile = this.Colonist.MainTile;
            var toolOffset = this.GetToolOffset() + this.Colonist.PositionOffset;

            // Render in a different tile if facing N, to fix render order problem
            if (this.Colonist.FacingDirection != Direction.E && this.Colonist.FacingDirection != Direction.W)
            {
                var nextTile = renderTile.GetTileToDirection(this.Colonist.FacingDirection);
                if (nextTile != null)
                {
                    renderTile = nextTile;
                    toolOffset.X += this.Colonist.MainTile.CentrePosition.X - renderTile.CentrePosition.X;
                    toolOffset.Y += this.Colonist.MainTile.CentrePosition.Y - renderTile.CentrePosition.Y;
                }
            }

            var z = 4.4f;
            var colourIndex = colourIndexes.ContainsKey(this.Plant.ThingType) ? colourIndexes[this.Plant.ThingType] : 2;
            for (int i = 0; i < 8; i++)
            {
                var tile = this.Plant.AllTiles[Rand.Next(this.Plant.AllTiles.Count)];
                var offsetX = (float)((Rand.NextDouble() - 0.5) * 10f) + tile.CentrePosition.X - renderTile.CentrePosition.X;
                var offsetY = (float)((Rand.NextDouble() - 0.5) * 5f) + tile.CentrePosition.Y - renderTile.CentrePosition.Y;

                MicrobotParticleController.AddParticle(renderTile, offsetX, offsetY, 0f, toolOffset.X, toolOffset.Y, z, this.Colonist.Id, true, colourIndex, false);
            }
        }
    }
}
