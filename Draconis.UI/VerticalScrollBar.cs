namespace Draconis.UI
{
    using System;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;
    using Draconis.Shared;
    using Internal;

    public class VerticalScrollBar : UIElementBase
    {
        private readonly ScrollbarIconButton upButton;
        private readonly ScrollbarIconButton downButton;
        private readonly VerticalScrollBarSlider slider;
        private int maxScrollPosition = 0;
        private int scrollPosition = 0;
        private float fractionVisible = 1f;
        private int mouseDragStartY;
        private int mouseDragStartScrollY;

        public static string ScrollUpTexturePath = "Textures\\Icons\\ScrollUp";
        public static string ScrollDownTexturePath = "Textures\\Icons\\ScrollDown";

        public event EventHandler<EventArgs> ScrollPositionChange;

        public bool IsMouseDragging { get; private set; }
        public float ScrollPositionExact => this.slider.ScrollPosition;
        public int ScrollSpeed { get; set; } = 1;

        public VerticalScrollBar(IUIElement parent, int x, int y, int height, int pageSize)
            : base(parent, x, y, Scale(18) + 2, height)
        {
            this.upButton = new ScrollbarIconButton(this, 1, 1, ScrollUpTexturePath)
            {
                AnchorRight = true,
                AnchorLeft = false,
                IsEnabled = false
            };

            this.AddChild(this.upButton);
            this.upButton.MouseLeftClick += this.UpButton_MouseLeftClick;
            this.upButton.MouseLeftButtonHold += this.UpButton_MouseLeftClick;

            this.downButton = new ScrollbarIconButton(this, 1, height - Scale(14) - 1, ScrollDownTexturePath)
            {
                AnchorRight = true,
                AnchorBottom = true,
                AnchorTop = false,
                AnchorLeft = false,
                IsEnabled = false
            };

            this.AddChild(this.downButton);
            this.downButton.MouseLeftClick += this.DownButton_MouseLeftClick;
            this.downButton.MouseLeftButtonHold += this.DownButton_MouseLeftClick;

            this.slider = new VerticalScrollBarSlider(this, 1, Scale(14) + 1, height - Scale(28) - 1)
            {
                AnchorRight = true,
                AnchorBottom = true,
                AnchorLeft = false
            };

            this.Parent.MouseScrollDown += this.DownButton_MouseLeftClick;
            this.Parent.MouseScrollUp += this.UpButton_MouseLeftClick;
            this.slider.MouseScrollDown += this.DownButton_MouseLeftClick;
            this.slider.MouseScrollUp += this.UpButton_MouseLeftClick;
            this.slider.MouseLeftClick += Slider_MouseLeftClick;
            this.slider.MouseLeftDragRelease += Slider_MouseLeftClick;
            this.slider.MouseLeftDrag += Slider_MouseDrag;
            this.slider.MouseRightDrag += Slider_MouseDrag;
            this.AddChild(this.slider);

            this.PageSize = pageSize;
            this.IsInteractive = true;

            this.UpdateButtons();
        }

        protected override void OnMouseScrollUp(MouseEventArgs e)
        {
            this.UpButton_MouseLeftClick(this, e);
        }

        protected override void OnMouseScrollDown(MouseEventArgs e)
        {
            this.DownButton_MouseLeftClick(this, e);
        }

        private void Slider_MouseDrag(object sender, MouseEventArgs e)
        {
            if (this.maxScrollPosition == 0 || (!this.slider.IsMouseOverDragger && !this.IsMouseDragging))
            {
                return;
            }

            if (!this.IsMouseDragging)
            {
                this.IsMouseDragging = true;
                this.mouseDragStartY = e.LastMouseState.Y;
                this.mouseDragStartScrollY = this.ScrollPosition;
            }

            var top = this.maxScrollPosition > 0 ? (int)(this.slider.H * this.scrollPosition * (1f - this.fractionVisible) / this.maxScrollPosition) : 0;
            top += this.slider.ScreenY;
            var bottom = top + (int)(this.slider.H * this.fractionVisible);
            if (e.IsDraggingLeft || (e.LastMouseState.Y >= top && e.LastMouseState.Y <= bottom))
            {
                var range = this.slider.H + top - bottom;
                var scrollY = this.mouseDragStartScrollY;
                var deltaY = e.CurrentMouseState.Y - this.mouseDragStartY;
                if (deltaY != 0)
                {
                    this.ScrollPosition = Math.Max(0, Math.Min(this.MaxScrollPosition, scrollY + (int)(this.MaxScrollPosition * deltaY / (float)range)));
                    this.slider.ScrollPosition = Math.Max(0, Math.Min(this.MaxScrollPosition, scrollY + this.MaxScrollPosition * deltaY / (float)range));
                }
            }

            this.UpdateButtons();
        }

        private void Slider_MouseLeftClick(object sender, MouseEventArgs e)
        {
            if (e.IsDraggingLeft)
            {
                return;
            }

            var top = this.maxScrollPosition > 0 ? (int)(this.slider.H * this.scrollPosition * (1f - this.fractionVisible) / this.maxScrollPosition) : 0;
            top += this.slider.ScreenY;
            var bottom = top + (int)(this.slider.H * this.fractionVisible);
            var pageSize = this.PageSize >= 0 ? this.PageSize : (int)(this.maxScrollPosition / ((1f / this.fractionVisible) - 1f));
            if (e.CurrentMouseState.Y < top)
            {
                this.ScrollPosition = Math.Max(0, this.ScrollPosition - pageSize); 
            }
            else if (e.CurrentMouseState.Y > bottom)
            {
                this.ScrollPosition = Math.Min(this.maxScrollPosition, this.ScrollPosition + pageSize);
            }

            this.UpdateButtons();
        }

        private void UpButton_MouseLeftClick(object sender, MouseEventArgs e)
        {
            this.ScrollPosition = Math.Max(0, this.scrollPosition - this.ScrollSpeed);
            this.UpdateButtons();
        }

        private void DownButton_MouseLeftClick(object sender, MouseEventArgs e)
        {
            this.ScrollPosition = Math.Min(this.maxScrollPosition, this.scrollPosition + this.ScrollSpeed);
            this.UpdateButtons();
        }

        public int MaxScrollPosition
        {
            get
            {
                return this.maxScrollPosition;
            }

            set
            {
                if (value != this.maxScrollPosition)
                {
                    this.maxScrollPosition = value;
                    this.slider.MaxScrollPosition = value;
                    this.UpdateButtons();
                }
            }
        }

        public int ScrollPosition
        {
            get
            {
                return this.scrollPosition;
            }

            set
            {
                if (value != this.scrollPosition)
                {
                    this.scrollPosition = value;
                    this.slider.ScrollPosition = value;
                    this.ScrollPositionChange?.Invoke(this, new EventArgs());
                    this.UpdateButtons();
                }
            }
        }

        public float FractionVisible
        {
            get
            {
                return this.fractionVisible;
            }

            set
            {
                if (value != this.fractionVisible)
                {
                    this.fractionVisible = value;
                    this.slider.FractionVisible = value;
                    this.UpdateButtons();
                }
            }
        }

        public int PageSize { get; set; } = 0;

        public override void LoadContent()
        {
            base.LoadContent();

            this.texture = new Texture2D(UIStatics.Graphics, this.W, this.H);
            Color[] color = new Color[this.W * this.H];

            int index = 0;
            for (int y = 0; y < this.H; ++y)
            {
                for (int x = 0; x < this.W; ++x, ++index)
                {
                    if (x == 0 || y == 0 || x == this.W - 1 || y == this.H - 1)
                        color[index] = new Color(64, 64, 64, 255);
                    else
                        color[index] = new Color(0, 0, 0, 120);
                }
            }

            this.texture.SetData(color);
        }

        public override void ApplyScale()
        {
            this.W = Scale(18) + 1;
            this.H = this.Rescale(this.H);
            this.IsContentChangedSinceDraw = true;
        }

        public override void ApplyLayout()
        {
            this.upButton.X = 1;
            this.upButton.Y = 1;
            this.upButton.ApplyScale();
            this.upButton.ApplyLayout();
            this.slider.X = 1;
            this.slider.Y = Scale(14) + 1;
            this.slider.H = this.H - Scale(28) - 1;
            this.slider.W = Scale(18);
            this.slider.ApplyLayout();
            this.downButton.X = 1;
            this.downButton.Y = this.H - Scale(14) - 1;
            this.downButton.ApplyScale();
            this.downButton.ApplyLayout();
            this.appliedScale = UIStatics.Scale;
            this.IsContentChangedSinceDraw = true;

            if (this.texture != null) this.texture.Dispose();

            this.texture = new Texture2D(UIStatics.Graphics, this.W, this.H);
            Color[] color = new Color[this.W * this.H];

            int index = 0;
            for (int y = 0; y < this.H; ++y)
            {
                for (int x = 0; x < this.W; ++x, ++index)
                {
                    if (x == 0 || y == 0 || x == this.W - 1 || y == this.H - 1)
                        color[index] = new Color(64, 64, 64, 255);
                    else
                        color[index] = new Color(0, 0, 0, 120);
                }
            }

            this.texture.SetData(color);
        }

        public override void Update()
        {
            if (UIStatics.CurrentMouseState.LeftButton == ButtonState.Released && this.IsMouseDragging)
            {
                this.IsMouseDragging = false;
                this.slider.ScrollPosition = this.scrollPosition;  // Snap to position when release drag
                this.UpdateButtons();
            }

            base.Update();
        }

        private void UpdateButtons()
        {
            this.upButton.IsEnabled = this.scrollPosition > 0;
            this.downButton.IsEnabled = this.scrollPosition < this.maxScrollPosition;
        }
    }
}
