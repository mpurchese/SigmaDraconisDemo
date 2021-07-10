namespace SigmaDraconis.UI
{
    using System.Text;
    using Draconis.UI;
    using Config;
    using Language;
    using Shared;
    using World;

    public class InventorySlotTooltip : SimpleTooltip
    {
        private readonly InventoryDisplay inventoryDisplay;

        public InventorySlotTooltip(IUIElement parent, InventoryDisplay inventoryDisplay)
            : base(parent, inventoryDisplay)
        {
            this.inventoryDisplay = inventoryDisplay;
        }

        public void UpdateContent()
        {
            this.IsEnabled = false;
            if (!this.inventoryDisplay.IsMouseOver)
            {
                base.Update();
                return;
            }

            foreach (var slot in this.inventoryDisplay.Slots)
            {
                if (!slot.IsMouseOver) continue;
                if (slot.ItemType == ItemType.None) break;

                var description = LanguageManager.Get<ItemType>(slot.ItemType);
                this.IsEnabled = slot.ItemType != ItemType.None && slot.ItemCount > 0;
                this.SetTitle($"{slot.ItemCount} {description}");

                if (slot.ItemType == ItemType.Food)
                {
                    var sb = new StringBuilder();
                    var first = true;
                    foreach (var kv in World.GetFoodCounts())
                    {
                        var name = CropDefinitionManager.GetDefinition(kv.Key).DisplayNameLong;
                        if (!first) sb.Append("|");
                        sb.Append($"{kv.Value} {name}");
                        first = false;
                    }

                    this.SetText(sb.ToString());
                }
                else
                {
                    this.SetText("");
                }

                this.IsEnabled = true;
                break;
            }

            base.Update();
        }
    }
}
