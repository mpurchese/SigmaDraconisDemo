namespace SigmaDraconis.UI
{
    using Draconis.UI;
    using Draconis.Shared;
    using Shared;
    using World.Projects;
    using WorldInterfaces;

    public class CompostFactoryPanel : FactoryBuildingPanel
    {
        private readonly TickBoxIconButton allowMushButton;
        private readonly TickBoxIconButton allowOrganicsButton;
        private readonly FlowDiagramFactory flowDiagram2;

        public CompostFactoryPanel(IUIElement parent, int y) : base(parent, y, true, true, 146)
        {
            this.allowOrganicsButton = new TickBoxIconButton(this, Scale(262), Scale(44), Scale(50), Scale(22), new Icon("Textures\\Icons\\Items", 13), (int)ItemType.Biomass - 1) { IsTicked = true };
            this.allowOrganicsButton.MouseLeftClick += this.OnAllowOrganicsButtonClick;
            this.AddChild(this.allowOrganicsButton);

            this.allowMushButton = new TickBoxIconButton(this, Scale(262), Scale(68), Scale(50), Scale(22), new Icon("Textures\\Icons\\Items", 13), (int)ItemType.Mush - 1) { IsTicked = true };
            this.allowMushButton.MouseLeftClick += this.OnAllowMushButtonClick;
            this.AddChild(this.allowMushButton);

            var energy = Constants.CompostFactoryEnergyUse * Constants.CompostFactoryFramesToProcess / Constants.FramesPerHour;
            this.flowDiagram = new FlowDiagramFactory(this, 0, Scale(100), this.W, ItemType.Biomass, ItemType.None, energy, Constants.CompostFactoryFramesToProcess, ItemType.Compost, 1);
            this.AddChild(this.flowDiagram);

            this.flowDiagram2 = new FlowDiagramFactory(this, 0, Scale(122), this.W, ItemType.Mush, ItemType.None, energy, Constants.CompostFactoryFramesToProcess, ItemType.Compost, 1);
            this.AddChild(this.flowDiagram2);

            this.statusControl.X = 0;
            this.maintenanceControl.X = 0;
            this.suppressDeconstructConduitNode = true;   // No room for this
        }

        public override void Update()
        {
            base.Update();
            if (this.building is ICompostFactory cf && this.IsBuildingUiVisible)
            {
                if (!this.allowMushButton.IsVisible)
                {
                    this.allowMushButton.IsVisible = true;
                    this.allowOrganicsButton.IsVisible = true;
                }

                this.allowMushButton.IsTicked = cf.AllowMush;
                this.allowOrganicsButton.IsTicked = cf.AllowOrganics;

                if (this.flowDiagram is FlowDiagramFactory f)
                {
                    var frames = ProjectManager.GetDefinition(9)?.IsDone == true ? Constants.CompostFactoryFramesToProcessImproved : Constants.CompostFactoryFramesToProcess;
                    var energy = Constants.CompostFactoryEnergyUse * frames / Constants.FramesPerHour;
                    f.Frames = frames;
                    f.Energy = energy;
                    this.flowDiagram2.Frames = frames;
                    this.flowDiagram2.Energy = energy;
                    this.flowDiagram2.IsVisible = true;
                    f.IsEnabled = cf.AllowOrganics;
                    this.flowDiagram2.IsEnabled = cf.AllowMush;
                }
            }
            else
            {
                this.allowMushButton.IsVisible = false;
                this.allowOrganicsButton.IsVisible = false;
                this.flowDiagram2.IsVisible = true;
            }
        }

        private void OnAllowMushButtonClick(object sender, MouseEventArgs e)
        {
            if (this.building is ICompostFactory f) f.AllowMush = !f.AllowMush;
        }

        private void OnAllowOrganicsButtonClick(object sender, MouseEventArgs e)
        {
            if (this.building is ICompostFactory f) f.AllowOrganics = !f.AllowOrganics;
        }
    }
}
