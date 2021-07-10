namespace SigmaDraconis.WorldInterfaces
{
    using System.Collections.Generic;

    public interface IConduitNode : IBuildableThing
    {
        IEnumerable<IBuildableThing> ConnectedConduits { get; }

        void ConnectConduit(IBuildableThing conduit);
        void DisconnectConduit(IBuildableThing conduit);
        bool IsConnectedToLander(int? excludedNode);
    }
}