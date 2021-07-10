namespace SigmaDraconis.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Draconis.UI;
    using Config;
    using Language;
    using Shared;
    using WorldInterfaces;

    internal sealed class PanelManager
    {
        private const int unscaledLhsPanelY = 48;
        private const int unscaledLhsPanelSpacing = 8;

        private readonly GameScreen screen;
        private readonly PanelGroup panelsLeft = new PanelGroup();
        private readonly PanelGroup panelsRight = new PanelGroup();

        private readonly List<IThingPanel> thingPanels = new List<IThingPanel>();
        private readonly Dictionary<ThingType, IThingPanel> thingPanelsByThingType = new Dictionary<ThingType, IThingPanel>();

        public AchievementsPanel AchievementsPanel { get; private set; }
        public AnimalPanel AnimalPanel { get; private set; }
        public ChecklistPanel ChecklistPanel { get; private set; }
        public ColonistPanel ColonistPanel { get; private set; }
        public ConstructPanel ConstructPanel { get; private set; }
        public FarmPanel FarmPanel { get; private set; }
        public LanderPanel LanderPanel { get; private set; }
        public LandingPodPanel LandingPodPanel { get; private set; }
        public OptionsPanel OptionsPanel { get; private set; }
        public SleepPodPanel SleepPodPanel { get; private set; }

        public bool IsChecklistPanelShown => this.panelsRight.CurrentPanels.Contains(this.ChecklistPanel);
        public bool IsOptionsPanelShown => this.panelsRight.CurrentPanels.Contains(this.OptionsPanel);

        public IEnumerable<PanelBase> CurrentPanels => this.panelsLeft.CurrentPanels.Concat(this.panelsRight.CurrentPanels);

        public event EventHandler<EventArgs> DeconstructConduitNodeClick;
        public event EventHandler<EventArgs> DeconstructFoundationClick;

        public PanelManager(GameScreen screen)
        {
            this.screen = screen;
        }

        public void Init()
        {
            var lhsPanelY = Scale(unscaledLhsPanelY);

            this.AnimalPanel = this.AddThingPanel(new AnimalPanel(this.screen, lhsPanelY), ThingType.BlueBug, ThingType.RedBug, ThingType.Tortoise, ThingType.SnowTortoise);
            this.ColonistPanel = this.AddThingPanel(new ColonistPanel(this.screen, lhsPanelY), ThingType.Colonist);
            this.LanderPanel = this.AddThingPanel(new LanderPanel(this.screen, lhsPanelY), ThingType.Lander);
            this.LandingPodPanel = this.AddThingPanel(new LandingPodPanel(this.screen, lhsPanelY), ThingType.LandingPod);
            this.SleepPodPanel = this.AddThingPanel(new SleepPodPanel(this.screen, lhsPanelY), ThingType.SleepPod);
            this.AddThingPanel(new PlantPanel(this.screen, lhsPanelY), ThingTypeManager.PlantThingTypes.ToArray());
            this.AddThingPanel(new ResourceStackPanel(this.screen, lhsPanelY), Constants.ResourceStackTypes.Values.ToArray());

            this.AchievementsPanel = this.panelsRight.Add(this.screen, new AchievementsPanel(this.screen, lhsPanelY)) as AchievementsPanel;
            this.ConstructPanel = this.panelsLeft.Add(this.screen, new ConstructPanel(this.screen, lhsPanelY + Scale(unscaledLhsPanelSpacing + 130))) as ConstructPanel;
            this.FarmPanel = this.panelsLeft.Add(this.screen, new FarmPanel(this.screen, lhsPanelY + Scale(unscaledLhsPanelSpacing + 130))) as FarmPanel;
            this.ChecklistPanel = this.panelsRight.Add(this.screen, new ChecklistPanel(this.screen, lhsPanelY)) as ChecklistPanel;
            this.OptionsPanel = this.panelsRight.Add(this.screen, new OptionsPanel(this.screen, lhsPanelY)) as OptionsPanel;

            this.AchievementsPanel.HideClick += this.OnAchievementsPanelHideClick;
            this.LanderPanel.DeconstructFoundationClick += this.OnDeconstructFoundationClick;
        }

        public IThingPanel GetThingPanel(ThingType thingType)
        {
            if (this.thingPanelsByThingType.ContainsKey(thingType)) return this.thingPanelsByThingType[thingType];

            var lhsPanelY = Scale(unscaledLhsPanelY);

            switch (thingType)
            {
                case ThingType.AlgaePool: return this.AddThingPanel(new AlgaePoolPanel(this.screen, lhsPanelY), ThingType.AlgaePool);
                case ThingType.Battery: return this.AddThingPanel(new BatteryPanel(this.screen, lhsPanelY), ThingType.Battery);
                case ThingType.BatteryCellFactory: return this.AddThingPanel(new BatteryCellFactory(this.screen, lhsPanelY), ThingType.BatteryCellFactory, ItemType.BatteryCells);
                case ThingType.Biolab: return this.AddThingPanel(new BiolabPanel(this.screen, lhsPanelY), ThingType.Biolab);
                case ThingType.BiomassPower: return this.AddThingPanel(new BiomassPowerPanel(this.screen, lhsPanelY), ThingType.BiomassPower);
                case ThingType.CoalPower: return this.AddThingPanel(new CoalPowerPanel(this.screen, lhsPanelY), ThingType.CoalPower);
                case ThingType.CompositesFactory: return this.AddThingPanel(new CompositesFactoryPanel(this.screen, lhsPanelY), ThingType.CompositesFactory, ItemType.Composites);
                case ThingType.CompostFactory: return this.AddThingPanel(new CompostFactoryPanel(this.screen, lhsPanelY), ThingType.CompostFactory, ItemType.Compost);
                case ThingType.CharcoalMaker: return this.AddThingPanel(new CharcoalMakerPanel(this.screen, lhsPanelY), ThingType.CharcoalMaker, ItemType.Coal);
                case ThingType.DirectionalHeater: return this.AddThingPanel(new DirectionalHeaterPanel(this.screen, lhsPanelY), ThingType.DirectionalHeater);
                case ThingType.Door: return this.AddThingPanel(new DoorPanel(this.screen, lhsPanelY), ThingType.Door);
                case ThingType.ElectricFurnace: return this.AddThingPanel(new ElectricFurnacePanel(this.screen, lhsPanelY), ThingType.ElectricFurnace, ItemType.Metal);
                case ThingType.EnvironmentControl: return this.AddThingPanel(new EnvironmentControlPanel(this.screen, lhsPanelY), ThingType.EnvironmentControl);
                case ThingType.FoodDispenser: return this.AddThingPanel(new FoodDispenserPanel(this.screen, lhsPanelY), ThingType.FoodDispenser);
                case ThingType.FoodStorage: return this.AddThingPanel(new SiloPanel(this.screen, lhsPanelY, StringsForThingPanels.FoodStored, "Textures\\Misc\\FoodBarColour"), ThingType.FoodStorage);
                case ThingType.FuelFactory: return this.AddThingPanel(new FuelFactoryPanel(this.screen, lhsPanelY), ThingType.FuelFactory, ItemType.LiquidFuel);
                case ThingType.Generator: return this.AddThingPanel(new GeneratorPanel(this.screen, lhsPanelY), ThingType.Generator);
                case ThingType.GeologyLab: return this.AddThingPanel(new GeologyLabPanel(this.screen, lhsPanelY), ThingType.GeologyLab);
                case ThingType.GlassFactory: return this.AddThingPanel(new GlassFactoryPanel(this.screen, lhsPanelY), ThingType.GlassFactory, ItemType.Glass);
                case ThingType.HydrogenBurner: return this.AddThingPanel(new HydrogenBurnerPanel(this.screen, lhsPanelY), ThingType.HydrogenBurner);
                case ThingType.HydrogenStorage: return this.AddThingPanel(new SiloPanel(this.screen, lhsPanelY, StringsForThingPanels.HydrogenStored, "Textures\\Misc\\HydrogenBarColour"), ThingType.HydrogenStorage);
                case ThingType.ItemsStorage: return this.AddThingPanel(new SiloPanel(this.screen, lhsPanelY, StringsForThingPanels.ItemsStored, "Textures\\Misc\\ItemsBarColour"), ThingType.ItemsStorage);
                case ThingType.Lamp: return this.AddThingPanel(new LampPanel(this.screen, lhsPanelY), ThingType.Lamp);
                case ThingType.LaunchPad: return this.AddThingPanel(new LaunchPadPanel(this.screen, lhsPanelY), ThingType.LaunchPad);
                case ThingType.MaterialsLab: return this.AddThingPanel(new MaterialsLabPanel(this.screen, lhsPanelY), ThingType.MaterialsLab);
                case ThingType.Mine: return this.AddThingPanel(new MinePanel(this.screen, lhsPanelY), ThingType.Mine, ItemType.None);
                case ThingType.MushFactory: return this.AddThingPanel(new MushFactoryPanel(this.screen, lhsPanelY), ThingType.MushFactory, ItemType.Mush);
                case ThingType.OreScanner: return this.AddThingPanel(new OreScannerPanel(this.screen, lhsPanelY), ThingType.OreScanner);
                case ThingType.ResourceProcessor: return this.AddThingPanel(new FactoryBuildingPanel(this.screen, lhsPanelY, false, false), ThingType.ResourceProcessor);
                case ThingType.Silo: return this.AddThingPanel(new SiloPanel(this.screen, lhsPanelY), ThingType.Silo);
                case ThingType.SolarCellFactory: return this.AddThingPanel(new SolarCellFactoryPanel(this.screen, lhsPanelY), ThingType.SolarCellFactory, ItemType.SolarCells);
                case ThingType.SolarPanelArray: return this.AddThingPanel(new PassiveGeneratorPanel(this.screen, lhsPanelY, "SunSmall", Constants.SolarPanelEnergyProduction), ThingType.SolarPanelArray);
                case ThingType.StackingArea: return this.AddThingPanel(new StackingAreaPanel(this.screen, lhsPanelY), ThingType.StackingArea);
                case ThingType.StoneFurnace: return this.AddThingPanel(new StoneFurnacePanel(this.screen, lhsPanelY), ThingType.StoneFurnace, ItemType.Metal);
                case ThingType.Wall: return this.AddThingPanel(new WallPanel(this.screen, lhsPanelY), ThingType.Wall);
                case ThingType.WaterStorage: return this.AddThingPanel(new WaterStoragePanel(this.screen, lhsPanelY), ThingType.WaterStorage);
                case ThingType.WindTurbine: return this.AddThingPanel(new PassiveGeneratorPanel(this.screen, lhsPanelY, "WindSmall", Constants.WindTurbineEnergyProduction), ThingType.WindTurbine);
                case ThingType.Cooker:
                case ThingType.KekFactory: return this.AddThingPanel(new CookerPanel(this.screen, lhsPanelY), ThingType.Cooker, ThingType.KekFactory);
                case ThingType.ConduitNode:
                case ThingType.FoundationMetal:
                case ThingType.FoundationStone:
                case ThingType.TableMetal:
                case ThingType.TableStone: return this.AddThingPanel(new DefaultBuildingPanel(this.screen, lhsPanelY), ThingType.ConduitNode, ThingType.FoundationMetal, ThingType.FoundationStone, ThingType.TableMetal, ThingType.TableStone);
                case ThingType.KekDispenser:
                case ThingType.WaterDispenser: return this.AddThingPanel(new DispenserPanel(this.screen, lhsPanelY), ThingType.FoodDispenser, ThingType.WaterDispenser, ThingType.KekDispenser);
                case ThingType.PlanterHydroponics:
                case ThingType.PlanterStone: return this.AddThingPanel(new PlanterPanel(this.screen, lhsPanelY), ThingType.PlanterHydroponics, ThingType.PlanterStone);
                case ThingType.RockSmall:
                case ThingType.RockLarge: return this.AddThingPanel(new DeconstructableThingPanel(this.screen, lhsPanelY), ThingType.RockSmall, ThingType.RockLarge);
                case ThingType.ShorePump:
                case ThingType.WaterPump: return this.AddThingPanel(new WaterPumpPanel(this.screen, lhsPanelY), ThingType.ShorePump, ThingType.WaterPump);
            }

            return null;
        }

        public void HideLeft()
        {
            this.panelsLeft.HideAll();
        }

        public void HideRight()
        {
            this.panelsRight.HideAll();
        }

        public void HideAll()
        {
            this.panelsLeft.HideAll();
            this.panelsRight.HideAll();
        }

        public void UpdateAll()
        {
            this.panelsLeft.Update();
            this.panelsRight.Update();
        }

        public void ShowColonistAndSleepPodPanels(IColonist colonist, ISleepPod sleepPod)
        {
            this.ColonistPanel.Thing = colonist;
            this.SleepPodPanel.Thing = sleepPod;
            this.ColonistPanel.Y = this.SleepPodPanel.Bottom + Scale(unscaledLhsPanelSpacing);
            this.panelsLeft.Show(new List<PanelBase> { this.ColonistPanel, this.SleepPodPanel });
        }

        public void ShowRelevantPanel(IThing thing)
        {
            var relevantPanel = this.GetThingPanel(thing.ThingType) as PanelLeft;
            if (!relevantPanel.IsShown) relevantPanel.Y = Scale(unscaledLhsPanelY);

            (relevantPanel as IThingPanel).Thing = thing;

            if (this.panelsLeft.CurrentPanels.OfType<IThingPanel>().Count() > 1)   // E.g. colonist and sleep pod panel
            {
                this.panelsLeft.Show(new List<PanelBase> { relevantPanel });
            }
            else if (!this.panelsLeft.CurrentPanels.Contains(relevantPanel))
            {
                if (this.panelsLeft.CurrentPanels.Contains(this.ConstructPanel) && relevantPanel.Bottom < this.ConstructPanel.Y)
                {
                    this.panelsLeft.Show(new List<PanelBase> { relevantPanel, this.ConstructPanel });
                }
                else if (this.panelsLeft.CurrentPanels.Contains(this.FarmPanel) && relevantPanel.Bottom < this.FarmPanel.Y)
                {
                    this.panelsLeft.Show(new List<PanelBase> { relevantPanel, this.FarmPanel });
                }
                else this.panelsLeft.Show(new List<PanelBase> { relevantPanel });
            }
        }

        public void CloseIfOpen(IThing thing)
        {
            var thingPanel = this.panelsLeft.CurrentPanels.OfType<IThingPanel>().FirstOrDefault(s => s.Thing == thing);
            if (thingPanel is PanelBase panel) this.panelsLeft.Hide(panel);
        }

        public void ShowAchievementsPanel()
        {
            this.panelsRight.Show(this.AchievementsPanel);
        }

        public void ShowChecklistPanel()
        {
            this.panelsRight.Show(this.ChecklistPanel);
        }

        public void ShowOptionsPanel()
        {
            this.panelsRight.Show(this.OptionsPanel);
        }

        public void ToggleConstructPanel()
        {
            // Add or remove from group
            var group = this.panelsLeft.CurrentPanels.ToList();
            if (group.Contains(this.ConstructPanel)) group.Remove(this.ConstructPanel);
            else
            {
                foreach (var p in group.Where(p => p.Bottom >= this.ConstructPanel.Y).ToList()) group.Remove(p);
                group.Add(this.ConstructPanel);
            }

            this.panelsLeft.Show(group);
        }

        public void ToggleFarmPanel()
        {
            // Add or remove from group
            var group = this.panelsLeft.CurrentPanels.ToList();
            if (group.Contains(this.FarmPanel)) group.Remove(this.FarmPanel);
            else
            {
                foreach (var p in group.Where(p => p.Bottom >= this.FarmPanel.Y).ToList()) group.Remove(p);
                group.Add(this.FarmPanel);
            }

            this.panelsLeft.Show(group);
        }

        private T AddThingPanel<T>(T panel, params ThingType[] thingTypes) where T : PanelBase
        {
            this.panelsLeft.Add(this.screen, panel);
            this.thingPanels.Add(panel as IThingPanel);
            foreach (var thingType in thingTypes) this.thingPanelsByThingType.Add(thingType, panel as IThingPanel);

            if (panel is BuildingPanel buildingPanel)
            {
                buildingPanel.DeconstructConduitNodeClick += this.OnDeconstructConduitNodeClick;
                buildingPanel.DeconstructFoundationClick += this.OnDeconstructFoundationClick;
            }

            return panel;
        }

        private T AddThingPanel<T>(T panel, ThingType thingType, ItemType itemType) where T : FactoryBuildingPanel
        {
            this.panelsLeft.Add(this.screen, panel);
            this.thingPanels.Add(panel);
            this.thingPanelsByThingType.Add(thingType, panel);
            panel.SetInventoryTarget(itemType, 8);

            if (panel is BuildingPanel buildingPanel)
            {
                buildingPanel.DeconstructConduitNodeClick += this.OnDeconstructConduitNodeClick;
                buildingPanel.DeconstructFoundationClick += this.OnDeconstructFoundationClick;
            }

            return panel;
        }

        private void OnDeconstructConduitNodeClick(object sender, EventArgs e)
        {
            this.DeconstructConduitNodeClick?.Invoke(sender, new EventArgs());
        }

        private void OnDeconstructFoundationClick(object sender, EventArgs e)
        {
            this.DeconstructFoundationClick?.Invoke(sender, new EventArgs());
        }

        private void OnAchievementsPanelHideClick(object sender, EventArgs e)
        {
            this.panelsRight.Hide(this.AchievementsPanel);
        }

        private static int Scale(int coord)
        {
            return coord * UIStatics.Scale / 100;
        }
    }
}
