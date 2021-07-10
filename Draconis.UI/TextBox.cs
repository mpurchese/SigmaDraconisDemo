namespace Draconis.UI
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;

    using Draconis.Shared;

    public class TextBox : UIElementBase, IKeyboardHandler
    {
        #region Private Fields

        private string text = "";
        private Color textColour = new Color(192, 192, 192, 255);
        private Color backgroundColour = new Color(0, 0, 0, 100);
        private bool isReadOnly = false;
        private readonly TextCursor cursor;
        private readonly TextLabel textLabel;
        private int cursorPos = 0;
        private bool hasFocus = true;
        private bool textureUpdateRequired = true;

        #endregion

        #region Constructor

        public TextBox(IUIElement parent, int x, int y, int width, int height)
            : base(parent, x, y, width, height)
        {
            this.cursor = new TextCursor(this, 3, Scale(2), 1, Scale(16))
            {
                AnchorTop = true,
                AnchorBottom = true,
                IsVisible = false,
                IsInteractive = false
            };

            this.AddChild(cursor);

            this.textLabel = new TextLabel(this, Scale(2), Scale(2), string.Empty, Color.LightGray);
            this.AddChild(this.textLabel);

            this.MaxLength = (width - 4) / UIStatics.TextRenderer.LetterSpace;
            this.IsMultiline = IsMultiline;
        }

        #endregion

        #region Public Events

        public event EventHandler<EventArgs> TextChange;
        public event EventHandler<EventArgs> EnterPress;

        #endregion

        #region Public Properties

        public Color BackgroundColour
        {
            get
            {
                return this.backgroundColour;
            }
            set
            {
                if (this.backgroundColour != value)
                {
                    this.backgroundColour = value;
                    this.IsContentChangedSinceDraw = true;
                }
            }
        }

        public Color TextColour
        {
            get
            {
                return this.textColour;
            }
            set
            {
                if (this.textLabel.Colour != value)
                {
                    this.textLabel.Colour = value;
                }
            }
        }

        public string Text
        {
            get
            {
                return this.text;
            }
            set
            {
                if (this.text != value)
                {
                    this.text = value;
                    this.IsContentChangedSinceDraw = true;
                    this.cursorPos = this.text.Length;
                }
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return this.isReadOnly;
            }
            set
            {
                if (this.isReadOnly != value)
                {
                    this.isReadOnly = value;
                    this.IsContentChangedSinceDraw = true;
                }
            }
        }

        public int MaxLength { get; set; }

        public string CharacterMask { get; set; } = ".";

        public string WatermarkText { get; set; }

        public bool IsClearButtonVisble { get; set; }

        public bool IsMultiline { get; set; }

        #endregion

        public override void ApplyLayout()
        {
            this.textLabel.X = Scale(2);
            this.textLabel.Y = Scale(2);
            this.cursor.Y = Scale(2);
            //this.cursor.H = Scale(16);
            this.appliedScale = UIStatics.Scale;
            this.IsContentChangedSinceDraw = true;
        }

        public override void Update()
        {
            this.cursor.IsVisible = this.IsVisible && this.hasFocus;
            this.cursor.X = (this.cursorPos * UIStatics.TextRenderer.LetterSpace) + Scale(3);

            if (this.IsVisible == true)
            {
                if (string.IsNullOrEmpty(this.Text) == false)
                {
                    string str = this.Text;
                    if (str.Length > (this.W - Scale(4)) / 6)
                    {
                        str = str.Substring(0, (this.W - Scale(4)) / 6);
                    }

                    this.textLabel.Text = str;
                    this.textLabel.Colour = this.TextColour;
                }
                else if (this.hasFocus == false && string.IsNullOrEmpty(this.WatermarkText) == false)
                {
                    string str = this.WatermarkText;
                    if (str.Length > (this.W - Scale(4)) / 6)
                        str = str.Substring(0, (this.W - Scale(4)) / 6);

                    this.textLabel.Text = str;
                    this.textLabel.Colour = new Color(128, 128, 128, 128);
                }
                else
                {
                    this.textLabel.Text = string.Empty;
                }
            }

            base.Update();
        }

        public override void LoadContent()
        {
            base.LoadContent();
            this.GenerateTexture();
        }

        protected virtual void GenerateTexture()
        {
            texture = new Texture2D(UIStatics.Graphics, this.W, this.H);
            Color[] textureData = new Color[this.W * this.H];

            for (int x = 0; x < this.W; x++)
            {
                for (int y = 0; y < this.H; y++)
                {
                    Color colour;
                    if (x == 0 || y == 0)
                    {
                        colour = this.hasFocus ? new Color(64, 48, 0, 128) : new Color(64, 64, 64, 128);
                    }
                    else if (x == this.W - 1 || y == this.H - 1)
                    {
                        colour = this.hasFocus ? new Color(128, 96, 0, 128) : new Color(128, 128, 128, 128);
                    }
                    else if (this.IsClearButtonVisble
                        && (y > Scale(2) && y < this.H - Scale(4) && x < this.W - Scale(2) && ((x + (this.H - 1) - this.W == y) || (this.W - x - 1 == y))))
                    {
                        colour = new Color(96, 96, 96, 255);
                    }
                    else
                    {
                        colour = this.BackgroundColour;
                    }

                    textureData[(y * this.W) + x] = colour;
                }
            }

            this.texture.SetData(textureData);
            textureUpdateRequired = false;
        }

        protected override void DrawContent()
        {
            if (this.textureUpdateRequired) this.GenerateTexture();
            base.DrawContent();
        }


        public void HandleKeyPress(Keys key)
        {
            if (!this.IsReadOnly) this.HandleKey(key);
        }

        public void HandleKeyHold(Keys key)
        {
            if (!this.IsReadOnly) this.HandleKey(key);
        }

        public void HandleKeyRelease(Keys key)
        {
            if (!this.IsReadOnly && key == Keys.Enter) this.EnterPress?.Invoke(this, new EventArgs());
        }

        private void HandleKey(Keys key)
        {
            if (key == Keys.Back)
            {
                this.DeletePrevChar();
            }
            else if (key == Keys.Delete)
            {
                this.DeleteNextChar();
            }
            else if (key == Keys.Left)
            {
                if (this.cursorPos > 0)
                    this.cursorPos--;
            }
            else if (key == Keys.Right)
            {
                if (this.cursorPos < this.Text.Length)
                    this.cursorPos++;
            }
            else if (key == Keys.Home)
            {
                this.cursorPos = 0;
            }
            else if (key == Keys.End)
            {
                this.cursorPos = this.Text.Length;
            }
            else if (key != Keys.Enter)
            {
                string s = GetKeyStr(key, this.IsShiftDown(key));
                if (!string.IsNullOrEmpty(s) && Regex.IsMatch(s, this.CharacterMask))
                {
                    this.AddChar(s[0]);
                }
            }
        }

        private static string GetKeyStr(Keys key, bool isShiftDown)
        {
            // TODO: This is UK keyboard layout.  Do other keyboard layouts

            if (key == Keys.Space) return " ";

            if (isShiftDown)
            {
                if (key == Keys.Decimal || key == Keys.OemPeriod) return ">";
                if (key == Keys.Add || key == Keys.OemPlus) return "+";
                if (key == Keys.Subtract || key == Keys.OemMinus) return "_";
                if (key == Keys.OemBackslash) return @"|";
                if (key == Keys.OemCloseBrackets) return "}";
                if (key == Keys.OemComma) return "<";
                if (key == Keys.OemOpenBrackets) return "{";
                if (key == Keys.OemQuestion) return "?";
                if (key == Keys.OemQuotes) return "@";
                if (key == Keys.OemSemicolon) return ":";
                if (key == Keys.OemTilde) return "@";
                if (key == Keys.D1) return "!";
                if (key == Keys.D2) return "\"";
                if (key == Keys.D3) return "£";
                if (key == Keys.D4) return "$";
                if (key == Keys.D5) return "%";
                if (key == Keys.D6) return "^";
                if (key == Keys.D7) return "&";
                if (key == Keys.D8) return "*";
                if (key == Keys.D9) return "(";
                if (key == Keys.D0) return ")";
            }
            else
            {
                if (key == Keys.Decimal || key == Keys.OemPeriod) return ".";
                if (key == Keys.Add || key == Keys.OemPlus) return "=";
                if (key == Keys.Subtract || key == Keys.OemMinus) return "-";
                if (key == Keys.OemBackslash) return @"\";
                if (key == Keys.OemCloseBrackets) return "]";
                if (key == Keys.OemComma) return ",";
                if (key == Keys.OemOpenBrackets) return "[";
                if (key == Keys.OemQuestion) return "/";
                if (key == Keys.OemSemicolon) return ";";
                if (key == Keys.OemTilde) return "'";
            }

            string s = key.ToString();
            if (s.Length == 1) return isShiftDown ? s : s.ToLowerInvariant();                                     // Letters
            if ((s.Length == 2 && s.StartsWith("D") && !isShiftDown) || s.StartsWith("NumPad")) return s.Substring(s.Length - 1); // Numbers

            return "";
        }

        protected override void OnMouseLeftClick(MouseEventArgs e)
        {
            if (e.EventType == MouseEventType.LeftClick && this.IsInteractive && this.IsVisible
                && this.IsClearButtonVisble
                && e.CurrentMouseState.Y > this.ScreenY
                && e.CurrentMouseState.Y < this.ScreenY + this.H - Scale(4)
                && e.CurrentMouseState.X > this.ScreenX + this.W + Scale(4) - this.H
                && e.CurrentMouseState.X < this.ScreenX + this.W)
            {
                this.text = "";
                this.cursorPos = 0;
                this.IsContentChangedSinceDraw = true;
            }
        }

        private void AddChar(char c)
        {
            if (this.Text.Length < this.MaxLength)
            {
                this.text = this.Text.Substring(0, this.cursorPos) + c + this.Text.Substring(this.cursorPos);
                this.textureUpdateRequired = true;
                this.cursorPos++;
                this.RaiseTextChangedEvent();
            }
        }

        private void DeletePrevChar()
        {
            if (this.Text.Length > 0)
            {
                string str1 = this.cursorPos > 1 ? this.Text.Substring(0, this.cursorPos - 1) : "";
                string str2 = this.cursorPos < this.Text.Length ? this.Text.Substring(this.cursorPos) : "";
                this.text = str1 + str2;
                this.textureUpdateRequired = true;
                this.cursorPos--;
                this.RaiseTextChangedEvent();
            }
        }

        private void DeleteNextChar()
        {
            if (this.Text.Length > 0)
            {
                string str1 = this.cursorPos > 0 ? this.Text.Substring(0, this.cursorPos) : "";
                string str2 = this.cursorPos < this.Text.Length - 1 ? this.Text.Substring(this.cursorPos + 1) : "";
                this.text = str1 + str2;
                this.textureUpdateRequired = true;
                this.RaiseTextChangedEvent();
            }
        }

        private bool IsShiftDown(Keys key)
        {
            Keys[] keys = Keyboard.GetState().GetPressedKeys();
            return keys.Contains(Keys.LeftShift) || keys.Contains(Keys.RightShift)
                || (key >= Keys.A && key <= Keys.Z && this.IsCapsLocked());
        }

        private bool IsCapsLocked()
        {
            return Keyboard.GetState().CapsLock;
        }

        private void RaiseTextChangedEvent()
        {
            this.TextChange?.Invoke(this, new EventArgs());
        }
    }
}
