namespace SigmaDraconis.UI
{
    using Draconis.UI;
    using Language;
    using WorldInterfaces;
    using Microsoft.Xna.Framework.Graphics;

    /// <summary>
    /// A component on the colonist panel
    /// </summary>
    public class ColonistCurrentActionLabel : TextLabel
    {
        public ColonistCurrentActionLabel(IUIElement parent, int x, int y, int width) 
            : base(parent, x, y, width, Scale(18), "", UIColour.DefaultText)
        {
            var workRateTooltipAttachPoint = new EmptyElement(this, 0, 0, width, Scale(20));
            this.AddChild(workRateTooltipAttachPoint);
        }

        public override void LoadContent()
        {
            this.texture = UIStatics.Content.Load<Texture2D>("Textures\\Misc\\ColonistPanelActionLabelBackground");
            base.LoadContent();
        }

        public override void Update()
        {
            if ((this.Parent as IThingPanel)?.Thing is IColonist colonist)
            {
                this.Text = colonist.IsDead ? LanguageManager.Get<StringsForColonistPanel>(StringsForColonistPanel.Dead) : colonist.CurrentActivityDescription;
                this.IsVisible = true;
            }
            else this.IsVisible = false;

            base.Update();
        }
    }
}
