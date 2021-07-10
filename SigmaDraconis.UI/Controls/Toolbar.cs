namespace SigmaDraconis.UI
{
    using System;
    using System.Linq;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.Shared;
    using Draconis.UI;
    using CheckList;
    using Language;
    using Shared;
    using World;
    using WorldInterfaces;

    public class Toolbar : RenderTargetElement
    {
        private Texture2D energyBarBackground;
        private readonly TextLabel energyGenLabel;
        private readonly TextLabel energyUseLabel;
        private readonly NetworkEnergyDisplay energyDisplay;
        private readonly NetworkWaterDisplay waterDisplay;
        private readonly EnergyTooltip energyTooltip;
        private readonly WaterTooltip waterTooltip;
        private readonly EmptyElement energyTooltipAttachPoint;
        private readonly InventoryDisplay foodInventoryDisplay;
        private readonly InventoryDisplay resourceInventoryDisplay;
        private readonly InventoryDisplay itemsInventoryDisplay;
        private readonly InventoryDisplay hydrogenInventoryDisplay;
        private int prevWidth;

        private ILander lander;
        public ILander Lander
        {
            get { return this.lander; }
            set
            {
                this.lander = value;
                if (lander != null && World.ResourceNetwork != null) this.energyDisplay.SetValues(World.ResourceNetwork.EnergyTotal);
            }
        }

        public IconButton OptionsButton { get; private set; }
        public ChecklistToolbarButton ChecklistButton { get; private set; }

        public Toolbar(IUIElement parent, int x, int y, int width, int height)
            : base(parent, x, y, width, height)
        {
            this.prevWidth = width;
            this.energyUseLabel = new TextLabel(this, 0, Scale(12), Scale(58), Scale(20), "", UIColour.RedText, TextAlignment.TopRight);
            this.AddChild(this.energyUseLabel);
            this.energyGenLabel = new TextLabel(this, 0, Scale(12), Scale(50), Scale(20), "", UIColour.GreenText, TextAlignment.TopLeft);
            this.AddChild(this.energyGenLabel);
            this.energyDisplay = new NetworkEnergyDisplay(this, 0, Scale(12));
            this.AddChild(this.energyDisplay);
            this.waterDisplay = new NetworkWaterDisplay(this, 0, Scale(12) - 1);
            this.AddChild(this.waterDisplay);

            this.energyTooltipAttachPoint = new EmptyElement(this, 0, Scale(4), Scale(294), Scale(30));
            this.AddChild(this.energyTooltipAttachPoint);
            this.energyTooltip = new EnergyTooltip(this, this.energyTooltipAttachPoint);
            TooltipParent.Instance.AddChild(energyTooltip);

            this.waterTooltip = new WaterTooltip(this, this.waterDisplay);
            TooltipParent.Instance.AddChild(waterTooltip);

            this.resourceInventoryDisplay = new InventoryDisplay(this, 0, -Scale(4), InventoryDisplayType.Resources);
            this.AddChild(this.resourceInventoryDisplay);

            this.itemsInventoryDisplay = new InventoryDisplay(this, 0, -Scale(4), InventoryDisplayType.Items);
            this.AddChild(this.itemsInventoryDisplay);

            this.foodInventoryDisplay = new InventoryDisplay(this, 0, -Scale(4), InventoryDisplayType.Food);
            this.AddChild(this.foodInventoryDisplay);

            this.hydrogenInventoryDisplay = new InventoryDisplay(this, 0, -Scale(4), InventoryDisplayType.Hydrogen);
            this.AddChild(this.hydrogenInventoryDisplay);

            this.OptionsButton = new IconButton(this, this.W - Scale(36), Scale(4), "Textures\\Icons\\Options", 1f, true);
            this.OptionsButton.MouseLeftClick += this.IconButtonClick;
            this.OptionsButton.AnchorRight = true;
            this.OptionsButton.AnchorLeft = false;
            this.AddChild(this.OptionsButton);

            this.ChecklistButton = new ChecklistToolbarButton(this, this.W - Scale(72), Scale(4));
            this.ChecklistButton.MouseLeftClick += this.IconButtonClick;
            this.ChecklistButton.AnchorRight = true;
            this.ChecklistButton.AnchorLeft = false;
            this.AddChild(this.ChecklistButton);

            this.UpdateHorizontalLayout();

            this.backgroundColour = new Color(0, 0, 0, UIStatics.BackgroundAlpha);
            this.IsInteractive = true;
        }

        public override void LoadContent()
        {
            this.energyBarBackground = UIStatics.Content.Load<Texture2D>("Textures\\Misc\\EnergyBarBackground");
            base.LoadContent();
        }

        public override void Update()
        {
            if (this.backgroundColour.A != UIStatics.BackgroundAlpha)
            {
                this.backgroundColour = new Color(0, 0, 0, UIStatics.BackgroundAlpha);
                this.IsContentChangedSinceDraw = true;
            }

            var network = this.Lander != null ? World.ResourceNetwork : null;
            if (network != null && network.EnergyCapacity > 0)
            {
                this.energyDisplay.SetValues(Math.Min(network.EnergyTotal, network.EnergyCapacity), network.EnergyCapacity, network.EnergyGenTotal, network.EnergyUseTotal);
                this.energyDisplay.IsVisible = true;

                var energyGen = network.EnergyGenTotal.KWh;
                var energyUse = network.EnergyUseTotal.KWh;

                this.energyGenLabel.Text = $"+{energyGen * 3600:N1}{LanguageHelper.KW}";
                this.energyUseLabel.Text = $"-{energyUse * 3600:N1}{LanguageHelper.KW}";
            }

            if (network != null)
            {
                this.waterDisplay.SetValues(network.WaterLevelForDisplay / 100M, network.WaterCapacity / 100M, network.WaterGenTotal / 100f, network.WaterUseTotal / 100f);
            }

            if (network != this.foodInventoryDisplay.ResourceNetwork) this.foodInventoryDisplay.ResourceNetwork = network;
            if (network != this.resourceInventoryDisplay.ResourceNetwork) this.resourceInventoryDisplay.ResourceNetwork = network;
            if (network != this.itemsInventoryDisplay.ResourceNetwork) this.itemsInventoryDisplay.ResourceNetwork = network;
            if (network != this.hydrogenInventoryDisplay.ResourceNetwork) this.hydrogenInventoryDisplay.ResourceNetwork = network;

            if (this.W != prevWidth)
            {
                // Update layout if width changes
                prevWidth = this.W;
                this.UpdateHorizontalLayout();
            }

            this.hydrogenInventoryDisplay.IsVisible = World.GetThings<IBuildableThing>(ThingType.HydrogenStorage).Any(t => t.IsReady);
            this.ChecklistButton.SetCount(CheckListController.NewItemCount, GameScreen.Instance.IsCheckListPanelShown);

            base.Update();
        }

        public override void ApplyScale()
        {
            this.W = this.Parent.W;
            this.H = this.Rescale(this.H);
            this.IsContentChangedSinceDraw = true;
        }

        public override void ApplyLayout()
        {
            foreach (var child in this.Children)
            {
                child.Y = (child == this.waterDisplay) ? Scale(12) - 1 : this.Rescale(child.Y);
                child.ApplyScale();
                child.ApplyLayout();
            }

            this.UpdateHorizontalLayout();

            this.appliedScale = UIStatics.Scale;
            this.suppressOnParentResize = true;
            this.IsContentChangedSinceDraw = true;
        }

        protected override void DrawBaseLayer()
        {
            spriteBatch.Begin();
            if (this.energyBarBackground != null && this.energyDisplay.IsVisible)
            {
                var sy = 0;
                if (UIStatics.Scale == 200) sy = 40;
                else if (UIStatics.Scale == 150) sy = 16;
                Rectangle source = new Rectangle(0, sy, Scale(200), Scale(16));
                Rectangle dest = new Rectangle(this.energyUseLabel.X + Scale(60), Scale(12) - 1, Scale(200), Scale(16));
                spriteBatch.Draw(this.energyBarBackground, dest, source, Color.White);
            }

            spriteBatch.End();
        }

        public event EventHandler<EventArgs> HelpButtonClick;
        public event EventHandler<EventArgs> OptionsButtonClick;

        public void SelectHelpButton()
        {
            this.SelectRightButton(this.ChecklistButton);
        }

        public void SelectRightButton(IUIElement button)
        {
            this.OptionsButton.IsSelected = button == this.OptionsButton;
            this.ChecklistButton.IsSelected = button == this.ChecklistButton;
        }

        private void IconButtonClick(object sender, MouseEventArgs e)
        {
            if (sender == this.OptionsButton && this.OptionsButtonClick != null)
            {
                this.OptionsButtonClick(this, new EventArgs());
            }
            else if (sender == this.ChecklistButton && this.HelpButtonClick != null)
            {
                this.HelpButtonClick(this, new EventArgs());
            }
        }

        private void UpdateHorizontalLayout()
        {
            var lhsW = Scale(574);
            var rhsW = resourceInventoryDisplay.W + itemsInventoryDisplay.W + foodInventoryDisplay.W + hydrogenInventoryDisplay.W + Scale(24);

            var extraSpace = Math.Max(0, Math.Min((this.W / 2) - lhsW - 100, (this.W / 2) - rhsW - 100)) / 2;  // Extra space in middle to spread things out

            var pushLeft = Math.Max(0, (this.W / 2) + extraSpace + rhsW - (this.W - Scale(80)));   // Prevent overlap with help button

            var lhsX = (this.W / 2) - lhsW - extraSpace - pushLeft;
            this.energyUseLabel.X = lhsX;
            this.energyGenLabel.X = lhsX + Scale(262);
            this.energyDisplay.X = lhsX + Scale(74) + 1;
            this.energyTooltipAttachPoint.X = lhsX;
            this.waterDisplay.X = lhsX + Scale(324);

            var rhsX = (this.W / 2) + extraSpace - pushLeft;
            this.resourceInventoryDisplay.X = rhsX;
            this.itemsInventoryDisplay.X = this.resourceInventoryDisplay.Right + Scale(8);
            this.foodInventoryDisplay.X = this.itemsInventoryDisplay.Right + Scale(8);
            this.hydrogenInventoryDisplay.X = this.foodInventoryDisplay.Right + Scale(8);

            this.ChecklistButton.X = this.W - Scale(72);
            this.OptionsButton.X = this.W - Scale(36);
        }
    }
}
