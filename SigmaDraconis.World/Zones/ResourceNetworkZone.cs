namespace SigmaDraconis.World.Zones
{
    using System.Collections.Generic;
    using System.Linq;
    using ResourceNetworks;
    using Shared;
    using WorldInterfaces;

    public class ResourceNetworkZone
    {
        public Dictionary<int, ResourceNetworkNode> Nodes { get; set; } = new Dictionary<int, ResourceNetworkNode>();

        public ResourceNetworkZone()
        {
            EventManager.Subscribe(EventType.Building, EventSubType.Ready, delegate (object obj) { this.OnBuildingReady(obj); });
            EventManager.Subscribe(EventType.Building, EventSubType.Recycling, delegate (object obj) { this.OnBuildingRecycling(obj); });
        }

        public ResourceNetworkNode AddNode(int tileIndex)
        {
            var tile = World.GetSmallTile(tileIndex);
            var node = new ResourceNetworkNode(tileIndex);

            var resourceBuilding = tile.ThingsAll.FirstOrDefault(b => b is IBuildableThing building && ResourceNetwork.IsNetworkBuilding(building) && building.IsReady);
            if (resourceBuilding != null) node.ResourceBuilding = resourceBuilding as IBuildableThing;

            if (tile.TileToNE != null && this.Nodes.ContainsKey(tile.TileToNE.Index))
            {
                node.LinkNE = this.Nodes[tile.TileToNE.Index];
                node.LinkNE.LinkSW = node;
            }

            if (tile.TileToSE != null && this.Nodes.ContainsKey(tile.TileToSE.Index))
            {
                node.LinkSE = this.Nodes[tile.TileToSE.Index];
                node.LinkSE.LinkNW = node;
            }

            if (tile.TileToSW != null && this.Nodes.ContainsKey(tile.TileToSW.Index))
            {
                node.LinkSW = this.Nodes[tile.TileToSW.Index];
                node.LinkSW.LinkNE = node;
            }

            if (tile.TileToNW != null && this.Nodes.ContainsKey(tile.TileToNW.Index))
            {
                node.LinkNW = this.Nodes[tile.TileToNW.Index];
                node.LinkNW.LinkSE = node;
            }

            this.Nodes.Add(tileIndex, node);
            return node;
        }

        public void RemoveNode(int tileIndex)
        {
            var tile = World.GetSmallTile(tileIndex);

            if (tile.TileToNE != null && this.Nodes.ContainsKey(tile.TileToNE.Index))
            {
                this.Nodes[tile.TileToNE.Index] = null;
            }

            if (tile.TileToSE != null && this.Nodes.ContainsKey(tile.TileToSE.Index))
            {
                this.Nodes[tile.TileToSE.Index] = null;
            }

            if (tile.TileToSW != null && this.Nodes.ContainsKey(tile.TileToSW.Index))
            {
                this.Nodes[tile.TileToSW.Index] = null;
            }

            if (tile.TileToNW != null && this.Nodes.ContainsKey(tile.TileToNW.Index))
            {
                this.Nodes[tile.TileToNW.Index] = null;
            }

            this.Nodes.Remove(tileIndex);
        }

        public void Clear()
        {
            this.Nodes.Clear();
        }

        public bool ContainsNode(int tileIndex)
        {
            return this.Nodes.ContainsKey(tileIndex);
        }

        private void OnBuildingReady(object obj)
        {
            var building = obj as IBuildableThing;
            if (building == null || !ResourceNetwork.IsNetworkBuilding(building)) return;

            var nodes = this.Nodes.Where(n => building.AllTiles.Select(t => t.Index).Contains(n.Key));
            foreach (var node in nodes)
            {
                node.Value.ResourceBuilding = building;
            }
        }

        private void OnBuildingRecycling(object obj)
        {
            var building = obj as IBuildableThing;
            if (building == null || !ResourceNetwork.IsNetworkBuilding(building)) return;

            var nodes = this.Nodes.Where(n => building.AllTiles.Select(t => t.Index).Contains(n.Key));
            foreach (var node in nodes)
            {
                node.Value.ResourceBuilding = null;
            }
        }
    }
}
