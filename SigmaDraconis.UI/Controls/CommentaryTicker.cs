namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.UI;
    using Commentary;
    using Shared;

    public class CommentaryTicker : UIElementBase
    {
        private readonly TextLabel textLabel1;
        private readonly TextLabel textLabel2;
        private string prevText = "";
        private string nextText = "";
        private int transitionPixelOffset;
        private bool isFadingOut;
        private float alpha = 1f;
        private Color nameColour;
        private Color textColour;
        private Texture2D smileyTexture;
        private int smileyIndex = -1;

        public CommentaryTicker(IUIElement parent) : base(parent, 0, 0, parent.W, parent.H)
        {
            this.textLabel1 = new TextLabel(this, 0, 0, parent.W, parent.H, "", UIColour.DefaultText);
            this.textLabel2 = new TextLabel(this, 0, 0, parent.W, parent.H, "", UIColour.DefaultText);
            this.AddChild(this.textLabel1);
            this.AddChild(this.textLabel2);
        }

        public override void LoadContent()
        {
            this.smileyTexture = UIStatics.Content.Load<Texture2D>("Textures\\Icons\\CommentSmileys");
            base.LoadContent();
        }

        public override void Update()
        {
            if (this.transitionPixelOffset != 0)
            {
                this.transitionPixelOffset -= 2;
                if (this.transitionPixelOffset < -16) this.transitionPixelOffset = 16;
            }

            if (this.isFadingOut)
            {
                this.alpha -= 0.0083f;
                if (this.alpha < 0)
                {
                    this.textLabel1.Text = "";
                    this.textLabel2.Text = "";
                    this.alpha = 1f;
                    this.isFadingOut = false;
                    this.transitionPixelOffset = 16;
                    this.smileyIndex = -1;
                }
                else
                {
                    this.textLabel1.Colour = this.nameColour * alpha;
                    this.textLabel2.Colour = this.textColour * alpha;
                }
            }

            var comment = CommentaryController.GetLatest();
            this.nextText = comment?.Text ?? "";

            if (this.prevText == "" && this.nextText == "") return;

            this.textLabel1.X = Scale(4);
            this.textLabel1.Y = Scale(6 + this.transitionPixelOffset) - 2;
            this.textLabel1.W = this.Parent.W - Scale(8);
            this.textLabel2.X = Scale(4);
            this.textLabel2.Y = Scale(6 + this.transitionPixelOffset) - 2;
            this.textLabel2.W = this.Parent.W - Scale(8);

            if (this.nextText != this.prevText && !this.isFadingOut && this.transitionPixelOffset >= 0)
            {
                if (this.transitionPixelOffset == 0)
                {
                    if (this.nextText == "") this.isFadingOut = true;
                    else this.transitionPixelOffset = this.prevText == "" ? 16 : -2;
                }
                else
                {
                    this.prevText = this.nextText;
                    if (this.nextText == "")
                    {
                        this.textLabel1.Text = "";
                        this.textLabel2.Text = "";
                        this.smileyIndex = -1;
                    }
                    else
                    {
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

                        var length = comment.ColonistName.Length + 2 + text.Length;
                        this.textLabel1.Text = $"{comment.ColonistName}: ".PadRight(length);
                        this.nameColour = GetNameColour(comment.ColonistSkillType);
                        this.textLabel1.Colour = this.nameColour;
                        this.textLabel2.Text = text.PadLeft(length);
                        this.textColour = GetTextColour(comment.IsImportant, comment.IsUrgent);
                        this.textLabel2.Colour = this.textColour;
                    }
                }
            }
        }

        protected override void DrawContent()
        {
            base.DrawContent();

            if (this.smileyIndex >= 0 && this.smileyTexture != null)
            {
                var x = this.textLabel2.X + (this.textLabel2.W / 2) + ((this.textLabel2.Text.Length * (7 * UIStatics.Scale / 100)) / 2);
                var rDest = new Rectangle(this.RenderX + x, this.RenderY + Scale(this.transitionPixelOffset) + (this.H / 2) - Scale(6), Scale(12), Scale(12));
                var sx = this.smileyIndex * Scale(12);
                var sy = this.appliedScale < 200 ? (this.appliedScale == 100 ? 42 : 24) : 0;
                var rSource = new Rectangle(sx, sy, Scale(12), Scale(12));
                this.spriteBatch.Begin();
                this.spriteBatch.Draw(this.smileyTexture, rDest, rSource, Color.White * this.alpha);
                this.spriteBatch.End();
            }
        }

        private static Color GetNameColour(SkillType skillType)
        {
            switch (skillType)
            {
                case SkillType.Engineer: return new Color(100, 180, 255);
                case SkillType.Botanist: return new Color(120, 255, 120);
                case SkillType.Geologist: return new Color(255, 200, 150);
                case SkillType.Programmer: return new Color(200, 200, 200);
            }

            return UIColour.DefaultText;
        }

        private static Color GetTextColour(bool isImportant, bool isUrgent)
        {
            if (isUrgent) return UIColour.RedText;
            if (isImportant) return UIColour.YellowText;
            return UIColour.DefaultText;
        }
    }
}
