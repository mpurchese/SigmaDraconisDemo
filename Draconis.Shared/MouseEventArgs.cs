namespace Draconis.Shared
{
    using System;
    using Microsoft.Xna.Framework.Input;

    public class MouseEventArgs : EventArgs
    {
        public MouseEventType EventType { get; set; }
        public MouseState LastMouseState { get; set; }
        public MouseState CurrentMouseState { get; set; }
        public bool IsDraggingLeft { get; set; }
        public bool IsDraggingRight { get; set; }
        public bool Handled { get; set; }

        public MouseEventArgs(MouseEventType mouseEventType)
        {
            this.EventType = mouseEventType;
            this.Handled = false;
        }

        public MouseEventArgs(MouseEventType mouseEventType, MouseState lastMouseState, MouseState currentMouseState, bool isDraggingLeft, bool isDraggingRight)
        {
            this.EventType = mouseEventType;
            this.LastMouseState = lastMouseState;
            this.CurrentMouseState = currentMouseState;
            this.IsDraggingLeft = isDraggingLeft;
            this.IsDraggingRight = isDraggingRight;
            this.Handled = false;
        }
    }
}
