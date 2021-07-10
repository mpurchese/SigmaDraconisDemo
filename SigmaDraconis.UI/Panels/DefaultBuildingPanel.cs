namespace SigmaDraconis.UI
{
    using Draconis.UI;
    using Language;

    public class DefaultBuildingPanel : BuildingPanel, IThingPanel
    {
        private readonly TextLabel label;

        public DefaultBuildingPanel(IUIElement parent, int y)
            : base(parent, y)
        {
            this.label = UIHelper.AddTextLabel(this, 0, 24, 320, StringsForThingPanels.NoActionsAvailable);
        }

        public override void Update()
        {
            this.label.IsVisible = this.IsBuildingUiVisible;
            base.Update();
        }
    }
}
