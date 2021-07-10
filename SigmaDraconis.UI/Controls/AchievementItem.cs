namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.UI;
    using Language;

    internal class AchievementItem : UIElementBase
    {
        private readonly Icon icon;
        private readonly TextLabel nameLabel;
        private readonly TextArea descriptionTextArea;

        private readonly StringsForAchievement nameStringId;
        private readonly StringsForAchievement textStringId;

        public AchievementItem(IUIElement parent, int x, int y, int achievementNumber)
            : base(parent, x, y, Scale(320), Scale(80))
        {
            this.nameStringId = (StringsForAchievement)((achievementNumber * 2) - 2);
            this.textStringId = (StringsForAchievement)((achievementNumber * 2) - 1);

            this.nameLabel = UIHelper.AddTextLabel(this, 80, 2, 240, UIColour.WhiteText, LanguageManager.Get<StringsForAchievement>(this.nameStringId));
            this.descriptionTextArea = this.AddChild(new TextArea(this, Scale(80), Scale(16), Scale(240), Scale(48), UIColour.DefaultText, UIColour.Transparent));

            this.descriptionTextArea.SetText(LanguageManager.Get<StringsForAchievement>(this.textStringId));

            this.icon = new Icon("Textures\\Achievements\\Achievement" + achievementNumber, 2, false);
        }

        public override void LoadContent()
        {
            this.icon.LoadContent();
            base.LoadContent();
        }

        protected override void DrawContent()
        {
            if (this.texture == null) this.CreateTexture();

            var r = new Rectangle(this.RenderX, this.RenderY, this.W, this.H);
            var r2 = new Rectangle(this.RenderX, this.RenderY, this.W, Scale(16));

            this.spriteBatch.Begin();

            this.spriteBatch.Draw(this.texture, r, UIColour.ButtonBackground);
            this.spriteBatch.Draw(this.texture, r2, UIColour.ButtonBackground);

            this.icon.Draw(this.spriteBatch, this.RenderX, this.RenderY, 1);

            // Borders
            this.spriteBatch.Draw(this.texture, new Rectangle(r.X, r.Y, r.Width, 1), UIColour.BorderLight);
            this.spriteBatch.Draw(this.texture, new Rectangle(r.X, r.Y, 1, r.Height), UIColour.BorderLight);
            this.spriteBatch.Draw(this.texture, new Rectangle(r.X, r.Bottom - 1, r.Width, 1), UIColour.BorderDark);
            this.spriteBatch.Draw(this.texture, new Rectangle(r.Right - 1, r.Y, 1, r.Height), UIColour.BorderDark);

            this.spriteBatch.End();
            this.IsContentChangedSinceDraw = false;
        }

        protected override void HandleLanguageChange()
        {
            this.nameLabel.Text = LanguageManager.Get<StringsForAchievement>(this.nameStringId);
            this.descriptionTextArea.SetText(LanguageManager.Get<StringsForAchievement>(this.textStringId));

            base.HandleLanguageChange();
        }

        private void CreateTexture()
        {
            this.texture = new Texture2D(UIStatics.Graphics, 1, 1);
            this.texture.SetData(new Color[1] { Color.White });
        }
    }
}
