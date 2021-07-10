namespace SigmaDraconis.UI
{
    using System;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Draconis.Shared;
    using Draconis.UI;
    using IO;
    using Language;
    
    public class DeleteGameDialog : DialogBase
    {
        private readonly TextLabel label2;
        private readonly TextButton deleteButton;
        private readonly TextButton cancelButton;
        private string fileName;
        private Texture2D pixelTexture2;
        
        public event EventHandler<EventArgs> CloseClick;

        public string FileName
        {
            get
            {
                return this.fileName;
            }
            set
            {
                this.fileName = value;
                this.label2.Text = this.fileName;
            }
        }

        public bool IsAutosave { get; set; }

        public DeleteGameDialog(IUIElement parent)
            : base(parent, Scale(320), Scale(140), StringsForDialogTitles.DeleteGame)
        {
            this.IsVisible = false;

            UIHelper.AddTextLabel(this, 0, 28, 320, UIColour.DefaultText, StringsForDeleteGameDialog.Confirm);
            this.label2 = UIHelper.AddTextLabel(this, 0, 44, 320, UIColour.DefaultText);

            this.deleteButton = new TextButton(this, (this.W * 1 / 4) - Scale(40), this.H - Scale(30), Scale(100), Scale(20), LanguageHelper.GetForButton(StringsForButtons.Delete)) { TextColour = UIColour.RedText, IsSelected = true };
            this.deleteButton.MouseLeftClick += this.OnDeleteClick;
            this.AddChild(this.deleteButton);

            this.cancelButton = new TextButton(this, (this.W * 3 / 4) - Scale(60), this.H - Scale(30), Scale(100), Scale(20), LanguageHelper.GetForButton(StringsForButtons.Cancel));
            this.cancelButton.MouseLeftClick += this.OnCancelClick;
            this.AddChild(this.cancelButton);
        }

        public override void LoadContent()
        {
            this.pixelTexture2 = new Texture2D(UIStatics.Graphics, 1, 1);
            Color[] color2 = new Color[1] { new Color(0, 0, 0, 64) };
            this.pixelTexture2.SetData(color2);

            base.LoadContent();
        }

        protected override void DrawBaseLayer()
        {
            if (this.pixelTexture2 != null && this.IsVisible)
            {
                Rectangle r2 = new Rectangle(0, 0, this.W, Scale(14));
                Rectangle r3 = new Rectangle(Scale(12), Scale(24), this.W - Scale(24), this.H - Scale(68));

                spriteBatch.Begin();
                spriteBatch.Draw(pixelTexture2, r2, Color.White);
                spriteBatch.Draw(pixelTexture, r3, Color.White);
                spriteBatch.End();
            }
        }

        protected override void HandleLanguageChange()
        {
            this.deleteButton.Text = LanguageHelper.GetForButton(StringsForButtons.Delete);
            this.deleteButton.X = (this.W * 1 / 4) - Scale(40);

            this.cancelButton.Text = LanguageHelper.GetForButton(StringsForButtons.Cancel);
            this.cancelButton.X = (this.W * 3 / 4) - Scale(60);

            base.HandleLanguageChange();
        }

        protected override void HandleEscapeKey()
        {
            this.CloseClick?.Invoke(this, new EventArgs());
            base.HandleEscapeKey();
        }

        protected override void HandleEnterOrSpaceKey()
        {
            if (this.deleteButton.IsSelected) SaveGameManager.Delete(this.fileName, this.IsAutosave);
            this.CloseClick?.Invoke(this, new EventArgs());
            base.HandleEnterOrSpaceKey();
        }

        protected override void HandleLeftKey()
        {
            this.deleteButton.IsSelected = !this.deleteButton.IsSelected;
            this.cancelButton.IsSelected = !this.cancelButton.IsSelected;
            base.HandleLeftKey();
        }

        protected override void HandleRightKey()
        {
            this.deleteButton.IsSelected = !this.deleteButton.IsSelected;
            this.cancelButton.IsSelected = !this.cancelButton.IsSelected;
            base.HandleRightKey();
        }

        protected override void HandleUpKey()
        {
            this.deleteButton.IsSelected = !this.deleteButton.IsSelected;
            this.cancelButton.IsSelected = !this.cancelButton.IsSelected;
            base.HandleUpKey();
        }

        protected override void HandleDownKey()
        {
            this.deleteButton.IsSelected = !this.deleteButton.IsSelected;
            this.cancelButton.IsSelected = !this.cancelButton.IsSelected;
            base.HandleDownKey();
        }

        private void OnDeleteClick(object sender, MouseEventArgs e)
        {
            SaveGameManager.Delete(this.fileName, this.IsAutosave);
            this.CloseClick?.Invoke(this, new EventArgs());
        }

        private void OnCancelClick(object sender, MouseEventArgs e)
        {
            this.CloseClick?.Invoke(this, new EventArgs());
        }
    }
}
