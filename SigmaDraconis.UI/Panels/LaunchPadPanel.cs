namespace SigmaDraconis.UI
{
    using System.Linq;
    using Draconis.Shared;
    using Draconis.UI;
    using Config;
    using Language;
    using Managers;
    using Shared;
    using World;
    using World.Buildings;
    using WorldControllers;

    public class LaunchPadPanel : BuildingPanel, IThingPanel
    {
        private readonly TextLabel label1;
        private readonly TextLabel label2;
        private readonly TextLabel label3;
        private readonly TextButton buildGantryButton;
        private readonly TextButton buildRocketButton;
        private readonly TextButton launchRocketButton;
        private readonly BuildingConstructionControl gantryAndRocketConstructionControl;

        public LaunchPadPanel(IUIElement parent, int y)
            : base(parent, y)
        {
            var gantryDefinition = ThingTypeManager.GetDefinition(ThingType.RocketGantry);

            this.label1 = new TextLabelAutoScaling(this, 0, 24, 320, 20, "", UIColour.DefaultText) { IsVisible = false };
            this.label2 = new TextLabelAutoScaling(this, 0, 42, 320, 20, "", UIColour.DefaultText) { IsVisible = false };
            this.label3 = new TextLabelAutoScaling(this, 0, 60, 320, 20, "", UIColour.DefaultText) { IsVisible = false };
            this.AddChild(this.label1);
            this.AddChild(this.label2);
            this.AddChild(this.label3);

            this.buildGantryButton = new TextButton(this, Scale(80), Scale(70), Scale(160), Scale(18), GetString(StringsForThingPanels.ConstructGantry)) { TextColour = UIColour.GreenText, IsVisible = false };
            this.buildRocketButton = new TextButton(this, Scale(80), Scale(78), Scale(160), Scale(18), GetString(StringsForThingPanels.ConstructRocket)) { TextColour = UIColour.GreenText, IsVisible = false };
            this.launchRocketButton = new TextButton(this, Scale(80), Scale(70), Scale(160), Scale(18), GetString(StringsForThingPanels.LaunchRocket)) { TextColour = UIColour.GreenText, IsVisible = false };
            this.AddChild(this.buildGantryButton);
            this.AddChild(this.buildRocketButton);
            this.AddChild(this.launchRocketButton);
            this.buildGantryButton.MouseLeftClick += this.OnBuildGantryButtonClick;
            this.buildRocketButton.MouseLeftClick += this.OnBuildRocketButtonClick;
            this.launchRocketButton.MouseLeftClick += this.OnLaunchRocketButtonClick;

            this.gantryAndRocketConstructionControl = new BuildingConstructionControl(this, Scale(36), Scale(44), Scale(248), Scale(20));
            this.AddChild(this.gantryAndRocketConstructionControl);
            this.gantryAndRocketConstructionControl.PriorityChanged += this.OnGantryAndRocketConstructionPriorityChanged;
        }

        public override void Update()
        {
            this.label1.IsVisible = false;
            this.label2.IsVisible = false;
            this.label3.IsVisible = false;
            this.buildGantryButton.IsVisible = false;
            this.buildRocketButton.IsVisible = false;
            this.launchRocketButton.IsVisible = false;
            this.gantryAndRocketConstructionControl.IsVisible = false;

            if (this.building is LaunchPad launchPad && this.IsBuildingUiVisible)
            {
                var network = World.ResourceNetwork;
                if (network != null)
                {
                    var gantry = launchPad.MainTile.ThingsPrimary.FirstOrDefault(t => t.ThingType == ThingType.RocketGantry) as RocketGantry;
                    var rocket = launchPad.MainTile.ThingsPrimary.FirstOrDefault(t => t.ThingType == ThingType.Rocket) as Rocket;

                    if (rocket != null && rocket.IsLaunching)
                    {
                        this.label1.IsVisible = true;
                        this.label1.Text = GetString(StringsForThingPanels.RocketLaunchInProgress);
                    }
                    else if (rocket != null && rocket.IsReady)
                    {
                        this.label1.IsVisible = true;
                        this.launchRocketButton.IsVisible = true;
                        this.label1.Text = GetString(StringsForThingPanels.HydrogenRequiredToLaunch, Constants.RocketFuelToLaunch);
                        this.launchRocketButton.IsEnabled = network.GetItemTotal(ItemType.LiquidFuel) >= Constants.RocketFuelToLaunch;
                    }
                    else if (rocket != null)
                    {
                        if (rocket.IsRecycling)
                        {
                            this.label1.IsVisible = true;
                            this.label1.Text = GetString(StringsForThingPanels.RocketUnderDeconstruction);
                        }
                        else
                        {
                            var rocketBlueprint = World.ConfirmedBlueprints.Values.FirstOrDefault(b => b.MainTileIndex == gantry.MainTileIndex && b.ThingType == ThingType.Rocket);
                            this.gantryAndRocketConstructionControl.IsVisible = true;
                            this.gantryAndRocketConstructionControl.Priority = rocketBlueprint?.BuildPriority ?? 0;
                            this.gantryAndRocketConstructionControl.Progress = rocket?.ConstructionProgress * 0.01f ?? 0f;
                        }
                    }
                    else if (gantry != null && gantry.IsReady)
                    {
                        this.label1.IsVisible = true;
                        this.label2.IsVisible = true;
                        this.label3.IsVisible = true;
                        this.buildRocketButton.IsVisible = true;
                        var rocketDefinition = ThingTypeManager.GetDefinition(ThingType.Rocket);
                        var costs = rocketDefinition.ConstructionCosts;
                        this.label1.Text = GetString(StringsForThingPanels.RocketConstructionRequires);
                        this.label2.Text = GetString(StringsForThingPanels.LaunchPadRocketRequirements1, costs[ItemType.Metal], costs[ItemType.SolarCells], costs[ItemType.BatteryCells]);
                        this.label3.Text = GetString(StringsForThingPanels.LaunchPadRocketRequirements2, costs[ItemType.Composites], rocketDefinition.EnergyCost.KWh);
                        var canBuild = costs.All(kv => kv.Value == 0 || network.GetItemTotal(kv.Key) >= kv.Value) && network.CanTakeEnergy(rocketDefinition.EnergyCost);
                        this.buildRocketButton.IsEnabled = canBuild;
                    }
                    else if (gantry != null)
                    {
                        if (gantry.IsRecycling)
                        {
                            this.label1.IsVisible = true;
                            this.label1.Text = GetString(StringsForThingPanels.GantryUnderDeconstruction);
                        }
                        else
                        {
                            var gantryBlueprint = World.ConfirmedBlueprints.Values.FirstOrDefault(b => b.MainTileIndex == gantry.MainTileIndex && b.ThingType == ThingType.RocketGantry);
                            this.gantryAndRocketConstructionControl.IsVisible = true;
                            this.gantryAndRocketConstructionControl.Priority = gantryBlueprint?.BuildPriority ?? 0;
                            this.gantryAndRocketConstructionControl.Progress = gantry?.ConstructionProgress * 0.01f ?? 0f;
                        }
                    }
                    else
                    {
                        var gantryDefinition = ThingTypeManager.GetDefinition(ThingType.RocketGantry);
                        this.label1.IsVisible = true;
                        this.label2.IsVisible = true;
                        this.label1.Text = GetString(StringsForThingPanels.GantryConstructionRequires);
                        this.label2.Text = GetString(StringsForThingPanels.MetalAndEnergy, gantryDefinition.ConstructionCosts[ItemType.Metal], gantryDefinition.EnergyCost.KWh);
                        this.buildGantryButton.IsVisible = true;
                        this.buildGantryButton.IsEnabled = network.GetItemTotal(ItemType.Metal) >= gantryDefinition.ConstructionCosts[ItemType.Metal] && network.CanTakeEnergy(gantryDefinition.EnergyCost);
                    }
                }
            }

            base.Update();
        }

        protected override void HandleLanguageChange()
        {
            this.buildGantryButton.Text = GetString(StringsForThingPanels.ConstructGantry);
            this.buildRocketButton.Text = GetString(StringsForThingPanels.ConstructRocket);
            this.launchRocketButton.Text = GetString(StringsForThingPanels.LaunchRocket);
            base.HandleLanguageChange();
        }

        private void OnBuildGantryButtonClick(object sender, MouseEventArgs e)
        {
            var launchPad = this.building as LaunchPad;
            PlayerWorldInteractionManager.SetCurrentActivity(PlayerActivityType.None, PlayerActivitySubType.None);
            BlueprintController.AddVirtualBuilding(launchPad.MainTile, ThingType.RocketGantry);
            PlayerWorldInteractionManager.Build();
            this.gantryAndRocketConstructionControl.ConstructionStringId = StringsForThingPanels.GantryConstructionProgress;
            this.gantryAndRocketConstructionControl.IsDeconstructing = false;
        }

        private void OnBuildRocketButtonClick(object sender, MouseEventArgs e)
        {
            var launchPad = this.building as LaunchPad;
            PlayerWorldInteractionManager.SetCurrentActivity(PlayerActivityType.None, PlayerActivitySubType.None);
            BlueprintController.AddVirtualBuilding(launchPad.MainTile, ThingType.Rocket);
            PlayerWorldInteractionManager.Build();
            this.gantryAndRocketConstructionControl.ConstructionStringId = StringsForThingPanels.RocketConstructionProgress;
            this.gantryAndRocketConstructionControl.IsDeconstructing = false;
        }

        private void OnLaunchRocketButtonClick(object sender, MouseEventArgs e)
        {
            var launchPad = this.building as LaunchPad;
            if (World.ResourceNetwork != null && launchPad.MainTile.ThingsPrimary.FirstOrDefault(t => t.ThingType == ThingType.Rocket) is Rocket rocket)
            {
                World.ResourceNetwork.TakeItems(launchPad, ItemType.LiquidFuel, Constants.RocketFuelToLaunch);
                rocket.Launch();
                EventManager.RaiseEvent(EventType.RocketLaunchClick, this.building);
            }
        }

        private void OnGantryAndRocketConstructionPriorityChanged(object sender, MouseEventArgs e)
        {
            if (this.building == null) return;

            var gantryBlueprint = World.ConfirmedBlueprints.Values.FirstOrDefault(b => b.MainTileIndex == this.building.MainTileIndex && b.ThingType == ThingType.RocketGantry);
            if (gantryBlueprint != null) gantryBlueprint.BuildPriority = this.gantryAndRocketConstructionControl.Priority;

            var rocketBlueprint = World.ConfirmedBlueprints.Values.FirstOrDefault(b => b.MainTileIndex == this.building.MainTileIndex && b.ThingType == ThingType.Rocket);
            if (rocketBlueprint != null) rocketBlueprint.BuildPriority = this.gantryAndRocketConstructionControl.Priority;
        }
    }
}
