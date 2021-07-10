namespace SigmaDraconis.UI
{
    using System.Collections.Generic;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.UI;
    using Shared;

    public class FlowDiagramItemBox : FlowDiagramBoxBase
    {
        private ItemType itemType = ItemType.None;
        private int quantity = 0;
        private int iconIndex = 0;

        private readonly static Dictionary<ItemType, int> IconIndexMap = new Dictionary<ItemType, int>
        {
            { ItemType.Biomass, 0 },
            { ItemType.Coal, 1 },
            { ItemType.Mush, 2 },
            { ItemType.LiquidFuel, 3 },
            { ItemType.Metal, 4 },
            { ItemType.IronOre, 5 },
            { ItemType.Stone, 6 },
            { ItemType.BatteryCells, 7 },
            { ItemType.Compost, 8 },
            { ItemType.SolarCells, 9 },
            { ItemType.Glass, 10 },
            { ItemType.Composites, 11 }
        };

        public ItemType ItemType
        {
            get => this.itemType;
            set 
            {
                if (this.itemType != value)
                { 
                    this.itemType = value;
                    this.IsContentChangedSinceDraw = true;
                    if (value != ItemType.None && IconIndexMap.TryGetValue(value, out var i)) this.iconIndex = i;
                    else this.iconIndex = 0;
                    this.UpdateLabel();
                }
            }
        }

        public int Quantity
        {
            get => this.quantity;
            set
            {
                if (this.quantity != value)
                {
                    this.quantity = value;
                    this.UpdateLabel();
                    this.IsContentChangedSinceDraw = true;
                }
            }
        }

        public FlowDiagramItemBox(IUIElement parent, ItemType itemType, int quantity) : base(parent, 0, 0, Scale(46) + 2)
        {
            this.label = new TextLabel(this, Scale(30), 0, Scale(18), this.H, "", UIColour.DefaultText, TextAlignment.MiddleCentre, true) { IsVisible = false };
            this.AddChild(this.label);

            this.quantity = quantity;
            this.itemType = itemType;
            if (itemType != ItemType.None && IconIndexMap.TryGetValue(itemType, out var i)) this.iconIndex = i;

            this.UpdateLabel();
        }

        public override void LoadContent()
        {
            this.texture = UIStatics.Content.Load<Texture2D>("Textures\\Icons\\FlowDiagramItems");
            base.LoadContent();
        }

        public override void ApplyScale()
        {
            this.W = Scale(46) + 2;
            this.H = Scale(16) + 3;
            this.IsContentChangedSinceDraw = true;
        }

        public override void ApplyLayout()
        {
            this.label.X = Scale(30);
            this.label.W = Scale(18);
            this.label.H = this.H;
            this.appliedScale = UIStatics.Scale;
            this.suppressOnParentResize = true;
            this.IsContentChangedSinceDraw = true;
        }

        protected override void DrawIcon()
        {
            if (this.itemType == ItemType.None) return;

            var rSource = this.GetTextureSourceRect();
            var rDest = new Rectangle(this.RenderX + Scale(2), this.RenderY, rSource.Value.Width, rSource.Value.Height);
            spriteBatch.Draw(this.texture, rDest, rSource, Color.White);
        }

        private void UpdateLabel()
        {
            if (this.itemType == ItemType.None)
            {
                this.label.IsVisible = false;
                return;
            }

            this.label.IsVisible = true;
            this.label.Text = this.quantity.ToString();
            this.IsContentChangedSinceDraw = true;
        }

        private Rectangle? GetTextureSourceRect()
        {
            var x = 56 * this.iconIndex;
            if (UIStatics.Scale == 200) return new Rectangle(x, 46, 56, 35);
            if (UIStatics.Scale == 150) return new Rectangle(x, 19, 43, 27);
            return new Rectangle(x, 0, 30, 19);
        }
    }
}
