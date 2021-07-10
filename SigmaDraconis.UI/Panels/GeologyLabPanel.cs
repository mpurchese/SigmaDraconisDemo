namespace SigmaDraconis.UI
{
    using Draconis.UI;

    public class GeologyLabPanel : LabPanel, IThingPanel
    {
        public GeologyLabPanel(IUIElement parent, int y)
            : base(parent, y, Scale(152))
        {
            this.AddProjectButton(24, 90, 201, "Textures\\Icons\\MineFaster1");
            this.AddProjectButton(80, 90, 202, "Textures\\Icons\\MineFaster2");
            this.AddProjectButton(136, 90, 203, "Textures\\Icons\\OreScanner");
            this.AddProjectButton(192, 90, 204, "Textures\\Icons\\OreScannerSpeed");
            this.AddProjectButton(248, 90, 205, "Textures\\Icons\\OreScannerRange");
        }
    }
}
