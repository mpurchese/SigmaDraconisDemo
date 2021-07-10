namespace Draconis.UI
{
    /// <summary>
    /// An empty element, typiclly used for tooltip hit testing
    /// </summary>
    public class EmptyElement : UIElementBase
    {
        public EmptyElement(IUIElement parent, int x, int y, int w, int h) : base(parent, x, y, w, h)
        {
            this.IsInteractive = true;
        }
    }
}
