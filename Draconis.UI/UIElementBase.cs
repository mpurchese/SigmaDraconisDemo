namespace Draconis.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Shared;

    public abstract class UIElementBase : IUIElement
    {
        private static int nextId = 1;

        private int x;
        private int y;
        private int w;
        private int h;
        private bool isVisible = true;
        protected bool isContentInvalidated = true;
        private bool isMouseOver;
        protected bool isContentLoaded;
        protected bool suppressOnParentResize;
        protected int appliedScale;
        protected bool isApplyingScale;
        protected int currentLanguageId = 0;

        // Used for monitoring for parent resizing
        private int parentWidth;
        private int parentHeight;

        private readonly List<IUIElement> children;

        protected Texture2D texture;
        protected SpriteBatch spriteBatch;

        public bool IsContentChangedSinceDraw { get; protected set; }

        public virtual int X { get { return this.x; } set { if (this.x != value) { this.x = value; this.IsContentChangedSinceDraw = true; } } }
        public virtual int Y { get { return this.y; } set { if (this.y != value) { this.y = value; this.IsContentChangedSinceDraw = true; } } }
        public virtual int W { get { return this.w; } set { if (this.w != value) { this.w = value; this.IsContentChangedSinceDraw = true; } } }
        public virtual int H { get { return this.h; } set { if (this.h != value) { this.h = value; this.IsContentChangedSinceDraw = true; } } }
        public int Right => this.x + this.w;
        public int Bottom => this.y + this.h;
        public int ScreenX => this.x + (Parent?.ScreenX ?? 0);
        public int ScreenY => this.y + (Parent?.ScreenY ?? 0);
        public virtual int RenderX => this.x + (Parent?.RenderX ?? 0);
        public virtual int RenderY => this.y + (Parent?.RenderY ?? 0);
        public bool IsVisible { get { return this.isVisible; } set { if (this.isVisible != value) { this.isVisible = value; if (value) this.OnShown(); } } }
        public bool IsVisibleIncludeParents => this.IsVisible && this.Parent.IsVisible;
        public bool IsDraggable { get; set; }
        public bool IsInteractive { get; set; }

        public bool AnchorLeft { get; set; } = true;
        public bool AnchorRight { get; set; }
        public bool AnchorTop { get; set; } = true;
        public bool AnchorBottom { get; set; }
        public bool IsRightDraggable { get; protected set; }

        public HorizontalAlignment HorizontalAlignment { get; protected set; }
        public VerticalAlignment VerticalAlignment { get; protected set; }
        public int AlignmentOffsetX { get; protected set; }
        public int AlignmentOffsetY { get; protected set; }

        public IUIElement Parent { get; private set; }
        public IReadOnlyList<IUIElement> Children => this.children.AsReadOnly();

        public int Id { get; }

        public event EventHandler<MouseEventArgs> MouseMove;
        public event EventHandler<MouseEventArgs> MouseLeftButtonHold;
        public event EventHandler<MouseEventArgs> MouseLeftClick;
        public event EventHandler<MouseEventArgs> MouseRightClick;
        public event EventHandler<MouseEventArgs> MouseLeftDrag;
        public event EventHandler<MouseEventArgs> MouseRightDrag;
        public event EventHandler<MouseEventArgs> MouseLeftDragRelease;
        public event EventHandler<MouseEventArgs> MouseRightDragRelease;
        public event EventHandler<MouseEventArgs> MouseScrollUp;
        public event EventHandler<MouseEventArgs> MouseScrollDown;

        public bool IsMouseOver
        {
            get { return this.isMouseOver; }
            set
            {
                if (this.isMouseOver != value)
                {
                    this.isMouseOver = value;
                    if (value) this.OnMouseEnter();
                    else this.OnMouseLeave();
                }
            }
        }

        public bool IsMouseOverNotChildren
        {
            get
            {
                return this.isMouseOver && this.children.All(c => !c.IsMouseOver || !c.IsInteractive || !c.IsVisible);
            }
        }

        public UIElementBase(IUIElement parent, int x, int y, int w, int h)
        {
            this.Id = ++nextId;

            this.spriteBatch = new SpriteBatch(UIStatics.Graphics);

            this.Parent = parent;
            this.parentHeight = parent?.H ?? 0;
            this.parentWidth = parent?.W ?? 0;

            this.x = x;
            this.y = y;
            this.w = w;
            this.h = h;
            this.appliedScale = UIStatics.Scale;
            this.children = new List<IUIElement>();
        }

        public UIElementBase(IUIElement parent, int w, int h, HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment, int offsetX, int offsetY)
        {
            this.Id = ++nextId;

            this.spriteBatch = new SpriteBatch(UIStatics.Graphics);

            this.Parent = parent;
            this.parentHeight = parent?.H ?? 0;
            this.parentWidth = parent?.W ?? 0;

            this.HorizontalAlignment = horizontalAlignment;
            this.VerticalAlignment = verticalAlignment;
            this.AlignmentOffsetX = offsetX;
            this.AlignmentOffsetY = offsetY;
            this.w = w;
            this.h = h;
            this.appliedScale = UIStatics.Scale;
            this.currentLanguageId = UIStatics.CurrentLanguageId;

            this.UpdateHorizontalPosition(0);
            this.UpdateVerticalPosition(0);

            this.children = new List<IUIElement>();
        }

        public virtual void UpdateHorizontalPosition(int prevW = 0)
        {
            switch (this.HorizontalAlignment)
            {
                case HorizontalAlignment.Left:
                    this.x = this.AlignmentOffsetX;
                    break;
                case HorizontalAlignment.Centre:
                    this.x = AlignmentOffsetX + (((this.Parent?.W ?? 0) - this.w) / 2);
                    break;
                case HorizontalAlignment.Right:
                    this.x = this.AlignmentOffsetX + (this.Parent?.W ?? 0) - this.w;
                    break;
                default:
                    // No alignment, use anchoring system
                    if (prevW != (this.Parent?.W ?? 0))
                    {
                        if (this.AnchorRight)
                        {
                            if (this.AnchorLeft) this.W += (this.Parent?.W ?? 0) - this.parentWidth;  // Left AND right, so stretch
                            else this.X += (this.Parent?.W ?? 0) - this.parentWidth;
                        }

                        this.parentWidth = this.Parent?.W ?? 0;
                    }
                    break;
            }
        }

        public virtual void UpdateVerticalPosition(int prevH = 0)
        {
            switch (this.VerticalAlignment)
            {
                case VerticalAlignment.Top:
                    this.y = this.AlignmentOffsetY;
                    break;
                case VerticalAlignment.OneThird:
                    this.y = this.AlignmentOffsetY + (((this.Parent?.H ?? 0) - this.h) / 3);
                    break;
                case VerticalAlignment.Middle:
                    this.y = this.AlignmentOffsetY + (((this.Parent?.H ?? 0) - this.h) / 2);
                    break;
                case VerticalAlignment.TwoThirds:
                    this.y = this.AlignmentOffsetY + (((this.Parent?.H ?? 0) - this.h) * 2 / 3);
                    break;
                case VerticalAlignment.Bottom:
                    this.y = this.AlignmentOffsetY + (this.Parent?.H ?? 0) - this.h;
                    break;
                default:
                    // No alignment, use anchoring system
                    if (prevH != (this.Parent?.H ?? 0))
                    {
                        if (this.AnchorBottom)
                        {
                            if (this.AnchorTop) this.H += (this.Parent?.H ?? 0) - this.parentHeight; // Top AND bottom, so stretch
                            else this.Y += (this.Parent?.H ?? 0) - this.parentHeight;
                        }

                        this.parentHeight = this.Parent?.H ?? 0;
                    }
                    break;
            }
        }

        public virtual void Draw()
        {
            if (!this.isVisible) return;

            if (!this.isContentLoaded) this.LoadContent();
            else if (this.isContentInvalidated || this.texture?.IsDisposed == true) this.ReloadContent();

            this.DrawContent();
            this.IsContentChangedSinceDraw = false;

            this.DrawChildren();
        }

        public virtual T AddChild<T>(T child) where T : IUIElement
        {
            this.children.Add(child);
            return child;
        }

        public T AddBefore<T>(T child, IUIElement e) where T: IUIElement
        {
            var i = this.children.IndexOf(e);
            this.children.Insert(Math.Max(0, i), child);
            return child;
        }

        public virtual void RemoveChild(IUIElement child)
        {
            this.children.Remove(child);
        }

        protected virtual void DrawContent()
        {
            if (this.texture != null)
            {
                var r = new Rectangle(this.x + (Parent?.RenderX ?? 0), this.y + (Parent?.RenderY ?? 0), this.W, this.H);
                this.spriteBatch.Begin();
                this.spriteBatch.Draw(this.texture, r, Color.White);
                this.spriteBatch.End();
            }

            this.IsContentChangedSinceDraw = false;
        }

        protected virtual void DrawChildren()
        {
            foreach (IUIElement child in this.Children) child.Draw();
        }

        public virtual void LoadContent()
        {
            foreach (IUIElement child in this.Children) child.LoadContent();
            this.isContentInvalidated = false;
            this.isContentLoaded = true;
        }

        public virtual void Update()
        {
            if (!this.isVisible)
            {
                this.isMouseOver = false;
                return;
            }
            
            if (this.currentLanguageId != UIStatics.CurrentLanguageId)
            {
                this.currentLanguageId = UIStatics.CurrentLanguageId;
                this.HandleLanguageChange();
            }

            // Parent resized?
            if (this.parentWidth != (this.Parent?.W ?? 0) || this.parentHeight != (this.Parent?.H ?? 0))
            {
                if (!this.suppressOnParentResize) this.OnParentResized(this.parentWidth, this.parentHeight);
                this.parentWidth = this.Parent?.W ?? 0;
                this.parentHeight = this.Parent?.H ?? 0;
            }

            this.suppressOnParentResize = false;
            this.IsMouseOver = this.CheckIsMouseOver();

            foreach (IUIElement child in this.Children.ToList()) child.Update();
        }

        public virtual void ApplyScale()
        {
            this.isApplyingScale = true;
            this.W = this.Rescale(this.W);
            this.H = this.Rescale(this.H);
            this.isApplyingScale = false;
            this.IsContentChangedSinceDraw = true;
        }

        public virtual void ApplyLayout()
        {
            foreach (var child in this.children)
            { 
                child.X = this.Rescale(child.X);
                child.Y = this.Rescale(child.Y);
                child.ApplyScale();
                child.ApplyLayout();
            }

            this.appliedScale = UIStatics.Scale;
            this.suppressOnParentResize = true;
            this.IsContentChangedSinceDraw = true;
        }

        protected virtual bool CheckIsMouseOver()
        {
            var mouseState = UIStatics.CurrentMouseState;
            return this.IsVisible
                && mouseState.X >= this.ScreenX && mouseState.X <= this.ScreenX + this.w && mouseState.Y >= this.ScreenY && mouseState.Y <= this.ScreenY + this.h
                && (ModalBackgroundBox.Instance?.IsInteractive != true || this is Dialog || this.IsDescendantOfDialog());
        }

        protected virtual void OnParentResized(int prevW, int prevH)
        {
            this.UpdateHorizontalPosition(prevW);
            this.UpdateVerticalPosition(prevH);
        }

        public virtual void Invalidate()
        {
            this.IsContentChangedSinceDraw = true;
        }

        protected virtual void OnShown()
        {
            this.IsContentChangedSinceDraw = true;
        }

        protected virtual void ReloadContent()
        {
            this.isContentInvalidated = false;
        }

        protected void InvalidateTexture()
        {
            this.isContentInvalidated = true;
        }

        protected virtual void OnMouseMove(MouseEventArgs e)
        {
            this.MouseMove?.Invoke(this, e);
        }

        protected virtual void OnMouseLeftClick(MouseEventArgs e)
        {
            this.MouseLeftClick?.Invoke(this, e);
        }

        protected virtual void OnMouseRightClick(MouseEventArgs e)
        {
            this.MouseRightClick?.Invoke(this, e);
        }

        protected virtual void OnMouseLeftButtonHold(MouseEventArgs e)
        {
            this.MouseLeftButtonHold?.Invoke(this, e);
        }

        protected virtual void OnMouseLeftDrag(MouseEventArgs e)
        {
            this.MouseLeftDrag?.Invoke(this, e);
        }

        protected virtual void OnMouseRightDrag(MouseEventArgs e)
        {
            this.MouseRightDrag?.Invoke(this, e);
        }

        protected virtual void OnMouseLeftDragRelease(MouseEventArgs e)
        {
            this.MouseLeftDragRelease?.Invoke(this, e);
        }

        protected virtual void OnMouseRightDragRelease(MouseEventArgs e)
        {
            this.MouseRightDragRelease?.Invoke(this, e);
        }

        protected virtual void OnMouseScrollUp(MouseEventArgs e)
        {
            this.MouseScrollUp?.Invoke(this, e);
        }

        protected virtual void OnMouseScrollDown(MouseEventArgs e)
        {
            this.MouseScrollDown?.Invoke(this, e);
        }

        protected virtual void OnMouseEnter()
        {
        }

        protected virtual void OnMouseLeave()
        {
        }

        public void TriggerClickEvent()
        {
            this.OnMouseLeftClick(new MouseEventArgs(MouseEventType.LeftButtonUp));
        }

        public void HandleMouseEvent(MouseEventArgs e)
        {
            if (!this.IsInteractive || (ModalBackgroundBox.Instance?.IsInteractive == true && !this.IsDescendantOfDialog())) return;

            switch (e.EventType)
            {
                case MouseEventType.Move:
                    this.OnMouseMove(e);
                    break;
                case MouseEventType.LeftDrag:
                    this.OnMouseLeftDrag(e);
                    break;
                case MouseEventType.RightDrag:
                    this.OnMouseRightDrag(e);
                    break;
                case MouseEventType.LeftDragRelease:
                    this.OnMouseLeftDragRelease(e);
                    break;
                case MouseEventType.RightDragRelease:
                    this.OnMouseRightDragRelease(e);
                    break;
                case MouseEventType.LeftClick:
                    this.OnMouseLeftClick(e);
                    break;
                case MouseEventType.RightClick:
                    this.OnMouseRightClick(e);
                    break;
                case MouseEventType.ScrollUp:
                    this.OnMouseScrollUp(e);
                    break;
                case MouseEventType.ScrollDown:
                    this.OnMouseScrollDown(e);
                    break;
                case MouseEventType.LeftButtonHold:
                    this.OnMouseLeftButtonHold(e);
                    break;
            }
        }

        public bool IsDescendantOfDialog()
        {
            var e = this as IUIElement;
            while (e.Parent != null)
            {
                e = e.Parent;
                if (e is Dialog) return true;
            }

            return false;
        }

        protected virtual void HandleLanguageChange()
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var child in this.Children)
                {
                    child.Dispose();
                }
            }
        }

        // Helper
        protected static int Scale(int coord)
        {
            return coord * UIStatics.Scale / 100;
        }

        protected static int UnScale(int coord, int appliedScale)
        {
            return coord * 100 / appliedScale;
        }

        protected int UnScale(int coord)
        {
            return coord * 100 / this.appliedScale;
        }

        protected int Rescale(int coord)
        {
            return (int)(coord * UIStatics.Scale / (float)this.appliedScale);
        }
    }
}
