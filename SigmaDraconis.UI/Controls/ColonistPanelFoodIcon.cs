namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.UI;
    using Config;
    using Language;
    using Shared;

    public class ColonistPanelFoodIcon : UIElementBase
    {
        private Texture2D iconsTexture;
        private CropDefinition cropDef;
        private int? opinion;
        private long? frame;
        private readonly SimpleTooltip tooltip;

        public ColonistPanelFoodIcon(IUIElement parent, int x, int y, int width, int height)
            : base(parent, x, y, width, height)
        {
            this.tooltip = UIHelper.AddSimpleTooltip(parent.Parent, this);
        }

        public override void LoadContent()
        {
            this.texture = new Texture2D(UIStatics.Graphics, 1, 1);
            this.texture.SetData(new Color[1] { Color.White });
            this.iconsTexture = UIStatics.Content.Load<Texture2D>("Textures\\Icons\\ColonistPanelFoodIcons");
            base.LoadContent();
        }

        public void SetMealType(CropDefinition cropDef, int? opinion, long? frame)
        {
            if (cropDef?.Id == this.cropDef?.Id && opinion == this.opinion && frame == this.frame) return;

            this.cropDef = cropDef;
            this.opinion = opinion;
            this.frame = frame;

            this.UpdateTooltip();
            this.IsContentChangedSinceDraw = true;
        }

        protected override void HandleLanguageChange()
        {
            this.UpdateTooltip();
            base.HandleLanguageChange();
        }

        private void UpdateTooltip()
        {
            if (this.cropDef == null)
            {
                this.tooltip.IsEnabled = false;
                return;
            }

            this.tooltip.IsEnabled = true;
            switch (this.opinion)
            {
                case 1: 
                    this.tooltip.SetTitle($"{this.cropDef.DisplayName} ({GetString(StringsForColonistPanel.Liked)})");
                    this.tooltip.TitleColour = UIColour.GreenText;
                    break;
                case 0: 
                    this.tooltip.SetTitle($"{this.cropDef.DisplayName} ({GetString(StringsForColonistPanel.Neutral)})");
                    this.tooltip.TitleColour = UIColour.DefaultText;
                    break;
                default:
                    this.tooltip.SetTitle($"{this.cropDef.DisplayName} ({GetString(StringsForColonistPanel.Disliked)})");
                    this.tooltip.TitleColour = UIColour.RedText;
                    break;
            }
            
            if (this.frame > 0)
            {
                var hour = this.frame / 3600;
                var day = (hour / WorldTime.HoursInDay) + 1;
                hour = (hour % WorldTime.HoursInDay) + 1;
                this.tooltip.SetText(LanguageManager.Get<StringsForColonistPanel>(StringsForColonistPanel.TimeFormat, day, hour));
            }
            else
            {
                this.tooltip.SetText("");
            }
        }

        protected override void DrawContent()
        {
            if (this.texture == null) return;

            var r = new Rectangle(this.RenderX, this.RenderY, this.W, this.H);

            this.spriteBatch.Begin();

            switch (this.opinion)
            {
                case 1: this.spriteBatch.Draw(this.texture, new Rectangle(r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2), new Color(0, 70, 0, 192)); break;
                case 0: this.spriteBatch.Draw(this.texture, new Rectangle(r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2), new Color(56, 50, 46, 192)); break;
                case -1: this.spriteBatch.Draw(this.texture, new Rectangle(r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2), new Color(112, 0, 0, 192)); break;
            }

            // Borders
            var borderColour = UIColour.BorderDark;
            this.spriteBatch.Draw(this.texture, new Rectangle(r.X, r.Y, r.Width, 1), borderColour);
            this.spriteBatch.Draw(this.texture, new Rectangle(r.X, r.Y, 1, r.Height), borderColour);
            this.spriteBatch.Draw(this.texture, new Rectangle(r.X, r.Bottom - 1, r.Width, 1), borderColour);
            this.spriteBatch.Draw(this.texture, new Rectangle(r.Right - 1, r.Y, 1, r.Height), borderColour);

            // Icon
            if (this.cropDef != null)
            {
                var iconSize = 18;
                var sourceY = 63;
                if (UIStatics.Scale == 200)
                {
                    iconSize = 36;
                    sourceY = 0;
                }
                else if (UIStatics.Scale == 150)
                {
                    iconSize = 27;
                    sourceY = 36;
                }

                var sourceRec = new Rectangle(this.cropDef.IconIndex * iconSize, sourceY, iconSize, iconSize);
                var destRec = new Rectangle(this.RenderX + ((this.W - iconSize) / 2), this.RenderY + ((this.H - iconSize) / 2), iconSize, iconSize);
                this.spriteBatch.Draw(this.iconsTexture, destRec, sourceRec, Color.White);
            }

            this.spriteBatch.End();

            this.IsContentChangedSinceDraw = false;
        }

        private static string GetString(StringsForColonistPanel key)
        {
            return LanguageManager.Get<StringsForColonistPanel>(key).ToLowerInvariant();
        }
    }
}
