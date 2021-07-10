namespace SigmaDraconis.UI
{
    using Draconis.Shared;
    using Draconis.UI;
    using Microsoft.Xna.Framework;
    using System;

    internal class CheckListItem : UIElementBase
    {
        private readonly TextButton button;
        private readonly Icon icon;
        private bool isChecked;
        private bool isNew;
        private static Color defaultBackgroundColour = new Color(0, 0, 0, 100);
        private static Color newBackgroundColour = new Color(0, 56, 56, 56);

        public bool IsChecked { get { return this.isChecked; } set { if (this.isChecked != value) { this.isChecked = value; this.IsContentChangedSinceDraw = true; } } }
        public bool IsSelected { get { return this.button.IsSelected; } set { if (this.button.IsSelected != value) { this.button.IsSelected = value; } } }
        public bool IsNew { get { return this.isNew; } set { if (this.isNew != value) { this.isNew = value; this.button.BackgroundColour = value ? newBackgroundColour : defaultBackgroundColour; this.button.Invalidate(); } } }
        public int ItemId { get; }
        public string Text { get { return this.button.Text; } set { this.button.Text = value; } }

        public event EventHandler<MouseEventArgs> Click;

        public CheckListItem(IUIElement parent, int x, int y, int width, int height, string text, int id)
            : base(parent, x, y, width, height)
        {
            this.ItemId = id;

            this.button = new TextButton(this, Scale(22), 0, this.W - Scale(22), this.H, text);
            this.AddChild(this.button);
            this.button.MouseLeftClick += this.OnButtonClick;
            this.button.MouseScrollDown += this.OnButtonScrollDown;
            this.button.MouseScrollUp += this.OnButtonScrollUp;
            this.button.BorderColourSelected = new Color(0, 128, 256, 256);

            this.icon = new Icon("Textures\\Icons\\TickBox2", 2);
        }

        private void OnButtonClick(object sender, MouseEventArgs e)
        {
            this.Click?.Invoke(this, e);
        }

        private void OnButtonScrollDown(object sender, MouseEventArgs e)
        {
            this.OnMouseScrollDown(e);
        }

        private void OnButtonScrollUp(object sender, MouseEventArgs e)
        {
            this.OnMouseScrollUp(e);
        }

        public override void LoadContent()
        {
            this.icon.LoadContent();
            base.LoadContent();
        }

        protected override void DrawContent()
        {
            this.spriteBatch.Begin();
            this.icon.Draw(this.spriteBatch, this.RenderX, this.RenderY, this.isChecked ? 1 : 0);
            this.spriteBatch.End();
            this.IsContentChangedSinceDraw = false;
        }
    }
}
