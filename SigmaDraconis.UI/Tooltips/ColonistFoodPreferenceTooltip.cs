namespace SigmaDraconis.UI
{
    using Draconis.UI;
    using Microsoft.Xna.Framework;
    using System.Collections.Generic;
    using System.Linq;
    using Language;

    public class ColonistFoodPreferenceTooltip : SimpleTooltip
    {
        private List<Color> colours = new List<Color>();
        private readonly StringsForColonistPanel titleTextId;

        public ColonistFoodPreferenceTooltip(IUIElement attachedElement, StringsForColonistPanel titleTextId, Color titleColour)
            : base(TooltipParent.Instance, attachedElement, LanguageManager.Get<StringsForColonistPanel>(titleTextId))
        {
            this.TitleColour = titleColour;
            this.titleTextId = titleTextId;
        }

        public void SetLineColours(IEnumerable<Color> colours)
        {
            this.colours = colours.ToList();
            this.IsContentChangedSinceDraw = true;
        }

        protected override void HandleLanguageChange()
        {
            this.SetTitle(LanguageManager.Get<StringsForColonistPanel>(this.titleTextId));
            base.HandleLanguageChange();
        }

        protected override Color GetColourForLine(int lineNumber)
        {
            return lineNumber > 0 && this.colours.Count >= lineNumber ? this.colours[lineNumber - 1] : UIColour.DefaultText;
        }
    }
}
