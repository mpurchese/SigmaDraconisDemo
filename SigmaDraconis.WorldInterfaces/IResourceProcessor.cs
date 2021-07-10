namespace SigmaDraconis.WorldInterfaces
{
    using Shared;

    public interface IResourceProcessor : IColonistInteractive, IResourceProviderBuilding
    {
        /// <summary>
        /// Gets the type of the visible item on the processor
        /// </summary>
        ItemType InputItemType { get; }

        /// <summary>
        /// Called by colonist to check whether an item can be added to the processor.  Call before using AddResource.
        /// </summary>
        /// <param name="colonistId">If colonist is rejected, they'll get priority next time</param>
        /// <returns>True when the item can be added.</returns>
        bool CanAddResource(int colonistId);

        /// <summary>
        /// Called by colonist to add an item to the processor.  Check open first using CanAddResource.
        /// </summary>
        /// <param name="itemType">Type of resource to add</param>
        void AddResource(ItemType itemType);

        /// <summary>
        /// Called repeatedly by colonist to run the processor in reverse and take an item out of the network.
        /// </summary>
        /// <param name="itemType">Type of resource to take</param>
        /// <returns>True when the item is available.  At this point it should be added to the colonist's inventory.</returns>
        bool RequestUnprocessedResource(ItemType itemType);
    }
}