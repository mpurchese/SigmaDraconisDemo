namespace SigmaDraconis.UI
{
    using Draconis.UI;
        
    public interface IPowerButton : IKeyboardHandler, IUIElement
    {
        bool IsOn { get; set; }
    }
}
