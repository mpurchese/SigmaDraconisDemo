namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.UI;
    using Shared;
    
    public class InventorySlot : UIElementBase
    {
        private Effect effect;
        private readonly TextLabel label;
        private int prevCount;
        private int flashTimer;

        private ItemType itemType;
        private int itemCount;

        public ItemType ItemType { get { return this.itemType; } set { if (this.itemType != value) { this.itemType = value; this.IsContentChangedSinceDraw = true; } } }
        public int ItemCount { get { return this.itemCount; } set { if (this.itemCount != value) { this.itemCount = value; this.IsContentChangedSinceDraw = true; } } }

        public InventorySlot(IUIElement parent, int x, int y, ItemType itemType)
            : base(parent, x, y, Scale(36), Scale(36))
        {
            this.IsInteractive = true;

            this.ItemType = itemType;
            this.label = new TextLabel(this, 0, Scale(20), Scale(36), Scale(18), "0", UIColour.DefaultText, hasShadow: true);
            this.AddChild(this.label);
        }

        public override void LoadContent()
        {
            this.texture = UIStatics.Content.Load<Texture2D>("Textures\\Icons\\InventorySlots");

            this.effect = UIStatics.Content.Load<Effect>("Effects\\SimpleEffect").Clone();
            this.effect.Parameters["xTexture"].SetValue(this.texture);
            this.effect.CurrentTechnique = this.effect.Techniques["SimpleTechnique"];

            base.LoadContent();
        }

        public override void Update()
        {
            if (this.ItemCount > this.prevCount)
            {
                this.flashTimer = 15;
                this.label.Colour = UIColour.GreenText;
            }
            else if (this.ItemCount < this.prevCount)
            {
                this.flashTimer = 15;
                this.label.Colour = UIColour.RedText;
            }
            else if (this.flashTimer > 0)
            {
                this.flashTimer--;
                if (this.flashTimer == 0) this.label.Colour = UIColour.DefaultText;
            }

            if (this.ItemType != ItemType.None && this.ItemCount > 0) this.label.Text = this.ItemCount.ToString();
            else this.label.Text = "";

            this.prevCount = this.ItemCount;
            base.Update();
        }

        protected override void DrawContent()
        {
            Matrix projection = Matrix.CreateOrthographicOffCenter(0, UIStatics.Graphics.Viewport.Width, UIStatics.Graphics.Viewport.Height, 0, 0, 1);
            this.effect.Parameters["xViewProjection"].SetValue(projection);

            var size = Scale(36);
            var sy = 0;
            if (UIStatics.Scale == 200) sy = 90;
            else if (UIStatics.Scale == 150) sy = 36;
            var rDest = new Rectangle(this.ScreenX, this.ScreenY, size, size);
            var rSource = new Rectangle(this.ItemCount > 0 ? (int)this.ItemType * size : 0, sy, size, size);

            this.spriteBatch.Begin(effect: this.effect);
            this.spriteBatch.Draw(this.texture, rDest, rSource, Color.White);
            this.spriteBatch.End();

            this.IsContentChangedSinceDraw = false;
        }
    }
}
