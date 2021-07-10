namespace SigmaDraconis.UI
{
    using Draconis.UI;
    using Draconis.Shared;
    using Shared;
    using WorldInterfaces;

    public class DispenserPanel : BuildingPanel, IThingPanel
    {
        private readonly PowerButton powerButton;
        private readonly BuildingStatusControl statusControl;

        public DispenserPanel(IUIElement parent, int y)
            : base(parent, y)
        {
            this.powerButton = new PowerButton(this, Scale(290), Scale(16));
            this.AddChild(this.powerButton);
            this.powerButton.MouseLeftClick += this.OnOnOffButtonClick;

            this.statusControl = new BuildingStatusControl(this, Scale(38), Scale(44), Scale(248), Scale(20));
            this.AddChild(this.statusControl);
        }

        public override void Update()
        {
            if (this.building is IDispenser dispenser && this.IsBuildingUiVisible)
            {
                this.powerButton.IsVisible = true;
                this.powerButton.IsOn = dispenser.IsDispenserSwitchedOn;
                this.statusControl.IsVisible = true;
                this.statusControl.ProgressFraction = dispenser.DispenserProgress * 0.01;

                switch (dispenser.DispenserStatus)
                {
                    case DispenserStatus.Standby:
                        if (!dispenser.IsDispenserSwitchedOn) this.statusControl.SetStatus(BuildingDisplayStatus.Offline, UIColour.RedText, UIColour.BuildingWorkBar);
                        else this.statusControl.SetStatus(BuildingDisplayStatus.Standby, UIColour.GreenText, UIColour.BuildingWorkBar);
                        break;
                    case DispenserStatus.NoResource:
                        if (!dispenser.IsDispenserSwitchedOn) this.statusControl.SetStatus(BuildingDisplayStatus.Offline, UIColour.RedText, UIColour.BuildingWorkBar);
                        else this.statusControl.SetStatus(BuildingDisplayStatus.NoResource, UIColour.RedText, UIColour.BuildingWorkBar);
                        break;
                    case DispenserStatus.Preparing:
                        this.statusControl.SetStatus(BuildingDisplayStatus.Filling, UIColour.OrangeText, UIColour.GreenText);
                        break;
                    case DispenserStatus.Full:
                        this.statusControl.SetStatus(BuildingDisplayStatus.Standby, UIColour.GreenText, UIColour.GreenText);
                        break;
                    case DispenserStatus.InUse:
                        this.statusControl.SetStatus(BuildingDisplayStatus.InUse, UIColour.OrangeText, UIColour.BuildingWorkBar);
                        break;
                }
            }
            else
            {
                this.powerButton.IsVisible = false;
                this.statusControl.IsVisible = false;
            }

            base.Update();
        }

        private void OnOnOffButtonClick(object sender, MouseEventArgs e)
        {
            this.powerButton.IsOn = !this.powerButton.IsOn;
            (this.building as IDispenser).IsDispenserSwitchedOn = this.powerButton.IsOn;
        }
    }
}
