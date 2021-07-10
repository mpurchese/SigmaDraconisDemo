namespace SigmaDraconis.UI
{
    using Draconis.UI;

    public class BiolabPanel : LabPanel, IThingPanel
    {
        public BiolabPanel(IUIElement parent, int y)
            : base(parent, y, Scale(272))
        {
            this.AddProjectButton(24, 90, 6, "Textures\\Icons\\PlanterHydroponics");
            this.AddProjectButton(80, 90, 7, "Textures\\Icons\\HydroponicsGrowth1");
            this.AddProjectButton(136, 90, 8, "Textures\\Icons\\CompostFactory");
            this.AddProjectButton(192, 90, 9, "Textures\\Icons\\CompostFactoryFaster");
            this.AddProjectButton(248, 90, 10, "Textures\\Icons\\PlanterStoneFaster");

            this.AddProjectButton(24, 150, 1, "Textures\\Icons\\AlgaePool");
            this.AddProjectButton(80, 150, 2, "Textures\\Icons\\AlgaeFastGrowing");
            this.AddProjectButton(136, 150, 3, "Textures\\Icons\\AlgaeHighDensity");
            this.AddProjectButton(192, 150, 4, "Textures\\Icons\\AlgaeGrowInTheDark");
            this.AddProjectButton(248, 150, 5, "Textures\\Icons\\AlgaeColdWater");

            this.AddProjectButton(108, 210, 11, "Textures\\Icons\\Crop6");
            this.AddProjectButton(164, 210, 12, "Textures\\Icons\\KekImproved");
        }
    }
}
