namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.UI;
    using Cards.Interface;
    
    public class ColonistPanelCard : UIElementBase
    {
        private readonly ICard card;

        public int DisplayOrder => this.card.DisplayOrder;
        public CardDisplayType DisplayType => this.card.DisplayType;

        public ColonistPanelCard(IUIElement parent, int x, int y, ICard card)
            : base(parent, x, y, Scale(22), Scale(28))
        {
            this.card = card;
        }

        public string GetDescription(string colonistName)
        {
            return this.card.GetDescription(colonistName);
        }

        public string GetTitle()
        {
            return this.card.GetTitle();
        }

        public override void LoadContent()
        {
            this.texture = UIStatics.Content.Load<Texture2D>(this.card.GetTexturePath());
            base.LoadContent();
        }

        protected override void DrawContent()
        {
            if (this.texture != null)
            {
                var r = new Rectangle(this.RenderX, this.RenderY, this.W, this.H);
                var rSource = this.GetTextureSourceRect();
                this.spriteBatch.Begin();
                this.spriteBatch.Draw(this.texture, r, rSource, Color.White);
                this.spriteBatch.End();
            }

            this.IsContentChangedSinceDraw = false;
        }

        protected virtual Rectangle? GetTextureSourceRect()
        {
            if (UIStatics.Scale == 200) return new Rectangle(this.W * 5 / 4, 0, this.W, this.H);
            if (UIStatics.Scale == 150) return new Rectangle(this.W * 2 / 3, 0, this.W, this.H);
            return new Rectangle(0, 0, this.W, this.H);
        }
    }
}
