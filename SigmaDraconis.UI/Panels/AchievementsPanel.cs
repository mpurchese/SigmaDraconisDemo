namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework;
    using System;

    using Draconis.Shared;
    using Draconis.UI;
    using Language;

    public class AchievementsPanel : PanelRight
    {
        private readonly TextLabel statusLabel;
        private readonly TextButton hideButton;

        public event EventHandler<EventArgs> HideClick;

        public AchievementsPanel(IUIElement parent, int y) : base(parent, y, Scale(360), Scale(404), GetString(StringsForAchievementsPanel.Title))
        {
            this.AddChild(new AchievementsContainer(this, Scale(8), Scale(34), Scale(344), Scale(336), 4));

            var notInDemo = GetString(StringsForAchievementsPanel.AchievementsDisabled);
            this.statusLabel = this.AddChild(new TextLabel(this, Scale(8), Scale(16), Scale(344), Scale(16), notInDemo, UIColour.DefaultText, TextAlignment.MiddleCentre));

            this.hideButton = UIHelper.AddTextButton(this, 270, 378, 80, GetString(StringsForAchievementsPanel.Hide));
            this.hideButton.MouseLeftClick += this.OnHideButtonClick;
        }

        protected override void HandleLanguageChange()
        {
            this.Title = GetString(StringsForAchievementsPanel.Title);

            this.statusLabel.Text = GetString(StringsForAchievementsPanel.AchievementsDisabled);

            this.hideButton.Text = GetString(StringsForAchievementsPanel.Hide);

            base.HandleLanguageChange();
        }

        protected override void DrawBaseLayer()
        {
            Rectangle r = new Rectangle(this.RenderX, this.RenderY, this.W, Scale(14));
            Rectangle r2 = new Rectangle(this.RenderX + Scale(8), this.RenderY + Scale(16), this.W - Scale(16), Scale(16));

            spriteBatch.Begin();
            spriteBatch.Draw(this.pixelTexture, r, Color.White);
            spriteBatch.Draw(this.pixelTexture, r2, new Color(0, 0, 0, 180));
            spriteBatch.End();
        }

        private void OnHideButtonClick(object sender, MouseEventArgs e)
        {
            this.HideClick?.Invoke(this, new EventArgs());
        }

        private static string GetString(object value)
        {
            return LanguageManager.Get<StringsForAchievementsPanel>(value);
        }
    }
}
