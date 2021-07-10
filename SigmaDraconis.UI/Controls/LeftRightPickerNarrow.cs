namespace SigmaDraconis.UI
{
    using System.Collections.Generic;
    using Microsoft.Xna.Framework;
    using Draconis.UI;

    public class LeftRightPickerNarrow : LeftRightPicker
    {
        public LeftRightPickerNarrow(IUIElement parent, int x, int y, int width, List<string> options, int defaultOptionIndex)
            : base(parent, x, y, width, Scale(16))
        {
            this.selectedIndex = defaultOptionIndex;
            this.Options = options;
            this.maxOptionIndex = options.Count > 0 ? options.Count - 1 : 0;
            if (this.SelectedIndex >= options.Count) this.SelectedIndex = options.Count - 1;

            this.leftButton = new PickerIconButton(this, 0, 0, "Textures\\Icons\\LeftNarrow");
            this.leftButton.MouseLeftClick += this.OnLeftClick;
            this.AddChild(this.leftButton);

            this.rightButton = new PickerIconButton(this, this.W - Scale(18), 0, "Textures\\Icons\\RightNarrow");
            this.rightButton.MouseLeftClick += this.OnRightClick;
            this.AddChild(this.rightButton);

            this.textLabel = new TextLabel(this, 0, Scale(1), width, Scale(16), options[this.SelectedIndex], Color.LightGray);
            this.AddChild(textLabel);

            this.UpdateButtonIsEnabledValues();
        }

        public override void ApplyLayout()
        {
            foreach (var child in this.Children)
            {
                child.X = this.Rescale(child.X);
                child.Y = child == this.textLabel ? Scale(1) : 0;
                child.ApplyScale();
                child.ApplyLayout();
            }

            this.appliedScale = UIStatics.Scale;
            this.suppressOnParentResize = true;
            this.IsContentChangedSinceDraw = true;
        }
    }
}
