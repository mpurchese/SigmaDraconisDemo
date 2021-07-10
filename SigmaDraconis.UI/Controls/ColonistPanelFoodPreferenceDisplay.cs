namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using System.Collections.Generic;
    using System.Linq;
    using Draconis.UI;
    using Config;
    using Language;
    using Shared;

    public class ColonistPanelFoodPreferenceDisplay : UIElementBase
    {
        private Color topEdgeColour = new Color(64, 64, 64);
        private Color bottomEdgeColour = new Color(64, 64, 64);
        private Texture2D backgroundTexture;
        private Texture2D foodIconsTexture;
        private readonly List<CropDefinition> dislikes = new List<CropDefinition>();
        private readonly List<CropDefinition> neutral = new List<CropDefinition>();
        private readonly List<CropDefinition> likes = new List<CropDefinition>();
        private readonly ColonistFoodPreferenceTooltip tooltip1;
        private readonly ColonistFoodPreferenceTooltip tooltip2;
        private readonly ColonistFoodPreferenceTooltip tooltip3;
        private readonly EmptyElement tooltipAttachElement1;
        private readonly EmptyElement tooltipAttachElement2;
        private readonly EmptyElement tooltipAttachElement3;

        public ColonistPanelFoodPreferenceDisplay(IUIElement parent, int x, int y, int width, int height)
            : base(parent, x, y, width, height)
        {
            this.tooltipAttachElement1 = new EmptyElement(this, 0, 0, width / 4, height);
            this.AddChild(this.tooltipAttachElement1);

            this.tooltipAttachElement2 = new EmptyElement(this, width / 4, 0, width / 2, height);
            this.AddChild(this.tooltipAttachElement2);

            this.tooltipAttachElement3 = new EmptyElement(this, width * 3 / 4, 0, width / 4, height);
            this.AddChild(this.tooltipAttachElement3);

            this.tooltip1 = new ColonistFoodPreferenceTooltip(this.tooltipAttachElement1, StringsForColonistPanel.DislikedFoods, UIColour.RedText);
            TooltipParent.Instance.AddChild(this.tooltip1, this.Parent);

            this.tooltip2 = new ColonistFoodPreferenceTooltip(this.tooltipAttachElement2, StringsForColonistPanel.NeutralFoods, UIColour.GrayText);
            TooltipParent.Instance.AddChild(this.tooltip2, this.Parent);

            this.tooltip3 = new ColonistFoodPreferenceTooltip(this.tooltipAttachElement3, StringsForColonistPanel.LikedFoods, UIColour.GreenText);
            TooltipParent.Instance.AddChild(this.tooltip3, this.Parent);
        }

        public override void LoadContent()
        {
            this.texture = new Texture2D(UIStatics.Graphics, 1, 1);
            this.texture.SetData(new Color[1] { Color.White });
            this.backgroundTexture = UIStatics.Content.Load<Texture2D>("Textures\\Misc\\ColonistPanelFoodBackground");
            this.foodIconsTexture = UIStatics.Content.Load<Texture2D>("Textures\\Icons\\ColonistPanelFoodIcons");
            base.LoadContent();
        }

        public void Clear()
        {
            this.dislikes.Clear();
            this.neutral.Clear();
            this.likes.Clear();
        }

        public void UpdateList(List<CropDefinition> dislikes, List<CropDefinition> neutral, List<CropDefinition> likes)
        {
            if (this.dislikes.Count == dislikes.Count && this.dislikes.All(a => dislikes.Any(b => b.Id == a.Id))
                && this.neutral.Count == neutral.Count && this.neutral.All(a => neutral.Any(b => b.Id == a.Id))
                && this.likes.Count == likes.Count && this.likes.All(a => likes.Any(b => b.Id == a.Id))) return;

            this.Clear();
            this.dislikes.AddRange(dislikes.OrderBy(d => d.Id == Constants.MushFoodType ? 0 : 1).ThenBy(d => d.Id));
            this.neutral.AddRange(neutral.OrderBy(d => d.Id));
            this.likes.AddRange(likes.OrderBy(d => d.Id));
            this.IsContentChangedSinceDraw = true;

            this.tooltip1.SetLineColours(this.dislikes.Select(d => new Color(d.TextR, d.TextG, d.TextB)));
            this.tooltip2.SetLineColours(this.neutral.Select(d => new Color(d.TextR, d.TextG, d.TextB)));
            this.tooltip3.SetLineColours(this.likes.Select(d => new Color(d.TextR, d.TextG, d.TextB)));

            var likesStr = string.Join("|", this.likes.Select(s => s.DisplayName));
            var neutralStr = string.Join("|", this.neutral.Select(s => s.DisplayName));
            var dislikesStr = string.Join("|", this.dislikes.Select(s => s.DisplayName));
            this.tooltip1.SetText(dislikesStr);
            this.tooltip2.SetText(neutralStr);
            this.tooltip3.SetText(likesStr);
        }

        protected override void DrawContent()
        {
            if (this.texture == null) return;

            var r = new Rectangle(this.RenderX, this.RenderY, this.W, this.H);

            this.spriteBatch.Begin();

            // Borders
            this.spriteBatch.Draw(this.backgroundTexture, new Rectangle(r.X + 1, r.Y + 1, r.Width - 2, r.Height - 2), Color.White);
            this.spriteBatch.Draw(this.texture, new Rectangle(r.X, r.Y, r.Width, 1), this.topEdgeColour);
            this.spriteBatch.Draw(this.texture, new Rectangle(r.X, r.Y, 1, r.Height), this.topEdgeColour);
            this.spriteBatch.Draw(this.texture, new Rectangle(r.X, r.Bottom - 1, r.Width, 1), this.bottomEdgeColour);
            this.spriteBatch.Draw(this.texture, new Rectangle(r.Right - 1, r.Y, 1, r.Height), this.bottomEdgeColour);

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

            var iconX = this.RenderX + 1;
            var iconY = this.RenderY + (this.H / 2) - (Scale(18) / 2);

            foreach (var def in this.dislikes)
            {
                var sourceRec = new Rectangle(def.IconIndex * iconSize, sourceY, iconSize, iconSize);
                this.spriteBatch.Draw(this.foodIconsTexture, new Rectangle(iconX, iconY, iconSize, iconSize), sourceRec, Color.White);
                iconX += iconSize;
            }

            iconX = this.RenderX + ((this.W - (iconSize * this.neutral.Count)) / 2);
            foreach (var def in this.neutral)
            {
                var sourceRec = new Rectangle(def.IconIndex * iconSize, sourceY, iconSize, iconSize);
                this.spriteBatch.Draw(this.foodIconsTexture, new Rectangle(iconX, iconY, iconSize, iconSize), sourceRec, Color.White);
                iconX += iconSize;
            }

            iconX = this.RenderX + this.W - (iconSize * this.likes.Count) - 1;
            foreach (var def in this.likes)
            {
                var sourceRec = new Rectangle(def.IconIndex * iconSize, sourceY, iconSize, iconSize);
                this.spriteBatch.Draw(this.foodIconsTexture, new Rectangle(iconX, iconY, iconSize, iconSize), sourceRec, Color.White);
                iconX += iconSize;
            }

            this.spriteBatch.End();

            this.IsContentChangedSinceDraw = false;
        }
    }
}
