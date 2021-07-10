namespace SigmaDraconis.UI
{
    using System;
    using Draconis.UI;

    public class ToolbarTooltip : SimpleTooltip
    {
        public ToolbarTooltip(IUIElement attachedElement, string title, string detail = "")
            : base(TooltipParent.Instance, attachedElement, title, detail)
        {
        }

        protected override void UpdatePostion()
        {
            this.Y = this.attachedElement.ScreenY - (this.H + 4);
            this.X = Math.Min(UIStatics.CurrentMouseState.X + Scale(8), GameScreen.Instance.W - this.W - 1);
        }
    }
}
