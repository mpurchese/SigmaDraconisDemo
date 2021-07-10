namespace Draconis.UI
{
    using System;
    using System.Collections.Generic;
    using Shared;

    public interface IUIElement : IMouseHandler, IDisposable
    {
        int Id { get; }
        int X { get; set; }
        int Y { get; set; }
        int W { get; set; }
        int H { get; set; }
        int ScreenX { get; }
        int ScreenY { get; }
        int RenderX { get; }
        int RenderY { get; }
        bool IsInteractive { get; set; }
        bool IsVisible { get; set; }
        bool IsVisibleIncludeParents { get; }
        bool IsMouseOver { get; set; }
        bool IsContentChangedSinceDraw { get; }

        IUIElement Parent { get; }
        IReadOnlyList<IUIElement> Children { get; }

        event EventHandler<MouseEventArgs> MouseMove;
        event EventHandler<MouseEventArgs> MouseLeftButtonHold;
        event EventHandler<MouseEventArgs> MouseLeftClick;
        event EventHandler<MouseEventArgs> MouseRightClick;
        event EventHandler<MouseEventArgs> MouseLeftDrag;
        event EventHandler<MouseEventArgs> MouseRightDrag;
        event EventHandler<MouseEventArgs> MouseLeftDragRelease;
        event EventHandler<MouseEventArgs> MouseRightDragRelease;
        event EventHandler<MouseEventArgs> MouseScrollUp;
        event EventHandler<MouseEventArgs> MouseScrollDown;

        T AddChild<T>(T child) where T : IUIElement;
        void RemoveChild(IUIElement child);
        void LoadContent();
        void Update();
        void Draw();
        void Invalidate();
        void ApplyScale();
        void ApplyLayout();
    }
}
