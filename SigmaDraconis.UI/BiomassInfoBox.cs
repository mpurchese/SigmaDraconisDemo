namespace SigmaDraconis.UI
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Shared;
    using World.Managers;
    using World;

    public class BiomassInfoBox : TexturedElement
    {
        private SimpleTextLabel titleLabel;
        private List<BorderedTextLabel> column1Labels = new List<BorderedTextLabel>();
        private List<BorderedTextLabel> column2Labels = new List<BorderedTextLabel>();

        public BiomassInfoBox(GraphicsDevice graphicsDevice, UIElement parent, int x, int y, int width, int height)
            : base(graphicsDevice, parent, x, y, width, height)
        {
            this.titleLabel = new SimpleTextLabel(this, 0, 4, this.Width, 18, "BIOMASS PRODUCTION", Color.Green);
            this.Children.Add(this.titleLabel);

            for (int i = 0; i < 3; ++i)
            {
                var column1Label = new BorderedTextLabel(graphicsDevice, this, 8, 22 + (i * 18), this.Width - 98, 19, "", new Color(160, 160, 160), TextAlignment.TopLeft)
                {
                    IsBackgroundVisible = true,
                    IsBorderVisible = true
                };

                if (i == 2)
                {
                    column1Label.Colour = new Color(200, 200, 200);
                }

                this.column1Labels.Add(column1Label);
                this.Children.Add(column1Label);

                var column2Label = new BorderedTextLabel(graphicsDevice, this, this.Width - 91, 22 + (i * 18), 82, 19, "", new Color(160, 160, 160), TextAlignment.TopRight)
                {
                    IsBackgroundVisible = true,
                    IsBorderVisible = true
                };

                this.column2Labels.Add(column2Label);
                this.Children.Add(column2Label);
            }
        }

        public override void Update()
        {
            //var row = 0;
            //var total = ResourceManager.Instance.BiomassProductionHourlyAmounts.Sum(x => x.Value);
            //var totalStr = total < 100 ? $"{total:N1} kg/h" : $"{total:N0} kg/h";
            //foreach (var pair in ResourceManager.Instance.BiomassProductionHourlyAmounts.OrderByDescending(i => i.Value).Where(i => i.Value != 0))
            //{
            //    var definition = ThingTypeRegister.Definitions[pair.Key];
            //    var amountStr = pair.Value < 100 ? $"{pair.Value:N1} kg/h" : $"{pair.Value:N0} kg/h";
            //    var count = ResourceManager.Instance.BiomassProductionHourlyCounts[pair.Key];

            //    this.column1Labels[row].Text = definition.Name + (count > 1 ? $" x{count}" : "");
            //    this.column2Labels[row].Text = pair.Value > 0 ? $"+{amountStr}" : amountStr;
            //    this.column2Labels[row].Colour = pair.Value > 0 ? Color.Green : Color.Red;

            //    row++;
            //    if (row >= this.column1Labels.Count - 1)
            //    {
            //        break;
            //    }
            //}

            //while (row < this.column1Labels.Count - 1)
            //{
            //    this.column1Labels[row].Text = "";
            //    this.column2Labels[row].Text = "";
            //    row++;
            //}

            //this.column1Labels[row].Text = "Total";
            //this.column2Labels[row].Text = total > 0 ? $"+{totalStr}" : totalStr;
            //this.column2Labels[row].Colour = total > 0 ? new Color(0, 160, 0): new Color(255, 32, 0);

            base.Update();
        }

        protected override void GenerateTexture()
        {
            this.texture = new Texture2D(this.graphicsDevice, 1, 1);
            Color[] color = new Color[1] { Color.White };
            this.texture.SetData(color);

            base.GenerateTexture();
        }

        public override void Draw()
        {
            if (this.IsVisible && this.texture != null)
            {
                int parentX = Parent == null ? 0 : Parent.ScreenX;
                int parentY = Parent == null ? 0 : Parent.ScreenY;
                Rectangle r = new Rectangle(rectangle.X + parentX, rectangle.Y + parentY, this.Width, this.Height);
                var borderColour = new Color(64, 64, 64, 255);

                spriteBatch.Begin();

                // Background
                spriteBatch.Draw(this.texture, r, new Color(0, 0, 0, 128));

                // Borders
                spriteBatch.Draw(this.texture, new Rectangle(r.X, r.Y, r.Width, 1), borderColour);
                spriteBatch.Draw(this.texture, new Rectangle(r.X, r.Y, 1, r.Height), borderColour);
                spriteBatch.Draw(this.texture, new Rectangle(r.X, r.Bottom - 1, r.Width, 1), borderColour);
                spriteBatch.Draw(this.texture, new Rectangle(r.Right - 1, r.Y, 1, r.Height), borderColour);

                spriteBatch.End();

                foreach (var child in this.Children)
                {
                    child.Draw();
                }
            }
        }
    }
}
