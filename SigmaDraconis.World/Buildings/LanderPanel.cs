namespace SigmaDraconis.World.Buildings
{
    using ProtoBuf;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class LanderPanel : Building
    {
        public LanderPanel() : base(ThingType.LanderPanel)
        {
        }

        public LanderPanel(ISmallTile mainTile, int frame) : base(ThingType.LanderPanel, mainTile, 1)
        {
            this.animationFrame = frame;
        }
    }
}
