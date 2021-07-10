namespace SigmaDraconis.UI
{
    using Draconis.UI;
    using Microsoft.Xna.Framework;
    using System;

    public class CommentaryHistoryPopup : PanelBottom
    {
        private readonly CommentListContainer commentListContainer;

        public event EventHandler<EventArgs> ArchiveButtonClick;

        public CommentaryHistoryPopup(IUIElement parent) : base(parent, Scale(480), Scale(120), "")
        {
            this.backgroundColour = new Color(0, 0, 0, 64);

            this.commentListContainer = new CommentListContainer(this, Scale(8), Scale(2), this.W - Scale(16), this.H - Scale(8), 15, 3, true);
            this.AddChild(this.commentListContainer);
            this.commentListContainer.ArchiveButtonClick += this.OnArchiveButtonClick;
        }

        public void ResetScrollPosition()
        {
            this.commentListContainer.ResetScrollPosition();
        }

        public override void Update()
        {
            this.commentListContainer.Update();
            base.Update();
        }

        protected override void DrawBaseLayer()
        {
            // Override prevents title bar from being drawn
        }

        private void OnArchiveButtonClick(object sender, EventArgs e)
        {
            this.ArchiveButtonClick?.Invoke(this, e);
        }
    }
}
