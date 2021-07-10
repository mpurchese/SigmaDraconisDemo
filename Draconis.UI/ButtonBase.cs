namespace Draconis.UI
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Input;
    using Draconis.Shared;

    public abstract class ButtonBase : UIElementBase, IKeyboardHandler
    {
        protected bool isSelected;
        protected bool isHighlighted;
        protected bool isEnabled = true;
        protected bool isMouseOver;

        public bool IsSelected { get { return this.isSelected; } set { if (this.isSelected != value) { this.isSelected = value; this.IsContentChangedSinceDraw = true; } } }
        public bool IsHighlighted { get { return this.isHighlighted; } set { if (this.isHighlighted != value) { this.isHighlighted = value; this.IsContentChangedSinceDraw = true; } } }
        public virtual bool IsEnabled { get { return this.isEnabled; } set { if (this.isEnabled != value) { this.isEnabled = value; this.IsContentChangedSinceDraw = true; } } }

        public string Tag { get; set; }

        public Color BorderColour1 { get; set; } = new Color(92, 92, 92, 255);
        public Color BorderColour2 { get; set; } = new Color(64, 64, 64, 255);
        public virtual Color BorderColourSelected { get; set; } = new Color(0, 255, 255, 255);
        public Color BorderColourMouseOver { get; set; } = new Color(128, 128, 128, 255);

        public ButtonBase(IUIElement parent, int x, int y, int width, int height)
            : base(parent, x, y, width, height)
        {
            this.IsInteractive = true;
        }

        public override void Update()
        {
            if (this.isMouseOver != this.IsMouseOver)
            {
                this.IsContentChangedSinceDraw = true;
                this.isMouseOver = this.IsMouseOver;
            }

            base.Update();
        }

        public virtual void HandleKeyPress(Keys key)
        {
        }

        public virtual void HandleKeyHold(Keys key)
        {
        }

        public virtual void HandleKeyRelease(Keys key)
        {
            if (key == Keys.Enter) this.OnMouseLeftClick(new MouseEventArgs(MouseEventType.LeftClick));
        }
    }
}
