namespace SigmaDraconis.UI
{
    using Draconis.UI;
    using Shared;
    using SigmaDraconis.WorldInterfaces;

    public class MinePanel : FactoryBuildingPanel, IThingPanel
    {
        private readonly MineResourceDiamond resourceDiamond;

        public MinePanel(IUIElement parent, int y)
            : base(parent, y, true, true, 236)
        {
            this.resourceDiamond = new MineResourceDiamond(this, Scale(62), Scale(88));
            this.AddChild(this.resourceDiamond);
        }

        public override void SetInventoryTarget(ItemType producedItemType, int defaultTarget)
        {
            base.SetInventoryTarget(producedItemType, defaultTarget);
            this.deconstructConduitNodeButton.Y = this.H - Scale(50);
            this.deconstructFoundationButton.Y = this.H - Scale(50);
        }

        public override void Update()
        {
            base.Update();

            if (this.building is IMine mine && this.IsBuildingUiVisible)
            {
                this.resourceDiamond.IsVisible = true;
                this.resourceDiamond.SetMine(mine);
            }
            else
            {
                this.resourceDiamond.IsVisible = false;
            }
        }

        protected override void UpdateStatusControl(IFactoryBuilding building)
        {
            if (!(building is IMine mine)) return;

            this.statusControl.ProgressFraction = building.FactoryProgress;
            this.statusControl.IsAutoRestartEnabled = building.IsAutoRestartEnabled;
            this.statusControl.SetTimeRemaining(building.FactoryStatus == FactoryStatus.InProgress ? building.FramesRemaining : 0);

            switch (building.FactoryStatus)
            {
                case FactoryStatus.Offline:
                    this.statusControl.SetStatus(BuildingDisplayStatus.Offline, UIColour.RedText);
                    break;
                case FactoryStatus.Initialising:
                    this.statusControl.SetStatus(BuildingDisplayStatus.Initialising, UIColour.OrangeText);
                    break;
                case FactoryStatus.Standby:
                    this.statusControl.SetStatus(BuildingDisplayStatus.Standby, UIColour.OrangeText);
                    break;
                case FactoryStatus.InProgress:
                    if (mine.IsMiningUnknownResource) this.statusControl.SetStatus(BuildingDisplayStatus.MiningUnknown, UIColour.GreenText);
                    else if (mine.CurrentResource == ItemType.IronOre) this.statusControl.SetStatus(BuildingDisplayStatus.MiningOre, UIColour.GreenText);
                    else if (mine.CurrentResource == ItemType.Coal) this.statusControl.SetStatus(BuildingDisplayStatus.MiningCoal, UIColour.GreenText);
                    else if (mine.CurrentResource == ItemType.Stone) this.statusControl.SetStatus(BuildingDisplayStatus.MiningStone, UIColour.GreenText);
                    break;
                case FactoryStatus.Pausing:
                    this.statusControl.SetStatus(BuildingDisplayStatus.Pausing, UIColour.OrangeText);
                    break;
                case FactoryStatus.Paused:
                    this.statusControl.SetStatus(BuildingDisplayStatus.Paused, UIColour.OrangeText);
                    break;
                case FactoryStatus.Resuming:
                    this.statusControl.SetStatus(BuildingDisplayStatus.Resuming, UIColour.OrangeText);
                    break;
                case FactoryStatus.WaitingToDistribute:
                    this.statusControl.SetStatus(BuildingDisplayStatus.SilosFull, UIColour.OrangeText);
                    break;
                case FactoryStatus.Broken:
                    this.statusControl.SetStatus(BuildingDisplayStatus.Broken, UIColour.RedText);
                    break;
                case FactoryStatus.NoPower:
                    this.statusControl.SetStatus(BuildingDisplayStatus.NoPower, UIColour.RedText);
                    break;
                case FactoryStatus.NoResource:
                    this.statusControl.SetStatus(BuildingDisplayStatus.NoResource, UIColour.RedText);
                    break;
            }
        }
    }
}
