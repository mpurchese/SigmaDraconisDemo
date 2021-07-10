namespace SigmaDraconis.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Xna.Framework;
    using Draconis.UI;
    using Language;
    using Shared;
    using World.ResourceNetworks;

    public enum InventoryDisplayType { Resources, Items, Food, Hydrogen };

    public class InventoryDisplay : UIElementBase
    {
        public ResourceNetwork ResourceNetwork { get; set; }

        private readonly InventorySlot[] slots;
        private readonly ProgressBar usageBar;
        private readonly InventorySlotTooltip inventorySlotTooltip;
        private readonly SimpleTooltip usageTooltip;
        private readonly ItemType[] itemTypes;
        private readonly InventoryDisplayType displayType;
        private Color barColour;

        public IReadOnlyCollection<InventorySlot> Slots => this.slots;

        public InventoryDisplay(IUIElement parent, int x, int y, InventoryDisplayType displayType)
            : base(parent, x, y, Scale(40), Scale(50))
        {
            this.displayType = displayType;
            
            switch (displayType)
            {
                case InventoryDisplayType.Food:
                    this.itemTypes = Constants.StorageTypesByItemType.Where(kv => kv.Value == ThingType.FoodStorage).Select(kv => kv.Key).ToArray();
                    this.barColour = UIColour.FoodStorageBar;
                    break;
                case InventoryDisplayType.Resources: 
                    this.itemTypes = Constants.StorageTypesByItemType.Where(kv => kv.Value == ThingType.Silo).Select(kv => kv.Key).ToArray();
                    this.barColour = UIColour.StorageBar;
                    break;
                case InventoryDisplayType.Items:
                    this.itemTypes = Constants.StorageTypesByItemType.Where(kv => kv.Value == ThingType.ItemsStorage).Select(kv => kv.Key).ToArray();
                    this.barColour = UIColour.ItemsStorageBar;
                    break;
                case InventoryDisplayType.Hydrogen: 
                    this.itemTypes = new ItemType[] { ItemType.LiquidFuel };
                    this.barColour = UIColour.HydrogenStorageBar;
                    break;
            }

            var count = itemTypes.Length;
            this.W = count * Scale(40);

            this.slots = new InventorySlot[count];
            for (int i = 0; i < count; i++)
            {
                this.slots[i] = new InventorySlot(this, Scale(2) + (i * Scale(40)), 0, ItemType.None);
                this.AddChild(this.slots[i]);
            }

            this.usageBar = new ProgressBar(this, 0, Scale(37), this.W, Scale(4)) { BarColour = this.barColour };
            this.AddChild(this.usageBar);

           // var tooltipAttachPoint1 = new EmptyElement(this, 0, 0, this.W, Scale(37));
           // this.AddChild(tooltipAttachPoint1);
            this.inventorySlotTooltip = new InventorySlotTooltip(TooltipParent.Instance, this);
            TooltipParent.Instance.AddChild(this.inventorySlotTooltip, this.Parent);

            var tooltipAttachPoint2 = new EmptyElement(this, 0, Scale(37) + 1, this.W, Scale(6));
            this.AddChild(tooltipAttachPoint2);
            this.usageTooltip = UIHelper.AddSimpleTooltip(this.Parent, tooltipAttachPoint2);
        }

        public override void Update()
        {
            if (this.IsVisible && this.ResourceNetwork != null)
            {
                var thingTypesWithoutSlot = this.itemTypes.Where(t => this.ResourceNetwork.GetItemTotal(t) > 0 && this.slots.All(s => s.ItemType != t)).ToList();
                for (int i = 0; i < this.itemTypes.Length; i++)
                {
                    if (this.slots[i].ItemType != ItemType.None)
                    {
                        this.slots[i].ItemCount = this.ResourceNetwork.GetItemTotal(this.slots[i].ItemType);
                    }

                    if (this.slots[i].ItemCount == 0 && thingTypesWithoutSlot.Any())
                    {
                        this.slots[i].ItemType = thingTypesWithoutSlot.First();
                        this.slots[i].ItemCount = this.ResourceNetwork.GetItemTotal(this.slots[i].ItemType);
                        thingTypesWithoutSlot.RemoveAt(0);
                    }
                }

                if (this.displayType == InventoryDisplayType.Resources)
                {
                    this.usageBar.Fraction = Math.Min(this.ResourceNetwork.CountResources / (double)this.ResourceNetwork.ResourcesCapacity, 1.0);
                    this.usageBar.BarColour = this.usageBar.Fraction > 0.999 ? UIColour.RedText : this.barColour;

                    // Tooltip to show used capacity
                    var silosStr = LanguageManager.Get<StringsForInventoryDisplay>(StringsForInventoryDisplay.ResourceStorage);
                    var usageDescription = $"{silosStr}: {this.ResourceNetwork.CountResources} / {this.ResourceNetwork.ResourcesCapacity}";
                    this.usageTooltip.SetTitle(usageDescription);
                }
                else if (this.displayType == InventoryDisplayType.Items)
                {
                    this.usageBar.Fraction = Math.Min(this.ResourceNetwork.CountItems / (double)this.ResourceNetwork.ItemsCapacity, 1.0);
                    this.usageBar.BarColour = this.usageBar.Fraction > 0.999 ? UIColour.RedText : this.barColour;

                    // Tooltip to show used capacity
                    var silosStr = LanguageManager.Get<StringsForInventoryDisplay>(StringsForInventoryDisplay.ItemsStorage);
                    var usageDescription = $"{silosStr}: {this.ResourceNetwork.CountItems} / {this.ResourceNetwork.ItemsCapacity}";
                    this.usageTooltip.SetTitle(usageDescription);
                }
                else if (this.displayType == InventoryDisplayType.Food)
                {
                    this.usageBar.Fraction = Math.Min(this.ResourceNetwork.CountFood / (double)this.ResourceNetwork.FoodCapacity, 1.0);
                    this.usageBar.BarColour = this.usageBar.Fraction > 0.999 ? UIColour.RedText : this.barColour;

                    // Tooltip to show used capacity
                    var silosStr = LanguageManager.Get<StringsForInventoryDisplay>(StringsForInventoryDisplay.FoodStorage);
                    var usageDescription = $"{silosStr}: {this.ResourceNetwork.CountFood} / {this.ResourceNetwork.FoodCapacity}";
                    this.usageTooltip.SetTitle(usageDescription);
                }
                else // if (this.displayType == NetworkInventoryDisplayType.Hydrogen)
                {
                    this.usageBar.Fraction = this.ResourceNetwork.CountHydrogen / (double)this.ResourceNetwork.HydrogenCapacity;
                    this.usageBar.BarColour = this.usageBar.Fraction > 0.999 ? UIColour.RedText : this.barColour;

                    // Tooltip to show used capacity
                    var silosStr = LanguageManager.Get<StringsForInventoryDisplay>(StringsForInventoryDisplay.HydrogenStorage);
                    var usageDescription = $"{silosStr}: {this.ResourceNetwork.CountHydrogen} / {this.ResourceNetwork.HydrogenCapacity}";
                   // this.usageTooltip.W = 10 + (usageDescription.Length * 7);
                    this.usageTooltip.SetTitle(usageDescription);
                }

                if (this.IsMouseOver) this.inventorySlotTooltip.UpdateContent();
            }

            base.Update();
        }

        public override void ApplyLayout()
        {
            foreach (var child in this.Children)
            {
                child.X = this.Rescale(child.X);
                child.Y = child == this.usageBar ? Scale(37) : this.Rescale(child.Y);
                child.ApplyScale();
                child.ApplyLayout();
            }

            this.appliedScale = UIStatics.Scale;
            this.suppressOnParentResize = true;
            this.IsContentChangedSinceDraw = true;
        }
    }
}
