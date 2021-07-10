namespace SigmaDraconis.AI
{
    using Draconis.Shared;
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using Shared;
    using World;
    using World.Buildings;
    using World.Particles;
    using WorldInterfaces;

    [ProtoContract]
    public class ActionConstructRoof : ActionBase
    {
        public List<IBlueprint> Targets { get; private set; }

        [ProtoMember(1)]
        private readonly List<int> targetIds;

        // Deserialisation ctor
        protected ActionConstructRoof() { }

        public ActionConstructRoof(IColonist colonist, IBlueprint target) : base(colonist)
        {
            this.Targets = new List<IBlueprint> { target };
            this.targetIds = new List<int> { target.Id };

            // Get connected roofs
            var openNodes = new List<IBlueprint> { target };
            var closedNodes = new HashSet<int> { target.MainTileIndex };
            var allBlueprints = World.ConfirmedBlueprints.Values.Where(b => b.ThingType == ThingType.Roof).ToList();
            while (openNodes.Any())
            {
                var list = openNodes.ToList();
                openNodes.Clear();
                foreach (var n in list)
                {
                    foreach (var b in allBlueprints)
                    {
                        if (!closedNodes.Contains(b.MainTileIndex))
                        {
                            if ((n.MainTile.TileToNE == b.MainTile && b.MainTile.ThingsPrimary.OfType<IWall>().All(w => w.Direction != Direction.SW))
                                || (n.MainTile.TileToNW == b.MainTile && b.MainTile.ThingsPrimary.OfType<IWall>().All(w => w.Direction != Direction.SE))
                                || (n.MainTile.TileToSE == b.MainTile && n.MainTile.ThingsPrimary.OfType<IWall>().All(w => w.Direction != Direction.SE))
                                || (n.MainTile.TileToSW == b.MainTile && n.MainTile.ThingsPrimary.OfType<IWall>().All(w => w.Direction != Direction.SW)))
                            {
                                openNodes.Add(b);
                                closedNodes.Add(b.MainTileIndex);
                                this.Targets.Add(b);
                                this.targetIds.Add(b.Id);
                            }
                        }
                    }
                }
            }

            colonist.IsMoving = false;
        }

        public override void AfterDeserialization()
        {
            try
            {
                this.Targets = this.targetIds.Select(t => World.ConfirmedBlueprints[t] as IBlueprint).Where(t => t != null).ToList();
            }
            catch
            {
                // Blueprints missing
                this.IsFinished = true;
            }

            base.AfterDeserialization();

            if (!this.Targets.Any()) this.IsFinished = true;
        }

        public override void Update()
        {
            if (!this.targetIds.Any() || !World.ConfirmedBlueprints.ContainsKey(this.targetIds.First()))
            {
                // Build was cancelled
                this.Colonist.IsWorking = false;
                this.IsFinished = true;
                this.IsFailed = true;
                return;
            }

            var buildings = this.GetOrCreateBuildings();
            if (buildings.Any(b => b.IsDesignatedForRecycling))
            {
                this.Colonist.IsWorking = false;
                this.IsFinished = true;
                this.IsFailed = true;
                return;
            }

            this.Colonist.IsWorking = true;
            this.OpenDoorIfExists();

            (this.Targets.First() as IColonistInteractive).AssignColonist(this.Colonist.Id, this.Colonist.MainTile.Index);

            if (this.Colonist.RaisedArmsFrame == 18)
            {
                var isFinished = true;
                foreach (var building in buildings)
                {
                    isFinished &= building.IncrementConstructionProgress(100.0 / (60.0 * building.Definition.ConstructionTimeMinutes * buildings.Count));
                }

                if (isFinished)
                {
                    this.Colonist.IsWorking = false;  // Finished

                    foreach (var building in buildings)
                    {
                        building.AfterAddedToWorld();
                        building.AfterConstructionComplete();
                    }

                    foreach (var blueprint in this.Targets)
                    { 
                        World.ConfirmedBlueprints.Remove(blueprint.Id);
                        EventManager.RaiseEvent(EventType.Blueprint, EventSubType.Removed, blueprint);
                    }
                }
                else this.DoParticles();
            }

            if (!this.Colonist.IsWorking && this.Colonist.RaisedArmsFrame == 0) this.IsFinished = true;

            base.Update();
        }

        private List<IBuildableThing> GetOrCreateBuildings()
        {
            var buildings = new List<IBuildableThing>();
            foreach (var target in this.Targets)
            {
                var building = target.MainTile.ThingsPrimary.OfType<IBuildableThing>().FirstOrDefault(t => t.ThingType == ThingType.Roof);
                if (building == null)
                {
                    building = BuildingFactory.Get(ThingType.Roof, target.MainTile);
                    building.RenderAlpha = 0f;
                    building.ShadowAlpha = 0f;
                    World.AddThing(building);
                    building.AfterAddedToWorld();
                }

                buildings.Add(building);
            }

            return buildings;
        }

        private void DoParticles()
        {
            var renderTile = this.Colonist.MainTile;
            var toolOffset = this.GetToolOffset() + this.Colonist.PositionOffset;

            // Render in a different tile if facing N, to fix render order problem
            if (this.Colonist.FacingDirection.In(Direction.NW, Direction.N, Direction.NE) && this.Colonist.IsRenderLayer1)
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
            var target = this.Targets.First();
            for (int i = 0; i < 8; i++)
            {
                var offsetX = (float)((Rand.NextDouble() - 0.5) * 10f) + toolOffset.X;
                var offsetY = (float)((Rand.NextDouble() - 0.5) * 5f) + toolOffset.Y;

                if (target?.RenderAlpha >= 0.999f || (target?.RenderAlpha >= 0.5f && Rand.NextDouble() > 0.5))
                {
                    MicrobotParticleController.AddParticle(renderTile, toolOffset.X, toolOffset.Y, z, offsetX, offsetY, 12f, this.Colonist.Id, false, 1);
                }
                else
                {
                    MicrobotParticleController.AddParticle(renderTile, offsetX, offsetY, 12f, toolOffset.X, toolOffset.Y, z, this.Colonist.Id, true, 1);
                }
            }
        }
    }
}
