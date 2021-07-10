namespace SigmaDraconis.UI
{
    using Draconis.Shared;
    using Draconis.UI;
    using WorldInterfaces;

    public class GeneratorPanelBase : BuildingPanel, IThingPanel
    {
        protected PowerButtonWithUsageDisplay powerButton;
        protected BuildingStatusControl statusControl;
        protected BuildingMaintenanceControl maintenanceControl;

        public GeneratorPanelBase(IUIElement parent, int y)
            : base(parent, y)
        {
            this.AddPowerButton();
        }

        public GeneratorPanelBase(IUIElement parent, int y, int h)
            : base(parent, y, h)
        {
            this.AddPowerButton();
        }

        private void AddPowerButton()
        {
            this.powerButton = new PowerButtonWithUsageDisplay(this, 0, Scale(16));
            this.powerButton.X = Scale(312) - this.powerButton.W;
            this.powerButton.MouseLeftClick += this.OnOnOffButtonClick;
            this.AddChild(this.powerButton);
        }

        protected virtual void OnOnOffButtonClick(object sender, MouseEventArgs e)
        {
            this.powerButton.IsOn = !this.powerButton.IsOn;
            if (this.building is IFactoryBuilding factory && factory.IsSwitchedOn != this.powerButton.IsOn) factory.TogglePower();
        }

        public override void ApplyLayout()
        {
            base.ApplyLayout();
            this.powerButton.X = Scale(312) - this.powerButton.W;
        }
    }
}
