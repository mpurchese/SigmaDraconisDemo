namespace SigmaDraconis.UI
{
    using System;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Draconis.Shared;
    using Draconis.UI;

    public class NumberPicker : UIElementBase
    {
        protected TextLabel textLabel;
        protected TextButton minus5Button;
        protected TextButton minus1Button;
        protected TextButton plus1Button;
        protected TextButton plus5Button;
        protected bool isEnabled = true;
        protected int max;
        protected int min;
        protected int currentValue;

        public int Value
        {
            get
            {
                return this.currentValue;
            }
            set
            {
                if (this.currentValue == value) return;
                this.currentValue = value;
                this.textLabel.Text = value.ToString();
                this.UpdateButtonIsEnabledValues();
            }
        }

        public int Min
        {
            get
            {
                return this.min;
            }
            set
            {
                if (this.min == value) return;
                this.min = value;
                if (this.min > this.currentValue)
                {
                    this.currentValue = this.min;
                    this.textLabel.Text = this.currentValue.ToString();
                }

                this.UpdateButtonIsEnabledValues();
            }
        }

        public int Max
        {
            get
            {
                return this.max;
            }
            set
            {
                if (this.max == value) return;
                this.max = value;
                if (this.max < this.currentValue)
                {
                    this.currentValue = this.min;
                    this.textLabel.Text = this.currentValue.ToString();
                }

                this.UpdateButtonIsEnabledValues();
            }
        }

        public bool IsEnabled
        {
            get { return this.isEnabled; }
            set
            {
                this.isEnabled = value;
                this.textLabel.Colour = value ? UIColour.DefaultText : UIColour.DarkGrayText;
                this.UpdateButtonIsEnabledValues();
            }
        }

        protected void UpdateButtonIsEnabledValues()
        {
            this.minus5Button.IsEnabled = this.isEnabled && this.currentValue - 1 >= this.min;
            this.minus1Button.IsEnabled = this.isEnabled && this.currentValue - 1 >= this.min;
            this.plus1Button.IsEnabled = this.isEnabled && this.currentValue + 1 <= this.max;
            this.plus5Button.IsEnabled = this.isEnabled && this.currentValue + 1 <= this.max;
        }

        public event EventHandler<EventArgs> ValueChanged;

        protected NumberPicker(IUIElement parent, int x, int y, int width, int height)
                : base(parent, x, y, width, height)
        {
        }

        public NumberPicker(IUIElement parent, int x, int y, int width, int min, int max, int initialValue = 0)
            : base(parent, x, y, width, Scale(18))
        {
            this.min = min;
            this.max = max;
            this.currentValue = initialValue;

            this.minus5Button = new TextButton(this, 0, 0, Scale(18), Scale(18), "-5") { TextColour = UIColour.LightGrayText };
            this.minus5Button.MouseLeftClick += this.OnMinus5Click;
            this.minus5Button.MouseLeftButtonHold += this.OnMinus5Click;
            this.AddChild(this.minus5Button);

            this.minus1Button = new TextButton(this, Scale(18), 0, Scale(18), Scale(18), "-1") { TextColour = UIColour.LightGrayText };
            this.minus1Button.MouseLeftClick += this.OnMinus1Click;
            this.minus1Button.MouseLeftButtonHold += this.OnMinus1Click;
            this.AddChild(this.minus1Button);

            this.textLabel = new TextLabel(this, Scale(36), Scale(2), width - Scale(72), Scale(16), initialValue.ToString(), UIColour.DefaultText);
            this.AddChild(textLabel);

            this.plus1Button = new TextButton(this, width - Scale(36), 0, Scale(18), Scale(18), "+1") { TextColour = UIColour.LightGrayText };
            this.plus1Button.MouseLeftClick += this.OnPlus1Click;
            this.plus1Button.MouseLeftButtonHold += this.OnPlus1Click;
            this.AddChild(this.plus1Button);

            this.plus5Button = new TextButton(this, width - Scale(18), 0, Scale(18), Scale(18), "+5") { TextColour = UIColour.LightGrayText };
            this.plus5Button.MouseLeftClick += this.OnPlus5Click;
            this.plus5Button.MouseLeftButtonHold += this.OnPlus5Click;
            this.AddChild(this.plus5Button);

            this.UpdateButtonIsEnabledValues();
        }

        public override void LoadContent()
        {
            this.texture = new Texture2D(UIStatics.Graphics, 1, 1);
            this.texture.SetData(new Color[1] { Color.White });

            base.LoadContent();
        }

        protected void OnMinus5Click(object sender, MouseEventArgs e)
        {
            if (!this.IsEnabled || !this.minus5Button.IsEnabled) return;

            this.currentValue -= 5;
            if (this.currentValue < this.min) this.currentValue = this.min;

            this.textLabel.Text = this.currentValue.ToString();

            this.UpdateButtonIsEnabledValues();
            this.ValueChanged?.Invoke(this, new EventArgs());
        }

        protected void OnMinus1Click(object sender, MouseEventArgs e)
        {
            if (!this.IsEnabled || !this.minus1Button.IsEnabled) return;

            this.currentValue--;
            if (this.currentValue < this.min) this.currentValue = this.min;

            this.textLabel.Text = this.currentValue.ToString();

            this.UpdateButtonIsEnabledValues();
            this.ValueChanged?.Invoke(this, new EventArgs());
        }

        protected void OnPlus1Click(object sender, MouseEventArgs e)
        {
            if (!this.IsEnabled || !this.plus1Button.IsEnabled) return;

            this.currentValue++;
            if (this.currentValue > this.max) this.currentValue = this.max;

            this.textLabel.Text = this.currentValue.ToString();

            this.UpdateButtonIsEnabledValues();
            this.ValueChanged?.Invoke(this, new EventArgs());
        }

        protected void OnPlus5Click(object sender, MouseEventArgs e)
        {
            if (!this.IsEnabled || !this.plus5Button.IsEnabled) return;

            this.currentValue += 5;
            if (this.currentValue > this.max) this.currentValue = this.max;

            this.textLabel.Text = this.currentValue.ToString();

            this.UpdateButtonIsEnabledValues();
            this.ValueChanged?.Invoke(this, new EventArgs());
        }

        protected override void DrawContent()
        {
            Rectangle r = new Rectangle(this.RenderX, this.RenderY, this.W, this.H);
            var borderColour = new Color(64, 64, 64);

            spriteBatch.Begin();

            // Background
            spriteBatch.Draw(this.texture, r, new Color(0, 0, 0, 100));

            // Borders
            spriteBatch.Draw(this.texture, new Rectangle(r.X, r.Y, r.Width, 1), borderColour);
            spriteBatch.Draw(this.texture, new Rectangle(r.X, r.Y, 1, r.Height), borderColour);
            spriteBatch.Draw(this.texture, new Rectangle(r.X, r.Bottom - 1, r.Width, 1), borderColour);
            spriteBatch.Draw(this.texture, new Rectangle(r.Right - 1, r.Y, 1, r.Height), borderColour);

            spriteBatch.End();

            this.IsContentChangedSinceDraw = false;
        }
    }
}
