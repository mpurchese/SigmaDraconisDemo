namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework;
    using Draconis.UI;
    using Language;
    using Shared;

    /// <summary>
    /// A status label e.g. "STATUS: ONLINE ", with the status in colour.
    /// </summary>
    public class BuildingPanelStatusLabel : UIElementBase
    {
        private readonly TextLabel textLabel1;
        private readonly TextLabel textLabel2;
        private BuildingDisplayStatus currentStatus;

        public BuildingPanelStatusLabel(IUIElement parent, int x, int y, int w) : base(parent, x, y, w, Scale(20))
        {
            this.textLabel1 = UIHelper.AddTextLabel(this, 0, 0, LanguageManager.Get<StringsForThingPanels>(StringsForThingPanels.Status));
            this.textLabel2 = UIHelper.AddTextLabel(this, 0, 0, UIColour.RedText);
        }

        public void SetStatus(BuildingDisplayStatus newStatus, Color colour)
        {
            this.textLabel2.Colour = colour;
            if (newStatus.CompareTo(this.currentStatus) != 0) this.SetStatus(newStatus);
        }

        protected override void HandleLanguageChange()
        {
            this.SetStatus(this.currentStatus);
            base.HandleLanguageChange();
        }

        private void SetStatus(BuildingDisplayStatus newStatus)
        {
            this.textLabel1.Text = LanguageManager.Get<StringsForThingPanels>(StringsForThingPanels.Status);
            this.textLabel2.Text = LanguageManager.Get<BuildingDisplayStatus>(newStatus);

            this.textLabel1.X = (this.W - (UIStatics.TextRenderer.LetterSpace * (this.textLabel1.Text.Length + this.textLabel2.Text.Length + 1))) / 2;
            this.textLabel2.X = this.textLabel1.X + (UIStatics.TextRenderer.LetterSpace * (this.textLabel1.Text.Length + 1));

            this.currentStatus = newStatus;
        }
    }
}
