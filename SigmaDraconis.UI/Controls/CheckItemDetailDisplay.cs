namespace SigmaDraconis.UI
{
    using Draconis.UI;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using CheckList;

    internal class CheckItemDetailDisplay : UIElementBase
    {
        private Color backgroundColour = new Color(0, 0, 0, 64);
        private Color titleBackgroundColour = new Color(0, 0, 0, 128);
        private Color borderColour = new Color(64, 64, 64, 255);
        private Item item;
        private bool isItemComplete;

        private readonly TextLabel titleLabel;
        private readonly TextArea textArea1;
        private readonly TextArea textArea2;
        private readonly Icon icon = new Icon("Textures\\Icons\\Checklist\\None", isMultiscaleTecture: false, hasBorder: true);
        private readonly Icon iconDone = new Icon("Textures\\Icons\\Checklist\\Done", isMultiscaleTecture: false, hasBorder: false);

        public CheckItemDetailDisplay(IUIElement parent, int x, int y, int width, int height)
            : base(parent, x, y, width, height)
        {
            this.titleLabel = UIHelper.AddTextLabel(this, 0, 2, UnScale(width), UIColour.DefaultText);

            this.textArea1 = new TextArea(this, Scale(72), Scale(20), width - Scale(76), Scale(48), UIColour.DefaultText);
            this.AddChild(this.textArea1);

            this.textArea2 = new TextArea(this, Scale(4), Scale(72), width - Scale(8), height - Scale(76), UIColour.DefaultText);
            this.AddChild(this.textArea2);
        }

        public void Clear()
        {
            this.titleLabel.Text = "";
            this.textArea1.SetText("");
            this.textArea2.SetText("");
            this.icon.LoadContent("Textures\\Icons\\Checklist\\None");
            this.IsContentChangedSinceDraw = true;
        }

        public void SetItem(Item item)
        {
            this.item = item;
            this.titleLabel.Text = item.Title;
            this.textArea1.SetText(item.Text1);
            this.textArea2.SetText(item.Text2);
            this.icon.LoadContent(!string.IsNullOrEmpty(item.IconName) ? $"Textures\\Icons\\Checklist\\{item.IconName}" : null);
            this.IsContentChangedSinceDraw = true;
        }

        public override void LoadContent()
        {
            base.LoadContent();
            this.icon.LoadContent();
            this.iconDone.LoadContent();
        }

        public override void Update()
        {
            if (this.isItemComplete != (this.item?.IsComplete ?? false))
            {
                this.isItemComplete = this.item?.IsComplete ?? false;
                this.IsContentChangedSinceDraw = true;
            }

            base.Update();
        }

        protected override void HandleLanguageChange()
        {
            if (item != null)
            {
                this.titleLabel.Text = item.Title;
                this.textArea1.SetText(item.Text1);
                this.textArea2.SetText(item.Text2);
            }

            base.HandleLanguageChange();
        }

        protected void CreateTexture()
        {
            this.texture = new Texture2D(UIStatics.Graphics, 1, 1);
            this.texture.SetData(new Color[1] { Color.White });
        }

        protected override void DrawContent()
        {
            if (this.texture == null) this.CreateTexture();

            var r1 = new Rectangle(this.RenderX, this.RenderY, this.W, this.H);
            var r2 = new Rectangle(this.RenderX, this.RenderY, this.W, Scale(16) + 1);

            this.spriteBatch.Begin();

            // Background
            this.spriteBatch.Draw(this.texture, r1, this.backgroundColour);
            this.spriteBatch.Draw(this.texture, r2, this.titleBackgroundColour);

            // Icon with border
            var iconX = this.RenderX + Scale(4);
            var iconY = this.RenderY + Scale(20);
            this.icon.Draw(this.spriteBatch, iconX, iconY);
            if (this.item?.IsComplete == true) this.iconDone.Draw(this.spriteBatch, iconX, iconY);

            // Borders
            this.spriteBatch.Draw(this.texture, new Rectangle(r1.X, r1.Y, r1.Width, 1), this.borderColour);
            this.spriteBatch.Draw(this.texture, new Rectangle(r1.X, r1.Y, 1, r1.Height), this.borderColour);
            this.spriteBatch.Draw(this.texture, new Rectangle(r1.X, r1.Bottom - 1, r1.Width, 1), this.borderColour);
            this.spriteBatch.Draw(this.texture, new Rectangle(r1.Right - 1, r1.Y, 1, r1.Height), this.borderColour);

            this.spriteBatch.End();

            this.IsContentChangedSinceDraw = false;
        }
    }
}
