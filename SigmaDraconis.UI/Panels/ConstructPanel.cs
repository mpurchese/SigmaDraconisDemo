namespace SigmaDraconis.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Draconis.Shared;
    using Draconis.UI;
    using Config;
    using Language;
    using Managers;
    using Settings;
    using Shared;
    using World;
    using World.Projects;
    using WorldControllers;
    using WorldInterfaces;

    public class ConstructPanel : PanelLeft
    {
        private readonly Dictionary<BlueprintButton, ThingType> buildingButtons = new Dictionary<BlueprintButton, ThingType>();
        private readonly Dictionary<BlueprintButton, ItemType> stackingAreaButtonItemTypes = new Dictionary<BlueprintButton, ItemType>();
        private readonly Dictionary<IUIElement, int> buttonPages = new Dictionary<IUIElement, int>();
        private readonly Dictionary<IUIElement, BlueprintTooltip> buildingButtonTooltips = new Dictionary<IUIElement, BlueprintTooltip>();
        private readonly List<ConstructPageButton> pageButtons = new List<ConstructPageButton>();
        private int currentPage = 1;
        private int frameCounter;
        private TemperatureUnit temperatureUnit;

        private IThing lander;
        public IThing Lander
        {
            get => this.lander;
            set
            {
                if (this.lander != value && value != null)
                {
                    this.lander = value;
                    this.UpdatePageButtonTexts();
                }
            }
        }

        public ConstructPanel(IUIElement parent, int y)
            : base(parent, y, Scale(320), Scale(172), GetString(StringsForConstructPanel.Title))
        {
            var strIndex = (int)StringsForConstructPanel.Page1;
            for (int i = 0; i < 8; i++)
            {
                var btn = new ConstructPageButton(this, Scale(9 + (i * 38)), Scale(20), i + 1);
                this.pageButtons.Add(btn);
                this.AddChild(btn);
                btn.MouseLeftClick += this.OnPageButtonClick;
                UIHelper.AddSimpleTooltip(this, this.pageButtons[i], (StringsForConstructPanel)strIndex);
                strIndex++;
            }

            this.pageButtons[0].IsSelected = true;

            this.temperatureUnit = SettingsManager.TemperatureUnit;
            SettingsManager.SettingsSaved += this.OnSettingsSaved;

            // Basics, water
            this.AddBuildingButton(1, Scale(52), Scale(58), ThingType.ResourceProcessor, "Textures\\Icons\\ResourceProcessor");
            this.AddBuildingButton(1, Scale(108), Scale(58), ThingType.FoundationStone, "Textures\\Icons\\FoundationStone");
            this.AddBuildingButton(1, Scale(164), Scale(58), ThingType.FoundationMetal, "Textures\\Icons\\FoundationMetal");
            this.AddBuildingButton(1, Scale(220), Scale(58), ThingType.ConduitNode, "Textures\\Icons\\ConduitNode");
            this.AddBuildingButton(1, Scale(24), Scale(114), ThingType.WaterPump, "Textures\\Icons\\WaterPump");
            this.AddBuildingButton(1, Scale(80), Scale(114), ThingType.ShorePump, "Textures\\Icons\\ShorePump");
            this.AddBuildingButton(1, Scale(136), Scale(114), ThingType.WaterDispenser, "Textures\\Icons\\WaterDispenser");
            this.AddBuildingButton(1, Scale(192), Scale(114), ThingType.FoodDispenser, "Textures\\Icons\\FoodDispenser");
            this.AddBuildingButton(1, Scale(248), Scale(114), ThingType.KekDispenser, "Textures\\Icons\\KekDispenser");

            // Materials
            this.AddBuildingButton(2, Scale(24), Scale(58), ThingType.CharcoalMaker, "Textures\\Icons\\CharcoalMaker");
            this.AddBuildingButton(2, Scale(80), Scale(58), ThingType.Mine, "Textures\\Icons\\Mine");
            this.AddBuildingButton(2, Scale(136), Scale(58), ThingType.StoneFurnace, "Textures\\Icons\\StoneFurnace");
            this.AddBuildingButton(2, Scale(192), Scale(58), ThingType.ElectricFurnace, "Textures\\Icons\\ElectricFurnace");
            this.AddBuildingButton(2, Scale(248), Scale(58), ThingType.OreScanner, "Textures\\Icons\\OreScanner");
            this.AddBuildingButton(2, Scale(24), Scale(114), ThingType.GlassFactory, "Textures\\Icons\\GlassFactory");
            this.AddBuildingButton(2, Scale(80), Scale(114), ThingType.CompositesFactory, "Textures\\Icons\\CompositesFactory");
            this.AddBuildingButton(2, Scale(136), Scale(114), ThingType.SolarCellFactory, "Textures\\Icons\\SolarCellFactory");
            this.AddBuildingButton(2, Scale(192), Scale(114), ThingType.BatteryCellFactory, "Textures\\Icons\\BatteryCellFactory");
            this.AddBuildingButton(2, Scale(248), Scale(114), ThingType.AlgaePool, "Textures\\Icons\\AlgaePool");

            // Food production
            this.AddBuildingButton(3, Scale(24), Scale(58), ThingType.MushFactory, "Textures\\Icons\\MushFactory");
            this.AddBuildingButton(3, Scale(80), Scale(58), ThingType.Cooker, "Textures\\Icons\\Cooker");
            this.AddBuildingButton(3, Scale(136), Scale(58), ThingType.PlanterHydroponics, "Textures\\Icons\\PlanterHydroponics");
            this.AddBuildingButton(3, Scale(192), Scale(58), ThingType.CompostFactory, "Textures\\Icons\\CompostFactory");
            this.AddBuildingButton(3, Scale(248), Scale(58), ThingType.PlanterStone, "Textures\\Icons\\PlanterStone");
            this.AddBuildingButton(3, Scale(136), Scale(114), ThingType.KekFactory, "Textures\\Icons\\KekFactory");

            // Energy
            this.AddBuildingButton(4, Scale(24), Scale(58), ThingType.Generator, "Textures\\Icons\\Generator");
            this.AddBuildingButton(4, Scale(80), Scale(58), ThingType.BiomassPower, "Textures\\Icons\\BiomassPower");
            this.AddBuildingButton(4, Scale(136), Scale(58), ThingType.CoalPower, "Textures\\Icons\\CoalPower");
            this.AddBuildingButton(4, Scale(192), Scale(58), ThingType.WindTurbine, "Textures\\Icons\\WindTurbine");
            this.AddBuildingButton(4, Scale(248), Scale(58), ThingType.SolarPanelArray, "Textures\\Icons\\SolarPanel");
            this.AddBuildingButton(4, Scale(108), Scale(114), ThingType.Battery, "Textures\\Icons\\Battery");
            this.AddBuildingButton(4, Scale(164), Scale(114), ThingType.HydrogenBurner, "Textures\\Icons\\HydrogenBurner");

            // Storage
            this.AddBuildingButton(5, Scale(24), Scale(58), ThingType.WaterStorage, "Textures\\Icons\\WaterStorage");
            this.AddBuildingButton(5, Scale(80), Scale(58), ThingType.Silo, "Textures\\Icons\\Silo");
            this.AddBuildingButton(5, Scale(136), Scale(58), ThingType.ItemsStorage, "Textures\\Icons\\ItemsStorage");
            this.AddBuildingButton(5, Scale(192), Scale(58), ThingType.FoodStorage, "Textures\\Icons\\FoodStorage");
            this.AddBuildingButton(5, Scale(248), Scale(58), ThingType.HydrogenStorage, "Textures\\Icons\\HydrogenStorage");
            this.AddBuildingButton(5, Scale(8), Scale(114), ThingType.StackingArea, "Textures\\Icons\\StackableAreaMetal", ItemType.Metal);
            this.AddBuildingButton(5, Scale(60), Scale(114), ThingType.StackingArea, "Textures\\Icons\\StackableAreaStone", ItemType.Stone);
            this.AddBuildingButton(5, Scale(112), Scale(114), ThingType.StackingArea, "Textures\\Icons\\StackableAreaOre", ItemType.IronOre);
            this.AddBuildingButton(5, Scale(164), Scale(114), ThingType.StackingArea, "Textures\\Icons\\StackableAreaCoal", ItemType.Coal);
            this.AddBuildingButton(5, Scale(216), Scale(114), ThingType.StackingArea, "Textures\\Icons\\StackableAreaOrganics", ItemType.Biomass);
            this.AddBuildingButton(5, Scale(268), Scale(114), ThingType.StackingArea, "Textures\\Icons\\StackableAreaCompost", ItemType.Compost);

            // Habitation
            this.AddBuildingButton(6, Scale(24), Scale(58), ThingType.SleepPod, "Textures\\Icons\\SleepPod");
            this.AddBuildingButton(6, Scale(80), Scale(58), ThingType.TableStone, "Textures\\Icons\\TableStone");
            this.AddBuildingButton(6, Scale(136), Scale(58), ThingType.TableMetal, "Textures\\Icons\\TableMetal");
            this.AddBuildingButton(6, Scale(192), Scale(58), ThingType.Lamp, "Textures\\Icons\\Lamp");
            this.AddBuildingButton(6, Scale(248), Scale(58), ThingType.DirectionalHeater, "Textures\\Icons\\DirectionalHeater");
            this.AddBuildingButton(6, Scale(52), Scale(114), ThingType.Wall, "Textures\\Icons\\Wall");
            this.AddBuildingButton(6, Scale(108), Scale(114), ThingType.Door, "Textures\\Icons\\Door");
            this.AddBuildingButton(6, Scale(164), Scale(114), ThingType.Roof, "Textures\\Icons\\Roof");
            this.AddBuildingButton(6, Scale(220), Scale(114), ThingType.EnvironmentControl, "Textures\\Icons\\EnvironmentControl");

            // Labs
            this.AddBuildingButton(7, Scale(80), Scale(58), ThingType.MaterialsLab, "Textures\\Icons\\MaterialsLab");
            this.AddBuildingButton(7, Scale(136), Scale(58), ThingType.Biolab, "Textures\\Icons\\Biolab");
            this.AddBuildingButton(7, Scale(192), Scale(58), ThingType.GeologyLab, "Textures\\Icons\\GeologyLab");

            // Rocketry
            this.AddBuildingButton(8, Scale(40), Scale(58), ThingType.FuelFactory, "Textures\\Icons\\FuelFactory");
            this.AddBuildingButton(8, Scale(96), Scale(58), ThingType.LaunchPad, "Textures\\Icons\\LaunchPad");
            this.AddBuildingButton(8, Scale(176), Scale(58), ThingType.RocketGantry, "Textures\\Icons\\RocketGantry");
            this.AddBuildingButton(8, Scale(232), Scale(58), ThingType.Rocket, "Textures\\Icons\\Rocket");

            this.AddBuildingButtonTooltips();
        }

        public override void LoadContent()
        {
            PlayerWorldInteractionManager.CurrentActivityChanged += this.OnCurrentActivityChanged;
            base.LoadContent();
        }

        public override void Update()
        {
            if (!this.IsVisible) return;

            if (this.Lander != null) this.UpdatePageButtonTexts();

            this.UpdateButtonVisibiity();
            foreach (var kv in this.buildingButtons.Where(b => b.Key.IsVisible))
            {
                var prefabCount = World.Prefabs.Count(kv.Value);
                if (prefabCount > 0)
                {
                    kv.Key.PrefabCount = prefabCount;
                    kv.Key.IsEnabled = true;
                    kv.Key.IsHighlighted = kv.Value == ThingType.ResourceProcessor;
                    if (kv.Key.IsMouseOver)
                    {
                        var tooltip = this.buildingButtonTooltips[kv.Key];
                        tooltip.IsLocked = false;
                        tooltip.PrefabCount = prefabCount;
                    }
                }
                else
                {
                    var def = ThingTypeManager.GetDefinition(kv.Value);
                    var isAllResourceAvailable = true;
                    var tooltip = kv.Key.IsMouseOver ? this.buildingButtonTooltips[kv.Key] : null;
                    foreach (var cost in def.ConstructionCosts.Where(c => c.Value > 0))
                    {
                        var isResourceAvailable = World.ResourceNetwork?.GetItemTotal(cost.Key) >= def.ConstructionCosts[cost.Key];
                        isAllResourceAvailable &= isResourceAvailable;
                        if (tooltip != null) tooltip.SetIsResourceAvailable(cost.Key, isResourceAvailable);
                    }

                    var isEnergyAvailable = World.ResourceNetwork?.EnergyTotal >= def.EnergyCost;
                    var isLocked = this.IsLocked(kv.Value);
                    var isRequirementsMet = !def.IsLaunchPadRequired 
                        || World.GetThings<IBuildableThing>(ThingType.LaunchPad).Any(b => b.IsReady && !b.IsDesignatedForRecycling && b.MainTile.ThingsPrimary.All(t => t.ThingType != ThingType.RocketGantry));
                    isRequirementsMet &= !def.IsRocketGantryRequired || World.GetThings<IBuildableThing>(ThingType.RocketGantry).Any(b => b.IsReady && !b.IsDesignatedForRecycling && b.MainTile.ThingsPrimary.All(t => t.ThingType != ThingType.Rocket));

                    kv.Key.PrefabCount = 0;
                    kv.Key.IsHighlighted = false;
                    kv.Key.IsEnabled = !isLocked && isAllResourceAvailable && isEnergyAvailable && isRequirementsMet;

                    if (tooltip != null)
                    {
                        tooltip.IsEnergyAvailable = isEnergyAvailable;
                        tooltip.IsLocked = isLocked;
                        tooltip.PrefabCount = 0;
                    }
                }
            }

            this.UpdatePageButtonTexts();
            base.Update();
        }

        private void OnSettingsSaved(object sender, EventArgs e)
        {
            if (SettingsManager.TemperatureUnit == this.temperatureUnit) return;

            this.temperatureUnit = SettingsManager.TemperatureUnit;
            foreach (var kv in this.buildingButtonTooltips) TooltipParent.Instance.RemoveChild(kv.Value);
            this.buildingButtonTooltips.Clear();
            this.AddBuildingButtonTooltips();
        }

        protected override void HandleLanguageChange()
        {
            this.Title = GetString(StringsForConstructPanel.Title);

            foreach (var kv in this.buildingButtonTooltips) TooltipParent.Instance.RemoveChild(kv.Value);
            this.buildingButtonTooltips.Clear();
            this.AddBuildingButtonTooltips();

            base.HandleLanguageChange();
        }

        private void UpdatePageButtonTexts()
        {
            var countTotals = new Dictionary<int, int>();
            var countCanBuilds = new Dictionary<int, int>();
            for (int i = 0; i < this.pageButtons.Count; i++)
            {
                countTotals.Add(i, 0);
                countCanBuilds.Add(i, 0);
            }

            var metal = World.ResourceNetwork?.GetItemTotal(ItemType.Metal) ?? 0;
            var stone = World.ResourceNetwork?.GetItemTotal(ItemType.Stone) ?? 0;
            var batteryCells = World.ResourceNetwork?.GetItemTotal(ItemType.BatteryCells) ?? 0;
            var composites = World.ResourceNetwork?.GetItemTotal(ItemType.Composites) ?? 0;
            var solarCells = World.ResourceNetwork?.GetItemTotal(ItemType.SolarCells) ?? 0;
            var glass = World.ResourceNetwork?.GetItemTotal(ItemType.Glass) ?? 0;
            var compost = World.ResourceNetwork?.GetItemTotal(ItemType.Compost) ?? 0;
            var energy = World.ResourceNetwork?.EnergyTotal ?? 0;

            var updateAllPages = this.frameCounter % 17 == 0;   // Optimisation: Most frames, only update current page
            this.frameCounter++;
            BlueprintButton waterPumpButton = null;
            foreach (var button in this.buildingButtons)
            {
                if (button.Value == ThingType.WaterPump) waterPumpButton = button.Key;
                if (!updateAllPages && this.buttonPages[button.Key] != this.currentPage) continue;

                countTotals[this.buttonPages[button.Key] - 1]++;
                if (this.CanBuild(button.Value, metal, stone, batteryCells, composites, solarCells, glass, compost, energy)) countCanBuilds[this.buttonPages[button.Key] - 1]++;
            }

            for (int i = 0; i < this.pageButtons.Count; i++)
            {
                if (!updateAllPages && i != this.currentPage - 1) continue;
                this.pageButtons[i].SetCounts(countCanBuilds[i], countTotals[i]);
            }

            var highlighWaterPump = WarningsController.IsShownWaterPumpNeededWarning && waterPumpButton != null;
            this.pageButtons[0].IsHighlighted = highlighWaterPump;
            waterPumpButton.IsHighlighted = highlighWaterPump;
        }

        private void OnCurrentActivityChanged(object sender, EventArgs e)
        {
            if (PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.Build)
            {
                foreach (var button in this.buildingButtons)
                {
                    button.Key.IsSelected = button.Value == PlayerWorldInteractionManager.CurrentThingTypeToBuild;
                }
            }
            else if (PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.PlaceStackingArea)
            {
                foreach (var button in this.buildingButtons)
                {
                    button.Key.IsSelected = button.Value == ThingType.StackingArea && stackingAreaButtonItemTypes[button.Key] == PlayerActivityPlaceStackingArea.CurrentItemTypeToBuild;
                }
            }
            else
            {
                this.DeselectBuildingButtons();
            }
        }

        private IconButton AddBuildingButton(int page, int x, int y, ThingType thingType, string texturePath, ItemType? itemType = null)
        {
            var button = new BlueprintButton(this, x, y, texturePath, thingType == ThingType.LaunchPad) { IsVisible = page == 1 };
            button.MouseLeftClick += this.OnBuildingButtonClick;
            this.buildingButtons.Add(button, thingType);
            if (itemType.HasValue) this.stackingAreaButtonItemTypes.Add(button, itemType.Value);
            this.AddChild(button);
            this.buttonPages.Add(button, page);

            return button;
        }

        private void AddBuildingButtonTooltips()
        {
            foreach (var pair in this.buildingButtons)
            {
                try
                {
                    var definition = ThingTypeManager.GetDefinition(pair.Value);
                    var displayName = LanguageManager.GetName(pair.Value);
                    var prefabCount = World.Prefabs != null ? World.Prefabs.Count(pair.Value) : 0;
                    if (prefabCount > 0)
                    {
                        var tooltip = new BlueprintTooltip(this, pair.Key as IUIElement, pair.Value, definition, displayName.ToUpperInvariant(), prefabCount);
                        TooltipParent.Instance.AddChild(tooltip, this);
                        this.buildingButtonTooltips.Add(pair.Key, tooltip);
                    }
                    else
                    {
                        var lockingProjects = ProjectManager.LockingProjects(pair.Value).ToList();
                        var lockedDescription = GetString(StringsForConstructPanel.NotInDemo);

                        var itemType = pair.Value == ThingType.StackingArea ? stackingAreaButtonItemTypes[pair.Key] : ItemType.None;
                        var title = pair.Value == ThingType.StackingArea ? displayName + " - " + LanguageManager.Get<ItemType>(itemType).ToUpperInvariant() : displayName.ToUpperInvariant();
                        var tooltip = new BlueprintTooltip(this, pair.Key, pair.Value, definition, title, 0, lockedDescription);
                        TooltipParent.Instance.AddChild(tooltip, this);
                        this.buildingButtonTooltips.Add(pair.Key, tooltip);
                    }
                }
                catch
                {
                    var tooltip = new BlueprintTooltip(this, pair.Key as IUIElement, pair.Value, new ThingTypeDefinition(), pair.Key.ToString());
                    TooltipParent.Instance.AddChild(tooltip, this);
                    this.buildingButtonTooltips.Add(pair.Key, tooltip);
                }
            }
        }

        private void OnBuildingButtonClick(object sender, MouseEventArgs e)
        {
            var button = sender as BlueprintButton;
            if (button.IsSelected)
            {
                PlayerWorldInteractionManager.SetCurrentActivity(PlayerActivityType.None, PlayerActivitySubType.None);
                MouseCursor.Instance.MouseCursorType = MouseCursorType.Normal;
               // FallbackMouseCursor = MouseCursorType.Normal;
                button.IsSelected = false;
            }
            else if (button.IsEnabled)
            {
                var thingType = this.buildingButtons[button];
                if (thingType == ThingType.StackingArea)
                {
                    var itemType = stackingAreaButtonItemTypes[button];
                    PlayerWorldInteractionManager.SetCurrentActivity(PlayerActivityType.PlaceStackingArea, PlayerActivitySubType.PlaceStackingArea, ThingType.StackingArea, itemType);
                }
                else PlayerWorldInteractionManager.SetCurrentActivity(PlayerActivityType.Build, PlayerActivitySubType.BuildFactory, thingType);

                MouseCursor.Instance.MouseCursorType = MouseCursorType.Normal;
                button.IsSelected = true;
            }
        }

        private void OnPageButtonClick(object sender, MouseEventArgs e)
        {
            var newPage = (sender as ConstructPageButton).Page;
            if (this.currentPage == newPage) return;

            foreach (var btn in this.pageButtons) btn.IsSelected = sender == btn;
            this.currentPage = newPage;
            this.DeselectBuildingButtons();
            this.UpdateButtonVisibiity();
        }

        private void UpdateButtonVisibiity()
        {
            foreach (var button in this.buildingButtons)
            {
                button.Key.IsVisible = this.buttonPages[button.Key] == this.currentPage;
                button.Key.IsLocked = this.IsLocked(button.Value);
            }
        }

        private void DeselectBuildingButtons()
        {
            foreach (var button in this.buildingButtons.Keys)
            {
                button.IsSelected = false;
            }

            if (PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.Build || PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.PlaceStackingArea)
            {
                PlayerWorldInteractionManager.SetCurrentActivity(PlayerActivityType.None, PlayerActivitySubType.None);
            }

            MouseCursor.Instance.MouseCursorType = MouseCursorType.Normal;
        }

        private bool CanBuild(ThingType thingType, int metal, int stone, int batteryCells, int composites, int solarCells, int glass, int compost, Energy energy)
        {
            if (World.ResourceNetwork == null) return false;
            if (this.IsLocked(thingType)) return false;

            if (World.Prefabs.Count(thingType) > 0) return true;

            var def = ThingTypeManager.GetDefinition(thingType);
            if (metal < def.ConstructionCosts[ItemType.Metal]) return false;
            if (stone < def.ConstructionCosts[ItemType.Stone]) return false;
            if (batteryCells < def.ConstructionCosts[ItemType.BatteryCells]) return false;
            if (composites < def.ConstructionCosts[ItemType.Composites]) return false;
            if (solarCells < def.ConstructionCosts[ItemType.SolarCells]) return false;
            if (glass < def.ConstructionCosts[ItemType.Glass]) return false;
            if (compost < def.ConstructionCosts[ItemType.Compost]) return false;
            if (energy < def.EnergyCost) return false;

            return true;
        }

        private bool IsLocked(ThingType thingType)
        {
            // DEMO
            if (thingType.In(ThingType.Biolab, ThingType.GeologyLab, ThingType.MaterialsLab)) return true;

            return ProjectManager.LockingProjects(thingType).Any();
        }

        private static string GetString(StringsForConstructPanel value)
        {
            return LanguageManager.Get<StringsForConstructPanel>(value);
        }
    }
}
