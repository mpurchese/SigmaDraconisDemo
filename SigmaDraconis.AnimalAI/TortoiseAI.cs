namespace SigmaDraconis.AnimalAI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using Draconis.Shared;
    using Shared;
    using World;
    using World.PathFinding;
    using World.Rooms;
    using World.Zones;
    using WorldInterfaces;

    [ProtoContract]
    public class TortoiseAI : IAnimalAI
    {
        private static readonly FastRandom random = new FastRandom();

        private static bool isEventSubscriptionActive;  // Only one instance of TortoiseAI needs to subscribe to plant events
        private static HashSet<int> tilesToAvoid = new HashSet<int>();

        public IAnimal Animal { get; private set; }

        [ProtoMember(1)]
        private readonly int animalId;

        [ProtoMember(2)]
        public ActivityBase CurrentActivity { get; private set; }

        // Deserialisation ctor
        protected TortoiseAI()
        {
            if (!isEventSubscriptionActive) DoEventSubscription();
        }

        public TortoiseAI(IAnimal animal)
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
            if (this.Animal.IsDead) return;
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

            if (this.IsInClosedTile() && this.Animal.MainTile.AdjacentTiles8.Any(t => ZoneManager.AnimalZone.ContainsNode(t.Index) && t.ThingsAll.All(u => u.TileBlockModel.In(TileBlockModel.None, TileBlockModel.Point) == true)))
            {
                this.LeaveClosedTile();
                return;
            }

            var eatHasPriority = this.Animal.IsHungry && World.Temperature >= 0;
            if (eatHasPriority)
            {
                if (this.TrySatisfyEat()) return;
                if (this.Animal.IsEating) this.Animal.FinishEating();

                if (this.Animal.IsTired && this.TrySatisfySleep()) return;
                if (this.Animal.IsResting) this.Animal.FinishResting();
            }
            else
            {
                if ((World.Temperature <= -1 || this.Animal.IsTired) && this.TrySatisfySleep()) return;
                if (this.Animal.IsResting) this.Animal.FinishResting();

                if (this.Animal.IsHungry && this.TrySatisfyEat()) return;
                if (this.Animal.IsEating) this.Animal.FinishEating();
            }

            if (this.TrySatisfyRoam()) return;

            this.CurrentActivity = new ActivityWait(this.Animal);

            return;
        }

        private bool TrySatisfySleep()
        {
            if (this.CurrentActivity is ActivitySleep && !this.CurrentActivity.IsFinished) return true;
            if (this.Animal.IsResting)
            {
                this.CurrentActivity = new ActivitySleep(this.Animal, 3600 * (3 + Rand.Next(3)));   // 3 - 5 hours
                return true;
            }

            if (GetTileSleepScore(this.Animal, this.Animal.MainTile, 0, -99) >= 40)
            {
                // Already in a great place for sleeping
                this.CurrentActivity = new ActivitySleep(this.Animal, 3600 * (3 + Rand.Next(3)));
                return true;
            }

            var i = ZoneManager.AnimalZone.FindBestTile(this.Animal, 10, 50, 8, -99, GetTileSleepScore, out int? score);
            if (i.HasValue && this.Animal.MainTileIndex != i)
            {
                var path = PathFinder.FindPath(this.Animal.MainTileIndex, i.Value, ZoneManager.AnimalZone.Nodes, tilesToAvoid);
                if (path?.RemainingNodes?.Any() == true)
                {
                    this.CurrentActivity = new ActivitySleep(this.Animal, 3600 * (3 + Rand.Next(3)), path);
                    return true;
                }
            }

            return false;
        }

        private bool TrySatisfyRoam()
        {
            if (this.CurrentActivity is ActivityWalk && !this.CurrentActivity.IsFinished) return true;

            var i = ZoneManager.AnimalZone.FindBestTile(this.Animal, 1, 37, 8, -99, GetTileRoamScore, out int? score);
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

        private bool TrySatisfyEat()
        {
            if (this.CurrentActivity is ActivityEat && !this.CurrentActivity.IsFinished) return true;

            foreach (var tile in this.Animal.MainTile.AdjacentTiles4)
            {
                var plant = tile.ThingsAll.OfType<IFruitPlant>()
                    .Where(p => p.CountFruitAvailable > 0 && !(p.HarvestJobProgress > 0) && (p.ReservedByAnimalID == 0 || p.ReservedByAnimalID == this.animalId)).FirstOrDefault();
                if (plant != null)
                {
                    // Already next to a suitable plant
                    plant.Reserve(this.animalId);
                    this.CurrentActivity = new ActivityEat(this.Animal, 360, tile, plant.Id);
                    return true;
                }
            }

            var i = ZoneManager.AnimalZone.FindBestTile(this.Animal, 1, 1, 8, -99, GetTileEatScore, out int? score);
            if (i.HasValue && this.Animal.MainTileIndex != i)
            {
                var path = PathFinder.FindPath(this.Animal.MainTileIndex, i.Value, ZoneManager.AnimalZone.Nodes, tilesToAvoid);
                if (path?.RemainingNodes?.Any() == true)
                {
                    var targetTile = World.GetSmallTile(i.Value);

                    foreach (var tile in targetTile.AdjacentTiles4)
                    {
                        var plant = tile.ThingsAll.OfType<IFruitPlant>().Where(p => p.CountFruitAvailable > 0 && !(p.HarvestJobProgress > 0) && (p.ReservedByAnimalID == 0 || p.ReservedByAnimalID == this.animalId)).FirstOrDefault();
                        if (plant != null)
                        {
                            plant.Reserve(this.animalId);
                            this.CurrentActivity = new ActivityEat(this.Animal, 360, tile, plant.Id, path);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        // Used to fix problem where animal ends up in a tile with no links
        private bool IsInClosedTile()
        {
            return !ZoneManager.AnimalZone.ContainsNode(this.Animal.MainTileIndex) || ZoneManager.AnimalZone.Nodes[this.Animal.MainTileIndex].AllLinks.All(l => tilesToAvoid.Contains(l.Index));
        }

        private void LeaveClosedTile()
        {
            this.CurrentActivity = new ActivityLeaveClosedTile(this.Animal);
        }

        private static int GetTileRoamScore(IAnimal colonist, ISmallTile tile, int distance, int minTemperature)
        {
            if (distance < 6) return 0;

            if (tile == null || tile.ThingsAll.Any(t => !(t is IWall) && !(t is IMoveableThing) && t.TileBlockModel != TileBlockModel.None)) return 0;
            if (RoomManager.GetRoom(tile.Index) != null) return 0;   // Animals don't go indoors

            var score = tile.IsCorridor ? 20 : 30;
            score += (int)random.NextFloat(8) - Math.Abs(distance - 8);   // Animals like to walk around 8 tiles at a time

            return score;
        }

        private static int GetTileSleepScore(IAnimal colonist, ISmallTile tile, int distance, int minTemperature)
        {
            if (tile == null || tile.ThingsAll.Any(t => !(t is IWall) && !(t is IMoveableThing) && t.TileBlockModel != TileBlockModel.None)) return 0;
            if (RoomManager.GetRoom(tile.Index) != null) return 0;   // Animals don't go indoors

            var score = tile.IsCorridor ? 10 : 20;
            score += 10 * tile.AdjacentTiles4.SelectMany(t => t.ThingsAll.OfType<IPlant>()).Count();   // Animal likes to sleep by plants

            return score;
        }

        private static int GetTileEatScore(IAnimal animal, ISmallTile tile, int distance, int minTemperature)
        {
            if (tile == null || tile.ThingsAll.Any(t => !(t is IWall) && !(t is IMoveableThing) && t.TileBlockModel != TileBlockModel.None)) return 0;
            if (RoomManager.GetRoom(tile.Index) != null) return 0;   // Animals don't go indoors

            var adjacentPlant = tile.AdjacentTiles4.SelectMany(t => t.ThingsAll).OfType<IFruitPlant>()
                .Where(p => p.CountFruitAvailable > 0 && !(p.HarvestJobProgress > 0) && (p.ReservedByAnimalID == 0 || p.ReservedByAnimalID == animal.Id)).FirstOrDefault();

            return adjacentPlant == null ? 0 : 1;
        }


        private static void DoEventSubscription()
        {
            isEventSubscriptionActive = true;
            OnPlantAddedOrRemoved();
            EventManager.Subscribe(EventType.Plant, EventSubType.Added, delegate (object obj) { OnPlantAddedOrRemoved(); });
            EventManager.Subscribe(EventType.Plant, EventSubType.Removed, delegate (object obj) { OnPlantAddedOrRemoved(); });
        }

        private static void OnPlantAddedOrRemoved()
        {
            // Animal zone includes bushes and roundleaf plants, for the benefit of bugs - but tortoises can't go through these.
            tilesToAvoid = World.GetThings(ThingType.Bush, ThingType.SmallPlant4).SelectMany(t => t.AllTiles).Select(t => t.Index).ToHashSet();
        }
    }
}
