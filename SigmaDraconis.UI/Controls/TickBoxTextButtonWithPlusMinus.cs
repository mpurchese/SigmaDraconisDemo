namespace SigmaDraconis.UI
{
    using System;
    using Draconis.UI;
    using Draconis.Shared;
    using Microsoft.Xna.Framework;

    public class TickBoxTextButtonWithPlusMinus : TickBoxTextButton
    {
        private int value;

        protected readonly IconButton plusButton;
        protected readonly IconButton minusButton;

        public event EventHandler<MouseEventArgs> ValueChanged;

        public int Min { get; protected set; }
        public int Max { get; protected set; }
        public int Value
        {
            get
            {
                return this.value;
            }
            set
            {
                this.value = value;
                this.plusButton.IsEnabled = value < this.Max;
                this.minusButton.IsEnabled = value > this.Min;
            }
        }

        public TickBoxTextButtonWithPlusMinus(IUIElement parent, int x, int y, int width, string text, int min, int max, int value)
            : base(parent, x, y, width, Scale(20), text)
        {
            this.textLabel.TextAlign = TextAlignment.MiddleLeft;
            this.textLabel.X = Scale(20);

            this.plusButton = new IconButton(this, this.W - Scale(12), 1, "Textures\\Icons\\ButtonPlus", 1f, true) { OnePixelLessX = true, OnePixelLessY = true } ;
            this.plusButton.MouseLeftClick += this.OnPlusButtonClick;
            this.AddChild(this.plusButton);

            this.minusButton = new IconButton(this, this.W - Scale(12), Scale(10), "Textures\\Icons\\ButtonMinus", 1f, true) { OnePixelLessX = true, OnePixelLessY = true };
            this.minusButton.MouseLeftClick += this.OnMinusButtonClick;
            this.AddChild(this.minusButton);

            this.Min = min;
            this.Max = max;
            this.Value = value;

           // this.BackgroundColour = new Color(0, 0, 0, 64);
        }

        public override void ApplyLayout()
        {
            //this.textLabel.ApplyScale();
            this.textLabel.X = Scale(20);
            this.textLabel.H = this.H;

            this.plusButton.X = this.W - Scale(12);
            this.plusButton.ApplyScale();
            this.plusButton.ApplyLayout();

            this.minusButton.X = this.W - Scale(12);
            this.minusButton.Y = Scale(10);
            this.minusButton.ApplyScale();
            this.minusButton.ApplyLayout();

            this.appliedScale = UIStatics.Scale;
            this.suppressOnParentResize = true;
            this.IsContentChangedSinceDraw = true;
        }

        private void OnPlusButtonClick(object sender, MouseEventArgs e)
        {
            if (this.Value < this.Max) this.Value++;

            this.minusButton.IsEnabled = true;
            if (this.Value >= this.Max) this.plusButton.IsEnabled = false;

            this.ValueChanged?.Invoke(this, e);
        }

        private void OnMinusButtonClick(object sender, MouseEventArgs e)
        {
            if (this.Value > this.Min) this.Value--;

            this.plusButton.IsEnabled = true;
            if (this.Value <= this.Min) this.minusButton.IsEnabled = false;

            this.ValueChanged?.Invoke(this, e);
        }
    }
}
