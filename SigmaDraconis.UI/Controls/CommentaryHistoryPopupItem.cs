namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using System;
    using Draconis.UI;
    using Commentary;
    using Language;
    using Shared;
    
    internal class CommentaryHistoryPopupItem : UIElementBase
    {
        private readonly TextLabel noCommentsLabel;
        private readonly TextButton archiveButton;
        private readonly TextLabel colonistNameLabel;
        private readonly TextLabel timeLabel;
        private readonly TextLabel messageLabel;
        private Comment comment;
        private string timeFormatString;
        private Texture2D smileyTexture;
        private int smileyIndex = -1;

        public event EventHandler<EventArgs> ArchiveButtonClick;

        public CommentaryHistoryPopupItem(IUIElement parent, int x, int y, int w, int h)
            : base(parent, x, y, w, h)
        {
            this.noCommentsLabel = new TextLabel(
                this, 
                0, (h / 2) - (7 * UIStatics.Scale / 100), w, h, 
                LanguageManager.Get<StringsForCommentaryHistoryPopup>(StringsForCommentaryHistoryPopup.NoPreviousComments), 
                UIColour.GrayText);

            this.archiveButton = new TextButton(this, (w / 2) - Scale(70), (h / 2) - Scale(10), Scale(140), Scale(20)
                , LanguageManager.Get<StringsForCommentaryHistoryPopup>(StringsForCommentaryHistoryPopup.ViewArchive)) { IsVisible = false };
            this.AddChild(this.archiveButton);
            this.archiveButton.MouseLeftClick += this.OnArchiveButtonClick;

            this.colonistNameLabel = new TextLabel(this, Scale(4), Scale(2), "", UIColour.DefaultText);
            this.timeLabel = new TextLabel(this, Scale(4), Scale(2), "", UIColour.GrayText);
            this.messageLabel = new TextLabel(this, Scale(4), Scale(18), "", UIColour.DefaultText);

            this.AddChild(this.noCommentsLabel);
            this.AddChild(this.colonistNameLabel);
            this.AddChild(this.timeLabel);
            this.AddChild(this.messageLabel);

            this.timeFormatString = " - " + LanguageManager.Get<StringsForStatusBar>(StringsForStatusBar.TimeFormat);
        }

        public void SetComment(Comment comment, bool showHistoryButton = false)
        {
            if (comment == null)
            {
                this.noCommentsLabel.IsVisible = !showHistoryButton;
                this.colonistNameLabel.IsVisible = false;
                this.timeLabel.IsVisible = false;
                this.messageLabel.IsVisible = false;
                this.comment = null;
                this.smileyIndex = -1;
                this.archiveButton.IsVisible = showHistoryButton;
                return;
            }

            if (comment == this.comment) return;
            this.comment = comment;

            this.archiveButton.IsVisible = false;
            this.noCommentsLabel.IsVisible = false;
            this.colonistNameLabel.IsVisible = true;
            this.timeLabel.IsVisible = true;
            this.messageLabel.IsVisible = true;

            this.colonistNameLabel.Text = comment.ColonistName;
            switch (comment.ColonistSkillType)
            {
                case SkillType.Engineer: this.colonistNameLabel.Colour = new Color(100, 180, 255); break;
                case SkillType.Botanist: this.colonistNameLabel.Colour = new Color(120, 255, 120); break;
                case SkillType.Geologist: this.colonistNameLabel.Colour = new Color(255, 200, 150); break;
                case SkillType.Programmer: this.colonistNameLabel.Colour = new Color(200, 200, 200); break;
                default: this.colonistNameLabel.Colour = UIColour.DefaultText; break;
            }

            if (comment.IsUrgent) this.messageLabel.Colour = UIColour.RedText;
            else if (comment.IsImportant) this.messageLabel.Colour = UIColour.YellowText;
            else this.messageLabel.Colour = UIColour.DefaultText;

            var hour = comment.FrameDisplayed / 3600;
            var day = (hour / WorldTime.HoursInDay) + 1;
            hour = (hour % WorldTime.HoursInDay) + 1;
            var timeStr = string.Format(this.timeFormatString, day, hour);
            this.timeLabel.Text = timeStr.PadLeft(comment.ColonistName.Length + timeStr.Length);

            var text = comment.Text;
            if (text.EndsWith(":-)"))
            {
                this.smileyIndex = 0;
                text = text.Substring(0, text.Length - 3);
            }
            else if (text.EndsWith(":-("))
            {
                this.smileyIndex = 1;
                text = text.Substring(0, text.Length - 3);
            }
            else this.smileyIndex = -1;

            this.messageLabel.Text = text;
        }

        public override void LoadContent()
        {
            this.texture = new Texture2D(UIStatics.Graphics, 1, 1);
            Color[] color = new Color[1] { new Color(0, 0, 0, 64) };
            this.texture.SetData(color);

            this.smileyTexture = UIStatics.Content.Load<Texture2D>("Textures\\Icons\\CommentSmileys");

            base.LoadContent();
        }

        protected override void DrawContent()
        {
            base.DrawContent();

            if (this.smileyIndex >= 0 && this.smileyTexture != null)
            {
                var x = this.messageLabel.X + (this.messageLabel.Text.Length * (7 * UIStatics.Scale / 100));
                var rDest = new Rectangle(this.RenderX + x, this.RenderY + Scale(18), Scale(12), Scale(12));
                var sx = this.smileyIndex * Scale(12);
                var sy = this.appliedScale < 200 ? (this.appliedScale == 100 ? 42 : 24) : 0;
                var rSource = new Rectangle(sx, sy, Scale(12), Scale(12));
                this.spriteBatch.Begin();
                this.spriteBatch.Draw(this.smileyTexture, rDest, rSource, Color.White);
                this.spriteBatch.End();
            }
        }

        public override void ApplyLayout()
        {
            base.ApplyLayout();
            this.noCommentsLabel.Y = (this.H / 2) - (7 * UIStatics.Scale / 100);
        }

        protected override void HandleLanguageChange()
        {
            this.noCommentsLabel.Text = LanguageManager.Get<StringsForCommentaryHistoryPopup>(StringsForCommentaryHistoryPopup.NoPreviousComments);
            this.archiveButton.Text = LanguageManager.Get<StringsForCommentaryHistoryPopup>(StringsForCommentaryHistoryPopup.ViewArchive);
            this.timeFormatString = " - " + LanguageManager.Get<StringsForStatusBar>(StringsForStatusBar.TimeFormat);

            base.HandleLanguageChange();
        }

        private void OnArchiveButtonClick(object sender, Draconis.Shared.MouseEventArgs e)
        {
            this.ArchiveButtonClick?.Invoke(this, new EventArgs());
        }
    }
}
