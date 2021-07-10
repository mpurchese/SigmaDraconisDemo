namespace Draconis.UI
{
    using Shared;

    public interface IMouseHandler
    {
        bool IsDraggable { get; set; }
        bool IsRightDraggable { get; }
        void HandleMouseEvent(MouseEventArgs e);
    }
}
