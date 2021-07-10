namespace SigmaDraconis.UI
{
    using Draconis.UI;
    using Shared;
    using WorldInterfaces;

    public class CookerPanel : FactoryBuildingPanel, IThingPanel
    {
        public CookerPanel(IUIElement parent, int y) : base(parent, y, true, false)
        {
        }

        protected override void UpdateStatusControl(IFactoryBuilding building)
        {
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
                case FactoryStatus.InProgressReverse:
                    this.statusControl.SetStatus(BuildingDisplayStatus.InProgress, UIColour.GreenText);
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
                case FactoryStatus.Opening:
                case FactoryStatus.Open:
                case FactoryStatus.Closing:
                    this.statusControl.SetStatus(BuildingDisplayStatus.Filling, UIColour.GreenText);
                    break;
                case FactoryStatus.NoResource:
                    this.statusControl.SetStatus(BuildingDisplayStatus.NotEnoughWater, UIColour.RedText);
                    break;
            }
        }
    }
}
