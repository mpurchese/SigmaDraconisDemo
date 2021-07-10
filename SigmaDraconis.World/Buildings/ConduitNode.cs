namespace SigmaDraconis.World.Buildings
{
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using Language;
    using WorldInterfaces;
    using Shared;

    [ProtoContract]
    public class ConduitNode : Building, IConduitNode
    {
        [ProtoMember(1)]
        private readonly HashSet<int> connectedConduits;

        public IEnumerable<IBuildableThing> ConnectedConduits => this.connectedConduits.Select(c => World.GetThing(c)).OfType<IBuildableThing>();

        public ConduitNode() : base()
        {
            if (this.connectedConduits == null) this.connectedConduits = new HashSet<int>();
        }

        public ConduitNode(ISmallTile tile) : base(ThingType.ConduitNode, tile, 1)
        {
            if (this.connectedConduits == null) this.connectedConduits = new HashSet<int>();
        }

        public void ConnectConduit(IBuildableThing conduit)
        {
            if (!this.connectedConduits.Contains(conduit.Id)) this.connectedConduits.Add(conduit.Id);
        }

        public void DisconnectConduit(IBuildableThing conduit)
        {
            if (this.connectedConduits.Contains(conduit.Id)) this.connectedConduits.Remove(conduit.Id);
        }

        public override void CancelRecycle()
        {
            World.ExpandBuildableAreaAroundTile(this.MainTile);
            base.CancelRecycle();
        }

        public override void Recycle()
        {
            World.RefreshBuildableArea(this.Id);
            base.Recycle();
        }

        public override void AfterAddedToWorld()
        {
            World.ExpandBuildableAreaAroundTile(this.MainTile);
            base.AfterAddedToWorld();
        }

        public override bool CanRecycle()
        {
            var buildableTilesAfterDeconstruct = new HashSet<int>();
            var buildableTilesAfterDeconstructIncludeUnderConstruction = new HashSet<int>();
            foreach (var c in World.GetThings<IBuildableThing>(ThingType.ConduitNode, ThingType.Lander).Where(t => !t.IsDesignatedForRecycling && t != this))
            {
                foreach (var t in c.MainTile.AdjacentTiles8.SelectMany(x => x.AdjacentTiles4).SelectMany(x => x.AdjacentTiles4))
                {
                    if (t.TerrainType == TerrainType.Dirt || t.TerrainType == TerrainType.Coast)
                    {
                        buildableTilesAfterDeconstructIncludeUnderConstruction.Add(t.Index);
                        if (c.IsReady) buildableTilesAfterDeconstruct.Add(t.Index);
                    }
                }
            }

            foreach (var t in this.MainTile.AdjacentTiles8.SelectMany(x => x.AdjacentTiles4).SelectMany(x => x.AdjacentTiles4))
            {
                if ((t.TerrainType == TerrainType.Dirt || t.TerrainType == TerrainType.Coast) && !buildableTilesAfterDeconstruct.Contains(t.Index))
                {
                    foreach (var thing in t.ThingsAll.OfType<IBuildableThing>().Where(b => b != this))
                    {
                        if (thing is IWall && thing.AllTiles.Any(a => buildableTilesAfterDeconstruct.Contains(a.Index))) continue;  // Walls and doors only need one tile in buildable area
                        if (thing.ThingType == ThingType.FuelFactory && thing.AllTiles.All(a => a.TerrainType == TerrainType.Coast || buildableTilesAfterDeconstruct.Contains(a.Index))) continue;
                        if (!thing.IsReady && buildableTilesAfterDeconstructIncludeUnderConstruction.Contains(t.Index)) continue;

                        // Something relies on this node
                        this.canRecycleReasonStringId = StringsForMouseCursor.InUse;
                        return false;   
                    }
                }
            }

            foreach (var conduit in this.connectedConduits.Select(c => World.GetThing(c)).OfType<IConduitMajor>())
            {
                var nodeId = conduit.Node1;
                if (nodeId == this.Id) nodeId = conduit.Node2.GetValueOrDefault();
                var node = nodeId > 0 ? World.GetThing(nodeId) as IConduitNode : null;
                if (node != null && !node.IsConnectedToLander(this.Id))
                {
                    // Would break chain to lander
                    this.canRecycleReasonStringId = StringsForMouseCursor.InUse;
                    return false;
                }
            }

            this.canRecycleReasonStringId = null;
            return true;
        }

        protected override void CompleteDeconstruction(bool refundEnergy = false)
        {
            // Cleanup major conduits
            foreach (var conduit in this.connectedConduits.Select(c => World.GetThing(c)).OfType<IConduitMajor>().Where(c => c.ThingType == ThingType.ConduitMajor))
            {
                if (conduit.Node1 != this.Id && World.GetThing(conduit.Node1) is IConduitNode node1) node1.DisconnectConduit(conduit);
                if (conduit.Node2.HasValue && conduit.Node2 != this.Id && World.GetThing(conduit.Node2.Value) is IConduitNode node2) node2.DisconnectConduit(conduit);
                World.RemoveThing(conduit);

                // Also remove any conduit blueprint
                foreach (var conduitBlueprint in World.ConfirmedBlueprints.Values.Where(bp => conduit.AllTiles.Contains(bp.MainTile) && bp.AnimationFrame == conduit.AnimationFrame && bp.ThingType == ThingType.ConduitMajor).ToList())
                {
                    World.ConfirmedBlueprints.Remove(conduitBlueprint.Id);
                    EventManager.RaiseEvent(EventType.Blueprint, EventSubType.Removed, conduitBlueprint);
                }
            }

            foreach (var conduit in this.MainTile.AdjacentTiles8.SelectMany(x => x.AdjacentTiles4).SelectMany(x => x.AdjacentTiles4)
                .SelectMany(t => t.ThingsPrimary.OfType<IConduitMinor>().Where(c => c.ConnectedNodeId == this.Id)).ToList())
            {
                World.RemoveThing(conduit);
                ConduitHelper.AddMinorConduit(conduit.MainTile, false);
            }

            base.CompleteDeconstruction(refundEnergy);
        }

        public bool IsConnectedToLander(int? excludedNode)
        {
            var openNodes = new List<IConduitNode> { this };
            var closedNodes = new HashSet<int>();
            while (openNodes.Any())
            {
                foreach (var node in openNodes.ToList())
                {
                    openNodes.Remove(node);
                    closedNodes.Add(node.Id);

                    foreach (var conduit in node.ConnectedConduits.Where(t => t.ThingType == ThingType.ConduitMajor).OfType<IConduitMajor>())
                    {
                        var nodeId = conduit.Node1;
                        if (nodeId == node.Id) nodeId = conduit.Node2.GetValueOrDefault();
                        if (nodeId == 0 || nodeId == excludedNode || closedNodes.Contains(nodeId)) continue;

                        var nextNode = World.GetThing(nodeId) as IConduitNode;
                        if (node.IsReady && !nextNode.IsReady) continue;

                        if (nextNode is ILander) return true;
                        if (nextNode != null) openNodes.Add(nextNode);
                    }
                }
            }

            return false;
        }
    }
}
