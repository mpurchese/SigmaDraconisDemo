namespace SigmaDraconis.World
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Blueprints;
    using Buildings;
    using Shared;
    using WorldInterfaces;

    public static class ConduitHelper
    {
        public static void AddMinorConduit(ISmallTile tile, bool isBlueprint)
        {
            var nodeAndFrame = GetNearbyConduitNodes(tile, false).FirstOrDefault();
            if (nodeAndFrame == null || nodeAndFrame.Item1 == null || nodeAndFrame.Item2 == 0) return;

            if (isBlueprint)
            {
                var blueprint = new Blueprint(ThingType.ConduitMinor, tile, 0f, 0f, 1f) { AnimationFrame = nodeAndFrame.Item2 };
                World.ConfirmedBlueprints.Add(blueprint.Id, blueprint);
                EventManager.RaiseEvent(EventType.Blueprint, EventSubType.Added, blueprint);
            }

            var building = BuildingFactory.Get(ThingType.ConduitMinor, tile) as IConduitMinor;
            building.RenderAlpha = isBlueprint ? 0f : 1f;
            building.ShadowAlpha = isBlueprint ? 0f : 1f;
            building.AnimationFrame = nodeAndFrame.Item2;
            building.ConnectedNodeId = nodeAndFrame.Item1.Id;
            World.AddThing(building);
            building.AfterAddedToWorld();

            nodeAndFrame.Item1.ConnectConduit(building);
        }

        public static void AddMajorConduits(IConduitNode node)
        {
            foreach (var nodeAndFrame in GetNearbyConduitNodes(node.MainTile, true))
            {
                if (nodeAndFrame == null || nodeAndFrame.Item1 == null || nodeAndFrame.Item2 == 0) continue;

                var blueprint = new Blueprint(ThingType.ConduitMajor, node.MainTile, 0f, 0f, 1f) { AnimationFrame = nodeAndFrame.Item2 };
                World.ConfirmedBlueprints.Add(blueprint.Id, blueprint);
                EventManager.RaiseEvent(EventType.Blueprint, EventSubType.Added, blueprint);

                var conduit = BuildingFactory.Get(ThingType.ConduitMajor, node.MainTile) as IConduitMajor;
                conduit.RenderAlpha = 0f;
                conduit.ShadowAlpha = 0f;
                conduit.AnimationFrame = nodeAndFrame.Item2;
                conduit.Node1 = node.Id;
                conduit.Node2 = nodeAndFrame.Item1.Id;
                World.AddThing(conduit);
                conduit.AfterAddedToWorld();

                node.ConnectConduit(conduit);
                nodeAndFrame.Item1.ConnectConduit(conduit);
            }
        }

        private static IEnumerable<Tuple<IConduitNode, int>> GetNearbyConduitNodes(ISmallTile tile, bool excludeCurrent)
        {
            var x = tile.TerrainPosition.X;
            var y = tile.TerrainPosition.Y;

            if (!excludeCurrent && GetConduitNodeInTile(x, y) is IConduitNode c0) yield return new Tuple<IConduitNode, int>(c0, 0);

            if (GetConduitNodeInTile(x, y + 1) is IConduitNode c14) yield return new Tuple<IConduitNode, int>(c14, 14);
            if (GetConduitNodeInTile(x - 1, y) is IConduitNode c15) yield return new Tuple<IConduitNode, int>(c15, 15);
            if (GetConduitNodeInTile(x + 1, y) is IConduitNode c22) yield return new Tuple<IConduitNode, int>(c22, 22);
            if (GetConduitNodeInTile(x, y - 1) is IConduitNode c23) yield return new Tuple<IConduitNode, int>(c23, 23);

            if (GetConduitNodeInTile(x + 1, y + 1) is IConduitNode c19) yield return new Tuple<IConduitNode, int>(c19, 19);
            if (GetConduitNodeInTile(x - 1, y - 1) is IConduitNode c20) yield return new Tuple<IConduitNode, int>(c20, 20);
            if (GetConduitNodeInTile(x + 1, y - 1) is IConduitNode c27) yield return new Tuple<IConduitNode, int>(c27, 27);
            if (GetConduitNodeInTile(x - 1, y + 1) is IConduitNode c10) yield return new Tuple<IConduitNode, int>(c10, 10);

            if (GetConduitNodeInTile(x, y + 2) is IConduitNode c9) yield return new Tuple<IConduitNode, int>(c9, 9);
            if (GetConduitNodeInTile(x - 2, y) is IConduitNode c11) yield return new Tuple<IConduitNode, int>(c11, 11);
            if (GetConduitNodeInTile(x + 2, y) is IConduitNode c26) yield return new Tuple<IConduitNode, int>(c26, 26);
            if (GetConduitNodeInTile(x, y - 2) is IConduitNode c28) yield return new Tuple<IConduitNode, int>(c28, 28);

            if (GetConduitNodeInTile(x - 1, y + 2) is IConduitNode c5) yield return new Tuple<IConduitNode, int>(c5, 5);
            if (GetConduitNodeInTile(x - 2, y + 1) is IConduitNode c6) yield return new Tuple<IConduitNode, int>(c6, 6);
            if (GetConduitNodeInTile(x - 2, y - 1) is IConduitNode c16) yield return new Tuple<IConduitNode, int>(c16, 16);
            if (GetConduitNodeInTile(x - 1, y - 2) is IConduitNode c24) yield return new Tuple<IConduitNode, int>(c24, 24);
            if (GetConduitNodeInTile(x + 1, y + 2) is IConduitNode c13) yield return new Tuple<IConduitNode, int>(c13, 13);
            if (GetConduitNodeInTile(x + 2, y + 1) is IConduitNode c21) yield return new Tuple<IConduitNode, int>(c21, 21);
            if (GetConduitNodeInTile(x + 2, y - 1) is IConduitNode c31) yield return new Tuple<IConduitNode, int>(c31, 31);
            if (GetConduitNodeInTile(x + 1, y - 2) is IConduitNode c32) yield return new Tuple<IConduitNode, int>(c32, 32);

            if (GetConduitNodeInTile(x - 2, y + 2) is IConduitNode c2) yield return new Tuple<IConduitNode, int>(c2, 2);
            if (GetConduitNodeInTile(x + 2, y + 2) is IConduitNode c17) yield return new Tuple<IConduitNode, int>(c17, 17);
            if (GetConduitNodeInTile(x - 2, y - 2) is IConduitNode c18) yield return new Tuple<IConduitNode, int>(c18, 18);
            if (GetConduitNodeInTile(x + 2, y - 2) is IConduitNode c35) yield return new Tuple<IConduitNode, int>(c35, 35);

            if (GetConduitNodeInTile(x - 3, y) is IConduitNode c7) yield return new Tuple<IConduitNode, int>(c7, 7);
            if (GetConduitNodeInTile(x, y + 3) is IConduitNode c4) yield return new Tuple<IConduitNode, int>(c4, 4);
            if (GetConduitNodeInTile(x + 3, y) is IConduitNode c30) yield return new Tuple<IConduitNode, int>(c30, 30);
            if (GetConduitNodeInTile(x, y - 3) is IConduitNode c33) yield return new Tuple<IConduitNode, int>(c33, 33);

            if (GetConduitNodeInTile(x - 1, y + 3) is IConduitNode c1) yield return new Tuple<IConduitNode, int>(c1, 1);
            if (GetConduitNodeInTile(x - 3, y + 1) is IConduitNode c3) yield return new Tuple<IConduitNode, int>(c3, 3);
            if (GetConduitNodeInTile(x + 1, y + 3) is IConduitNode c8) yield return new Tuple<IConduitNode, int>(c8, 8);
            if (GetConduitNodeInTile(x - 3, y - 1) is IConduitNode c12) yield return new Tuple<IConduitNode, int>(c12, 12);
            if (GetConduitNodeInTile(x + 3, y + 1) is IConduitNode c25) yield return new Tuple<IConduitNode, int>(c25, 25);
            if (GetConduitNodeInTile(x - 1, y - 3) is IConduitNode c29) yield return new Tuple<IConduitNode, int>(c29, 29);
            if (GetConduitNodeInTile(x + 3, y - 1) is IConduitNode c34) yield return new Tuple<IConduitNode, int>(c34, 34);
            if (GetConduitNodeInTile(x + 1, y - 3) is IConduitNode c36) yield return new Tuple<IConduitNode, int>(c36, 36);

            yield break;
        }

        private static IConduitNode GetConduitNodeInTile(int x, int y)
        {
            var tile = World.GetSmallTile(x, y);
            if (tile == null) return null;
            return tile.ThingsPrimary.OfType<IConduitNode>().FirstOrDefault(c => !c.IsDesignatedForRecycling);
        }
    }
}
