namespace SigmaDraconis.UI
{
    using System;
    using Draconis.Shared;
    using Draconis.UI;

    public class LandingPodPanel : DeconstructableThingPanel, IThingPanelWithFollowButton
    {
        private readonly CameraTrackingIconButton followButton;

        public event EventHandler<EventArgs> FollowButtonClick;

        public LandingPodPanel(IUIElement parent, int y) : base(parent, y)
        {
            this.followButton = new CameraTrackingIconButton(this, 0, 0);
            this.followButton.MouseLeftClick += this.OnFollowButtonClick;
            this.AddChild(this.followButton);
        }

        public override void Update()
        {
            if (this.followButton.IsSelected != (GameScreen.Instance.CameraTrackingThing == this.thing))
            {
                this.followButton.IsSelected = !this.followButton.IsSelected;
                this.IsContentChangedSinceDraw = true;
            }

            base.Update();
        }

        private void OnFollowButtonClick(object sender, MouseEventArgs e)
        {
            this.followButton.IsSelected = !this.followButton.IsSelected;
            this.IsContentChangedSinceDraw = true;
            this.FollowButtonClick?.Invoke(this, null);
        }
    }
}
