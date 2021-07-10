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

    public class WallPanel : BuildingPanel, IThingPanel
    {
        private readonly TextButton convertButton;
        private readonly SimpleTooltip convertButtonTooltip;
        private readonly Energy energyCost;

        public WallPanel(IUIElement parent, int y) : base(parent, y)
        {
            this.energyCost = ThingTypeManager.GetEnergyCost(ThingType.Door);

            this.convertButton = UIHelper.AddTextButton(this, 32, StringsForButtons.ConvertToDoor);
            this.convertButtonTooltip = UIHelper.AddSimpleTooltip(this, this.convertButton, GetString(StringsForThingPanels.WallDoorConversionCost, this.energyCost));

            this.convertButton.MouseLeftClick += this.OnConvertButtonClick;
        }

        public override void Update()
        {
            this.convertButton.IsVisible = this.IsBuildingUiVisible;
            if (World.ResourceNetwork != null)
            {
                this.convertButton.IsEnabled = World.ResourceNetwork.CanTakeEnergy(this.energyCost);
            }
            else this.convertButton.IsEnabled = false;

            base.Update();
        }

        protected override void HandleLanguageChange()
        {
            this.convertButtonTooltip.SetTitle(GetString(StringsForThingPanels.WallDoorConversionCost, this.energyCost));
            base.HandleLanguageChange();
        }

        private void OnConvertButtonClick(object sender, MouseEventArgs e)
        {
            if (!(this.building is IWall wall) || World.ResourceNetwork == null || !World.ResourceNetwork.CanTakeEnergy(this.energyCost)) return;

            BlueprintController.ClearVirtualBlueprint();
            BlueprintController.AddVirtualBuilding(wall.MainTile, ThingType.Door, direction: wall.Direction);
            PlayerWorldInteractionManager.Build();
        }
    }
}
