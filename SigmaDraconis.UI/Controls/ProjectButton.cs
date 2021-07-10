namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.UI;

    public class ProjectButton : IconButtonWithOverlay
    {
        protected static Texture2D textureLocked;
        protected Texture2D textureUnlocked;
        protected bool isLocked;

        public ProjectButton(IUIElement parent, int x, int y, string texturePath)
            : base(parent, x, y, texturePath, "Textures\\Icons\\ProjectComplete")
        {
        }

        public override void LoadContent()
        {
            base.LoadContent();
            if (textureLocked == null) textureLocked = UIStatics.Content.Load<Texture2D>("Textures\\Icons\\LockedBlueprint");
            this.textureUnlocked = this.texture;  // Base class loaded this
        }

        public bool IsLocked
        {
            get { return this.isLocked; }
            set
            {
                if (this.isLocked != value)
                {
                    this.isLocked = value;
                    this.texture = value ? textureLocked : textureUnlocked;
                    this.IsContentChangedSinceDraw = true;
                }
            }
        }
    }
}
