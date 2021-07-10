namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.UI;

    public class BlueprintButton : IconButton
    {
        protected static Texture2D textureLocked;
        protected static Texture2D textureLockedLarge;
        protected Texture2D textureUnlocked;
        protected bool isLocked;
        protected int prefabCount = 0;
        protected readonly TextLabel prefabCountLabel;
        protected bool isLarge;

        public BlueprintButton(IUIElement parent, int x, int y, string texturePath, bool isLarge)
            : base(parent, x, y, texturePath, 1f, true)
        {
            this.isLarge = isLarge;
            this.prefabCountLabel = new TextLabel(UIStatics.TextRenderer, this, 0, 0, 0, 0, "", UIColour.LightBlueText, TextAlignment.BottomRight);
            this.AddChild(this.prefabCountLabel);
        }

        public override void LoadContent()
        {
            base.LoadContent();

            if (!this.isLarge && textureLocked == null) textureLocked = UIStatics.Content.Load<Texture2D>("Textures\\Icons\\LockedBlueprint");
            if (this.isLarge && textureLockedLarge == null) textureLockedLarge = UIStatics.Content.Load<Texture2D>("Textures\\Icons\\LockedBlueprintLarge");
            this.textureUnlocked = this.texture;  // Base class loaded this

            this.prefabCountLabel.W = this.W - 2;
            this.prefabCountLabel.H = this.H - 2;
        }

        public bool IsLocked
        {
            get { return this.isLocked; }
            set
            {
                if (this.isLocked != value)
                {
                    this.isLocked = value;
                    this.texture = value ? (this.isLarge ? textureLockedLarge : textureLocked) : textureUnlocked;
                    this.IsContentChangedSinceDraw = true;
                }
            }
        }

        public int PrefabCount
        {
            get { return this.prefabCount; }
            set
            {
                if (this.prefabCount != value)
                {
                    this.prefabCount = value;
                    this.prefabCountLabel.Text = value > 0 ? value.ToString() : "";
                }
            }
        }
    }
}
