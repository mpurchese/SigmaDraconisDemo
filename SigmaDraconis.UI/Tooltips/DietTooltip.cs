namespace SigmaDraconis.UI
{
    using Draconis.UI;
    using Microsoft.Xna.Framework;
    using Language;

    public class DietTooltip : SimpleTooltip
    {
        private int total;
        private int likes;
        private int variety;

        public DietTooltip(IUIElement attachedElement)
            : base(TooltipParent.Instance, attachedElement, "")
        {
            this.UpdateText();
        }

        public void SetValues(int total, int likes, int variety)
        {
            if (total == this.total && likes == this.likes && variety == this.variety) return;

            this.total = total;
            this.likes = likes;
            this.variety = variety;

            this.UpdateText();
        }

        protected override void HandleLanguageChange()
        {
            this.UpdateText();
            base.HandleLanguageChange();
        }

        private void UpdateText()
        {
            this.SetText(LanguageManager.Get<StringsForColonistPanel>(StringsForColonistPanel.DietTooltip, Format(this.total), Format(this.likes), Format(this.variety)));
        }

        protected override Color GetColourForLine(int lineNumber)
        {
            if (lineNumber < this.lineCount || this.total == 0) return UIColour.DefaultText;
            if (this.total > 2) return UIColour.GreenText;
            if (this.total > 0) return UIColour.LightGreenText;
            if (this.total < -2) return UIColour.RedText;
            return UIColour.YellowText;
        }

        private static string Format(int number)
        {
            return number >= 0 ? $"+{number}" : number.ToString();
        }
    }
}
