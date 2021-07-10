namespace SigmaDraconis.UI
{
    using Draconis.Shared;
    using Draconis.UI;
    using Config;
    using Language;
    using Managers;
    using Shared;
    using World;
    using WorldControllers;
    using WorldInterfaces;

    public class DoorPanel : BuildingPanel, IThingPanel
    {
        private readonly TextButton convertButton;
        private readonly IconButton lockClosedButton;
        private readonly IconButton lockOpenButton;
        private readonly IconButton unlockButton;
        private readonly SimpleTooltip convertButtonTooltip;
        private readonly Energy energyCost;

        public DoorPanel(IUIElement parent, int y) : base(parent, y)
        {
            this.energyCost = ThingTypeManager.GetEnergyCost(ThingType.Wall);

            this.unlockButton = UIHelper.AddIconButton(this, 92, 24, "Textures\\Icons\\DoorUnlocked", this.OnUnlockClick);
            this.unlockButton.BackgroundColour = UIColour.ButtonBackground;
            UIHelper.AddSimpleTooltip(this, this.unlockButton, StringsForThingPanels.Unlock);

            this.lockOpenButton = UIHelper.AddIconButton(this, 140, 24, "Textures\\Icons\\DoorLockedOpen", this.OnLockOpenClick);
            this.lockOpenButton.BackgroundColour = UIColour.ButtonBackground;
            UIHelper.AddSimpleTooltip(this, this.lockOpenButton, StringsForThingPanels.LockOpen);

            this.lockClosedButton = UIHelper.AddIconButton(this, 188, 24, "Textures\\Icons\\DoorLockedClosed", this.OnLockClosedClick);
            this.lockClosedButton.BackgroundColour = UIColour.ButtonBackground;
            UIHelper.AddSimpleTooltip(this, this.lockClosedButton, StringsForThingPanels.LockClosed);

            this.convertButton = UIHelper.AddTextButton(this, 80, StringsForButtons.ConvertToWall);
            this.convertButtonTooltip = UIHelper.AddSimpleTooltip(this, this.convertButton, GetString(StringsForThingPanels.WallDoorConversionCost, ThingTypeManager.GetEnergyCost(ThingType.Wall)));

            this.convertButton.MouseLeftClick += this.OnConvertButtonClick;
        }

        public override void Update()
        {
            if (this.IsBuildingUiVisible && this.building is IDoor door)
            {
                this.convertButton.IsVisible = true;
                this.unlockButton.IsVisible = true;
                this.lockClosedButton.IsVisible = true;
                this.lockOpenButton.IsVisible = true;

                this.unlockButton.IsSelected = door.State == DoorState.Unlocked;
                this.lockClosedButton.IsSelected = door.State == DoorState.LockedClosed;
                this.lockOpenButton.IsSelected = door.State == DoorState.LockedOpen;

                if (World.ResourceNetwork != null && (this.building as IDoor)?.IsOpen != true)
                {
                    this.convertButton.IsEnabled = World.ResourceNetwork.CanTakeEnergy(this.energyCost);
                }
                else this.convertButton.IsEnabled = false;
            }
            else
            {
                this.convertButton.IsVisible = false;
                this.unlockButton.IsVisible = false;
                this.lockClosedButton.IsVisible = false;
                this.lockClosedButton.IsVisible = false;
            }

            base.Update();
        }

        protected override void HandleLanguageChange()
        {
            this.convertButtonTooltip.SetTitle(GetString(StringsForThingPanels.WallDoorConversionCost, this.energyCost));
            base.HandleLanguageChange();
        }

        private void OnUnlockClick(object sender, MouseEventArgs e)
        {
            if (!(this.building is IDoor door)) return;
            door.SetState(DoorState.Unlocked);
        }

        private void OnLockClosedClick(object sender, MouseEventArgs e)
        {
            if (!(this.building is IDoor door)) return;
            door.SetState(DoorState.LockedClosed);
        }

        private void OnLockOpenClick(object sender, MouseEventArgs e)
        {
            if (!(this.building is IDoor door)) return;
            door.SetState(DoorState.LockedOpen);
        }

        private void OnConvertButtonClick(object sender, MouseEventArgs e)
        {
            if (!(this.building is IWall door) || World.ResourceNetwork == null || !World.ResourceNetwork.CanTakeEnergy(this.energyCost)) return;

            BlueprintController.ClearVirtualBlueprint();
            BlueprintController.AddVirtualBuilding(door.MainTile, ThingType.Wall, direction: door.Direction);
            PlayerWorldInteractionManager.Build();
        }
    }
}
