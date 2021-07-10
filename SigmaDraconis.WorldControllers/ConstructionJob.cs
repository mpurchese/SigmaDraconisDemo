namespace SigmaDraconis.WorldControllers
{
    using ProtoBuf;
    using Shared;
    using World;
    using World.Buildings;
    using WorldInterfaces;

    [ProtoContract]
    public class ConstructionJob
    {
        protected static int nextId = 0;

        private IBlueprint targetBlueprint;
        private IBuildableThing building;

        [ProtoMember(1)]
        public int ID { get; private set; }

        [ProtoMember(3)]
        public int? BuildingId { get; private set; }

        [ProtoMember(4)]
        public int TargetBlueprintId { get; private set; }

        [ProtoMember(5)]
        public int TotalFrames { get; private set; }

        [ProtoMember(6)]
        public int ElapsedFrames { get; private set; }

        public bool IsFinished => this.ElapsedFrames >= this.TotalFrames;

        // Ctor for deserialization
        public ConstructionJob()
        {
        }

        public ConstructionJob(IBlueprint target, int frames)
        {
            this.ID = ++nextId;
            this.targetBlueprint = target;
            this.TargetBlueprintId = target.Id;
            this.TotalFrames = frames;
            this.ElapsedFrames = 0;
        }

        [ProtoAfterDeserialization]
        private void AfterDeserialization()
        {
            if (this.ID >= nextId) nextId = this.ID + 1;
            this.targetBlueprint = World.GetThing(this.TargetBlueprintId) as IBlueprint;
            if (this.BuildingId.HasValue) this.building = World.GetThing(this.BuildingId.Value) as IBuildableThing;
            if (this.IsFinished) return;
        }

        public void Start()
        {
            this.CreateBuilding();
        }

        public void Update()
        {
            // TODO
        }

        protected void CreateBuilding()
        {
            var building = BuildingFactory.Get(this.targetBlueprint.ThingType, this.targetBlueprint.MainTile, this.targetBlueprint.Definition.Size.X, this.targetBlueprint.Direction);
            if (building.ThingType != ThingType.SolarPanelArray && building.ThingType != ThingType.WindTurbine) building.AnimationFrame = this.targetBlueprint.AnimationFrame;
            building.RenderAlpha = 0f;
            building.ShadowAlpha = 0f;
            World.AddThing(building);
            building.AfterAddedToWorld();
            this.BuildingId = building.Id;
        }
    }
}
