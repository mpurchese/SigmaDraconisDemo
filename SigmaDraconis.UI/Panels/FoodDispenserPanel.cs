namespace SigmaDraconis.UI
{
    using Draconis.UI;
    using Draconis.Shared;
    using Config;
    using Language;
    using Shared;
    using World;
    using WorldInterfaces;

    public class FoodDispenserPanel : BuildingPanel, IThingPanel
    {
        private readonly PowerButton powerButton;
        private readonly BuildingStatusControl statusControl;
        private readonly TickBoxIconButtonWithCount allowMushButton;
        private readonly TickBoxIconButtonWithCount allowFoodButton;
        private readonly TextLabel foodTypeLabel;

        public FoodDispenserPanel(IUIElement parent, int y)
            : base(parent, y)
        {
            this.powerButton = new PowerButton(this, Scale(290), Scale(16));
            this.AddChild(this.powerButton);
            this.powerButton.MouseLeftClick += this.OnOnOffButtonClick;

            this.allowFoodButton = new TickBoxIconButtonWithCount(this, Scale(248), Scale(36), ItemType.Food) { IsTicked = true };
            this.allowFoodButton.MouseLeftClick += this.OnAllowFoodButtonClick;
            this.AddChild(this.allowFoodButton);

            this.allowMushButton = new TickBoxIconButtonWithCount(this, Scale(248), Scale(60), ItemType.Mush) { IsTicked = true };
            this.allowMushButton.MouseLeftClick += this.OnAllowMushButtonClick;
            this.AddChild(this.allowMushButton);

            this.statusControl = new BuildingStatusControl(this, 0, Scale(44), Scale(248), Scale(20));
            this.AddChild(this.statusControl);

            this.foodTypeLabel = new TextLabel(this, 0, Scale(70), Scale(248), Scale(20), "", UIColour.DefaultText);
            this.AddChild(this.foodTypeLabel);
        }

        public override void Update()
        {
            if (this.building is IFoodDispenser dispenser && this.IsBuildingUiVisible)
            {
                this.powerButton.IsVisible = true;
                this.powerButton.IsOn = dispenser.IsDispenserSwitchedOn;
                this.statusControl.IsVisible = true;
                this.statusControl.ProgressFraction = dispenser.DispenserProgress * 0.01;

                this.allowFoodButton.IsVisible = true;
                this.allowMushButton.IsVisible = true;
                this.allowMushButton.Count = World.ResourceNetwork?.GetItemTotal(ItemType.Mush) ?? 0;
                this.allowFoodButton.Count = World.ResourceNetwork?.GetItemTotal(ItemType.Food) ?? 0;
                this.allowMushButton.IsTicked = dispenser.AllowMush;
                this.allowFoodButton.IsTicked = dispenser.AllowFood;

                switch (dispenser.DispenserStatus)
                {
                    case DispenserStatus.NoResource:
                        this.foodTypeLabel.Text = "";
                        if (!dispenser.IsDispenserSwitchedOn) this.statusControl.SetStatus(BuildingDisplayStatus.Offline, UIColour.RedText, UIColour.BuildingWorkBar);
                        else this.statusControl.SetStatus(BuildingDisplayStatus.NoFood, UIColour.RedText, UIColour.BuildingWorkBar);
                        break;
                    case DispenserStatus.Standby:
                        this.foodTypeLabel.Text = "";
                        if (!dispenser.IsDispenserSwitchedOn) this.statusControl.SetStatus(BuildingDisplayStatus.Offline, UIColour.RedText, UIColour.BuildingWorkBar);
                        else this.statusControl.SetStatus(BuildingDisplayStatus.Standby, UIColour.GreenText, UIColour.BuildingWorkBar);
                        break;
                    case DispenserStatus.Preparing:
                        this.statusControl.SetStatus(BuildingDisplayStatus.Preparing, UIColour.OrangeText, UIColour.GreenText);
                        this.foodTypeLabel.Text = GetString(StringsForThingPanels.DispensingFoodType, CropDefinitionManager.GetDefinition(dispenser.CurrentFoodType).DisplayNameLong);
                        break;
                    case DispenserStatus.Full:
                    case DispenserStatus.InUse:
                        this.statusControl.SetStatus(BuildingDisplayStatus.InUse, UIColour.OrangeText, UIColour.BuildingWorkBar);
                        this.foodTypeLabel.Text = GetString(StringsForThingPanels.DispensingFoodType, CropDefinitionManager.GetDefinition(dispenser.CurrentFoodType).DisplayNameLong);
                        break;
                }
            }
            else
            {
                this.powerButton.IsVisible = false;
                this.statusControl.IsVisible = false;
                this.allowFoodButton.IsVisible = false;
                this.allowMushButton.IsVisible = false;
            }

            base.Update();
        }

        private void OnAllowMushButtonClick(object sender, MouseEventArgs e)
        {
            if (this.building is IFoodDispenser f) f.AllowMush = !f.AllowMush;
        }

        private void OnAllowFoodButtonClick(object sender, MouseEventArgs e)
        {
            if (this.building is IFoodDispenser f) f.AllowFood = !f.AllowFood;
        }

        private void OnOnOffButtonClick(object sender, MouseEventArgs e)
        {
            this.powerButton.IsOn = !this.powerButton.IsOn;
            (this.building as IDispenser).IsDispenserSwitchedOn = this.powerButton.IsOn;
        }
    }
}
