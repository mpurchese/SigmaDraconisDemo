namespace SigmaDraconis.AI
{
    using Draconis.Shared;
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using Config;
    using Shared;
    using World;
    using World.Buildings;
    using World.Particles;
    using WorldInterfaces;

    [ProtoContract]
    public class ActionConstruct : ActionBase
    {
        public IBlueprint Target { get; private set; }
        public List<IBlueprint> ConduitBlueprints { get; private set; }
        public List<IBuildableThing> Conduits { get; private set; }

        [ProtoMember(1)]
        private readonly int targetId;

        [ProtoMember(4)]
        private readonly List<int> conduitBlueprintIds;

        [ProtoMember(5)]
        private readonly List<int> conduitIds;

        // Deserialisation ctor
        protected ActionConstruct() { }

        public ActionConstruct(IColonist colonist, IBlueprint target) : base(colonist)
        {
            this.Target = target;
            this.targetId = target.Id;
            colonist.IsMoving = false;

            this.ConduitBlueprints = new List<IBlueprint>();
            this.conduitBlueprintIds = new List<int>();
            this.Conduits = new List<IBuildableThing>();
            this.conduitIds = new List<int>();

            if (target.ThingType == ThingType.ConduitNode)
            {
                var tile = target.MainTile;
                foreach (var conduitBlueprintId in World.ConfirmedBlueprints.Keys.Where(x => x > 0 && World.ConfirmedBlueprints[x].ThingType == ThingType.ConduitMajor && World.ConfirmedBlueprints[x].MainTile == tile))
                {
                    this.ConduitBlueprints.Add(World.ConfirmedBlueprints[conduitBlueprintId]);
                    this.conduitBlueprintIds.Add(conduitBlueprintId);
                }

                foreach (var conduit in tile.ThingsAll.Where(t => t.ThingType == ThingType.ConduitMajor).OfType<IConduitMajor>())
                {
                    this.Conduits.Add(conduit);
                    this.conduitIds.Add(conduit.Id);
                }
            }
            else
            {
                var conduitThingType = ThingTypeManager.GetDefinition(target.ThingType).ConduitType;
                if (conduitThingType != ThingType.None)
                {
                    foreach (var tile in target.AllTiles)
                    {
                        var conduitBlueprintId = World.ConfirmedBlueprints.Keys.FirstOrDefault(x => World.ConfirmedBlueprints[x].ThingType == conduitThingType && World.ConfirmedBlueprints[x].MainTile == tile);
                        if (conduitBlueprintId > 0)
                        {
                            this.ConduitBlueprints.Add(World.ConfirmedBlueprints[conduitBlueprintId]);
                            this.conduitBlueprintIds.Add(conduitBlueprintId);
                        }

                        if (tile.ThingsPrimary.FirstOrDefault(t => t.ThingType == conduitThingType) is IBuildableThing conduit && !conduit.IsReady)
                        {
                            this.Conduits.Add(conduit);
                            this.conduitIds.Add(conduit.Id);
                        }
                    }
                }
            }
        }

        public override void AfterDeserialization()
        {
            try
            {
                this.Target = World.ConfirmedBlueprints[this.targetId];
            }
            catch
            {
                // Blueprint is missing
                this.IsFinished = true;
            }

            if (this.conduitBlueprintIds != null) this.ConduitBlueprints = this.conduitBlueprintIds.Where(i => World.ConfirmedBlueprints.ContainsKey(i)).Select(i => World.ConfirmedBlueprints[i]).OfType<IBlueprint>().ToList();
            else this.ConduitBlueprints = new List<IBlueprint>();

            if (this.conduitIds != null) this.Conduits = this.conduitIds.Select(i => World.GetThing(i)).OfType<IBuildableThing>().ToList();
            else this.Conduits = new List<IBuildableThing>();

            base.AfterDeserialization();
        }

        public override void Update()
        {
            if (!World.ConfirmedBlueprints.ContainsKey(this.targetId) || this.Target?.BuildPriority == WorkPriority.Disabled)
            {
                // Build was cancelled or paused
                this.Colonist.IsWorking = false;
                this.IsFinished = true;
                this.IsFailed = true;
                return;
            }

            var building = this.GetOrCreateBuilding();
            if (building == null || building.IsDesignatedForRecycling)
            {
                this.Colonist.IsWorking = false;
                this.IsFinished = true;
                this.IsFailed = true;
                return;
            }

            this.Colonist.IsWorking = true;
            this.OpenDoorIfExists();

            if (World.ConfirmedBlueprints.ContainsKey(this.targetId))
            {
                (this.Target as IColonistInteractive).AssignColonist(this.Colonist.Id, this.Colonist.MainTile.Index);

                if (this.Colonist.RaisedArmsFrame == 18)
                {
                    var workSpeed = Colonist.GetWorkRate();
                    if (workSpeed < 0.1) workSpeed = 0.1;
                    var conduitProgressIncrement = 100.0 * workSpeed / Constants.FramesToConstructConduitMinor;
                    foreach (var conduit in this.Conduits)
                    {
                        if (conduit.IsReady == false && conduit.IncrementConstructionProgress(conduitProgressIncrement))
                        {
                            conduit.IsReady = true;
                            var blueprint = this.ConduitBlueprints.FirstOrDefault(b => b.MainTile == conduit.MainTile && b.AnimationFrame == conduit.AnimationFrame);
                            if (blueprint != null)
                            {
                                World.ConfirmedBlueprints.Remove(blueprint.Id);
                                EventManager.RaiseEvent(EventType.Blueprint, EventSubType.Removed, blueprint);
                            }
                        }
                    }

                    var progressIncrement = 100.0 * workSpeed / (60.0 * building.Definition.ConstructionTimeMinutes);
                    if (building.IncrementConstructionProgress(progressIncrement))
                    {
                        this.Colonist.IsWorking = false;  // Finished

                        building.AfterAddedToWorld();
                        building.AfterConstructionComplete();

                        World.ConfirmedBlueprints.Remove(this.targetId);
                        foreach (var conduit in this.Conduits)
                        {
                            if (conduit.IsReady == false)
                            {
                                // Failsafe in case conduit isn't done yet
                                conduit.IncrementConstructionProgress(100.0 - conduit.ConstructionProgress);
                                conduit.IsReady = true;
                                var blueprint = this.ConduitBlueprints.FirstOrDefault(b => b.MainTile == conduit.MainTile);
                                if (blueprint != null)
                                {
                                    World.ConfirmedBlueprints.Remove(blueprint.Id);
                                    EventManager.RaiseEvent(EventType.Blueprint, EventSubType.Removed, blueprint);
                                }
                            }
                        }

                        EventManager.RaiseEvent(EventType.Blueprint, EventSubType.Removed, this.Target);
                    }
                    else this.DoParticles();
                }
            }
            else
            {
                this.Colonist.IsWorking = false;   // Blueprint has gone
            }

            if (!this.Colonist.IsWorking && this.Colonist.RaisedArmsFrame == 0) this.IsFinished = true;

            base.Update();
        }

        private IBuildableThing GetOrCreateBuilding()
        {
            var building = this.Target.MainTile.ThingsPrimary.OfType<IBuildableThing>()
                .FirstOrDefault(t => t.ThingType == this.Target.ThingType && (!(t is IRotatableThing) || (t as IRotatableThing).Direction == this.Target.Direction));
            if (building == null)
            {
                building = BuildingFactory.Get(this.Target.ThingType, this.Target.MainTile, this.Target.Definition.Size.X, this.Target.Direction);
                if (building.ThingType != ThingType.SolarPanelArray && building.ThingType != ThingType.WindTurbine) building.AnimationFrame = this.Target.AnimationFrame;
                building.RenderAlpha = 0f;
                building.ShadowAlpha = 0f;
                World.AddThing(building);
                building.AfterAddedToWorld();
            }

            return building;
        }

        private void DoParticles()
        {
            var renderTile = this.Colonist.MainTile;
            var toolOffset = this.GetToolOffset() + this.Colonist.PositionOffset;

            // Render in target tile row
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
            var isWall = this.Target.ThingType.In(ThingType.Wall, ThingType.Door);
            var wallTile2 = isWall ? this.Target.MainTile.GetTileToDirection(this.Target.Direction) : null;
            for (int i = 0; i < 8; i++)
            {
                ISmallTile tile;
                float offsetX;
                float offsetY;
                if (this.Target.ThingType == ThingType.RocketGantry)
                {
                    tile = this.Target.AllTiles[12];
                    offsetX = (float)((Rand.NextDouble() - 0.5) * 14f) + tile.CentrePosition.X - renderTile.CentrePosition.X;
                    offsetY = (float)((Rand.NextDouble() - 0.5) * 7f) + tile.CentrePosition.Y - renderTile.CentrePosition.Y - 2;
                }
                else if (this.Target.ThingType == ThingType.Rocket)
                {
                    tile = this.Target.AllTiles[12];
                    offsetX = (float)((Rand.NextDouble() - 0.5) * 10f) + tile.CentrePosition.X - renderTile.CentrePosition.X;
                    offsetY = (float)((Rand.NextDouble() - 0.5) * 5f) + tile.CentrePosition.Y - renderTile.CentrePosition.Y - 2;
                }
                else if (this.Target.ThingType == ThingType.LaunchPad)
                {
                    // Special case for launchpad - only want central tiles
                    var r1 = Rand.Next(3);
                    var r2 = Rand.Next(3);
                    tile = this.Target.AllTiles[((r1 + 1) * 5) + r2 + 1];
                    offsetX = (float)((Rand.NextDouble() - 0.5) * 10f) + tile.CentrePosition.X - renderTile.CentrePosition.X;
                    offsetY = (float)((Rand.NextDouble() - 0.5) * 5f) + tile.CentrePosition.Y - renderTile.CentrePosition.Y;
                }
                else
                {
                    tile = this.Target.AllTiles[Rand.Next(this.Target.AllTiles.Count)];
                    offsetX = (float)((Rand.NextDouble() - 0.5) * 10f) + tile.CentrePosition.X - renderTile.CentrePosition.X;
                    offsetY = (float)((Rand.NextDouble() - 0.5) * 5f) + tile.CentrePosition.Y - renderTile.CentrePosition.Y;
                }

                if (wallTile2 != null)
                {
                    // Offset for walls / doors
                    offsetX += 0.5f * (wallTile2.CentrePosition.X - tile.CentrePosition.X);
                    offsetY += 0.5f * (wallTile2.CentrePosition.Y - tile.CentrePosition.Y);
                }
                else if (this.Target is IRenderOffsettable r)
                {
                    // Some things like grass may not be centered on the tile
                    offsetX += r.RenderPositionOffset.X;
                    offsetY += r.RenderPositionOffset.Y;
                }

                if (this.Target?.RenderAlpha >= 0.999f || (this.Target?.RenderAlpha >= 0.5f && Rand.NextDouble() > 0.5))
                {
                    MicrobotParticleController.AddParticle(renderTile, toolOffset.X, toolOffset.Y, z, offsetX, offsetY, 0f, this.Colonist.Id, false, 1);
                }
                else
                {
                    MicrobotParticleController.AddParticle(renderTile, offsetX, offsetY, 0f, toolOffset.X, toolOffset.Y, z, this.Colonist.Id, true, 1);
                }
            }
        }
    }
}
