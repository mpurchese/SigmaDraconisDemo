namespace Draconis.UI
{
    public interface IButton
    {
        bool IsEnabled { get; set; }
        bool IsMouseOver { get; }
        bool IsSelected { get; set; }
        bool IsVisible { get; set; }
    }
}
