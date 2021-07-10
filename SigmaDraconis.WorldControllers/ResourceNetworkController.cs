namespace SigmaDraconis.WorldControllers
{
    using Shared;
    using World;
    using World.Buildings;
    using World.ResourceNetworks;

    public static class ResourceNetworkController
    {
        public static void Init()
        {
            EventManager.Subscribe(EventType.Building, EventSubType.Added, delegate (object obj) { OnBuildingAdded(obj); });
        }

        public static void Clear()
        {
            World.ResourceNetwork = null;
        }

        public static void UpdateStartOfFrame(bool isPaused)
        {
            if (World.ResourceNetwork != null) World.ResourceNetwork.UpdateStartOfFrame(isPaused);
        }

        public static void UpdateEndOfFrame()
        {
            if (World.ResourceNetwork != null) World.ResourceNetwork.UpdateEndOfFrame();
        }

        private static void OnBuildingAdded(object obj)
        {
            if (obj is Lander) World.ResourceNetwork = new ResourceNetwork();
        }
    }
}
