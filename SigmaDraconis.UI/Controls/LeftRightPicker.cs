namespace SigmaDraconis.UI
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Draconis.Shared;
    using Draconis.UI;

    public class LeftRightPicker : UIElementBase
    {
        protected int selectedIndex;
        protected TextLabel textLabel;
        protected PickerIconButton leftButton;
        protected PickerIconButton rightButton;
        protected bool isEnabled = true;
        protected int? maxOptionIndex;

        public List<string> Options { get; protected set; }
        public List<object> Tags { get; set; }
        public int? MaxOptionIndex
        {
            get { return this.maxOptionIndex; }
            set
            {
                if (value != this.maxOptionIndex)
                {
                    this.maxOptionIndex = value;
                    this.UpdateButtonIsEnabledValues();
                }
            }
        }

        public object SelectedTag
        {
            get
            {
                return this.Tags.Count > this.selectedIndex ? this.Tags[this.selectedIndex] : null;
            }
            set
            {
                if (this.Tags.Contains(value)) this.SelectedIndex = this.Tags.IndexOf(value);
            }
        }

        public int SelectedIndex
        {
            get
            {
                return this.selectedIndex;
            }
            set
            {
                if (this.selectedIndex != value)
                {
                    this.selectedIndex = value;
                    this.textLabel.Text = this.Options[value];
                    this.UpdateButtonIsEnabledValues();
                }
            }
        }

        public string SelectedText { get { return this.textLabel.Text; } }

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
            this.leftButton.IsEnabled = this.isEnabled && (this.Loop || this.selectedIndex > 0);
            this.rightButton.IsEnabled = this.isEnabled && (this.Loop || (this.selectedIndex < this.Options.Count - 1 && this.selectedIndex < this.maxOptionIndex));
        }

        public bool Loop { get; private set; }

        public event EventHandler<EventArgs> SelectedIndexChanged;

        protected LeftRightPicker(IUIElement parent, int x, int y, int width, int height)
                : base(parent, x, y, width, height)
        {
        }

        public LeftRightPicker(TextRenderer textRenderer, IUIElement parent, int x, int y, int width, List<string> options, int defaultOptionIndex, bool loop = false)
            : base(parent, x, y, width, Scale(18))
        {
            this.selectedIndex = defaultOptionIndex;
            this.Options = options;
            this.maxOptionIndex = options.Count > 0 ? options.Count - 1 : 0;
            if (this.SelectedIndex >= options.Count) this.SelectedIndex = options.Count - 1;

            this.leftButton = new PickerIconButton(this, 0, 0, "Textures\\Icons\\Left");
            this.leftButton.MouseLeftClick += this.OnLeftClick;
            this.AddChild(this.leftButton);

            this.rightButton = new PickerIconButton(this, this.W - Scale(18), 0, "Textures\\Icons\\Right");
            this.rightButton.MouseLeftClick += this.OnRightClick;
            this.AddChild(this.rightButton);

            this.textLabel = new TextLabel(textRenderer, this, 0, Scale(2), width, Scale(16), options[this.SelectedIndex], UIColour.DefaultText);
            this.AddChild(textLabel);

            this.Loop = loop;

            this.UpdateButtonIsEnabledValues();
        }

        public LeftRightPicker(IUIElement parent, int x, int y, int width, List<string> options, int defaultOptionIndex, bool loop = false)
            : base(parent, x, y, width, Scale(18))
        {
            this.selectedIndex = defaultOptionIndex;
            this.Options = options;
            this.maxOptionIndex = options.Count > 0 ? options.Count - 1 : 0;
            if (this.SelectedIndex >= options.Count) this.SelectedIndex = options.Count - 1;

            this.leftButton = new PickerIconButton(this, 0, 0, "Textures\\Icons\\Left");
            this.leftButton.MouseLeftClick += this.OnLeftClick;
            this.AddChild(this.leftButton);

            this.rightButton = new PickerIconButton(this, this.W - Scale(18), 0, "Textures\\Icons\\Right");
            this.rightButton.MouseLeftClick += this.OnRightClick;
            this.AddChild(this.rightButton);

            this.textLabel = new TextLabel(this, 0, Scale(2), width, Scale(16), options[this.SelectedIndex], UIColour.DefaultText);
            this.AddChild(textLabel);

            this.Loop = loop;

            this.UpdateButtonIsEnabledValues();
        }

        public override void LoadContent()
        {
            this.texture = new Texture2D(UIStatics.Graphics, 1, 1);
            this.texture.SetData(new Color[1] { Color.White });

            base.LoadContent();
        }

        public void UpdateOptions(List<string> options, int defaultOptionIndex)
        {
            this.Options = options;
            this.SelectedIndex = defaultOptionIndex;
            this.MaxOptionIndex = options.Count > 0 ? options.Count - 1 : 0;
            if (this.SelectedIndex >= options.Count) this.SelectedIndex = options.Count - 1;
            this.textLabel.Text = options[this.SelectedIndex];

            this.UpdateButtonIsEnabledValues();
        }

        protected void OnLeftClick(object sender, MouseEventArgs e)
        {
            if (!this.IsEnabled || !this.leftButton.IsEnabled) return;

            if (this.SelectedIndex > 0)
            {
                this.SelectedIndex--;
                this.SelectedIndexChanged?.Invoke(this, null);
                this.textLabel.Text = this.Options[this.SelectedIndex];

                this.UpdateButtonIsEnabledValues();
            }
            else if (this.Loop)
            {
                this.SelectedIndex = this.Options.Count - 1;
                this.SelectedIndexChanged?.Invoke(this, null);
                this.textLabel.Text = this.Options[this.SelectedIndex];
            }
        }

        protected void OnRightClick(object sender, MouseEventArgs e)
        {
            if (!this.IsEnabled || !this.rightButton.IsEnabled) return;

            if (this.SelectedIndex < this.Options.Count - 1)
            {
                this.SelectedIndex++;
                this.SelectedIndexChanged?.Invoke(this, null);
                this.textLabel.Text = this.Options[this.SelectedIndex];

                this.UpdateButtonIsEnabledValues();
            }
            else if (this.Loop)
            {
                this.SelectedIndex = 0;
                this.SelectedIndexChanged?.Invoke(this, null);
                this.textLabel.Text = this.Options[this.SelectedIndex];
            }
        }

        protected override void DrawContent()
        {
            Rectangle r = new Rectangle(this.RenderX, this.RenderY, this.W, this.H);
            var borderColour = new Color(92, 92, 92);

            spriteBatch.Begin();

            // Background
            spriteBatch.Draw(this.texture, r, new Color(0, 0, 0, 128));

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
