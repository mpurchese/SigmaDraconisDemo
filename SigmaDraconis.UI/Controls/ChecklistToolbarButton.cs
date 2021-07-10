namespace SigmaDraconis.UI
{ 
    using System;
    using Microsoft.Xna.Framework;
    using Draconis.UI;

    public class ChecklistToolbarButton : IconButtonWithOverlay
    {
        private readonly TextLabel label;
        private int count;
        private bool isCountHighlighted;
        private bool highlightUpdateFlag;

        public ChecklistToolbarButton(IUIElement parent, int x, int y)
            : base(parent, x, y, "Textures\\Icons\\Help", "Textures\\Icons\\HelpOverlay")
        {
            this.label = UIHelper.AddTextLabel(this, 14, 19, 18, UIColour.LightBlueText);
        }

        public void SetCount(int newCount, bool isPanelOpen)
        {
            if (newCount > 9) newCount = 9;
            this.isCountHighlighted |= newCount > this.count && !isPanelOpen;
            if (this.isCountHighlighted && (isPanelOpen || newCount == 0))
            {
                this.isCountHighlighted = false;
                this.label.Colour = UIColour.LightBlueText;
            }

            if (newCount == this.count) return;

            this.count = newCount;
            this.label.Text = newCount > 0 ? $"+{newCount}" : "";
            this.IsOverlayVisible = newCount > 0;
        }

        public override void Update()
        {
            if (this.isCountHighlighted && !GameScreen.Instance.IsPaused)
            {
                this.highlightUpdateFlag = !this.highlightUpdateFlag;
                if (this.highlightUpdateFlag)
                {
                    var dt = DateTime.Now;
                    var ms = dt.Millisecond;
                    
                    this.label.Colour = dt.Second % 2 == 0 
                        ? new Color(120 + (ms / 8), 200 + (ms / 19), 255)
                        : new Color(245 - (ms / 8), 252 - (ms / 19), 255);
                }
            }

            base.Update();
        }
    }
}
