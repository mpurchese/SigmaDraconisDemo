namespace SigmaDraconis.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Draconis.Shared;
    using Draconis.UI;
    using Language;
    using Shared;
    using WorldInterfaces;

    public class LanderPanel : PanelLeft, IThingPanel
    {
        private static readonly Dictionary<ItemType, string> itemTypeNamesUpper = new Dictionary<ItemType, string>();

        private ILander lander;
        private readonly EnergyGenDisplay energyGenDisplay;
        private readonly SimpleTooltip energyGenTooltip;
        private readonly StorageDisplay storageDisplay;
        private readonly StorageDisplay itemsStorageDisplay;
        private readonly StorageDisplay foodStorageDisplay;
        private readonly SimpleTooltip storageTooltip;
        private readonly SimpleTooltip itemsStorageTooltip;
        private readonly SimpleTooltip foodStorageTooltip;
        private readonly TextButton deconstructFoundationButton;
        private readonly SimpleTooltip deconstructFoundationTooltip;

        public event EventHandler<EventArgs> DeconstructFoundationClick;

        private int? currentResourceCount;
        private int? currentResourceCapacity;
        private int? currentItemCount;
        private int? currentItemCapacity;
        private int? currentFoodCount;
        private int? currentFoodCapacity;

        public IThing Thing
        {
            get { return this.lander as IThing; }
            set { this.lander = value as ILander; }
        }

        public LanderPanel(IUIElement parent, int y)
            : base(parent, y, Scale(320), Scale(130), GetString(StringsForThingPanels.Lander))
        {
            this.energyGenDisplay = new EnergyGenDisplay(this, Scale(8), Scale(16), 78, "SunSmall");
            this.AddChild(this.energyGenDisplay);

            this.storageDisplay = new StorageDisplay(this, Scale(70), Scale(40), 180);
            this.AddChild(this.storageDisplay);
            this.storageTooltip = UIHelper.AddSimpleTooltip(this, this.storageDisplay);

            this.itemsStorageDisplay = new StorageDisplay(this, Scale(70), Scale(64), 180, StringsForThingPanels.ItemsStored, "Textures\\Misc\\ItemsBarColour");
            this.AddChild(this.itemsStorageDisplay);
            this.itemsStorageTooltip = UIHelper.AddSimpleTooltip(this, this.itemsStorageDisplay);

            this.foodStorageDisplay = new StorageDisplay(this, Scale(70), Scale(88), 180, StringsForThingPanels.FoodStored, "Textures\\Misc\\FoodBarColour");
            this.AddChild(this.foodStorageDisplay);
            this.foodStorageTooltip = UIHelper.AddSimpleTooltip(this, this.foodStorageDisplay);

            this.deconstructFoundationButton = new TextButton(this, (this.W / 4) - Scale(6), this.H - Scale(24), (this.W / 2) - Scale(12), Scale(18), GetString(StringsForThingPanels.DeconstructFoundation));
            this.AddChild(this.deconstructFoundationButton);
            this.deconstructFoundationButton.MouseLeftClick += this.OnDeconstructFoundationClick;

            this.energyGenTooltip = UIHelper.AddSimpleTooltip(this, this.energyGenDisplay);
            this.deconstructFoundationTooltip = UIHelper.AddSimpleTooltip(this, this.deconstructFoundationButton);
        }

        public override void Update()
        {
            if (this.IsVisible && this.lander != null)
            {
                if (this.energyGenDisplay.EnergyGen != this.lander.EnergyGenRate.KWh)
                {
                    this.energyGenDisplay.EnergyGen = this.lander.EnergyGenRate.KWh;
                    this.energyGenTooltip.SetTitle(GetString(StringsForThingPanels.SolarPanelOutput, this.lander.EnergyGenRate.KWh, Constants.LanderSolarPanelEnergyProduction));
                }

                if (this.lander.StorageLevel != this.currentResourceCount || this.lander.StorageCapacity != this.currentResourceCapacity)
                {
                    this.currentResourceCount = this.lander.StorageLevel;
                    this.currentResourceCapacity = this.lander.StorageCapacity;
                    this.storageDisplay.IsVisible = this.currentResourceCount.HasValue;
                    this.storageDisplay.SetCounts(this.currentResourceCount.GetValueOrDefault(), this.currentResourceCapacity.GetValueOrDefault());
                    this.storageTooltip.SetTitle(GetString(StringsForThingPanels.ResourcesStored, this.currentResourceCount.GetValueOrDefault(), this.currentResourceCapacity.GetValueOrDefault()));
                }

                if (this.lander.ItemsContainer.StorageLevel != this.currentResourceCount || this.lander.ItemsContainer.StorageCapacity != this.currentItemCapacity)
                {
                    this.currentItemCount = this.lander.ItemsContainer.StorageLevel;
                    this.currentItemCapacity = this.lander.ItemsContainer.StorageCapacity;
                    this.itemsStorageDisplay.IsVisible = this.currentItemCount.HasValue;
                    this.itemsStorageDisplay.SetCounts(this.currentItemCount.GetValueOrDefault(), this.currentItemCapacity.GetValueOrDefault());
                    this.itemsStorageTooltip.SetTitle(GetString(StringsForThingPanels.ItemsStored, this.currentItemCount.GetValueOrDefault(), this.currentItemCapacity.GetValueOrDefault()));
                }

                if (this.lander.FoodContainer.StorageLevel != this.currentFoodCount || this.lander.FoodContainer.StorageCapacity != this.currentFoodCapacity)
                {
                    this.currentFoodCount = this.lander.FoodContainer.StorageLevel;
                    this.currentFoodCapacity = this.lander.FoodContainer.StorageCapacity;
                    this.foodStorageDisplay.IsVisible = this.currentFoodCount.HasValue;
                    this.foodStorageDisplay.SetCounts(this.currentFoodCount.GetValueOrDefault(), this.currentFoodCapacity.GetValueOrDefault());
                    this.foodStorageTooltip.SetTitle(GetString(StringsForThingPanels.FoodStored, this.currentFoodCount.GetValueOrDefault(), this.currentFoodCapacity.GetValueOrDefault()));
                }
            }

            if (this.IsVisible && this.lander != null && this.lander.MainTile.ThingsPrimary.FirstOrDefault(t => t is IFoundation) is IFoundation foundation)
            {
                this.deconstructFoundationButton.IsVisible = true;
                var definition = foundation.Definition;
                if (definition.ConstructionCosts[ItemType.Metal] > 0)
                {
                    this.deconstructFoundationTooltip.SetText(LanguageHelper.GetForMouseCursor(StringsForMouseCursor.DeconstructGeneral1, foundation.DisplayName, definition.ConstructionCosts[ItemType.Metal], GetItemTypeName(ItemType.Metal)));
                }
                else if (definition.ConstructionCosts[ItemType.Stone] > 0)
                {
                    this.deconstructFoundationTooltip.SetText(LanguageHelper.GetForMouseCursor(StringsForMouseCursor.DeconstructGeneral1, foundation.DisplayName, definition.ConstructionCosts[ItemType.Stone], GetItemTypeName(ItemType.Stone)));
                }
            }
            else this.deconstructFoundationButton.IsVisible = false;

            base.Update();
        }

        private void OnDeconstructFoundationClick(object sender, MouseEventArgs e)
        {
            this.DeconstructFoundationClick?.Invoke(this, new EventArgs());
        }

        private static string GetItemTypeName(ItemType itemType)
        {
            if (!itemTypeNamesUpper.ContainsKey(itemType)) itemTypeNamesUpper.Add(itemType, LanguageManager.Get<ItemType>(itemType).ToUpperInvariant());
            return itemTypeNamesUpper[itemType];
        }

        protected override void HandleLanguageChange()
        {
            itemTypeNamesUpper.Clear();
            this.deconstructFoundationButton.Text = GetString(StringsForThingPanels.DeconstructFoundation);
            base.HandleLanguageChange();
        }

        protected static string GetString(StringsForThingPanels key)
        {
            return LanguageManager.Get<StringsForThingPanels>(key);
        }

        protected static string GetString(StringsForThingPanels key, object arg0)
        {
            return LanguageManager.Get<StringsForThingPanels>(key, arg0);
        }

        protected static string GetString(StringsForThingPanels key, object arg0, object arg1)
        {
            return LanguageManager.Get<StringsForThingPanels>(key, arg0, arg1);
        }
    }
}
