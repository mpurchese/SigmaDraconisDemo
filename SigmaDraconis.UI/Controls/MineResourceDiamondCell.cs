namespace SigmaDraconis.UI
{
    using System;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.UI;
    using Language;
    using Shared;
    using World.Projects;
    using WorldInterfaces;

    public class MineResourceDiamondCell : ButtonBase
    {
        private readonly Icon resourceDensityIcon = new Icon("Textures\\Icons\\MineResourceDensity", 5);
        private readonly Icon resourceTypeIcon = new Icon("Textures\\Icons\\Items", 13);

        protected Texture2D baseTexture = null;
        protected Texture2D selectTexture = null;
        protected Texture2D progressTexture = null;
        protected readonly TextLabel label;
        protected readonly SimpleTooltip tooltip;
        protected ItemType resourceType = ItemType.None;
        protected int resourceCount;
        protected MineResourceDensity resourceDenstiy;
        protected bool resourceIsVisible;

        public Direction Direction { get; private set; }

        public double MineProgress { get; set; }
        public bool CanSelect { get; set; }

        public MineResourceDiamondCell(IUIElement parent, Direction direction, int x, int y)
            : base(parent, Scale(x), Scale(y), Scale(68), Scale(52))
        {
            this.Direction = direction;

            this.label = new TextLabel(UIStatics.TextRenderer, this, Scale(20), Scale(31), Scale(28), Scale(20), "", UIColour.DefaultText, TextAlignment.TopCentre, true);
            this.AddChild(this.label);

            this.tooltip = UIHelper.AddSimpleTooltip(this.Parent.Parent, this);
        }

        public override void ApplyLayout()
        {
            this.label.X = Scale(20);
            this.label.Y = Scale(31);
            this.label.ApplyScale();
            this.label.ApplyLayout();

            this.appliedScale = UIStatics.Scale;
            this.suppressOnParentResize = true;
            this.IsContentChangedSinceDraw = true;
        }

        public override void LoadContent()
        {
            base.LoadContent();

            this.resourceDensityIcon.LoadContent();
            this.resourceTypeIcon.LoadContent();

            this.baseTexture = UIStatics.Content.Load<Texture2D>("Textures\\Icons\\MineResourceDiamondCell");
            this.selectTexture = UIStatics.Content.Load<Texture2D>("Textures\\Icons\\MineResourceDiamondCellSelect");
            this.resourceTypeIcon.LoadContent();

            this.progressTexture = new Texture2D(UIStatics.Graphics, 1, 1);
            Color[] colour = new Color[1] { UIColour.ProgressBar };
            this.progressTexture.SetData(colour);
        }

        public void SetResources(IMineTileResource resource, bool isSelected, double mineProgress, bool canSelect)
        {
            var type = resource != null ? resource.Type : ItemType.None;
            var count = resource != null ? resource.Count : 0;
            var density = resource != null ? resource.Density : MineResourceDensity.None;
            var isVisible = resource != null && resource.IsVisible;
            if (type == this.resourceType && count == this.resourceCount && density == this.resourceDenstiy 
                && isVisible == this.resourceIsVisible && this.isSelected == isSelected && this.MineProgress == mineProgress && this.CanSelect == canSelect) return;

            this.resourceType = type;
            this.resourceCount = count;
            this.resourceDenstiy = density;
            this.resourceIsVisible = isVisible;
            this.isSelected = isSelected;
            this.MineProgress = mineProgress;

            var rateStr = "";
            if (!isVisible)
            {
                this.label.Text = "?";
                this.tooltip.SetTitle(GetTooltipString(StringsForMineResourceDiamondCellTooltip.UnknownResource));
                
            }
            else if (this.resourceCount > 0)
            {
                this.label.Text = this.resourceCount.ToString();
                var typeStr = LanguageManager.Get<ItemType>(type);
                var densityStr = LanguageManager.Get<MineResourceDensity>(density);
                this.tooltip.SetTitle(string.Format(GetTooltipString(StringsForMineResourceDiamondCellTooltip.Normal), count, typeStr, densityStr));
                rateStr = string.Format(GetTooltipString(StringsForMineResourceDiamondCellTooltip.MineRate), 3600.0 / GetFramesToProcess(density));
            }
            else
            {
                this.label.Text = "";
                this.tooltip.SetTitle(GetTooltipString(StringsForMineResourceDiamondCellTooltip.NoResource));
            }

            this.CanSelect = canSelect;
            this.tooltip.SetText(canSelect ? rateStr : GetTooltipString(StringsForMineResourceDiamondCellTooltip.ClaimedByOther));
            this.IsContentChangedSinceDraw = true;
        }

        private static int GetFramesToProcess(MineResourceDensity density)
        {
            var multiplier = 1.0;
            if (ProjectManager.GetDefinition(202)?.IsDone == true) multiplier = 0.8;
            else if (ProjectManager.GetDefinition(201)?.IsDone == true) multiplier = 0.9;

            if (density == MineResourceDensity.VeryLow) return (int)(Constants.MineFramesToProcessVeryLowDensity * multiplier);
            if (density == MineResourceDensity.Low) return (int)(Constants.MineFramesToProcessLowDensity * multiplier);
            if (density == MineResourceDensity.Medium) return (int)(Constants.MineFramesToProcessMediumDensity * multiplier);
            if (density == MineResourceDensity.High) return (int)(Constants.MineFramesToProcessHighDensity * multiplier);
            return (int)(Constants.MineFramesToProcessVeryHighDensity * multiplier);
        }

        protected override void DrawContent()
        {
            if (this.baseTexture == null) return;

            var rSource = new Rectangle(this.resourceCount == 0 ? this.baseTexture.Width / 2 : 0, this.isMouseOver ? this.baseTexture.Height / 2 : 0, this.baseTexture.Width / 2, this.baseTexture.Height / 2);
            var rDest = new Rectangle(this.RenderX, this.RenderY, this.W, this.H);

            this.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            this.spriteBatch.Draw(this.baseTexture, rDest, rSource, Color.White);

            if (!this.CanSelect && (!this.resourceIsVisible || this.resourceType != ItemType.None))
            {
                // Red - claimed by another mine
                rSource = new Rectangle(this.selectTexture.Width / 2, 0, this.selectTexture.Width / 2, this.selectTexture.Height);
                spriteBatch.Draw(this.selectTexture, rDest, rSource, Color.White);
            }
            else if (this.isSelected)
            {
                // Green - claimed by this mine
                rSource = new Rectangle(0, 0, this.selectTexture.Width / 2, this.selectTexture.Height);
                spriteBatch.Draw(this.selectTexture, rDest, rSource, Color.White);
            }

            if (this.resourceIsVisible && this.resourceType != ItemType.None && this.resourceCount > 0)
            {
                this.resourceTypeIcon.Draw(this.spriteBatch, this.RenderX + Scale(18), this.RenderY + Scale(16), (int)this.resourceType - 1);
                this.resourceDensityIcon.Draw(this.spriteBatch, this.RenderX + Scale(28), this.RenderY + Scale(3), (int)this.resourceDenstiy - 1);
            }

            if (this.MineProgress > 0)
            {
                rDest = new Rectangle(this.RenderX + Scale(22), this.RenderY + Scale(14), (int)(this.MineProgress * Scale(24)), Scale(1));
                spriteBatch.Draw(this.progressTexture, rDest, Color.White);
            }

            this.spriteBatch.End();

            this.IsContentChangedSinceDraw = false;
        }

        protected override bool CheckIsMouseOver()
        {
            var mouseState = UIStatics.CurrentMouseState;
            var x = mouseState.X - this.ScreenX;
            var y = mouseState.Y - this.ScreenY;
            return base.CheckIsMouseOver() && Math.Abs(x - Scale(34)) + (1.3 * Math.Abs(y - Scale(26))) < Scale(31);
        }

        private static string GetTooltipString(StringsForMineResourceDiamondCellTooltip id)
        {
            return LanguageManager.Get<StringsForMineResourceDiamondCellTooltip>(id);
        }
    }
}
