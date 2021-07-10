namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.UI;
    using Language;
    using Shared;
    using WorldInterfaces;

    public class MothershipColonistSummary : ButtonBase
    {
        private readonly TextLabel nameLabel;
        private readonly TextLabel statusLabel;
        private readonly IconButton skillIcon;
        private IColonistPlaceholder colonist;

        public Color BackgroundColour { get; set; } = new Color(0, 0, 0, 64);

        public MothershipColonistSummary(IUIElement parent, int x, int y, int w, int h, IColonistPlaceholder colonist) : base(parent, x, y, w, h)
        {
            this.nameLabel = new TextLabel(this, Scale(4), Scale(2), colonist.Name.ToUpperInvariant(), UIColour.DefaultText);
            this.AddChild(this.nameLabel);

            this.statusLabel = new TextLabel(this, Scale(4), h - Scale(17), "", UIColour.DefaultText);
            this.AddChild(this.statusLabel);

            this.skillIcon = new IconButton(this, this.W - Scale(34), Scale(2), "Textures\\Icons\\" + colonist.Skill.ToString(), 1f, true) { IsInteractive = false };
            this.AddChild(skillIcon);

            this.colonist = colonist;
        }

        public void SetColonist(IColonistPlaceholder colonist)
        {
            this.colonist = colonist;

            this.nameLabel.Text = colonist.Name.ToUpperInvariant();
            this.skillIcon.SetTexture("Textures\\Icons\\" + colonist.Skill.ToString());
            this.UpdateStatusLabel();

            this.IsContentChangedSinceDraw = true;
        }

        public override void LoadContent()
        {
            this.UpdateStatusLabel();
            this.CreateTexture();
            base.LoadContent();
        }

        public void UpdateStatusLabel()
        {
            var str = colonist.PlaceHolderStatus == ColonistPlaceholderStatus.Waking 
                ? LanguageManager.Get<ColonistPlaceholderStatus>(this.colonist.PlaceHolderStatus, colonist.TimeToArrivalInFrames / 3600, colonist.TimeToArrivalInFrames % 3600 / 60)
                : LanguageManager.Get<ColonistPlaceholderStatus>(this.colonist.PlaceHolderStatus);

            var statusColour = UIColour.DefaultText;
            if (colonist.PlaceHolderStatus == ColonistPlaceholderStatus.InStasis) statusColour = UIColour.LightBlueText;
            else if (colonist.PlaceHolderStatus == ColonistPlaceholderStatus.Waking) statusColour = UIColour.YellowText;
            else if (colonist.PlaceHolderStatus == ColonistPlaceholderStatus.Active) statusColour = UIColour.GreenText;
            else if (colonist.PlaceHolderStatus == ColonistPlaceholderStatus.Dead) statusColour = UIColour.RedText;
            this.statusLabel.Colour = statusColour;

            this.statusLabel.Text = str;
        }

        protected void CreateTexture()
        {
            this.texture = new Texture2D(UIStatics.Graphics, 1, 1);
            this.texture.SetData(new Color[1] { Color.White });
        }

        protected override void DrawContent()
        {
            if (this.texture == null) this.CreateTexture();

            var r = new Rectangle(this.RenderX, this.RenderY, this.W, this.H);
            var borderColour = this.IsSelected ? this.BorderColourSelected : (this.isMouseOver ? this.BorderColourMouseOver : this.BorderColour2);

            this.spriteBatch.Begin();

            // Background
            this.spriteBatch.Draw(this.texture, r, this.BackgroundColour);

            // Borders
            this.spriteBatch.Draw(this.texture, new Rectangle(r.X, r.Y, r.Width, 1), borderColour);
            this.spriteBatch.Draw(this.texture, new Rectangle(r.X, r.Y, 1, r.Height), borderColour);
            this.spriteBatch.Draw(this.texture, new Rectangle(r.X, r.Bottom - 1, r.Width, 1), borderColour);
            this.spriteBatch.Draw(this.texture, new Rectangle(r.Right - 1, r.Y, 1, r.Height), borderColour);

            this.spriteBatch.End();

            this.IsContentChangedSinceDraw = false;
        }

        public override void Update()
        {
            if (this.isMouseOver != this.IsMouseOver)
            {
                this.IsContentChangedSinceDraw = true;
                this.isMouseOver = this.IsMouseOver;
            }

            base.Update();
        }
    }
}
