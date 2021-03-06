namespace SigmaDraconis.AnimalAI
{
    using Draconis.Shared;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using Shared;
    using World;
    using World.PathFinding;
    using World.Rooms;
    using World.Zones;
    using WorldInterfaces;

    [ProtoContract]
    public class RedBugAI : IAnimalAI
    {
        private static readonly FastRandom random = new FastRandom();

        public IAnimal Animal { get; private set; }

        [ProtoMember(1)]
        private readonly int animalId;

        [ProtoMember(2)]
        public ActivityBase CurrentActivity { get; private set; }

        // Deserialisation ctor
        protected RedBugAI() { }

        public RedBugAI(IAnimal animal)
        {
            this.Animal = animal;
            this.animalId = animal.Id;
        }

        [ProtoAfterDeserialization]
        public void AfterDeserialization()
        {
            this.Animal = World.GetThing(this.animalId) as IAnimal;
        }

        public void Update()
        {
            if (this.CurrentActivity?.IsFinished != false) this.MakeChoice();
            if (this.CurrentActivity != null) this.CurrentActivity.Update();
        }

        public void DoBackgroundUpdate()
        {
            if (this.CurrentActivity is ActivityWalk a && a.CurrentAction is ActionWalk w)
            {
                w.DoBackgroundUpdate();
            }
        }

        private void MakeChoice()
        {
            // Release any tile blocks
            PathFinderBlockManager.RemoveBlocks(this.Animal.Id);
            ZoneManager.HomeZone.UpdateNode(this.Animal.MainTileIndex);
            ZoneManager.GlobalZone.UpdateNode(this.Animal.MainTileIndex);
            ZoneManager.AnimalZone.UpdateNode(this.Animal.MainTileIndex);
            EventManager.RaiseEvent(EventType.Zone, null);

            //Red bugs can't go through roundleaf plants.
            var tilesToAvoid = World.GetThings(ThingType.SmallPlant4).SelectMany(t => t.AllTiles).Select(t => t.Index).ToHashSet();

            if (this.IsInClosedTile(tilesToAvoid) && this.Animal.MainTile.AdjacentTiles8.Any(t => ZoneManager.AnimalZone.ContainsNode(t.Index) && t.ThingsAll.All(u => u.TileBlockModel.In(TileBlockModel.None, TileBlockModel.Point) == true)))
            {
                this.LeaveClosedTile();
                return;
            }

            if ((World.Temperature <= 0 || (World.WorldLight.Brightness < 0.5 && World.Temperature <= 10)) && this.TrySatisfySleep(tilesToAvoid)) return;
            if (this.TrySatisfyRoam(tilesToAvoid)) return;

            this.CurrentActivity = new ActivityWait(this.Animal);

            return;
        }

        private bool TrySatisfySleep(HashSet<int> tilesToAvoid)
        {
            if (this.CurrentActivity is ActivitySleep && !this.CurrentActivity.IsFinished) return true;
            if (this.Animal.IsResting || this.Animal.MainTile.ThingsAll.Any(t => t.ThingType == ThingType.Bush))
            {
                this.CurrentActivity = new ActivitySleep(this.Animal, 3600 * (3 + Rand.Next(3)));   // 3 - 5 hours
                return true;
            }

            var candidateTiles = World.GetThings<IAnimatedThing>(ThingType.Bush).Where(b => b.AnimationFrame > 4).SelectMany(b => b.AllTiles).Where(t => RoomManager.GetRoom(t.Index) == null).ToList();
            var path = this.FindPath(this.Animal.MainTile, candidateTiles, tilesToAvoid);

            if (path?.RemainingNodes?.Any() == true)
            {
                this.CurrentActivity = new ActivitySleep(this.Animal, 3600 * (3 + Rand.Next(3)), path);
                return true;
            }

            return false;
        }

        private bool TrySatisfyRoam(HashSet<int> tilesToAvoid)
        {
            if (this.CurrentActivity is ActivityWalk && !this.CurrentActivity.IsFinished) return true;

            var i = ZoneManager.AnimalZone.FindBestTile(this.Animal, 1, 33, 8, -99, GetTileRoamScore, out int? score);
            if (i.HasValue && this.Animal.MainTileIndex != i)
            {
                var path = PathFinder.FindPath(this.Animal.MainTileIndex, i.Value, ZoneManager.AnimalZone.Nodes, tilesToAvoid);
                if (path?.RemainingNodes?.Any() == true)
                {
                    this.CurrentActivity = new ActivityWalk(this.Animal, path);
                    return true;
                }
            }

            return false;
        }

        // Used to fix problem where animal ends up in a tile with no links
        private bool IsInClosedTile(HashSet<int> tilesToAvoid)
        {
            return !ZoneManager.AnimalZone.ContainsNode(this.Animal.MainTileIndex) || ZoneManager.AnimalZone.Nodes[this.Animal.MainTileIndex].AllLinks.All(l => tilesToAvoid.Contains(l.Index));
        }

        private void LeaveClosedTile()
        {
            this.CurrentActivity = new ActivityLeaveClosedTile(this.Animal);
        }

        private static int GetTileRoamScore(IAnimal colonist, ISmallTile tile, int distance, int minTemperature)
        {
            if (distance < 3) return 0;

            if (tile == null || tile.ThingsAll.Any(t => !(t is IWall) && !(t is IMoveableThing) && t.TileBlockModel != TileBlockModel.None)) return 0;

            var score = tile.IsCorridor ? 20 : 30;
            score += (int)random.NextFloat(5) - Math.Abs(distance - 5);   // Bugs like to walk around 5 tiles at a time

            return score;
        }

        private Path FindPath(ISmallTile startTile, List<ISmallTile> candidateTiles, HashSet<int> tilesToAvoid, int maxDistance = 8)
        {
            if (!candidateTiles.Any()) return null;

            ISmallTile targetTile = null;
            if (candidateTiles.Count > 1 && ZoneManager.AnimalZone.Nodes.ContainsKey(startTile.Index))
            {
                // Expanding circle method faster if there are many possible targets
                var openNodes = new HashSet<int> { startTile.Index };
                var closedNodes = new HashSet<int> { startTile.Index };
                foreach (var tileIndex in tilesToAvoid) closedNodes.Add(tileIndex);
                var distance = 0;
                while (openNodes.Any() && targetTile == null && distance <= maxDistance)
                {
                    var list = openNodes.ToList();
                    openNodes.Clear();
                    foreach (var n in list)
                    {
                        var o = ZoneManager.AnimalZone.Nodes[n];
                        foreach (var l in o.AllLinks)
                        {
                            if (!closedNodes.Contains(l.Index))
                            {
                                var tile = World.GetSmallTile(l.Index);
                                if (candidateTiles.Contains(tile))
                                {
                                    targetTile = tile;
                                    break;
                                }

                                openNodes.Add(l.Index);
                                closedNodes.Add(l.Index);
                            }
                        }

                        if (targetTile != null) break;
                    }

                    distance++;
                }
            }
            else if (candidateTiles.Count == 1) targetTile = candidateTiles[0];

            if (targetTile != null)
            {
                // If we are standing next to a stationary colonist or rover, find a way around it.
                var path = PathFinder.FindPath(startTile.Index, targetTile.Index, ZoneManager.AnimalZone.Nodes);
                if (path?.RemainingNodes?.Any() == true) return path;
            }

            return null;
        }
    }
}
