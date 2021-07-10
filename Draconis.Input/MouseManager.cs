namespace Draconis.Input
{ 
    using System;
    using System.Linq;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Input;
    using Microsoft.Xna.Framework.Graphics;

    using Shared;
    using UI;

    public static class MouseManager
    {
        public static GraphicsDevice GraphicsDevice;
        public static GameWindow Window;

        public static MouseState PreviousState;
        public static MouseState CurrentState;
        public static int DeltaX = 0;
        public static int DeltaY = 0;
        public static bool HasMoved;
        public static bool IsLeftDown;
        public static bool IsRightDown;
        public static bool PrevIsLeftDown;
        public static bool PrevIsRightDown;
        public static bool IsInSafeArea;
        public static bool IsDraggingLeft;
        public static bool IsDraggingRight;
        public static bool IsHoldingLeft;
        public static bool IsHoldingRight;

        private static DateTime leftHoldStartTime;

        public static IUIElement CurrentScreen;
        public static IMouseHandler CurrentMouseOverElement = null;
        public static bool IsWindowActive;

        public static void Update()
        {
            PreviousState = CurrentState;
            CurrentState = Mouse.GetState();

            UIStatics.PreviousMouseState = PreviousState;
            UIStatics.CurrentMouseState = CurrentState;

            var rectSafeArea = GraphicsDevice.Viewport.TitleSafeArea;
            if (IsInSafeArea && IsWindowActive && CurrentState.RightButton == ButtonState.Pressed)
            {
                // Don't allow to leave safe area with right button pressed
                SetPosition(
                    Math.Min(Math.Max(CurrentState.X, rectSafeArea.Left), rectSafeArea.Right),
                    Math.Min(Math.Max(CurrentState.Y, rectSafeArea.Top), rectSafeArea.Bottom));
            }

            IsInSafeArea = CurrentState.X >= rectSafeArea.Left && CurrentState.X <= rectSafeArea.Right
                && CurrentState.Y >= rectSafeArea.Top && CurrentState.Y <= rectSafeArea.Bottom;

            if (!IsWindowActive || !IsInSafeArea)
            {
                PreviousState = CurrentState;
                DeltaX = 0;
                DeltaY = 0;
                HasMoved = false;
                CurrentMouseOverElement = null;
                IsDraggingLeft = false;
                IsDraggingRight = false;
                return;
            }

            DeltaX = CurrentState.X - PreviousState.X;
            DeltaY = CurrentState.Y - PreviousState.Y;
            HasMoved = DeltaX != 0 || DeltaY != 0;
            IsLeftDown = CurrentState.LeftButton == ButtonState.Pressed;
            IsRightDown = CurrentState.RightButton == ButtonState.Pressed;
            PrevIsLeftDown = PreviousState.LeftButton == ButtonState.Pressed;
            PrevIsRightDown = PreviousState.RightButton == ButtonState.Pressed;

            if ((!IsLeftDown && !IsRightDown) || Draconis.Input.MouseManager.CurrentMouseOverElement == null)
            {
                CurrentMouseOverElement = GetCurrentUIElement();
            }

            var target = CurrentMouseOverElement;

            // Send events to UI element
            if (CurrentMouseOverElement != null)
            {
                //UIElement.CurrentMouseTarget = CurrentUIElement;  // Makes this one render on top

                if (HasMoved && IsLeftDown && target.IsDraggable)
                {
                    IsDraggingLeft = true;
                    target.HandleMouseEvent(new MouseEventArgs(MouseEventType.LeftDrag, PreviousState, CurrentState, IsDraggingLeft, IsDraggingRight));
                    CurrentState = Mouse.GetState();
                }

                if (HasMoved && IsRightDown && target.IsRightDraggable)
                {
                    IsDraggingRight = true;
                    target.HandleMouseEvent(new MouseEventArgs(MouseEventType.RightDrag, PreviousState, CurrentState, IsDraggingLeft, IsDraggingRight));
                }

                if (HasMoved && !IsLeftDown && !IsRightDown)
                {
                    target.HandleMouseEvent(new MouseEventArgs(MouseEventType.Move, PreviousState, CurrentState, IsDraggingLeft, IsDraggingRight));
                }

                if (PrevIsLeftDown && !IsLeftDown && !IsDraggingLeft)
                {
                    target.HandleMouseEvent(new MouseEventArgs(MouseEventType.LeftClick, PreviousState, CurrentState, IsDraggingLeft, IsDraggingRight));
                }

                if (PrevIsRightDown && !IsRightDown && !IsDraggingRight)
                {
                    target.HandleMouseEvent(new MouseEventArgs(MouseEventType.RightClick, PreviousState, CurrentState, IsDraggingLeft, IsDraggingRight));
                }

                if (IsLeftDown && !PrevIsLeftDown)
                {
                    target.HandleMouseEvent(new MouseEventArgs(MouseEventType.LeftDown, PreviousState, CurrentState, IsDraggingLeft, IsDraggingRight));
                }

                if (IsRightDown && !PrevIsRightDown)
                {
                    target.HandleMouseEvent(new MouseEventArgs(MouseEventType.RightDown, PreviousState, CurrentState, IsDraggingLeft, IsDraggingRight));
                }

                if (IsDraggingLeft && !IsLeftDown)
                {
                    IsDraggingLeft = false;
                    target.HandleMouseEvent(new MouseEventArgs(MouseEventType.LeftDragRelease, PreviousState, CurrentState, IsDraggingLeft, IsDraggingRight));
                }

                if (IsDraggingRight && !IsRightDown)
                {
                    IsDraggingRight = false;
                    target.HandleMouseEvent(new MouseEventArgs(MouseEventType.RightDragRelease, PreviousState, CurrentState, IsDraggingLeft, IsDraggingRight));
                }

                if (!IsLeftDown) IsHoldingLeft = false;
                else if (IsLeftDown && !PrevIsLeftDown) leftHoldStartTime = DateTime.UtcNow;

                // Mouse holding - left
                if (IsLeftDown && !IsHoldingLeft && PrevIsLeftDown && leftHoldStartTime.AddMilliseconds(500) < DateTime.UtcNow)
                {
                    IsHoldingLeft = true;
                    leftHoldStartTime = DateTime.UtcNow;
                    target.HandleMouseEvent(new MouseEventArgs(MouseEventType.LeftButtonHold, PreviousState, CurrentState, IsDraggingLeft, IsDraggingRight));
                }
                else if (IsLeftDown && IsHoldingLeft && leftHoldStartTime.AddMilliseconds(100) < DateTime.UtcNow)
                {
                    leftHoldStartTime = DateTime.UtcNow;
                    target.HandleMouseEvent(new MouseEventArgs(MouseEventType.LeftButtonHold, PreviousState, CurrentState, IsDraggingLeft, IsDraggingRight));
                }

                if (CurrentState.ScrollWheelValue > PreviousState.ScrollWheelValue)
                {
                    target.HandleMouseEvent(new MouseEventArgs(MouseEventType.ScrollUp, PreviousState, CurrentState, IsDraggingLeft, IsDraggingRight));
                }
                else if (CurrentState.ScrollWheelValue < PreviousState.ScrollWheelValue)
                {
                    target.HandleMouseEvent(new MouseEventArgs(MouseEventType.ScrollDown, PreviousState, CurrentState, IsDraggingLeft, IsDraggingRight));
                }
            }
        }

        private static void SetPosition(int x, int y)
        {
            if (x != CurrentState.X || y != CurrentState.Y)
            {
                Mouse.SetPosition(x, y);
                CurrentState = Mouse.GetState();
            }
        }

        private static IUIElement GetCurrentUIElement()
        {
            var current = CurrentScreen;
            var openNodes = CurrentScreen.Children.ToList();
            var closedNodes = openNodes.Select(c => c.Id);

            // Find the last child that is interactive and has mouse over
            while (openNodes.Any())
            {
                var copy = openNodes.ToList();
                openNodes.Clear();
                foreach (var node in copy.Where(n => n.IsVisible && n.IsMouseOver && !(n is TooltipParent)))
                {
                    if (node.IsInteractive) current = node;
                    openNodes.AddRange(node.Children);
                }
            }

            return current;
        }
    }
}
