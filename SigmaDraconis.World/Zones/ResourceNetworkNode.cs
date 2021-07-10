namespace SigmaDraconis.World.Zones
{
    using System.Collections.Generic;
    using WorldInterfaces;

    public class ResourceNetworkNode
    {
        private ResourceNetworkNode linkNE;
        private ResourceNetworkNode linkSE;
        private ResourceNetworkNode linkSW;
        private ResourceNetworkNode linkNW;
        private List<ResourceNetworkNode> allLinks = new List<ResourceNetworkNode>();

        public ResourceNetworkNode(int tileIndex)
        {
            this.TileIndex = tileIndex;
        }

        public int TileIndex { get; }

        public IBuildableThing ResourceBuilding { get; set; }

        public ResourceNetworkNode LinkNE
        {
            get { return this.linkNE; }
            set
            {
                this.linkNE = value;
                this.UpdateAllLinks();
            }
        }

        public ResourceNetworkNode LinkSE
        {
            get { return this.linkSE; }
            set
            {
                this.linkSE = value;
                this.UpdateAllLinks();
            }
        }

        public ResourceNetworkNode LinkSW
        {
            get { return this.linkSW; }
            set
            {
                this.linkSW = value;
                this.UpdateAllLinks();
            }
        }

        public ResourceNetworkNode LinkNW
        {
            get { return this.linkNW; }
            set
            {
                this.linkNW = value;
                this.UpdateAllLinks();
            }
        }

        public List<ResourceNetworkNode> AllLinks => this.allLinks;

        private void UpdateAllLinks()
        {
            this.allLinks = new List<ResourceNetworkNode>();
            if (this.LinkNE != null) this.allLinks.Add(this.LinkNE);
            if (this.LinkSE != null) this.allLinks.Add(this.LinkSE);
            if (this.LinkSW != null) this.allLinks.Add(this.LinkSW);
            if (this.LinkNW != null) this.allLinks.Add(this.LinkNW);
        }

        public override string ToString()
        {
            return $"Network Node {this.TileIndex}";
        }
    }
}