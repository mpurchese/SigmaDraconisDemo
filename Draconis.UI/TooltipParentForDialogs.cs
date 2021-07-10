namespace Draconis.UI
{
    /// <summary>
    /// Used for attaching tooltips to, so they always appear on top.  Singleton.
    /// </summary>
    public class TooltipParentForDialogs : UIElementBase
    {
        public static TooltipParentForDialogs Instance { get; private set; }

        public TooltipParentForDialogs(IUIElement parent)
            : base(parent, 0, 0, parent.W, parent.H)
        {
            if (Instance == null)
            {
                Instance = this;
                this.IsInteractive = false;
            }
            else
            {
                throw new System.ApplicationException("TooltipParentForDialogs instance already created");
            }
        }
    }
}
