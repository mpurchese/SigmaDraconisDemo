namespace SigmaDraconis.UI
{
    using System;

    public interface IThingPanelWithFollowButton : IThingPanel
    {
        event EventHandler<EventArgs> FollowButtonClick;
    }
}
