namespace SigmaDraconis.UI
{
    using Draconis.UI;
    using Language;
    using Shared;

    public class PlanterPanelStatusLabel : TextLabelAutoScaling
    {
        public PlanterPanelStatusLabel(IUIElement parent, int x, int y, int w) : base(parent, x, y, w, Scale(20), "", UIColour.DefaultText)
        {
        }

        public void SetStatus(BuildingDisplayStatus status)
        {
            this.Text = LanguageManager.Get<BuildingDisplayStatus>(status);
        }

        public void SetStatus(PlanterStatus status, double? progress = null)
        {
            this.Text = status == PlanterStatus.InProgress
                ? LanguageManager.Get<BuildingDisplayStatus>(BuildingDisplayStatus.InProgressPercent, FormatPercent(progress, "  0%"))
                : LanguageManager.Get<PlanterStatus>(status);
        }

        private static string FormatPercent(double? val, string defaultStr)
        {
            return val.HasValue ? string.Format("{0,3:D0}%", (int)(val * 100.0)) : defaultStr;
        }
    }
}
