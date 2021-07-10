namespace SigmaDraconis.UI
{
    using Draconis.UI;

    public class MaterialsLabPanel : LabPanel, IThingPanel
    {
        public MaterialsLabPanel(IUIElement parent, int y)
            : base(parent, y, Scale(152))
        {
            this.AddProjectButton(24, 90, 101, "Textures\\Icons\\Battery");
            this.AddProjectButton(80, 90, 102, "Textures\\Icons\\WindTurbine");
            this.AddProjectButton(136, 90, 103, "Textures\\Icons\\SolarPanel");
            this.AddProjectButton(192, 90, 104, "Textures\\Icons\\HydrogenStorage");
            this.AddProjectButton(248, 90, 105, "Textures\\Icons\\Rocket");
        }
    }
}
