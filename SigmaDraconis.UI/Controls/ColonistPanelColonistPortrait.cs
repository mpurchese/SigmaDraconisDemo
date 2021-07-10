namespace SigmaDraconis.UI
{
    using System.Linq;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Draconis.UI;
    using WorldInterfaces;

    public class ColonistPanelColonistPortrait : UIElementBase
    {
        private static Color[] colonistBodyColours;
        private static Color[] colonistHairColours;

        private IColonist colonist;

        protected Effect effect;
        protected Texture2D rawColonistTexture;
        protected Texture2D backTexture;
        protected Texture2D borderTexture;

        public Color BorderColour { get; set; } = new Color(64, 64, 64, 255);

        public ColonistPanelColonistPortrait(IUIElement parent, int x, int y, IColonist colonist)
            : base(parent, x, y, Scale(32), Scale(48))
        {
            this.colonist = colonist;
        }

        public override void LoadContent()
        {
            base.LoadContent();

            this.rawColonistTexture = UIStatics.Content.Load<Texture2D>("Textures\\Misc\\ColonistPortrait");

            this.effect = UIStatics.Content.Load<Effect>("Effects\\SimpleEffect").Clone();

            this.borderTexture = new Texture2D(UIStatics.Graphics, 1, 1);
            Color[] color = new Color[1] { Color.White };
            this.borderTexture.SetData(color);

            if (colonistBodyColours == null)
            {
                var colonistBodyColoursTex = UIStatics.Content.Load<Texture2D>("Textures\\Colonists\\ColonistColours");
                var colonistHairColoursTex = UIStatics.Content.Load<Texture2D>("Textures\\Colonists\\ColonistHairColours");
                colonistBodyColours = new Color[colonistBodyColoursTex.Width * colonistBodyColoursTex.Height];
                colonistHairColours = new Color[colonistHairColoursTex.Width * colonistHairColoursTex.Height];
                colonistBodyColoursTex.GetData(colonistBodyColours);
                colonistHairColoursTex.GetData(colonistHairColours);
            }

            if (backTexture == null) backTexture = UIStatics.Content.Load<Texture2D>("Textures\\Misc\\ColonistPortraitBack");

            this.UpdateTextureData();

            this.isContentInvalidated = false;
            this.isContentLoaded = true;
        }

        public void SetColonist(IColonist colonist)
        { 
            this.colonist = colonist;
            if (!this.isContentLoaded) return;

            this.UpdateTextureData();

            this.isContentInvalidated = false;
        }

        protected override void DrawContent()
        {
            Rectangle targetRect = new Rectangle(this.RenderX, this.RenderY, this.W, this.H);
            if (this.IsVisible && backTexture != null && this.colonist != null)
            {
                Matrix projection = Matrix.CreateOrthographicOffCenter(0, UIStatics.Graphics.Viewport.Width, UIStatics.Graphics.Viewport.Height, 0, 0, 1);
                this.effect.Parameters["xViewProjection"].SetValue(projection);

                this.effect.CurrentTechnique = this.effect.Techniques[this.colonist.IsDead ? "MonoTechnique" : "SimpleTechnique"];

                Rectangle sourceRect = new Rectangle(0, 0, 1, 96);
                spriteBatch.Begin(effect: this.effect);
                spriteBatch.Draw(backTexture, targetRect, sourceRect, Color.White);
                spriteBatch.End();

                // Border
                spriteBatch.Begin();
                spriteBatch.Draw(this.borderTexture, new Rectangle(targetRect.X, targetRect.Y, targetRect.Width, 1), this.BorderColour);
                spriteBatch.Draw(this.borderTexture, new Rectangle(targetRect.X, targetRect.Y, 1, targetRect.Height), this.BorderColour);
                spriteBatch.Draw(this.borderTexture, new Rectangle(targetRect.X, targetRect.Bottom - 1, targetRect.Width, 1), this.BorderColour);
                spriteBatch.Draw(this.borderTexture, new Rectangle(targetRect.Right - 1, targetRect.Y, 1, targetRect.Height), this.BorderColour);
                spriteBatch.End();

                base.DrawContent();
            }
        }

        private void UpdateTextureData()
        {
            var bodyColour = colonistBodyColours[this.colonist?.ColourCode ?? 0];
            var hairColour = colonistHairColours[this.colonist?.HairColourCode ?? 0];

            var data = new Color[this.rawColonistTexture.Width * this.rawColonistTexture.Height];
            this.rawColonistTexture.GetData(data);
            for (int i = 0; i < data.Count(); i++)
            {
                if (data[i].A > 96)
                {
                    var r = data[i].R / 255f;
                    var g = data[i].G / 255f;
                    var b = data[i].B / 255f;
                    data[i] = new Color(
                        (g * 1.6f * bodyColour.R / 255) + (r * 1.6f * hairColour.R / 255) + (b * 0.28f),
                        (g * 1.6f * bodyColour.G / 255) + (r * 1.6f * hairColour.G / 255) + (b * 0.22f),
                        (g * 1.6f * bodyColour.B / 255) + (r * 1.6f * hairColour.B / 255) + (b * 0.20f),
                        (data[i].A / 255f));
                }
            }

            if (this.texture != null) this.texture.Dispose();
            this.texture = new Texture2D(UIStatics.Graphics, this.rawColonistTexture.Width, this.rawColonistTexture.Height);
            this.texture.SetData(data);
            this.effect.Parameters["xTexture"].SetValue(this.texture);
        }
    }
}
