namespace SigmaDraconis.UI
{
    using Draconis.Shared;
    using Draconis.UI;
    using Language;
    using Shared;
    using WorldInterfaces;

    public class SiloPanel : BuildingPanel, IThingPanel
    {
        private readonly StorageDisplay storageDisplay;
        private readonly SimpleTooltip storageDisplayTooltip;
        private readonly TickBoxTextButton storageEnabledButton;

        private readonly StringsForThingPanels storageFormatStringKey;
        private int? currentItemCount;
        private int? currentCapacity;

        public SiloPanel(IUIElement parent, int y, StringsForThingPanels storageFormatStringKey = StringsForThingPanels.ResourcesStored, string barColourTexturePath = "Textures\\Misc\\StorageBarColour")
            : base(parent, y)
        {
            this.storageFormatStringKey = storageFormatStringKey;

            this.storageDisplay = new StorageDisplay(this, (this.W / 2) - Scale(100) - 1, Scale(16), 202, storageFormatStringKey, barColourTexturePath);
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

                if (silo.StorageLevel != this.currentItemCount || silo.StorageCapacity != this.currentCapacity)
                {
                    this.currentItemCount = silo.StorageLevel;
                    this.currentCapacity = silo.StorageCapacity;
                    this.storageDisplay.IsVisible = this.currentItemCount.HasValue;
                    this.storageDisplay.SetCounts(this.currentItemCount.GetValueOrDefault(), this.currentCapacity.GetValueOrDefault());
                    this.storageDisplayTooltip.SetTitle(GetString(this.storageFormatStringKey, this.currentItemCount.GetValueOrDefault(), this.currentCapacity.GetValueOrDefault()));
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
