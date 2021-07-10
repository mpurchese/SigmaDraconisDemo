namespace SigmaDraconis.UI
{
    using Draconis.UI;
    using Language;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using System;

    public class ColonistStatDisplay : UIElementBase
    {
        private Color topEdgeColour = new Color(64, 64, 64);
        private Color bottomEdgeColour = new Color(96, 96, 96);
        private Texture2D backgroundTexture;
        private Texture2D barTextureGreen;
        private Texture2D barTextureYellow;
        private Texture2D barTextureOrange;
        private Texture2D barTextureRed;
        private readonly SimpleTooltip tooltip;

        private readonly string backgroundTexturePath;
        private readonly double fracYellow;
        private readonly double fracOrange;
        private readonly double fracRed;
        private readonly StringsForColonistPanel textId;
        private double fraction;
        private double rateOfChange;
        private string tooltipTitleFormat;
        private string tooltipCustomText;

        public ColonistStatDisplay(IUIElement parent, int x, int y, StringsForColonistPanel textId, double fracYellow, double fracOrange, double fracRed, string backgroundTexturePath)
            : base(parent, x, y, Scale(72), Scale(22))
        {
            this.textId = textId;
            this.backgroundTexturePath = backgroundTexturePath;
            this.fracYellow = fracYellow;
            this.fracOrange = fracOrange;
            this.fracRed = fracRed;
            this.tooltipTitleFormat = LanguageManager.Get<StringsForColonistPanel>(textId) + ": {0:N0}%";
            this.tooltipCustomText = "";

            UIHelper.AddTextLabel(this, 0, 2, 72, textId);
            this.tooltip = UIHelper.AddSimpleTooltip(this.Parent, this, string.Format(this.tooltipTitleFormat, 0));
        }

        public override void LoadContent()
        {
            this.texture = new Texture2D(UIStatics.Graphics, 1, 1);
            this.texture.SetData(new Color[1] { Color.White });
            this.backgroundTexture = UIStatics.Content.Load<Texture2D>(this.backgroundTexturePath);
            this.barTextureGreen = UIStatics.Content.Load<Texture2D>("Textures\\Misc\\ColonistPanelStatBarGreen");
            this.barTextureYellow = UIStatics.Content.Load<Texture2D>("Textures\\Misc\\ColonistPanelStatBarYellow");
            this.barTextureOrange = UIStatics.Content.Load<Texture2D>("Textures\\Misc\\ColonistPanelStatBarOrange");
            this.barTextureRed = UIStatics.Content.Load<Texture2D>("Textures\\Misc\\ColonistPanelStatBarRed");

            base.LoadContent();
        }

        public void SetValue(double fraction, double rateOfChange)
        {
            if (this.fraction == fraction && this.rateOfChange == rateOfChange) return;

            this.fraction = fraction;
            this.rateOfChange = rateOfChange;
            this.IsContentChangedSinceDraw = true;

            this.tooltip.SetTitle(string.Format(this.tooltipTitleFormat, fraction * 100.0));
            this.UpdateTooltipText();
        }

        public void SetTooltipText(string text)
        {
            this.tooltipCustomText = text;
            this.UpdateTooltipText();
        }

        protected override void HandleLanguageChange()
        {
            this.tooltipTitleFormat = LanguageManager.Get<StringsForColonistPanel>(this.textId) + ": {0:N0}%";
            this.tooltip.SetTitle(string.Format(this.tooltipTitleFormat, fraction * 100.0));
            this.UpdateTooltipText();

            base.HandleLanguageChange();
        }

        private void UpdateTooltipText()
        {
            var rateOfChangeDisplay = Math.Round(this.rateOfChange, Math.Abs(this.rateOfChange) >= 10 ? 0 : 1);
            if (rateOfChangeDisplay > 0)
            {
                var increasing = LanguageManager.Get<StringsForColonistPanel>(StringsForColonistPanel.DecreasingAt, rateOfChangeDisplay);
                this.tooltip.SetText(this.tooltipCustomText == "" ? increasing : $"{increasing}||{this.tooltipCustomText}");
            }
            else if (rateOfChangeDisplay < 0)
            {
                var decreasing = LanguageManager.Get<StringsForColonistPanel>(StringsForColonistPanel.IncreasingAt, 0 - rateOfChangeDisplay);
                this.tooltip.SetText(this.tooltipCustomText == "" ? decreasing : $"{decreasing}||{this.tooltipCustomText}");
            }
            else
            {
                this.tooltip.SetText(this.tooltipCustomText);
            }
        }

        protected override void DrawContent()
        {
            if (this.texture == null) return;

            var r = new Rectangle(this.RenderX, this.RenderY, this.W, this.H);

            this.spriteBatch.Begin();

            this.spriteBatch.Draw(this.backgroundTexture, new Rectangle(r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2), this.GetBackgroundTextureSourceRect(), Color.White);
            this.spriteBatch.Draw(this.texture, new Rectangle(r.X, r.Y, r.Width, 1), this.topEdgeColour);
            this.spriteBatch.Draw(this.texture, new Rectangle(r.X, r.Y, 1, r.Height), this.topEdgeColour);
            this.spriteBatch.Draw(this.texture, new Rectangle(r.X, r.Bottom - 1, r.Width, 1), this.bottomEdgeColour);
            this.spriteBatch.Draw(this.texture, new Rectangle(r.Right - 1, r.Y, 1, r.Height), this.bottomEdgeColour);

            // Bar
            var sourceRec = this.GetBarTextureSourceRect();
            var targetRec = this.GetBarTextureTargetRect();
            this.spriteBatch.Draw(this.GetBarTexture(), targetRec, sourceRec, Color.White);

            this.spriteBatch.End();

            this.IsContentChangedSinceDraw = false;
        }

        protected Rectangle? GetBackgroundTextureSourceRect()
        {
            if (UIStatics.Scale == 200) return new Rectangle(0, 55, 144, 44);
            if (UIStatics.Scale == 150) return new Rectangle(0, 22, 108, 33);
            return new Rectangle(0, 0, 72, 22);
        }

        protected Rectangle? GetBarTextureSourceRect()
        {
            if (UIStatics.Scale == 200) return new Rectangle(0, 5, this.barTextureGreen.Width, 4);
            if (UIStatics.Scale == 150) return new Rectangle(0, 2, this.barTextureGreen.Width, 3);
            return new Rectangle(0, 0, this.barTextureGreen.Width, 2);
        }

        protected Rectangle GetBarTextureTargetRect()
        {
            var w = (int)(this.fraction * (this.W - 2));
            if (UIStatics.Scale == 200) return new Rectangle(this.RenderX + 1, this.RenderY + this.H - 9, w, 8);
            if (UIStatics.Scale == 150) return new Rectangle(this.RenderX + 1, this.RenderY + this.H - 7, w, 6);
            return new Rectangle(this.RenderX + 1, this.RenderY + this.H - 5, w, 4);
        }

        protected Texture2D GetBarTexture()
        {
            if (this.fraction >= this.fracRed) return this.barTextureRed;
            if (this.fraction >= this.fracOrange) return this.barTextureOrange;
            if (this.fraction >= this.fracYellow) return this.barTextureYellow;
            return this.barTextureGreen;
        }
    }
}
