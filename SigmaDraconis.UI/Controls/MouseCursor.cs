namespace SigmaDraconis.UI
{
    using System;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.UI;
    using Shared;

    public class MouseCursor : UIElementBase
    {
        public string[] TextLine = new string[5];
        public Color[] TextLineColour = new Color[5];

        public MouseCursorType MouseCursorType { get; set; } = MouseCursorType.Normal;

        private readonly TextLabel[] labelForeground = new TextLabel[5];
        private readonly TextLabel[] labelBackground = new TextLabel[5];

        public MouseCursor() : base(null, 0, 0, 0, 0)
        {
            if (Instance == null)
            {
                Instance = this;
                this.IsInteractive = false;

                for (int i = 0; i < 5; i++)
                {
                    this.TextLine[i] = "";
                    this.labelForeground[i] = new TextLabel(this, 0, i * 10, "", UIColour.DefaultText);
                    this.labelBackground[i] = new TextLabel(this, 0, i * 10, "", Color.Black);
                    this.AddChild(this.labelBackground[i]);
                    this.AddChild(this.labelForeground[i]);
                }

                this.TextLineColour[2] = UIColour.LightBlueText;
                this.TextLineColour[3] = UIColour.LightBlueText;
            }
            else
            {
                throw new ApplicationException("GameScreen instance already created");
            }
        }

        public override void LoadContent()
        {
            this.texture = UIStatics.Content.Load<Texture2D>("Textures\\Cursors\\Cursors");
            base.LoadContent();
        }

        public static MouseCursor Instance { get; private set; }

        public void Reset()
        {
            this.TextLine[0] = "";
            this.TextLine[1] = "";
            this.TextLine[2] = "";
            this.TextLine[3] = "";
            this.TextLine[4] = "";
        }

        protected override void DrawContent()
        {
            var mx = UIStatics.CurrentMouseState.X;
            var my = UIStatics.CurrentMouseState.Y;

            var ty = 0;
            if (UIStatics.Scale == 200) ty = 75;
            else if (UIStatics.Scale == 150) ty = 30;

            this.spriteBatch.Begin();

            // Attached resource icon
            var isOverGameScreen = GameScreen.Instance.IsMouseOverNotChildren;
            
            if (this.MouseCursorType == MouseCursorType.Drag)
            {
                this.spriteBatch.Draw(this.texture, new Vector2(mx - Scale(14), my - Scale(14)), new Rectangle(Scale(28), ty, Scale(28), Scale(28)), Color.White);
            }
            else if (this.MouseCursorType == MouseCursorType.Geology && isOverGameScreen)
            {
                this.spriteBatch.Draw(this.texture, new Vector2(mx - Scale(10), my - Scale(10)), new Rectangle(Scale(56), ty, Scale(28), Scale(30)), Color.White);
            }
            else if (this.MouseCursorType == MouseCursorType.Hammer && isOverGameScreen)
            {
                this.spriteBatch.Draw(this.texture, new Vector2(mx - Scale(10), my + Scale(10)), new Rectangle(Scale(84), ty, Scale(28), Scale(28)), Color.White);
            }
            else if (this.MouseCursorType == MouseCursorType.Recycle && isOverGameScreen)
            {
                this.spriteBatch.Draw(this.texture, new Vector2(mx - Scale(11), my - Scale(13)), new Rectangle(Scale(116), ty, Scale(22), Scale(22)), Color.White);
            }
            else if (this.MouseCursorType == MouseCursorType.Harvest && isOverGameScreen)
            {
                this.spriteBatch.Draw(this.texture, new Vector2(mx - Scale(11), my - Scale(13)), new Rectangle(Scale(138), ty, Scale(22), Scale(30)), Color.White);
            }
            else if (this.MouseCursorType == MouseCursorType.Farm && isOverGameScreen)
            {
                this.spriteBatch.Draw(this.texture, new Vector2(mx - Scale(11), my - Scale(13)), new Rectangle(Scale(160), ty, Scale(22), Scale(30)), Color.White);
            }
            else
            {
                this.spriteBatch.Draw(this.texture, new Vector2(mx - Scale(6), my), new Rectangle(0, ty, Scale(28), Scale(28)), Color.White);
            }

            this.spriteBatch.End();

            if (!string.IsNullOrEmpty(this.TextLine[0]) || !string.IsNullOrEmpty(this.TextLine[1]))
            {
                this.UpdateLabels();

                for (int i = 0; i < 5; i++)
                {
                    if (!string.IsNullOrEmpty(this.TextLine[i]))
                    {
                        this.labelBackground[i].Draw();
                        this.labelForeground[i].Draw();
                    }
                }
            }
            else
            {
                for (int i = 0; i < 5; i++)
                {
                    this.labelForeground[i].IsVisible = false;
                    this.labelBackground[i].IsVisible = false;
                }
            }

            this.IsContentChangedSinceDraw = false;
        }

        private void UpdateLabels()
        {
            var mx = UIStatics.CurrentMouseState.X;
            var my = UIStatics.CurrentMouseState.Y;
            var labelOffsetX = 0;
            var labelOffsetY = 0;
            if (MouseCursorType == MouseCursorType.Recycle)
            {
                labelOffsetX = Scale(8);
            }
            else if (MouseCursorType == MouseCursorType.Harvest)
            {
                labelOffsetX = Scale(8);
                labelOffsetY = Scale(-9);
            }
            else if (this.MouseCursorType == MouseCursorType.Normal)
            {
                labelOffsetX = Scale(24) - (this.TextLine[0].Length * Scale(4));
                labelOffsetY = Scale(19);
            }
            else if (this.MouseCursorType == MouseCursorType.Hammer)
            {
                labelOffsetX = UIStatics.Scale == 100 ? 19 : Scale(16);
                labelOffsetY = UIStatics.Scale == 200 ? 24 : Scale(10);
            }
            else if (this.MouseCursorType == MouseCursorType.Geology)
            {
                labelOffsetX = Scale(14);
                labelOffsetY = -Scale(2);
            }
            else if (this.MouseCursorType == MouseCursorType.Farm)
            {
                labelOffsetX = Scale(10);
                labelOffsetY = -Scale(10);
            }
            else if (this.MouseCursorType == MouseCursorType.Drag)
            {
                labelOffsetX = Scale(4);
                labelOffsetY = Scale(4);
            }

            var x = mx + labelOffsetX;
            var y = my + labelOffsetY;
            for (int i = 0; i < 5; i++)
            {
                if (!string.IsNullOrEmpty(this.TextLine[i]))
                {
                    this.labelForeground[i].Text = this.TextLine[i];
                    this.labelForeground[i].X = x;
                    this.labelForeground[i].Y = y;
                    this.labelForeground[i].Colour = this.TextLineColour[i];
                    this.labelForeground[i].IsVisible = true;
                    this.labelBackground[i].Text = this.TextLine[i];
                    this.labelBackground[i].X = x + 1;
                    this.labelBackground[i].Y = y + 1;
                    this.labelBackground[i].IsVisible = true;
                }
                else
                {
                    this.labelForeground[i].IsVisible = false;
                    this.labelBackground[i].IsVisible = false;
                }

                y += Scale(12);
            }
        }
    }
}
