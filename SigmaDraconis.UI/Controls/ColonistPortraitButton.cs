namespace SigmaDraconis.UI
{
    using System.Linq;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Draconis.Shared;
    using Draconis.UI;
    using Cards.Interface;
    using Shared;
    using World;
    using WorldInterfaces;

    using Managers;

    public class ColonistPortraitButton : IconButton
    {
        private static Color[] colonistBodyColours;
        private static Color[] colonistHairColours;

        private readonly int colonistID;
        private int insetIconId = 0;
        private int backColourIndex = 0;
        private bool isColonistDead;

        protected static Texture2D insetsTexture;
        protected static Texture2D backTexture;

        public ColonistPortraitButton(IUIElement parent, int x, int y, string texturePath, int colonistID)
            : base(parent, x, y, texturePath, 0.5f)
        {
            this.colonistID = colonistID;
            this.BorderColour1 = UIColour.BorderDark;
            this.BorderColour2 = UIColour.BorderDark;
            this.BorderColourMouseOver = UIColour.BorderMedium;
            this.BorderColourSelected = UIColour.BorderLight;
        }

        public override void LoadContent()
        {
            base.LoadContent();

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
            if (insetsTexture == null) insetsTexture = UIStatics.Content.Load<Texture2D>("Textures\\Icons\\ColonistPortraitInsets");

            var colonist = World.GetThing(this.colonistID) as IColonist;
            var bodyColour = colonistBodyColours[colonist?.ColourCode ?? 0];
            var hairColour = colonistHairColours[colonist?.HairColourCode ?? 0];

            var data = new Color[this.texture.Width * this.texture.Height];
            this.texture.GetData(data);
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

            this.texture = new Texture2D(UIStatics.Graphics, this.texture.Width, this.texture.Height); // Clone
            this.texture.SetData(data);
        }

        public override void Update()
        {
            base.Update();

            if (!(World.GetThing(this.colonistID) is IColonist colonist)) return;

            this.isColonistDead = colonist.IsDead;

            var newInsetId = 0;
            if (this.isColonistDead) newInsetId = 6;
            else if (colonist.Cards.Cards.Values.Any(c => c.IsVisible && c.DisplayType == CardDisplayType.Danger)) newInsetId = 3;
            else if (colonist.Cards.Cards.Values.Any(c => c.IsVisible && c.DisplayType == CardDisplayType.Warning)) newInsetId = 1;
            else if (newInsetId == 0 && colonist.Body.IsSleeping) newInsetId = 4;
            else if (newInsetId == 0 && colonist.IsIdle && colonist.StressLevel < StressLevel.High) newInsetId = 5;
            
            if (this.insetIconId != newInsetId)
            {
                this.insetIconId = newInsetId;
                this.IsContentChangedSinceDraw = true;
            }

            var newBackColourIndex = (5 - (colonist.Happiness / 2)).Clamp(0, 9);
            if (this.backColourIndex != newBackColourIndex)
            {
                this.backColourIndex = newBackColourIndex;
                this.IsContentChangedSinceDraw = true;
            }
        }

        protected override void DrawContent()
        {
            Rectangle targetRect = new Rectangle(this.X, this.Y, this.W, this.H);
            Rectangle targetRectInset = new Rectangle(this.X, this.Y + Scale(28), this.W, Scale(20));
            if (this.IsVisible && backTexture != null)
            {
                Matrix projection = Matrix.CreateOrthographicOffCenter(0, UIStatics.Graphics.Viewport.Width, UIStatics.Graphics.Viewport.Height, 0, 0, 1);
                this.effect.Parameters["xViewProjection"].SetValue(projection);

                this.IsEnabled = !this.isColonistDead;
                this.effect.CurrentTechnique = this.effect.Techniques[this.IsEnabled ? "SimpleTechnique" : "MonoTechnique"];

                Rectangle sourceRect = new Rectangle(this.backColourIndex, 0, 1, 96);
                spriteBatch.Begin(effect: this.effect);
                spriteBatch.Draw(backTexture, targetRect, sourceRect, Color.White);
                spriteBatch.End();

                base.DrawContent();
                this.IsEnabled = true;

                if (this.IsVisible && insetsTexture != null && this.insetIconId > 0)
                {
                    // Inset icon
                    var sy = 0;
                    if (UIStatics.Scale == 200) sy = 50;
                    else if (UIStatics.Scale == 150) sy = 20;
                    sourceRect = new Rectangle((this.insetIconId - 1) * Scale(32), sy, Scale(32), Scale(20));
                    spriteBatch.Begin(effect: this.effect);
                    spriteBatch.Draw(insetsTexture, targetRectInset, sourceRect, Color.White);
                    spriteBatch.End();
                }

                this.IsContentChangedSinceDraw = false;
            }
        }

        protected override void OnMouseLeftClick(MouseEventArgs e)
        {
            if (!(World.GetThing(this.colonistID) is IColonist colonist)) return;

            if (this.IsSelected)
            {
                this.IsSelected = false;
                if (PlayerWorldInteractionManager.SelectedThing == colonist)
                {
                    PlayerWorldInteractionManager.SelectedThing = null;
                    GameScreen.Instance.ShowRelevantLeftPanels(null);
                }
            }
            else
            {
                this.IsSelected = true;
                if (PlayerWorldInteractionManager.SelectedThing != colonist)
                {
                    PlayerWorldInteractionManager.SelectedThing = colonist;
                    GameScreen.Instance.ScrollToPosition(colonist.MainTile.CentrePosition);
                    GameScreen.Instance.ShowRelevantLeftPanels(colonist);
                }
            }
        }
    }
}
