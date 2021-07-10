namespace SigmaDraconis.UI
{
    using Draconis.Shared;
    using Draconis.UI;
    using Language;
    using Shared;
    using WorldInterfaces;

    public class WaterStoragePanel : BuildingPanel, IThingPanel
    {
        private readonly WaterDisplay storageDisplay;
        private readonly SimpleTooltip storageDisplayTooltip;
        private readonly TickBoxTextButton storageEnabledButton;

        private int? currentWater;
        private int? maxWater;

        public WaterStoragePanel(IUIElement parent, int y)
            : base(parent, y)
        {
            this.storageDisplay = new WaterDisplay(this, (this.W / 2) - Scale(100) - 1, Scale(16), 202);
            this.AddChild(this.storageDisplay);

            this.storageDisplayTooltip = new SimpleTooltip(TooltipParent.Instance, this.storageDisplay);
            TooltipParent.Instance.AddChild(this.storageDisplayTooltip);

            this.storageEnabledButton = new TickBoxTextButton(this, Scale(42), StringsForThingPanels.StorageEnabled);
            this.AddChild(this.storageEnabledButton);

            this.storageEnabledButton.MouseLeftClick += this.OnStorageEnabledButtonClick;
        }

        public override void Update()
        {
            if (this.IsBuildingUiVisible)
            {
                var silo = this.building as ISilo;

                this.storageDisplay.IsVisible = true;
                this.storageDisplay.X = (this.W / 2) - Scale(100) - 1;

                if (silo.StorageLevel != this.currentWater || silo.StorageCapacity != this.maxWater)
                {
                    this.currentWater = silo.StorageLevel;
                    this.maxWater = silo.StorageCapacity;
                    this.storageDisplay.IsVisible = this.currentWater.HasValue;
                    this.storageDisplay.SetWater(this.currentWater.GetValueOrDefault() / 100M, this.maxWater.GetValueOrDefault() / 100M);
                    this.storageDisplayTooltip.SetTitle(GetString(StringsForThingPanels.WaterStored, this.currentWater.GetValueOrDefault() / 100M, this.maxWater.GetValueOrDefault() / 100M));
                }

                this.storageEnabledButton.IsVisible = true;
                if (this.storageEnabledButton.IsTicked != silo.IsSiloSwitchedOn) this.storageEnabledButton.IsTicked = silo.IsSiloSwitchedOn;
            }
            else
            {
                this.storageDisplay.IsVisible = false;
                this.storageEnabledButton.IsVisible = false;
            }

            base.Update();
        }

        private void OnStorageEnabledButtonClick(object sender, MouseEventArgs e)
        {
            this.storageEnabledButton.IsTicked = !this.storageEnabledButton.IsTicked;

            var silo = this.building as ISilo;
            if (this.storageEnabledButton.IsTicked)
            {
                silo.IsSiloSwitchedOn = true;
                silo.SiloStatus = SiloStatus.Online;
            }
            else
            {
                silo.IsSiloSwitchedOn = false;
                silo.SiloStatus = silo.StorageLevel > 0 ? SiloStatus.WaitingToDistribute : SiloStatus.Offline;
            }
        }
    }
}
