namespace SigmaDraconis.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Draconis.Shared;
    using Draconis.UI;
    using Language;
    using Shared;
    using World;
    using WorldControllers;
    using WorldInterfaces;

    public class BuildingPanel : PanelLeft, IThingPanel
    {
        private static readonly Dictionary<ItemType, string> itemTypeNamesUpper = new Dictionary<ItemType, string>();

        protected BuildingConstructionControl constructionControl;
        protected IconButton deconstructFoundationButton;
        protected IconButton deconstructConduitNodeButton;
        protected SimpleTooltip deconstructFoundationTooltip;
        protected SimpleTooltip deconstructConduitNodeTooltip;
        protected FlowDiagramBase flowDiagram;
        protected bool suppressDeconstructConduitNode;
        protected bool showDeconstructsOnRight;

        protected IBuildableThing building;
        public IThing Thing
        {
            get { return this.building; }
            set
            {
                if (this.building != value)
                {
                    this.building = value as IBuildableThing;
                    this.OnBuildingChanged();
                }
            }
        }

        public event EventHandler<EventArgs> DeconstructConduitNodeClick;
        public event EventHandler<EventArgs> DeconstructFoundationClick;

        public BuildingPanel(IUIElement parent, int y)
            : base(parent, y, Scale(320), Scale(130), "")
        {
            this.AddChildren();
        }

        public BuildingPanel(IUIElement parent, int y, int h)
            : base(parent, y, Scale(320), h, "")
        {
            this.AddChildren();
        }

        private void AddChildren()
        {
            this.constructionControl = new BuildingConstructionControl(this, Scale(36), Scale(44), Scale(248), Scale(20));
            this.AddChild(this.constructionControl);
            this.constructionControl.PriorityChanged += this.OnConstructionPriorityChanged;

            this.deconstructFoundationButton = UIHelper.AddIconButton(this, 296, 106, "Textures\\Icons\\RecycleFoundation", this.OnDeconstructFoundationClick);
            this.deconstructFoundationTooltip = UIHelper.AddSimpleTooltip(this, this.deconstructFoundationButton, GetString(StringsForThingPanels.DeconstructFoundation));

            this.deconstructConduitNodeButton = UIHelper.AddIconButton(this, 296, 106, "Textures\\Icons\\RecycleConduit", this.OnDeconstructConduitNodeClick);
            this.deconstructConduitNodeTooltip = UIHelper.AddSimpleTooltip(this, this.deconstructConduitNodeButton, GetString(StringsForThingPanels.DeconstructConduitNode));
        }

        private void OnDeconstructConduitNodeClick(object sender, MouseEventArgs e)
        {
            this.DeconstructConduitNodeClick?.Invoke(this, new EventArgs());
            this.UpdateDeconstructButtons();
        }

        private void OnDeconstructFoundationClick(object sender, MouseEventArgs e)
        {
            this.DeconstructFoundationClick?.Invoke(this, new EventArgs());
            this.UpdateDeconstructButtons();
        }

        protected virtual void OnBuildingChanged()
        {
            this.titleLabel.Text = BuildingNameController.GetName(this.building).ToUpperInvariant();

            // Build priority
            if (this.building.ConstructionProgress < 100)
            {
                var blueprint = World.ConfirmedBlueprints.Values.FirstOrDefault(b => b.MainTileIndex == this.building.MainTileIndex && b.ThingType == this.building.ThingType);
                if (blueprint != null)
                {
                    this.constructionControl.Priority = blueprint.BuildPriority;
                    this.constructionControl.IsDeconstructing = true;
                }
            }
            else if (this.building.IsRecycling)
            {
                this.constructionControl.IsDeconstructing = false;
            }

            this.UpdateDeconstructButtons();
        }

        public override void Update()
        {
            if (!this.IsBuildingUiVisible && this.IsVisible)
            {
                if (this.flowDiagram != null) this.flowDiagram.IsVisible = false;

                if (this.building.ConstructionProgress < 100 && !this.building.IsRecycling)
                {
                    var blueprint = World.ConfirmedBlueprints.Values.FirstOrDefault(b => b.MainTileIndex == this.building.MainTileIndex && b.ThingType == this.building.ThingType);
                    if (blueprint != null)
                    {
                        this.constructionControl.IsVisible = true;
                        this.constructionControl.IsDeconstructing = false;
                        this.constructionControl.Progress = this.building.ConstructionProgress * 0.01;
                    }
                }
                else if (this.building.IsRecycling)
                {
                    this.constructionControl.IsVisible = true;
                    this.constructionControl.IsDeconstructing = true;
                    this.constructionControl.Progress = 1.0 - (this.building.ConstructionProgress * 0.01);
                }
                else
                {
                    this.constructionControl.IsVisible = false;
                }
            }
            else if (this.IsVisible)
            {
                this.constructionControl.IsVisible = false;
                if (this.flowDiagram != null) this.flowDiagram.IsVisible = true;
            }

            if (this.IsVisible) this.UpdateDeconstructButtons();  // Expensive
            base.Update();
        }

        protected virtual void UpdateDeconstructButtons()
        {
            if (!this.IsBuildingUiVisible || this.building is IConduitNode)
            {
                this.deconstructConduitNodeButton.IsVisible = false;
                this.deconstructFoundationButton.IsVisible = false;
                this.deconstructConduitNodeTooltip.IsEnabled = false;
                this.deconstructFoundationTooltip.IsEnabled = false;
                return;
            }

            // Foundation and conduit deconstruct buttons
            var countFoundation = this.building is IFoundation ? 0 : this.building.AllTiles.SelectMany(t => t.ThingsPrimary).OfType<IFoundation>().Count();
            var foundation = countFoundation > 0 ? this.building.AllTiles.SelectMany(t => t.ThingsPrimary).OfType<IFoundation>().FirstOrDefault() : null;
            var canDeconstructFoundation = countFoundation == 1 && this.building.Definition.FoundationsRequired == 0 && !foundation.IsRecycling && foundation.CanRecycle();

            var countConduitNode = this.building.AllTiles.SelectMany(t => t.ThingsPrimary).OfType<IConduitNode>().Count();
            var conduitNode = countConduitNode > 0 ? this.building.AllTiles.SelectMany(t => t.ThingsPrimary).OfType<IConduitNode>().FirstOrDefault() : null;
            var canDeconstructConduitNode = !suppressDeconstructConduitNode && countConduitNode == 1 && !conduitNode.IsRecycling && conduitNode.CanRecycle();

            if (this.deconstructFoundationButton.IsVisible != canDeconstructFoundation || this.deconstructConduitNodeButton.IsVisible != canDeconstructConduitNode)
            {
                this.deconstructFoundationButton.IsVisible = canDeconstructFoundation;
                this.deconstructConduitNodeButton.IsVisible = canDeconstructConduitNode;
                this.Invalidate();  // Doesn't automatically happen otherwise
            }

            if (this.flowDiagram?.IsVisible == true)
            {
                if (this.showDeconstructsOnRight)
                {
                    this.deconstructConduitNodeButton.X = Scale(canDeconstructFoundation ? 274 : 296);
                    this.deconstructFoundationButton.X = Scale(296);
                }
                else
                {
                    this.deconstructConduitNodeButton.X = Scale(8);
                    this.deconstructFoundationButton.X = Scale(canDeconstructConduitNode ? 30 : 8);
                }

                this.deconstructConduitNodeButton.Y = this.H - Scale(46);
                this.deconstructFoundationButton.Y = this.H - Scale(46);
            }
            else
            {
                this.deconstructConduitNodeButton.X = Scale(canDeconstructFoundation ? 274 : 296);
                this.deconstructFoundationButton.X = Scale(296);
                this.deconstructConduitNodeButton.Y = this.H - Scale(24);
                this.deconstructFoundationButton.Y = this.H - Scale(24);
            }

            if (this.deconstructFoundationButton.IsVisible)
            {
                this.deconstructFoundationTooltip.IsEnabled = true;
                var constructionCosts = foundation.Definition.ConstructionCosts;
                var displayName = foundation.DisplayNameLower;
                if (foundation.IsDesignatedForRecycling)
                {
                    this.deconstructFoundationTooltip.SetText(LanguageHelper.GetForMouseCursor(StringsForMouseCursor.ClickToCancelDeconstruction));
                }
                else if (constructionCosts[ItemType.Metal] > 0)
                {
                    this.deconstructFoundationTooltip.SetText(LanguageHelper.GetForMouseCursor(StringsForMouseCursor.DeconstructGeneral1, displayName, constructionCosts[ItemType.Metal], GetItemTypeName(ItemType.Metal)));
                }
                else if (constructionCosts[ItemType.Stone] > 0)
                {
                    this.deconstructFoundationTooltip.SetText(LanguageHelper.GetForMouseCursor(StringsForMouseCursor.DeconstructGeneral1, displayName, constructionCosts[ItemType.Stone], GetItemTypeName(ItemType.Stone)));
                }
            }
            else this.deconstructFoundationTooltip.IsEnabled = false;

            if (this.deconstructConduitNodeButton.IsVisible)
            {
                this.deconstructConduitNodeTooltip.IsEnabled = true;
                if (conduitNode.IsDesignatedForRecycling)
                {
                    this.deconstructConduitNodeTooltip.SetText(LanguageHelper.GetForMouseCursor(StringsForMouseCursor.ClickToCancelDeconstruction));
                }
                else
                {
                    this.deconstructConduitNodeTooltip.SetText(LanguageHelper.GetForMouseCursor(StringsForMouseCursor.DeconstructGeneral1, conduitNode.DisplayName, conduitNode.Definition.ConstructionCosts[ItemType.Metal], GetItemTypeName(ItemType.Metal)));
                }
            }
            else this.deconstructConduitNodeTooltip.IsEnabled = false;
        }

        protected bool IsBuildingUiVisible
        {
            get
            {
                return this.building != null && this.IsVisible && this.building.ConstructionProgress == 100 && !this.building.IsRecycling;
            }
        }

        private void OnConstructionPriorityChanged(object sender, MouseEventArgs e)
        {
            if (this.building != null && this.building.ConstructionProgress < 100)
            {
                foreach (var blueprint in World.ConfirmedBlueprints.Values.Where(b => b.MainTileIndex == this.building.MainTileIndex))
                {
                    blueprint.BuildPriority = this.constructionControl.Priority;
                }
            }
        }

        private static string GetItemTypeName(ItemType itemType)
        {
            if (!itemTypeNamesUpper.ContainsKey(itemType)) itemTypeNamesUpper.Add(itemType, LanguageManager.Get<ItemType>(itemType).ToUpperInvariant());
            return itemTypeNamesUpper[itemType];
        }

        protected override void HandleLanguageChange()
        {
            this.titleLabel.Text = BuildingNameController.GetName(this.building).ToUpperInvariant();

            itemTypeNamesUpper.Clear();
            this.deconstructFoundationTooltip.SetTitle(GetString(StringsForThingPanels.DeconstructFoundation));
            this.deconstructConduitNodeTooltip.SetTitle(GetString(StringsForThingPanels.DeconstructConduitNode));

            base.HandleLanguageChange();
        }

        protected static string GetString(StringsForThingPanels key)
        {
            return LanguageManager.Get<StringsForThingPanels>(key);
        }

        protected static string GetString(StringsForThingPanels key, object arg0)
        {
            return LanguageManager.Get<StringsForThingPanels>(key, arg0);
        }

        protected static string GetString(StringsForThingPanels key, object arg0, object arg1)
        {
            return LanguageManager.Get<StringsForThingPanels>(key, arg0, arg1);
        }

        protected static string GetString(StringsForThingPanels key, object arg0, object arg1, object arg2)
        {
            return LanguageManager.Get<StringsForThingPanels>(key, arg0, arg1, arg2);
        }
    }
}
