namespace SigmaDraconis.Shared
{
    using Draconis.Shared;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using ProtoBuf;

    [ProtoContract]
    public class ResourceContainer
    {
        [ProtoMember(1)]
        public Dictionary<ItemType, int> ItemTotals { get; private set; } = new Dictionary<ItemType, int>();

        [ProtoMember(2)]
        public int StorageLevel { get; private set; }

        [ProtoMember(4)]
        public int StorageCapacity { get; set; }

        // Deserialisation ctor
        private ResourceContainer()
        {
        }

        public ResourceContainer(int capacity)
        {
            this.StorageCapacity = capacity;

            foreach (var itemType in Enum.GetValues(typeof(ItemType)).Cast<ItemType>().Where(i => i != ItemType.None))
            {
                this.ItemTotals.Add(itemType, 0);
            }
        }

        [ProtoAfterDeserialization]
        [SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by ProtoBuf")]
        private void AfterDeserialization()
        {
            // This is for save version compatability when new item types are added
            foreach (var itemType in Enum.GetValues(typeof(ItemType)).Cast<ItemType>().Where(i => CanStoreItemType(i) && !this.ItemTotals.ContainsKey(i)))
            {
                this.ItemTotals.Add(itemType, 0);
            }
        }

        public int AddItems(ItemType itemType, int count)
        {
            if (this.StorageCapacity - this.StorageLevel < count) count = this.StorageCapacity - this.StorageLevel;
            if (count <= 0) return 0;

            this.ItemTotals[itemType] += count;
            this.StorageLevel += count;
            return count;
        }

        public bool CanTakeItems(ItemType itemType, int count)
        {
            return this.ItemTotals[itemType] >= count;
        }

        public int TakeItems(ItemType itemType, int count)
        {
            if (this.ItemTotals[itemType] < count) count = this.ItemTotals[itemType];
            this.ItemTotals[itemType] -= count;
            this.StorageLevel -= count;
            return count;
        }

        public int GetItemTotal(ItemType itemType)
        {
            return this.ItemTotals.ContainsKey(itemType) ? this.ItemTotals[itemType] : 0;
        }

        public IEnumerable<ItemType> GetStoredItemTypes()
        {
            return this.ItemTotals.Where(t => t.Value > 0).Select(i => i.Key);
        }

        private static bool CanStoreItemType(ItemType itemType)
        {
            return !itemType.In(ItemType.None, ItemType.Crop);
        }
    }
}
